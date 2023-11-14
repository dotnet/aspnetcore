// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matching;

internal sealed class HttpMethodDictionaryPolicyJumpTable : PolicyJumpTable
{
    private readonly HttpMethodDestinationsLookup _httpMethodDestinations;
    private readonly HttpMethodDestinationsLookup? _corsHttpMethodDestinations;

    public HttpMethodDictionaryPolicyJumpTable(
        HttpMethodDestinationsLookup destinations,
        HttpMethodDestinationsLookup? corsPreflightDestinations)
    {
        _httpMethodDestinations = destinations;
        _corsHttpMethodDestinations = corsPreflightDestinations;
    }

    public override int GetDestination(HttpContext httpContext)
    {
        var httpMethod = httpContext.Request.Method;
        if (_corsHttpMethodDestinations != null && HttpMethodMatcherPolicy.IsCorsPreflightRequest(httpContext, httpMethod, out var accessControlRequestMethod))
        {
            var corsHttpMethod = accessControlRequestMethod.ToString();
            return _corsHttpMethodDestinations.GetDestination(corsHttpMethod);
        }
        return _httpMethodDestinations.GetDestination(httpMethod);
    }
}
