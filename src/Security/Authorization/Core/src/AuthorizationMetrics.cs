// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Authorization;

internal sealed class AuthorizationMetrics : IDisposable
{
    public const string MeterName = "Microsoft.AspNetCore.Authorization";

    private readonly Meter _meter;
    private readonly Counter<long> _authorizeCount;

    public AuthorizationMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        _authorizeCount = _meter.CreateCounter<long>(
            "aspnetcore.authorization.authorized_requests",
            unit: "{request}",
            description: "The total number of requests requiring authorization");
    }

    public void AuthorizedRequest(string? policyName, AuthorizationResult result)
    {
        if (_authorizeCount.Enabled)
        {
            var resultTagValue = result.Succeeded ? "success" : "failure";

            _authorizeCount.Add(1, [
                new("aspnetcore.authorization.policy", policyName),
                new("aspnetcore.authorization.result", resultTagValue),
            ]);
        }
    }

    public void Dispose()
    {
        _meter.Dispose();
    }
}
