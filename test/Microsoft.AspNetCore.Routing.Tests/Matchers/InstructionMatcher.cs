// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    internal class InstructionMatcher : Matcher
    {
        private State _state;

        public InstructionMatcher(Instruction[] instructions, Candidate[] candidates, JumpTable[] tables)
        {
            _state = new State()
            {
                Instructions = instructions,
                Candidates = candidates,
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

            // This section tokenizes the path by marking the sequence of slashes, and their 
            // position in the string. The consuming code uses the sequence and the count of
            // slashes to deduce the length of each segment.
            //
            // If there is residue (text after last slash) then the length of the segment will
            // computed based on the string length.
            var buffer = stackalloc Segment[32];
            var count = 0;
            var start = 1; // PathString guarantees a leading /
            var end = 0;
            while ((end = path.IndexOf('/', start)) >= 0 && count < 32)
            {
                buffer[count++] = new Segment() { Start = start, Length = end - start, };
                start = end + 1; // resume search after the current character
            }

            // Residue
            if (start < path.Length)
            {
                buffer[count++] = new Segment() { Start = start, Length = path.Length - start, };
            }

            var i = 0;
            Candidate match = default(Candidate);
            while (i < state.Instructions.Length)
            {
                var instruction = state.Instructions[i];
                switch (instruction.Code)
                {
                    case InstructionCode.Accept:
                        {
                            if (count == instruction.Depth)
                            {
                                match = state.Candidates[instruction.Payload];
                            }
                            i++;
                            break;
                        }
                    case InstructionCode.Branch:
                        {
                            var table = state.Tables[instruction.Payload];
                            i = table.GetDestination(buffer, count, path);
                            break;
                        }
                    case InstructionCode.Jump:
                        {
                            i = instruction.Payload;
                            break;
                        }
                }
            }

            if (match.Endpoint != null)
            { 
                var values = new RouteValueDictionary();
                var parameters = match.Parameters;
                if (parameters != null)
                {
                    for (var j = 0; j < parameters.Length; j++)
                    {
                        var parameter = parameters[j];
                        if (parameter != null && buffer[j].Length == 0)
                        {
                            goto notmatch;
                        }
                        else if (parameter != null)
                        {
                            var value = path.Substring(buffer[j].Start, buffer[j].Length);
                            values.Add(parameter, value);
                        }
                    }
                }

                feature.Endpoint = match.Endpoint;
                feature.Values = values;

                notmatch:;
            }



            return Task.CompletedTask;
        }

        public struct Segment
        {
            public int Start;
            public int Length;
        }

        public struct Candidate
        {
            public Endpoint Endpoint;
            public string[] Parameters;
        }

        public class State
        {
            public Candidate[] Candidates;
            public Instruction[] Instructions;
            public JumpTable[] Tables;
        }

        [DebuggerDisplay("{ToDebugString(),nq}")]
        [StructLayout(LayoutKind.Explicit)]
        public struct Instruction
        {
            [FieldOffset(0)]
            public byte Depth;

            [FieldOffset(3)]
            public InstructionCode Code;

            [FieldOffset(4)]
            public int Payload;

            private string ToDebugString()
            {
                return $"{Code}: {Payload}";
            }
        }

        public enum InstructionCode : byte
        {
            Accept,
            Branch,
            Jump,
            Pop, // Only used during the instruction builder phase
        }

        public abstract class JumpTable
        {
            public unsafe abstract int GetDestination(Segment* segments, int count, string path);
        }

        public class JumpTableBuilder
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

        public class SimpleJumpTable : JumpTable
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

            public unsafe override int GetDestination(Segment* segments, int count, string path)
            {
                if (_depth == count)
                {
                    return _exit;
                }

                var start  = segments[_depth].Start;
                var length = segments[_depth].Length;

                for (var i = 0; i < _entries.Length; i++)
                {
                    if (length == _entries[i].text.Length &&
                        string.Compare(
                        path,
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
    }
}
