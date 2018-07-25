// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    internal class DfaMatcherBuilder : MatcherBuilder
    {
        private readonly List<MatcherEndpoint> _endpoints = new List<MatcherEndpoint>();

        private readonly MatchProcessorFactory _matchProcessorFactory;
        private readonly EndpointSelector _selector;
        private readonly MatcherPolicy[] _policies;
        private readonly INodeBuilderPolicy[] _nodeBuilders;
        private readonly MatcherEndpointComparer _comparer;

        public DfaMatcherBuilder(
            MatchProcessorFactory matchProcessorFactory,
            EndpointSelector selector,
            IEnumerable<MatcherPolicy> policies)
        {
            _matchProcessorFactory = matchProcessorFactory;
            _selector = selector;
            _policies = policies.OrderBy(p => p.Order).ToArray();

            // Taking care to use _policies, which has been sorted.
            _nodeBuilders = _policies.OfType<INodeBuilderPolicy>().ToArray();
            _comparer = new MatcherEndpointComparer(_policies.OfType<IEndpointComparerPolicy>().ToArray());
        }

        public override void AddEndpoint(MatcherEndpoint endpoint)
        {
            _endpoints.Add(endpoint);
        }

        public DfaNode BuildDfaTree()
        {
            // We build the tree by doing a BFS over the list of entries. This is important
            // because a 'parameter' node can also traverse the same paths that literal nodes
            // traverse. This means that we need to order the entries first, or else we will
            // miss possible edges in the DFA.
            _endpoints.Sort(_comparer);

            // Since we're doing a BFS we will process each 'level' of the tree in stages
            // this list will hold the set of items we need to process at the current
            // stage.
            var work = new List<(MatcherEndpoint endpoint, List<DfaNode> parents)>();

            var root = new DfaNode() { Depth = 0, Label = "/" };

            // To prepare for this we need to compute the max depth, as well as
            // a seed list of items to process (entry, root).
            var maxDepth = 0;
            for (var i = 0; i < _endpoints.Count; i++)
            {
                var endpoint = _endpoints[i];
                maxDepth = Math.Max(maxDepth, endpoint.RoutePattern.PathSegments.Count);

                work.Add((endpoint, new List<DfaNode>() { root, }));
            }

            // Now we process the entries a level at a time.
            for (var depth = 0; depth <= maxDepth; depth++)
            {
                // As we process items, collect the next set of items.
                var nextWork = new List<(MatcherEndpoint endpoint, List<DfaNode> parents)>();

                for (var i = 0; i < work.Count; i++)
                {
                    var (endpoint, parents) = work[i];

                    if (!HasAdditionalRequiredSegments(endpoint, depth))
                    {
                        for (var j = 0; j < parents.Count; j++)
                        {
                            var parent = parents[j];
                            parent.Matches.Add(endpoint);
                        }
                    }

                    // Find the parents of this edge at the current depth
                    var nextParents = new List<DfaNode>();
                    var segment = GetCurrentSegment(endpoint, depth);
                    if (segment == null)
                    {
                        continue;
                    }

                    for (var j = 0; j < parents.Count; j++)
                    {
                        var parent = parents[j];
                        var part = segment.Parts[0];
                        if (segment.IsSimple && part is RoutePatternLiteralPart literalPart)
                        {
                            var literal = literalPart.Content;
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
                        else if (segment.IsSimple && part is RoutePatternParameterPart parameterPart && parameterPart.IsCatchAll)
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

                            parent.CatchAll.Matches.Add(endpoint);
                        }
                        else if (segment.IsSimple && part.IsParameter)
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
                        nextWork.Add((endpoint, nextParents));
                    }
                }

                // Prepare the process the next stage.
                work = nextWork;
            }

            // Build the trees of policy nodes (like HTTP methods). Post-order traversal
            // means that we won't have infinite recursion.
            root.Visit(ApplyPolicies);

            return root;
        }

        private RoutePatternPathSegment GetCurrentSegment(MatcherEndpoint endpoint, int depth)
        {
            if (depth < endpoint.RoutePattern.PathSegments.Count)
            {
                return endpoint.RoutePattern.PathSegments[depth];
            }

            if (endpoint.RoutePattern.PathSegments.Count == 0)
            {
                return null;
            }

            var lastSegment = endpoint.RoutePattern.PathSegments[endpoint.RoutePattern.PathSegments.Count - 1];
            if (lastSegment.IsSimple && lastSegment.Parts[0] is RoutePatternParameterPart parameterPart && parameterPart.IsCatchAll)
            {
                return lastSegment;
            }

            return null;
        }

        public override Matcher Build()
        {
            var root = BuildDfaTree();

            var states = new List<DfaState>();
            var tableBuilders = new List<(JumpTableBuilder pathBuilder, PolicyJumpTableBuilder policyBuilder)>();
            AddNode(root, states, tableBuilders);

            var exit = states.Count;
            states.Add(new DfaState(Array.Empty<Candidate>(), null, null));
            tableBuilders.Add((new JumpTableBuilder() { DefaultDestination = exit, ExitDestination = exit, }, null));

            for (var i = 0; i < tableBuilders.Count; i++)
            {
                if (tableBuilders[i].pathBuilder?.DefaultDestination == JumpTableBuilder.InvalidDestination)
                {
                    tableBuilders[i].pathBuilder.DefaultDestination = exit;
                }

                if (tableBuilders[i].pathBuilder?.ExitDestination == JumpTableBuilder.InvalidDestination)
                {
                    tableBuilders[i].pathBuilder.ExitDestination = exit;
                }

                if (tableBuilders[i].policyBuilder?.ExitDestination == JumpTableBuilder.InvalidDestination)
                {
                    tableBuilders[i].policyBuilder.ExitDestination = exit;
                }
            }

            for (var i = 0; i < states.Count; i++)
            {
                states[i] = new DfaState(
                    states[i].Candidates, 
                    tableBuilders[i].pathBuilder?.Build(),
                    tableBuilders[i].policyBuilder?.Build());
            }

            return new DfaMatcher(_selector, states.ToArray());
        }

        private int AddNode(
            DfaNode node,
            List<DfaState> states,
            List<(JumpTableBuilder pathBuilder, PolicyJumpTableBuilder policyBuilder)> tableBuilders)
        {
            node.Matches.Sort(_comparer);

            var stateIndex = states.Count;

            var candidates = CreateCandidates(node.Matches);
            states.Add(new DfaState(candidates, null, null));

            var pathBuilder = new JumpTableBuilder();
            tableBuilders.Add((pathBuilder, null));

            foreach (var kvp in node.Literals)
            {
                if (kvp.Key == null)
                {
                    continue;
                }

                var transition = Transition(kvp.Value);
                pathBuilder.AddEntry(kvp.Key, transition);
            }

            if (node.Parameters != null &&
                node.CatchAll != null &&
                ReferenceEquals(node.Parameters, node.CatchAll))
            {
                // This node has a single transition to but it should accept zero-width segments
                // this can happen when a node only has catchall parameters.
                pathBuilder.DefaultDestination = Transition(node.Parameters);
                pathBuilder.ExitDestination = pathBuilder.DefaultDestination;
            }
            else if (node.Parameters != null && node.CatchAll != null)
            {
                // This node has a separate transition for zero-width segments
                // this can happen when a node has both parameters and catchall parameters.
                pathBuilder.DefaultDestination = Transition(node.Parameters);
                pathBuilder.ExitDestination = Transition(node.CatchAll);
            }
            else if (node.Parameters != null)
            {
                // This node has paramters but no catchall.
                pathBuilder.DefaultDestination = Transition(node.Parameters);
            }
            else if (node.CatchAll != null)
            {
                // This node has a catchall but no parameters
                pathBuilder.DefaultDestination = Transition(node.CatchAll);
                pathBuilder.ExitDestination = pathBuilder.DefaultDestination;
            }

            if (node.PolicyEdges.Count > 0)
            {
                var policyBuilder = new PolicyJumpTableBuilder(node.NodeBuilder);
                tableBuilders[stateIndex] = (pathBuilder, policyBuilder);

                foreach (var kvp in node.PolicyEdges)
                {
                    policyBuilder.AddEntry(kvp.Key, Transition(kvp.Value));
                }
            }

            return stateIndex;

            int Transition(DfaNode next)
            {
                // Break cycles
                return ReferenceEquals(node, next) ? stateIndex : AddNode(next, states, tableBuilders);
            }
        }

        // Builds an array of candidates for a node, assigns a 'score' for each
        // endpoint.
        internal Candidate[] CreateCandidates(IReadOnlyList<MatcherEndpoint> endpoints)
        {
            if (endpoints.Count == 0)
            {
                return Array.Empty<Candidate>();
            }

            var candiates = new Candidate[endpoints.Count];

            var score = 0;
            var examplar = endpoints[0];
            candiates[0] = CreateCandidate(examplar, score);

            for (var i = 1; i < endpoints.Count; i++)
            {
                var endpoint = endpoints[i];
                if (!_comparer.Equals(examplar, endpoint))
                {
                    // This endpoint doesn't have the same priority.
                    examplar = endpoint;
                    score++;
                }

                candiates[i] = CreateCandidate(endpoint, score);
            }

            return candiates;
        }

        // internal for tests
        internal Candidate CreateCandidate(MatcherEndpoint endpoint, int score)
        {
            var assignments = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var slots = new List<KeyValuePair<string, object>>();
            var captures = new List<(string parameterName, int segmentIndex, int slotIndex)>();
            (string parameterName, int segmentIndex, int slotIndex) catchAll = default;

            foreach (var kvp in endpoint.RoutePattern.Defaults)
            {
                assignments.Add(kvp.Key, assignments.Count);
                slots.Add(kvp);
            }

            for (var i = 0; i < endpoint.RoutePattern.PathSegments.Count; i++)
            {
                var segment = endpoint.RoutePattern.PathSegments[i];
                if (!segment.IsSimple)
                {
                    continue;
                }

                var parameterPart = segment.Parts[0] as RoutePatternParameterPart;
                if (parameterPart == null)
                {
                    continue;
                }

                if (!assignments.TryGetValue(parameterPart.Name, out var slotIndex))
                {
                    slotIndex = assignments.Count;
                    assignments.Add(parameterPart.Name, slotIndex);

                    var hasDefaultValue = parameterPart.Default != null || parameterPart.IsCatchAll;
                    slots.Add(hasDefaultValue ? new KeyValuePair<string, object>(parameterPart.Name, parameterPart.Default) : default);
                }

                if (parameterPart.IsCatchAll)
                {
                    catchAll = (parameterPart.Name, i, slotIndex);
                }
                else
                {
                    captures.Add((parameterPart.Name, i, slotIndex));
                }
            }

            var complexSegments = new List<(RoutePatternPathSegment pathSegment, int segmentIndex)>();
            for (var i = 0; i < endpoint.RoutePattern.PathSegments.Count; i++)
            {
                var segment = endpoint.RoutePattern.PathSegments[i];
                if (segment.IsSimple)
                {
                    continue;
                }

                complexSegments.Add((segment, i));
            }

            var matchProcessors = new List<MatchProcessor>();
            foreach (var kvp in endpoint.RoutePattern.Constraints)
            {
                var parameter = endpoint.RoutePattern.GetParameter(kvp.Key); // may be null, that's ok
                var constraintReferences = kvp.Value;
                for (var i = 0; i < constraintReferences.Count; i++)
                {
                    var constraintReference = constraintReferences[i];
                    var matchProcessor = _matchProcessorFactory.Create(parameter, constraintReference);
                    matchProcessors.Add(matchProcessor);
                }
            }

            return new Candidate(
                endpoint,
                score,
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
                if (!_comparer.Equals(exemplar, node.Matches[i]))
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

        private static bool HasAdditionalRequiredSegments(MatcherEndpoint endpoint, int depth)
        {
            for (var i = depth; i < endpoint.RoutePattern.PathSegments.Count; i++)
            {
                var segment = endpoint.RoutePattern.PathSegments[i];
                if (!segment.IsSimple)
                {
                    // Complex segments always require more processing
                    return true;
                }

                var parameterPart = segment.Parts[0] as RoutePatternParameterPart;
                if (parameterPart == null)
                {
                    // It's a literal
                    return true;
                }

                if (!parameterPart.IsOptional &&
                    !parameterPart.IsCatchAll &&
                    parameterPart.Default == null)
                {
                    return true;
                }
            }

            return false;
        }

        private void ApplyPolicies(DfaNode node)
        {
            if (node.Matches.Count == 0)
            {
                return;
            }

            // Start with the current node as the root.
            var work = new List<DfaNode>() { node, };
            for (var i = 0; i < _nodeBuilders.Length; i++)
            {
                var nodeBuilder = _nodeBuilders[i];

                // Build a list of each 
                var nextWork = new List<DfaNode>();

                for (var j = 0; j < work.Count; j++)
                {
                    var parent = work[j];
                    if (!nodeBuilder.AppliesToNode(parent.Matches))
                    {
                        // This node-builder doesn't care about this node, so add it to the list
                        // to be processed by the next node-builder.
                        nextWork.Add(parent);
                        continue;
                    }

                    // This node-builder does apply to this node, so we need to create new nodes for each edge,
                    // and then attach them to the parent.
                    var edges = nodeBuilder.GetEdges(parent.Matches);
                    for (var k = 0; k < edges.Count; k++)
                    {
                        var edge = edges[k];

                        var next = new DfaNode();

                        // TODO: https://github.com/aspnet/Routing/issues/648
                        next.Matches.AddRange(edge.Endpoints.Cast<MatcherEndpoint>().ToArray());
                        nextWork.Add(next);

                        parent.PolicyEdges.Add(edge.State, next);
                    }

                    // Associate the node-builder so we can build a jump table later.
                    parent.NodeBuilder = nodeBuilder;

                    // The parent no longer has matches, it's not considered a terminal node.
                    parent.Matches.Clear();
                }

                work = nextWork;
            }
        }
    }
}
