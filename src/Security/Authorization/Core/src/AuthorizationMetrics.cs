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
    private readonly Counter<long> _authorizedCount;

    public AuthorizationMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        _authorizedCount = _meter.CreateCounter<long>(
            "aspnetcore.authorization.attempts",
            unit: "{attempt}",
            description: "The total number of authorization attempts.");
    }

    public void AuthorizeAttemptCompleted(ClaimsPrincipal user, string? policyName, AuthorizationResult? result, Exception? exception)
    {
        if (_authorizedCount.Enabled)
        {
            AuthorizeAttemptCore(user, policyName, result, exception);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void AuthorizeAttemptCore(ClaimsPrincipal user, string? policyName, AuthorizationResult? result, Exception? exception)
    {
        var tags = new TagList([
            new("aspnetcore.user.is_authenticated", user.Identity?.IsAuthenticated ?? false)
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

        _authorizedCount.Add(1, tags);
    }
}
