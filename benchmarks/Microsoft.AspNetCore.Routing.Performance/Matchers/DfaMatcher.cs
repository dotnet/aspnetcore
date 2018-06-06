// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Template;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    internal class DfaMatcher : Matcher
    {
        public static MatcherBuilder CreateBuilder() => new Builder();

        private readonly State[] _states;

        private DfaMatcher(State[] states)
        {
            _states = states;
        }

        public override Task MatchAsync(HttpContext httpContext, IEndpointFeature feature)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (feature == null)
            {
                throw new ArgumentNullException(nameof(feature));
            }

            var states = _states;
            var current = 0;

            var path = httpContext.Request.Path.Value;

            var start = 1; // PathString always has a leading slash
            var length = 0;
            var end = 0;
            while ((end = path.IndexOf('/', start + 1)) >= 0)
            {
                current = states[current].Transitions.GetDestination(path, start, end - start);
                start = end;
            }

            // residue
            length = path.Length - start;
            if (length > 0)
            {
                current = states[current].Transitions.GetDestination(path, start, length);
            }

            var matches = states[current].Matches;
            feature.Endpoint = matches.Length == 0 ? null : matches[0];
            return Task.CompletedTask;
        }
        
        private struct State
        {
            public bool IsAccepting;
            public Endpoint[] Matches;
            public JumpTable Transitions;
        }

        private abstract class JumpTable
        {
            public abstract int GetDestination(string text, int start, int length);
        }

        private class JumpTableBuilder
        {
            private readonly List<(string text, int destination)> _entries = new List<(string text, int destination)>();

            public int Depth { get; set; }

            public int Exit { get; set; }

            public void AddEntry(string text, int destination)
            {
                _entries.Add((text, destination));
            }

            public JumpTable Build()
            {
                return new SimpleJumpTable(Depth, Exit, _entries.ToArray());
            }
        }

        private class SimpleJumpTable : JumpTable
        {
            private readonly (string text, int destination)[] _entries;
            private readonly int _depth;
            private readonly int _exit;

            public SimpleJumpTable(int depth, int exit, (string text, int destination)[] entries)
            {
                _depth = depth;
                _exit = exit;
                _entries = entries;
            }

            public override int GetDestination(string text, int start, int length)
            {
                for (var i = 0; i < _entries.Length; i++)
                {
                    if (length == _entries[i].text.Length &&
                        string.Compare(
                        text,
                        start,
                        _entries[i].text,
                        0,
                        length,
                        StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        return _entries[i].destination;
                    }
                }

                return _exit;
            }
        }

        private class Entry
        {
            public int Order;
            public decimal Precedence;
            public RouteTemplate Pattern;
            public Endpoint Endpoint;
        }

        private class Node
        {
            public int Depth { get; set; }

            public List<Entry> Matches { get; } = new List<Entry>();

            public Dictionary<string, Node> Literals { get; } = new Dictionary<string, Node>();
        }

        private class Builder : MatcherBuilder
        {
            private List<Entry> _entries = new List<Entry>();

            public override void AddEntry(string pattern, MatcherEndpoint endpoint)
            {
                var parsed = TemplateParser.Parse(pattern);
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

                // Since we overlay parameters onto the literal entries, we do two passes, first we create
                // all of the literal nodes, then we 'spread' parameters
                for (var i = 0; i < _entries.Count; i++)
                {
                    var entry = _entries[i];

                    var parent = root;

                    for (var depth = 0; depth < entry.Pattern.Segments.Count; depth++)
                    {
                        var segment = entry.Pattern.Segments[depth];
                        
                        if (segment.IsSimple && segment.Parts[0].IsLiteral)
                        {
                            if (!parent.Literals.TryGetValue(segment.Parts[0].Text, out var next))
                            {
                                next = new Node() { Depth = depth, };
                                parent.Literals.Add(segment.Parts[0].Text, next);
                            }

                            parent = next;
                        }
                        else if (segment.IsSimple && segment.Parts[0].IsParameter)
                        {
                            if (!parent.Literals.TryGetValue("*", out var next))
                            {
                                next = new Node() { Depth = depth, };
                                parent.Literals.Add("*", next);
                            }

                            parent = next;
                        }
                    }

                    parent.Matches.Add(entry);
                }

                for (var i = 0; i < _entries.Count; i++)
                {
                    var entry = _entries[i];

                    var parents = new List<Node>() { root, };

                    for (var depth = 0; depth < entry.Pattern.Segments.Count; depth++)
                    {
                        var segment = entry.Pattern.Segments[depth];

                        if (segment.IsSimple && segment.Parts[0].IsLiteral)
                        {
                            var next = new List<Node>();
                            for (var j = 0; j < parents.Count; j++)
                            {
                                if (!parents[j].Literals.TryGetValue(segment.Parts[0].Text, out var child))
                                {
                                    child = new Node() { Depth = depth, };
                                    if (parents[j].Literals.TryGetValue("*", out var parameter))
                                    {
                                        child.Matches.AddRange(parameter.Matches);
                                        foreach (var kvp in parameter.Literals)
                                        {
                                            child.Literals.Add(kvp.Key, DeepCopy(kvp.Value));
                                        }
                                    }

                                    parents[j].Literals.Add(segment.Parts[0].Text, child);
                                }

                                next.Add(child);
                            }

                            parents = next;
                        }
                        else if (segment.IsSimple && segment.Parts[0].IsParameter)
                        {
                            var next = new List<Node>();
                            for (var j = 0; j < parents.Count; j++)
                            {
                                next.AddRange(parents[j].Literals.Values);
                            }

                            parents = next;
                        }
                    }
                    
                    for (var j = 0; j < parents.Count; j++)
                    {
                        if (!parents[j].Matches.Contains(entry))
                        {
                            parents[j].Matches.Add(entry);
                        }
                    }
                }

                var states = new List<State>();
                var tables = new List<JumpTableBuilder>();
                AddNode(root, states, tables);

                var exit = states.Count;
                states.Add(new State() { IsAccepting = false, Matches = Array.Empty<Endpoint>(), });
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

            private static int AddNode(Node node, List<State> states, List<JumpTableBuilder> tables)
            {
                Sort(node.Matches);

                var index = states.Count;
                states.Add(new State() { Matches = node.Matches.Select(e => e.Endpoint).ToArray(), IsAccepting = node.Matches.Count > 0 });

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

            private static void Sort(List<Entry> entries)
            {
                entries.Sort((x, y) =>
                {
                    var comparison = x.Order.CompareTo(y.Order);
                    if (comparison != 0)
                    {
                        return comparison;
                    }

                    comparison = y.Precedence.CompareTo(x.Precedence);
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
        }
    }
}
