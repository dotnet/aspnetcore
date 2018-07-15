// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Routing.Template;
using static Microsoft.AspNetCore.Routing.Matchers.DfaMatcher;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    internal class DfaMatcherBuilder : MatcherBuilder
    {
        private List<MatcherBuilderEntry> _entries = new List<MatcherBuilderEntry>();

        public override void AddEndpoint(MatcherEndpoint endpoint)
        {
            var parsed = TemplateParser.Parse(endpoint.Template);
            _entries.Add(new MatcherBuilderEntry(endpoint));
        }

        public override Matcher Build()
        {
            _entries.Sort();

            var root = new Node() { Depth = -1 };

            // We build the tree by doing a BFS over the list of entries. This is important
            // because a 'parameter' node can also traverse the same paths that literal nodes traverse.
            var maxDepth = 0;
            for (var i = 0; i < _entries.Count; i++)
            {
                maxDepth = Math.Max(maxDepth, _entries[i].Pattern.Segments.Count);
            }

            for (var depth = 0; depth <= maxDepth; depth++)
            {
                for (var i = 0; i < _entries.Count; i++)
                {
                    var entry = _entries[i];
                    if (entry.Pattern.Segments.Count < depth)
                    {
                        continue;
                    }

                    // Find the parents of this edge at the current depth
                    var parents = new List<Node>() { root };
                    for (var j = 0; j < depth; j++)
                    {
                        var next = new List<Node>();
                        for (var k = 0; k < parents.Count; k++)
                        {
                            next.Add(Traverse(parents[k], entry.Pattern.Segments[j]));
                        }

                        parents = next;
                    }

                    if (entry.Pattern.Segments.Count == depth)
                    {
                        for (var j = 0; j < parents.Count; j++)
                        {
                            var parent = parents[j];
                            parent.Matches.Add(entry);
                        }

                        continue;
                    }

                    var segment = entry.Pattern.Segments[depth];
                    for (var j = 0; j < parents.Count; j++)
                    {
                        var parent = parents[j];
                        if (segment.IsSimple && segment.Parts[0].IsLiteral)
                        {
                            if (!parent.Literals.TryGetValue(segment.Parts[0].Text, out var next))
                            {
                                next = new Node() { Depth = depth, };
                                parent.Literals.Add(segment.Parts[0].Text, next);
                            }
                        }
                        else if (segment.IsSimple && segment.Parts[0].IsParameter)
                        {
                            if (!parent.Literals.TryGetValue("*", out var next))
                            {
                                next = new Node() { Depth = depth, };
                                parent.Literals.Add("*", next);
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException("We only support simple segments.");
                        }
                    }
                }
            }

            var states = new List<State>();
            var tables = new List<JumpTableBuilder>();
            AddNode(root, states, tables);

            var exit = states.Count;
            states.Add(new State(CandidateSet.Empty, null));
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
                states[i] = new State(states[i].Candidates, tables[i].Build());
            }

            return new DfaMatcher(states.ToArray());
        }

        private Node Traverse(Node node, TemplateSegment segment)
        {
            if (!segment.IsSimple)
            {
                throw new InvalidOperationException("We only support simple segments.");
            }

            if (segment.Parts[0].IsLiteral)
            {
                return node.Literals[segment.Parts[0].Text];
            }

            return node.Literals["*"];
        }

        private static int AddNode(Node node, List<State> states, List<JumpTableBuilder> tables)
        {
            node.Matches.Sort();

            var index = states.Count;

            // This is just temporary. This code ignores groups for now, and creates
            // a single group with all matches.
            var candidates = new CandidateSet(
                node.Matches.Select(CreateCandidate).ToArray(),
                CandidateSet.MakeGroups(new int[] { node.Matches.Count, }));

            // JumpTable temporarily null. Will be patched later.
            states.Add(new State(candidates, null));

            var table = new JumpTableBuilder();
            tables.Add(table);

            foreach (var kvp in node.Literals)
            {
                if (kvp.Key == "*")
                {
                    continue;
                }

                var transition = AddNode(kvp.Value, states, tables);
                table.AddEntry(kvp.Key, transition);
            }

            var defaultIndex = -1;
            if (node.Literals.TryGetValue("*", out var exit))
            {
                defaultIndex = AddNode(exit, states, tables);
            }

            table.DefaultDestination = defaultIndex;
            return index;
        }

        private static Candidate CreateCandidate(MatcherBuilderEntry entry)
        {
            var parameters = entry.Pattern.Segments
                .Select(s => s.IsSimple && s.Parts[0].IsParameter ? s.Parts[0].Name : null)
                .ToArray();
            return new Candidate(entry.Endpoint, parameters);
        }

        private static Node DeepCopy(Node node)
        {
            var copy = new Node() { Depth = node.Depth, };
            copy.Matches.AddRange(node.Matches);

            foreach (var kvp in node.Literals)
            {
                copy.Literals.Add(kvp.Key, DeepCopy(kvp.Value));
            }

            return node;
        }

        [DebuggerDisplay("{DebuggerToString(),nq}")]
        private class Node
        {
            public int Depth { get; set; }

            public List<MatcherBuilderEntry> Matches { get; } = new List<MatcherBuilderEntry>();

            public Dictionary<string, Node> Literals { get; } = new Dictionary<string, Node>(StringComparer.OrdinalIgnoreCase);

            private string DebuggerToString()
            {
                var builder = new StringBuilder();
                builder.Append("d:");
                builder.Append(Depth);
                builder.Append(" m:");
                builder.Append(Matches.Count);
                builder.Append(" c: ");
                builder.Append(string.Join(", ", Literals.Select(kvp => $"{kvp.Key}->({kvp.Value.DebuggerToString()})")));
                return builder.ToString();
            }
        }
    }
}
