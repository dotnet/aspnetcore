// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Authorization;

internal sealed class AuthorizationMetrics
{
    public const string MeterName = "Microsoft.AspNetCore.Authorization";

    private readonly Meter _meter;
    private readonly Counter<long> _authorizedRequestCount;

    public AuthorizationMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        _authorizedRequestCount = _meter.CreateCounter<long>(
            "aspnetcore.authorization.attempts",
            unit: "{request}",
            description: "The total number of requests for which authorization was attempted.");
    }

    public void AuthorizedRequestCompleted(ClaimsPrincipal user, string? policyName, AuthorizationResult? result, Exception? exception)
    {
        if (_authorizedRequestCount.Enabled)
        {
            AuthorizedRequestCompletedCore(user, policyName, result, exception);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void AuthorizedRequestCompletedCore(ClaimsPrincipal user, string? policyName, AuthorizationResult? result, Exception? exception)
    {
        var tags = new TagList([
            new("user.is_authenticated", user.Identity?.IsAuthenticated ?? false)
        ]);

        if (policyName is not null)
        {
            tags.Add("aspnetcore.authorization.policy", policyName);
        }

        if (result is not null)
        {
            var resultTagValue = result.Succeeded ? "success" : "failure";
            tags.Add("aspnetcore.authorization.result", resultTagValue);
        }

        if (exception is not null)
        {
            tags.Add("error.type", exception.GetType().FullName);
        }

        _authorizedRequestCount.Add(1, tags);
    }
}
