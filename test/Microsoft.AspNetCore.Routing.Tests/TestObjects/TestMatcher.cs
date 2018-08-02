// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Matching;

namespace Microsoft.AspNetCore.Routing.TestObjects
{
    internal class TestMatcher : Matcher
    {
        private readonly bool _isHandled;

        public TestMatcher(bool isHandled)
        {
            _isHandled = isHandled;
        }

        public override Task MatchAsync(HttpContext httpContext, IEndpointFeature feature)
        {
            if (_isHandled)
            {
                feature.Values = new RouteValueDictionary(new { controller = "Home", action = "Index" });
                feature.Endpoint = new TestEndpoint(EndpointMetadataCollection.Empty, "Test endpoint");
            }

            return Task.CompletedTask;
        }
    }
}
