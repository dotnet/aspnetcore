// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    internal class MinimalMatcher : Matcher
    {
        public static MatcherBuilder CreateBuilder() => new Builder();

        private readonly (string pattern, Endpoint endpoint)[] _entries;

        private MinimalMatcher((string pattern, Endpoint endpoint)[] entries)
        {
            _entries = entries;
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

            var path = httpContext.Request.Path.Value;
            for (var i = 0; i < _entries.Length; i++)
            {
                if (string.Equals(_entries[i].pattern, path, StringComparison.OrdinalIgnoreCase))
                {
                    feature.Endpoint = _entries[i].endpoint;
                    return Task.CompletedTask;
                }
            }

            return Task.CompletedTask;
        }

        private class Builder : MatcherBuilder
        {
            private List<(string pattern, Endpoint endpoint)> _entries = new List<(string pattern, Endpoint endpoint)>();

            public override void AddEntry(string pattern, MatcherEndpoint endpoint)
            {
                _entries.Add((pattern, endpoint)); 
            }

            public override Matcher Build()
            {
                return new MinimalMatcher(_entries.ToArray());
            }
        }
    }
}
