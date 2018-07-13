// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
            var buffer = stackalloc PathSegment[32];
            var count = FastPathTokenizer.Tokenize(path, buffer, 32);
            
            for (var i = 0; i < count; i++)
            {
                current = states[current].Transitions.GetDestination(path, buffer[i]);
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
    }
}
