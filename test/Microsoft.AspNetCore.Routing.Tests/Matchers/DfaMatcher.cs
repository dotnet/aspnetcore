// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    internal class DfaMatcher : Matcher
    {
        private readonly State[] _states;

        public DfaMatcher(State[] states)
        {
            _states = states;
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

            var states = _states;
            var current = 0;

            var path = httpContext.Request.Path.Value;

            // This section tokenizes the path by marking the sequence of slashes, and their 
            // position in the string. The consuming code uses the sequence and the count of
            // slashes to deduce the length of each segment.
            //
            // If there is residue (text after last slash) then the length of the segment will
            // computed based on the string length.
            var buffer = stackalloc Segment[32];
            var segment = 0;
            var start = 1; // PathString guarantees a leading /
            var end = 0;
            var length = 0;
            while ((end = path.IndexOf('/', start)) >= 0 && segment < 32)
            {
                length = end - start;
                buffer[segment++] = new Segment() { Start = start, Length = length, };
                current = states[current].Transitions.GetDestination(path, start, length);

                start = end + 1; // resume search after the current character
            }

            // Residue
            length = path.Length - start;
            if (length > 0)
            {
                buffer[segment++] = new Segment() { Start = start, Length = length, };
                current = states[current].Transitions.GetDestination(path, start, length);
            }

            var matches = new List<(Endpoint, RouteValueDictionary)>();

            var candidates = states[current].Matches;
            for (var i = 0; i < candidates.Length; i++)
            {
                var values = new RouteValueDictionary();
                var parameters = candidates[i].Parameters;
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

                matches.Add((candidates[i].Endpoint, values));

                notmatch: ;
            }
            
            feature.Endpoint = matches.Count == 0 ? null : matches[0].Item1;
            feature.Values = matches.Count == 0 ? null : matches[0].Item2;

            return Task.CompletedTask;
        }

        public struct Segment
        {
            public int Start;
            public int Length;
        }

        public struct State
        {
            public bool IsAccepting;
            public Candidate[] Matches;
            public JumpTable Transitions;
        }

        public struct Candidate
        {
            public Endpoint Endpoint;
            public string[] Parameters;
        }

        public abstract class JumpTable
        {
            public abstract int GetDestination(string text, int start, int length);
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
    }
}
