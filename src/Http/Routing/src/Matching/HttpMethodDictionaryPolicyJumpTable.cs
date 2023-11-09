// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matching;

internal sealed class HttpMethodDictionaryPolicyJumpTable : PolicyJumpTable
{
    private readonly int _exitDestination;
    private readonly KnownHttpMethodsJumpTable _knownHttpMethodDestinations;
    private readonly Dictionary<string, int>? _destinations;
    private readonly int _corsPreflightExitDestination;
    private readonly Dictionary<string, int>? _corsPreflightDestinations;
    private readonly KnownHttpMethodsJumpTable _knownCorsHttpMethodDestinations;
    private readonly bool _supportsCorsPreflight;

    public HttpMethodDictionaryPolicyJumpTable(
        int exitDestination,
        KnownHttpMethodsJumpTable knownDestinations,
        Dictionary<string, int>? destinations,
        bool hasCorsDestinations,
        int corsPreflightExitDestination,
        KnownHttpMethodsJumpTable knownCorsPreflightDestinations,
        Dictionary<string, int>? corsPreflightDestinations)
    {
        _exitDestination = exitDestination;
        _knownHttpMethodDestinations = knownDestinations;
        _knownCorsHttpMethodDestinations = knownCorsPreflightDestinations;
        _destinations = destinations;
        _corsPreflightExitDestination = corsPreflightExitDestination;
        _corsPreflightDestinations = corsPreflightDestinations;
        _supportsCorsPreflight = hasCorsDestinations;
    }

    public override int GetDestination(HttpContext httpContext)
    {
        int destination;
        var httpMethod = httpContext.Request.Method;
        if (_supportsCorsPreflight && HttpMethodMatcherPolicy.IsCorsPreflightRequest(httpContext, httpMethod, out var accessControlRequestMethod))
        {
            var corsHttpMethod = accessControlRequestMethod.ToString();
            if (_knownCorsHttpMethodDestinations.TryGetKnownValue(corsHttpMethod, out destination))
            {
                return destination;
            }
            return _corsPreflightDestinations!.TryGetValue(corsHttpMethod, out destination)
                ? destination
                : _corsPreflightExitDestination;
        }
        if (_knownHttpMethodDestinations.TryGetKnownValue(httpMethod, out destination))
        {
            return destination;
        }

        return _destinations != null &&
            _destinations.TryGetValue(httpMethod, out destination) ? destination : _exitDestination;
    }
}
