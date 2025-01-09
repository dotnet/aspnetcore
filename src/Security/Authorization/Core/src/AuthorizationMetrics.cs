// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Runtime.CompilerServices;
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
            "aspnetcore.authorization.requests",
            unit: "{request}",
            description: "The total number of requests for which authorization was attempted");
    }

    public void AuthorizedRequestSucceeded(string? policyName, AuthorizationResult result)
    {
        if (_authorizedRequestCount.Enabled)
        {
            AuthorizedRequestSucceededCore(policyName, result);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void AuthorizedRequestSucceededCore(string? policyName, AuthorizationResult result)
    {
        var tags = new TagList();
        TryAddPolicyTag(ref tags, policyName);

        var resultTagValue = result.Succeeded ? "success" : "failure";
        tags.Add("aspnetcore.authorization.result", resultTagValue);

        _authorizedRequestCount.Add(1, tags);
    }

    public void AuthorizedRequestFailed(string? policyName, Exception exception)
    {
        if (_authorizedRequestCount.Enabled)
        {
            AuthorizedRequestFailedCore(policyName, exception);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void AuthorizedRequestFailedCore(string? policyName, Exception exception)
    {
        var tags = new TagList();
        TryAddPolicyTag(ref tags, policyName);

        tags.Add("error.type", exception.GetType().FullName);

        _authorizedRequestCount.Add(1, tags);
    }

    private static void TryAddPolicyTag(ref TagList tags, string? policyName)
    {
        if (policyName is not null)
        {
            tags.Add("aspnetcore.authorization.policy", policyName);
        }
    }
}
