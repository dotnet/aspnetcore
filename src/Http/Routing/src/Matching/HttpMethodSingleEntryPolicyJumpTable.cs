// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matching;

internal sealed class HttpMethodSingleEntryPolicyJumpTable : PolicyJumpTable
{
    private readonly int _exitDestination;
    private readonly string _method;
    private readonly int _destination;
    private readonly int _corsPreflightExitDestination;
    private readonly int _corsPreflightDestination;

    private readonly bool _supportsCorsPreflight;

    public HttpMethodSingleEntryPolicyJumpTable(
        int exitDestination,
        string method,
        int destination,
        bool supportsCorsPreflight,
        int corsPreflightExitDestination,
        int corsPreflightDestination)
    {
        _exitDestination = exitDestination;
        _method = method;
        _destination = destination;
        _supportsCorsPreflight = supportsCorsPreflight;
        _corsPreflightExitDestination = corsPreflightExitDestination;
        _corsPreflightDestination = corsPreflightDestination;
    }

    public override int GetDestination(HttpContext httpContext)
    {
        var httpMethod = httpContext.Request.Method;
        if (_supportsCorsPreflight && HttpMethodMatcherPolicy.IsCorsPreflightRequest(httpContext, httpMethod, out var accessControlRequestMethod))
        {
            return HttpMethods.Equals(accessControlRequestMethod.ToString(), _method) ? _corsPreflightDestination : _corsPreflightExitDestination;
        }

        return HttpMethods.Equals(httpMethod, _method) ? _destination : _exitDestination;
    }
}
