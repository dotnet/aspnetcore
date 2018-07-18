// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Routing.EndpointConstraints;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    internal class DfaMatcherBuilder : MatcherBuilder
    {
        private readonly List<MatcherBuilderEntry> _entries = new List<MatcherBuilderEntry>();
        private readonly IInlineConstraintResolver _constraintResolver = new DefaultInlineConstraintResolver(Options.Create(new RouteOptions()));

        private readonly MatchProcessorFactory _matchProcessorFactory;
        private readonly EndpointSelector _endpointSelector;

        public DfaMatcherBuilder(
            MatchProcessorFactory matchProcessorFactory,
            EndpointSelector endpointSelector)
        {
            _matchProcessorFactory = matchProcessorFactory ?? throw new ArgumentNullException(nameof(matchProcessorFactory));
            _endpointSelector = endpointSelector ?? throw new ArgumentNullException(nameof(endpointSelector));
        }

        public override void AddEndpoint(MatcherEndpoint endpoint)
        {
            _entries.Add(new MatcherBuilderEntry(endpoint));
        }

        public DfaNode BuildDfaTree()
        {
            // We build the tree by doing a BFS over the list of entries. This is important
            // because a 'parameter' node can also traverse the same paths that literal nodes
            // traverse. This means that we need to order the entries first, or else we will
            // miss possible edges in the DFA.
            _entries.Sort();

            // Since we're doing a BFS we will process each 'level' of the tree in stages
            // this list will hold the set of items we need to process at the current
            // stage.
            var work = new List<(MatcherBuilderEntry entry, List<DfaNode> parents)>();

            var root = new DfaNode() { Depth = 0, Label = "/" };

            // To prepare for this we need to compute the max depth, as well as
            // a seed list of items to process (entry, root).
            var maxDepth = 0;
            for (var i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                maxDepth = Math.Max(maxDepth, entry.Pattern.Segments.Count);

                work.Add((entry, new List<DfaNode>() { root, }));
            }

            // Now we process the entries a level at a time.
            for (var depth = 0; depth <= maxDepth; depth++)
            {
                // As we process items, collect the next set of items.
                var nextWork = new List<(MatcherBuilderEntry entry, List<DfaNode> parents)>();

                for (var i = 0; i < work.Count; i++)
                {
                    var (entry, parents) = work[i];

                    if (!HasAdditionalRequiredSegments(entry, depth))
                    {
                        for (var j = 0; j < parents.Count; j++)
                        {
                            var parent = parents[j];
                            parent.Matches.Add(entry);
                        }
                    }

                    // Find the parents of this edge at the current depth
                    var nextParents = new List<DfaNode>();
                    var segment = GetCurrentSegment(entry, depth);
                    if (segment == null)
                    {
                        continue;
                    }

                    for (var j = 0; j < parents.Count; j++)
                    {
                        var parent = parents[j];
                        if (segment.IsSimple && segment.Parts[0].IsLiteral)
                        {
                            var literal = segment.Parts[0].Text;
                            if (!parent.Literals.TryGetValue(literal, out var next))
                            {
                                next = new DfaNode()
                                {
                                    Depth = parent.Depth + 1,
                                    Label = parent.Label + literal + "/",
                                };
                                parent.Literals.Add(literal, next);
                            }

                            nextParents.Add(next);
                        }
                        else if (segment.IsSimple && segment.Parts[0].IsCatchAll)
                        {
                            // A catch all should traverse all literal nodes as well as parameter nodes
                            // we don't need to create the parameter node here because of ordering
                            // all catchalls will be processed after all parameters.
                            nextParents.AddRange(parent.Literals.Values);
                            if (parent.Parameters != null)
                            {
                                nextParents.Add(parent.Parameters);
                            }

                            // We also create a 'catchall' here. We don't do further traversals
                            // on the catchall node because only catchalls can end up here. The
                            // catchall node allows us to capture an unlimited amount of segments
                            // and also to match a zero-length segment, which a parameter node
                            // doesn't allow.
                            if (parent.CatchAll == null)
                            {
                                parent.CatchAll = new DfaNode()
                                {
                                    Depth = parent.Depth + 1,
                                    Label = parent.Label + "{*...}/",
                                };

                                // The catchall node just loops.
                                parent.CatchAll.Parameters = parent.CatchAll;
                                parent.CatchAll.CatchAll = parent.CatchAll;
                            }

                            parent.CatchAll.Matches.Add(entry);
                        }
                        else if (segment.IsSimple && segment.Parts[0].IsParameter)
                        {
                            if (parent.Parameters == null)
                            {
                                parent.Parameters = new DfaNode()
                                {
                                    Depth = parent.Depth + 1,
                                    Label = parent.Label + "{...}/",
                                };
                            }

                            // A parameter should traverse all literal nodes as well as the parameter node
                            nextParents.AddRange(parent.Literals.Values);
                            nextParents.Add(parent.Parameters);
                        }
                        else
                        {
                            // Complex segment - we treat these are parameters here and do the
                            // expensive processing later. We don't want to spend time processing
                            // complex segments unless they are the best match, and treating them
                            // like parameters in the DFA allows us to do just that.
                            if (parent.Parameters == null)
                            {
                                parent.Parameters = new DfaNode()
                                {
                                    Depth = parent.Depth + 1,
                                    Label = parent.Label + "{...}/",
                                };
                            }

                            nextParents.AddRange(parent.Literals.Values);
                            nextParents.Add(parent.Parameters);
                        }
                    }

                    if (nextParents.Count > 0)
                    {
                        nextWork.Add((entry, nextParents));
                    }
                }

                // Prepare the process the next stage.
                work = nextWork;
            }

            return root;
        }

        private TemplateSegment GetCurrentSegment(MatcherBuilderEntry entry, int depth)
        {
            if (depth < entry.Pattern.Segments.Count)
            {
                return entry.Pattern.Segments[depth];
            }

            if (entry.Pattern.Segments.Count == 0)
            {
                return null;
            }

            var lastSegment = entry.Pattern.Segments[entry.Pattern.Segments.Count - 1];
            if (lastSegment.IsSimple && lastSegment.Parts[0].IsCatchAll)
            {
                return lastSegment;
            }

            return null;
        }

        public override Matcher Build()
        {
            var root = BuildDfaTree();

            var states = new List<DfaState>();
            var tables = new List<JumpTableBuilder>();
            AddNode(root, states, tables);

            var exit = states.Count;
            states.Add(new DfaState(CandidateSet.Empty, null));
            tables.Add(new JumpTableBuilder() { DefaultDestination = exit, ExitDestination = exit, });

            for (var i = 0; i < tables.Count; i++)
            {
                if (tables[i].DefaultDestination == JumpTableBuilder.InvalidDestination)
                {
                    tables[i].DefaultDestination = exit;
                }

                if (tables[i].ExitDestination == JumpTableBuilder.InvalidDestination)
                {
                    tables[i].ExitDestination = exit;
                }
            }

            for (var i = 0; i < states.Count; i++)
            {
                states[i] = new DfaState(states[i].Candidates, tables[i].Build());
            }

            return new DfaMatcher(_endpointSelector, states.ToArray());
        }

        private int AddNode(DfaNode node, List<DfaState> states, List<JumpTableBuilder> tables)
        {
            node.Matches.Sort();

            var stateIndex = states.Count;
            var candidates = new CandidateSet(
                node.Matches.Select(CreateCandidate).ToArray(),
                CandidateSet.MakeGroups(GetGroupLengths(node)));
            states.Add(new DfaState(candidates, null));

            var table = new JumpTableBuilder();
            tables.Add(table);

            foreach (var kvp in node.Literals)
            {
                if (kvp.Key == null)
                {
                    continue;
                }

                var transition = Transition(kvp.Value);
                table.AddEntry(kvp.Key, transition);
            }

            if (node.Parameters != null &&
                node.CatchAll != null &&
                ReferenceEquals(node.Parameters, node.CatchAll))
            {
                // This node has a single transition to but it should accept zero-width segments
                // this can happen when a node only has catchall parameters.
                table.DefaultDestination = Transition(node.Parameters);
                table.ExitDestination = table.DefaultDestination;
            }
            else if (node.Parameters != null && node.CatchAll != null)
            {
                // This node has a separate transition for zero-width segments
                // this can happen when a node has both parameters and catchall parameters.
                table.DefaultDestination = Transition(node.Parameters);
                table.ExitDestination = Transition(node.CatchAll);
            }
            else if (node.Parameters != null)
            {
                // This node has paramters but no catchall.
                table.DefaultDestination = Transition(node.Parameters);
            }
            else if (node.CatchAll != null)
            {
                // This node has a catchall but no parameters
                table.DefaultDestination = Transition(node.CatchAll);
                table.ExitDestination = table.DefaultDestination;
            }

            return stateIndex;

            int Transition(DfaNode next)
            {
                // Break cycles
                return ReferenceEquals(node, next) ? stateIndex : AddNode(next, states, tables);
            }
        }

        // internal for tests
        internal Candidate CreateCandidate(MatcherBuilderEntry entry)
        {
            var assignments = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var slots = new List<KeyValuePair<string, object>>();
            var captures = new List<(string parameterName, int segmentIndex, int slotIndex)>();
            (string parameterName, int segmentIndex, int slotIndex) catchAll = default;

            foreach (var kvp in entry.Endpoint.Defaults)
            {
                assignments.Add(kvp.Key, assignments.Count);
                slots.Add(kvp);
            }

            for (var i = 0; i < entry.Pattern.Segments.Count; i++)
            {
                var segment = entry.Pattern.Segments[i];
                if (!segment.IsSimple)
                {
                    continue;
                }

                var part = segment.Parts[0];
                if (!part.IsParameter)
                {
                    continue;
                }

                if (!assignments.TryGetValue(part.Name, out var slotIndex))
                {
                    slotIndex = assignments.Count;
                    assignments.Add(part.Name, slotIndex);

                    var hasDefaultValue = part.DefaultValue != null || part.IsCatchAll;
                    slots.Add(hasDefaultValue ? new KeyValuePair<string, object>(part.Name, part.DefaultValue) : default);
                }

                if (part.IsCatchAll)
                {
                    catchAll = (part.Name, i, slotIndex);
                }
                else
                {
                    captures.Add((part.Name, i, slotIndex));
                }
            }

            var complexSegments = new List<(RoutePatternPathSegment pathSegment, int segmentIndex)>();
            for (var i = 0; i < entry.Pattern.Segments.Count; i++)
            {
                var segment = entry.Pattern.Segments[i];
                if (segment.IsSimple)
                {
                    continue;
                }

                complexSegments.Add((segment.ToRoutePatternPathSegment(), i));
            }

            var matchProcessors = new List<MatchProcessor>();
            for (var i = 0; i < entry.Endpoint.MatchProcessorReferences.Count; i++)
            {
                var reference = entry.Endpoint.MatchProcessorReferences[i];
                var processor = _matchProcessorFactory.Create(reference);
                matchProcessors.Add(processor);
            }

            return new Candidate(
                entry.Endpoint,
                slots.ToArray(),
                captures.ToArray(),
                catchAll,
                complexSegments.ToArray(),
                matchProcessors.ToArray());
        }

        private int[] GetGroupLengths(DfaNode node)
        {
            if (node.Matches.Count == 0)
            {
                return Array.Empty<int>();
            }

            var groups = new List<int>();

            var length = 1;
            var exemplar = node.Matches[0];

            for (var i = 1; i < node.Matches.Count; i++)
            {
                if (!exemplar.PriorityEquals(node.Matches[i]))
                {
                    groups.Add(length);
                    length = 0;

                    exemplar = node.Matches[i];
                }

                length++;
            }

            groups.Add(length);

            return groups.ToArray();
        }

        private static bool HasAdditionalRequiredSegments(MatcherBuilderEntry entry, int depth)
        {
            for (var i = depth; i < entry.Pattern.Segments.Count; i++)
            {
                var segment = entry.Pattern.Segments[i];
                if (!segment.IsSimple)
                {
                    // Complex segments always require more processing
                    return true;
                }

                var part = segment.Parts[0];
                if (part.IsLiteral)
                {
                    return true;
                }

                if (!part.IsOptional &&
                    !part.IsCatchAll &&
                    part.DefaultValue == null &&
                    !entry.Endpoint.Defaults.ContainsKey(part.Name))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
