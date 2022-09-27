// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matching;

internal sealed class HttpMethodDictionaryPolicyJumpTable : PolicyJumpTable
{
    private readonly int _exitDestination;
    private readonly Dictionary<string, int>? _destinations;
    private readonly int _corsPreflightExitDestination;
    private readonly Dictionary<string, int>? _corsPreflightDestinations;

    private readonly bool _supportsCorsPreflight;

    public HttpMethodDictionaryPolicyJumpTable(
        int exitDestination,
        Dictionary<string, int>? destinations,
        int corsPreflightExitDestination,
        Dictionary<string, int>? corsPreflightDestinations)
    {
        _exitDestination = exitDestination;
        _destinations = destinations;
        _corsPreflightExitDestination = corsPreflightExitDestination;
        _corsPreflightDestinations = corsPreflightDestinations;

        _supportsCorsPreflight = _corsPreflightDestinations != null && _corsPreflightDestinations.Count > 0;
    }

    public override int GetDestination(HttpContext httpContext)
    {
        int destination;

        var httpMethod = httpContext.Request.Method;
        if (_supportsCorsPreflight && HttpMethodMatcherPolicy.IsCorsPreflightRequest(httpContext, httpMethod, out var accessControlRequestMethod))
        {
            return _corsPreflightDestinations!.TryGetValue(accessControlRequestMethod.ToString(), out destination)
                ? destination
                : _corsPreflightExitDestination;
        }

        return _destinations != null &&
            _destinations.TryGetValue(httpMethod, out destination) ? destination : _exitDestination;
    }
}
