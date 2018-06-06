// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Template;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    internal class InstructionMatcher : Matcher
    {
        public static MatcherBuilder CreateBuilder() => new Builder();

        private State _state;

        private InstructionMatcher(Instruction[] instructions, Endpoint[] endpoints, JumpTable[] tables)
        {
            _state = new State()
            {
                Instructions = instructions,
                Endpoints = endpoints,
                Tables = tables,
            };
        }

        public unsafe override Task MatchAsync(HttpContext httpContext, IEndpointFeature feature)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (feature == null)
            {
                throw new ArgumentNullException(nameof(feature));
            }

            var state = _state;

            var path = httpContext.Request.Path.Value;
            var buffer = stackalloc int[32];

            var segment = 0;
            var index = -1;
            while ((index = path.IndexOf('/', index + 1)) >= 0)
            {
                buffer[segment++] = index + 1;
            }

            buffer[segment] = path.Length;

            var i = 0;
            Endpoint result = null;
            while (i < state.Instructions.Length)
            {
                var instruction = state.Instructions[i];
                switch (instruction.Code)
                {
                    case InstructionCode.Accept:
                        {
                            result = state.Endpoints[instruction.Payload];
                            i++;
                            break;
                        }
                    case InstructionCode.Branch:
                        {
                            var table = state.Tables[instruction.Payload];
                            i = table.GetDestination(buffer, path);
                            break;
                        }
                    case InstructionCode.Jump:
                        {
                            i = instruction.Payload;
                            break;
                        }
                }
            }

            feature.Endpoint = result;
            return Task.CompletedTask;
        }

        private class State
        {
            public Endpoint[] Endpoints;
            public Instruction[] Instructions;
            public JumpTable[] Tables;
        }

        [DebuggerDisplay("{ToDebugString(),nq}")]
        [StructLayout(LayoutKind.Explicit)]
        private struct Instruction
        {
            [FieldOffset(3)]
            public InstructionCode Code;

            [FieldOffset(4)]
            public int Payload;

            private string ToDebugString()
            {
                return $"{Code}: {Payload}";
            }
        }

        private enum InstructionCode : byte
        {
            Accept,
            Branch,
            Jump,
            Pop, // Only used during the instruction builder phase
        }

        private abstract class JumpTable
        {
            public unsafe abstract int GetDestination(int* segments, string text);
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

            public unsafe override int GetDestination(int* segments, string text)
            {
                var start = segments[_depth];
                var length = segments[_depth + 1] - start;
                
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

        private class InstructionBuilder
        {
            private readonly List<Instruction> _instructions = new List<Instruction>();
            private readonly List<Endpoint> _endpoints = new List<Endpoint>();
            private readonly List<JumpTableBuilder> _tables = new List<JumpTableBuilder>();

            private readonly List<int> _blocks = new List<int>();

            public int Next => _instructions.Count;

            public void BeginBlock()
            {
                _blocks.Add(Next);
            }

            public void EndBlock()
            {
                var start = _blocks[_blocks.Count - 1];
                var end = Next;
                for (var i = start; i < end; i++)
                {
                    if (_instructions[i].Code == InstructionCode.Pop)
                    {
                        _instructions[i] = new Instruction() { Code = InstructionCode.Jump, Payload = end };
                    }
                }

                _blocks.RemoveAt(_blocks.Count - 1);
            }

            public int AddInstruction(Instruction instruction)
            {
                _instructions.Add(instruction);
                return _instructions.Count - 1;
            }

            public int AddEndpoint(Endpoint endpoint)
            {
                _endpoints.Add(endpoint);
                return _endpoints.Count - 1;
            }

            public int AddJumpTable(JumpTableBuilder table)
            {
                _tables.Add(table);
                return _tables.Count - 1;
            }

            public void Deconstruct(out Instruction[] instructions, out Endpoint[] endpoints, out JumpTable[] tables)
            {
                instructions = _instructions.ToArray();
                endpoints = _endpoints.ToArray();

                tables = new JumpTable[_tables.Count];
                for (var i = 0; i < _tables.Count; i++)
                {
                    tables[i] = _tables[i].Build();
                }
            }
        }

        private abstract class Node
        {
            public int Depth { get; protected set; }
            public List<Node> Children { get; } = new List<Node>();

            public abstract void Lower(InstructionBuilder builder);

            public TNode GetNode<TNode>() where TNode : Node
            {
                for (var i = 0; i < Children.Count; i++)
                {
                    if (Children[i] is TNode match)
                    {
                        return match;
                    }
                }

                return null;
            }

            public TNode AddNode<TNode>(TNode node) where TNode : Node
            {
                // We already ordered the routes into precedence order
                Children.Add(node);
                return node;
            }
        }

        private class SequenceNode : Node
        {
            public SequenceNode(int depth)
            {
                Depth = depth;
            }
            
            public override void Lower(InstructionBuilder builder)
            {
                for (var i = 0; i < Children.Count; i++)
                {
                    Children[i].Lower(builder);
                }
            }
        }

        private class OrderNode : SequenceNode
        {
            public OrderNode(int order)
                : base(0)
            {
                Order = order;
            }

            public int Order { get; }
        }

        private class BranchNode : Node
        {
            public BranchNode(int depth)
            {
                Depth = depth;
            }

            public List<string> Literals { get; } = new List<string>();

            public override void Lower(InstructionBuilder builder)
            {
                var table = new JumpTableBuilder() { Depth = Depth, };
                var index = builder.AddJumpTable(table);
                builder.AddInstruction(new Instruction() { Code = InstructionCode.Branch, Payload = index });

                builder.BeginBlock();

                for (var i = 0; i < Children.Count; i++)
                {
                    table.AddEntry(Literals[i], builder.Next);
                    Children[i].Lower(builder);
                    builder.AddInstruction(new Instruction() { Code = InstructionCode.Pop, });
                }

                builder.EndBlock();
                table.Exit = builder.Next;
            }
        }

        private class ParameterNode : Node
        {
            public ParameterNode(int depth)
            {
                Depth = depth;
            }

            public override void Lower(InstructionBuilder builder)
            {
                for (var i = 0; i < Children.Count; i++)
                {
                    Children[i].Lower(builder);
                }
            }
        }

        private class AcceptNode : Node
        {
            public AcceptNode(Endpoint endpoint)
            {
                Endpoint = endpoint;
            }

            public Endpoint Endpoint { get; }

            public override void Lower(InstructionBuilder builder)
            {
                builder.AddInstruction(new Instruction()
                {
                    Code = InstructionCode.Accept,
                    Payload = builder.AddEndpoint(Endpoint),
                });
            }
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
                _entries.Sort((x, y) =>
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

                var roots = new List<OrderNode>();

                for (var i = 0; i < _entries.Count; i++)
                {
                    var entry = _entries[i];

                    var parent = (SequenceNode)GetOrCreateRootNode(roots, entry.Order);

                    for (var depth = 0; depth < entry.Pattern.Segments.Count; depth++)
                    {
                        var segment = entry.Pattern.Segments[depth];
                        if (segment.IsSimple && segment.Parts[0].IsLiteral)
                        {
                            var branch = parent.GetNode<BranchNode>() ?? parent.AddNode(new BranchNode(depth));

                            var index = branch.Literals.IndexOf(segment.Parts[0].Text);
                            if (index == -1)
                            {
                                branch.Literals.Add(segment.Parts[0].Text);
                                branch.AddNode(new SequenceNode(depth + 1));
                                index = branch.Children.Count - 1;
                            }

                            parent = (SequenceNode)branch.Children[index];
                        }
                        else if (segment.IsSimple && segment.Parts[0].IsParameter)
                        {
                            var parameter = parent.GetNode<ParameterNode>() ?? parent.AddNode(new ParameterNode(depth));
                            if (parameter.Children.Count == 0)
                            {
                                parameter.AddNode(new SequenceNode(depth + 1));
                            }

                            parent = (SequenceNode)parameter.Children[0];
                        }
                        else
                        {
                            throw new InvalidOperationException("Not implemented!");
                        }
                    }

                    parent.AddNode(new AcceptNode(entry.Endpoint));
                }

                var builder = new InstructionBuilder();
                for (var i = 0; i < roots.Count; i++)
                {
                    roots[i].Lower(builder);
                }

                var (instructions, endpoints, tables) = builder;
                return new InstructionMatcher(instructions, endpoints, tables);
            }

            private OrderNode GetOrCreateRootNode(List<OrderNode> roots, int order)
            {
                OrderNode root = null;
                for (var j = 0; j < roots.Count; j++)
                {
                    if (roots[j].Order == order)
                    {
                        root = roots[j];
                        break;
                    }
                }

                if (root == null)
                {
                    // Nodes are guaranteed to be in order because the entries are in order.
                    root = new OrderNode(order);
                    roots.Add(root);
                }

                return root;
            }
        }
    }
}
