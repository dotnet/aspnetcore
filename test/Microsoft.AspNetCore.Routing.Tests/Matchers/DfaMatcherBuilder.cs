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
        private List<Entry> _entries = new List<Entry>();

        public override void AddEndpoint(MatcherEndpoint endpoint)
        {
            var parsed = TemplateParser.Parse(endpoint.Template);
            _entries.Add(new Entry()
            {
                Order = 0,
                Pattern = parsed,
                Precedence = RoutePrecedence.ComputeInbound(parsed),
                Endpoint = endpoint,
            });
        }

        public override Matcher Build()
        {
            Sort(_entries);

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
            states.Add(new State() { IsAccepting = false, Matches = Array.Empty<Candidate>(), });
            tables.Add(new JumpTableBuilder() { Exit = exit, });

            for (var i = 0; i < tables.Count; i++)
            {
                if (tables[i].Exit == -1)
                {
                    tables[i].Exit = exit;
                }
            }

            for (var i = 0; i < states.Count; i++)
            {
                states[i] = new State()
                {
                    IsAccepting = states[i].IsAccepting,
                    Matches = states[i].Matches,
                    Transitions = tables[i].Build(),
                };
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
            Sort(node.Matches);

            var index = states.Count;
            states.Add(new State()
            {
                Matches = node.Matches.Select(CreateCandidate).ToArray(),
                IsAccepting = node.Matches.Count > 0,
            });

            var table = new JumpTableBuilder() { Depth = node.Depth, };
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

            var exitIndex = -1;
            if (node.Literals.TryGetValue("*", out var exit))
            {
                exitIndex = AddNode(exit, states, tables);
            }

            table.Exit = exitIndex;
            return index;
        }

        private static Candidate CreateCandidate(Entry entry)
        {
            return new Candidate()
            {
                Endpoint = entry.Endpoint,
                Parameters = entry.Pattern.Segments.Select(s => s.IsSimple && s.Parts[0].IsParameter ? s.Parts[0].Name : null).ToArray(),
            };
        }

        private static void Sort(List<Entry> entries)
        {
            entries.Sort((x, y) =>
            {
                var comparison = x.Order.CompareTo(y.Order);
                if (comparison != 0)
                {
                    return comparison;
                }

                comparison = x.Precedence.CompareTo(y.Precedence);
                if (comparison != 0)
                {
                    return comparison;
                }

                return x.Pattern.TemplateText.CompareTo(y.Pattern.TemplateText);
            });
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

        private class Entry
        {
            public int Order;
            public decimal Precedence;
            public RouteTemplate Pattern;
            public Endpoint Endpoint;
        }

        [DebuggerDisplay("{DebuggerToString(),nq}")]
        private class Node
        {
            public int Depth { get; set; }

            public List<Entry> Matches { get; } = new List<Entry>();

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
