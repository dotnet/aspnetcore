// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.AspNetCore.Routing.ShortCircuit;

namespace Microsoft.AspNetCore.Routing.TestObjects;

internal class TestMatcherFactory : MatcherFactory
{
    private readonly bool _isHandled;
    private readonly Action<HttpContext> _setEndpointCallback;

    public TestMatcherFactory(bool isHandled, Action<HttpContext> setEndpointCallback = null)
    {
        _isHandled = isHandled;
        _setEndpointCallback = setEndpointCallback;
    }

    public override Matcher CreateMatcher(EndpointDataSource dataSource)
    {
        return new TestMatcher(_isHandled, _setEndpointCallback);
    }
}

internal class ShortCircuitMatcherFactory : MatcherFactory
{
    private readonly int? _statusCode;
    private readonly bool _hasAuthMetadata;
    private readonly bool _hasCorsMetadata;

    public ShortCircuitMatcherFactory(int? statusCode, bool hasAuthMetadata, bool hasCorsMetadata)
    {
        _statusCode = statusCode;
        _hasAuthMetadata = hasAuthMetadata;
        _hasCorsMetadata = hasCorsMetadata;
    }

    public override Matcher CreateMatcher(EndpointDataSource dataSource)
    {
        return new ShortCircuitMatcher(_statusCode, _hasAuthMetadata, _hasCorsMetadata);
    }
}

internal class ShortCircuitMatcher : Matcher
{
    private readonly int? _statusCode;
    private readonly bool _hasAuthMetadata;
    private readonly bool _hasCorsMetadata;

    public ShortCircuitMatcher(int? statusCode, bool hasAuthMetadata, bool hasCorsMetadata)
    {
        _statusCode = statusCode;
        _hasAuthMetadata = hasAuthMetadata;
        _hasCorsMetadata = hasCorsMetadata;
    }

    public override Task MatchAsync(HttpContext httpContext)
    {
        var metadataList = new List<object>
        {
            new ShortCircuitMetadata(_statusCode)
        };

        if (_hasAuthMetadata)
        {
            metadataList.Add(new AuthorizeAttribute());
        }

        if (_hasCorsMetadata)
        {
            metadataList.Add(new CorsMetadata());
        }

        var metadata = new EndpointMetadataCollection(metadataList);
        httpContext.SetEndpoint(new Endpoint(TestConstants.ShortCircuitRequestDelegate, metadata, "Short Circuit Endpoint"));

        return Task.CompletedTask;
    }
}

internal class CorsMetadata : ICorsMetadata
{
}
