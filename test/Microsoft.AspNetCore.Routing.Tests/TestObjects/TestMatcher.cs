// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Matchers;

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
                feature.Endpoint = new TestEndpoint(EndpointMetadataCollection.Empty, "Test endpoint", address: null);
            }

            return Task.CompletedTask;
        }
    }
}
