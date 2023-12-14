// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Matching;

namespace Microsoft.AspNetCore.Routing.TestObjects;

internal class TestMatcher : Matcher
{
    private readonly bool _isHandled;
    private readonly Action<HttpContext> _setEndpointCallback;

    public TestMatcher(bool isHandled, Action<HttpContext> setEndpointCallback = null)
    {
        _isHandled = isHandled;

        setEndpointCallback ??= static c =>
            {
                c.Request.RouteValues = new RouteValueDictionary(new { controller = "Home", action = "Index" });
                c.SetEndpoint(new Endpoint(TestConstants.EmptyRequestDelegate, EndpointMetadataCollection.Empty, "Test endpoint"));
            };
        _setEndpointCallback = setEndpointCallback;
    }

    public override Task MatchAsync(HttpContext httpContext)
    {
        if (_isHandled)
        {
            _setEndpointCallback(httpContext);
        }

        return Task.CompletedTask;
    }
}
