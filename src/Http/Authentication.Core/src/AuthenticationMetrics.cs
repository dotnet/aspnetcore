// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Microsoft.AspNetCore.Authentication;

internal sealed class AuthenticationMetrics : IDisposable
{
    public const string MeterName = "Microsoft.AspNetCore.Authentication";

    private readonly Meter _meter;
    private readonly Counter<long> _authenticatedRequestCount;
    private readonly Counter<long> _challengeCount;
    private readonly Counter<long> _forbidCount;
    private readonly Counter<long> _signInCount;
    private readonly Counter<long> _signOutCount;

    public AuthenticationMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        _authenticatedRequestCount = _meter.CreateCounter<long>(
            "aspnetcore.authentication.authenticated_requests",
            unit: "{request}",
            description: "The total number of authenticated requests");

        _challengeCount = _meter.CreateCounter<long>(
            "aspnetcore.authentication.challenges",
            unit: "{request}",
            description: "The total number of times a scheme is challenged");

        _forbidCount = _meter.CreateCounter<long>(
            "aspnetcore.authentication.forbids",
            unit: "{request}",
            description: "The total number of times an authenticated user attempts to access a resource they are not permitted to access");

        _signInCount = _meter.CreateCounter<long>(
            "aspnetcore.authentication.sign_ins",
            unit: "{request}",
            description: "The total number of times a principal is signed in");

        _signOutCount = _meter.CreateCounter<long>(
            "aspnetcore.authentication.sign_outs",
            unit: "{request}",
            description: "The total number of times a scheme is signed out");
    }

    public void AuthenticatedRequest(string scheme, AuthenticateResult result)
    {
        if (_authenticatedRequestCount.Enabled)
        {
            var resultTagValue = result switch
            {
                { Succeeded: true } => "success",
                { Failure: not null } => "failure",
                { None: true } => "none",
                _ => throw new UnreachableException($"Could not determine the result state of the {nameof(AuthenticateResult)}"),
            };

            _authenticatedRequestCount.Add(1, [
                new("aspnetcore.authentication.scheme", scheme),
                new("aspnetcore.authentication.result", resultTagValue),
            ]);
        }
    }

    public void Challenge(string scheme)
    {
        if (_challengeCount.Enabled)
        {
            _challengeCount.Add(1, [new("aspnetcore.authentication.scheme", scheme)]);
        }
    }

    public void Forbid(string scheme)
    {
        if (_forbidCount.Enabled)
        {
            _forbidCount.Add(1, [new("aspnetcore.authentication.scheme", scheme)]);
        }
    }

    public void SignIn(string scheme)
    {
        if (_signInCount.Enabled)
        {
            _signInCount.Add(1, [new("aspnetcore.authentication.scheme", scheme)]);
        }
    }

    public void SignOut(string scheme)
    {
        if (_signOutCount.Enabled)
        {
            _signOutCount.Add(1, [new("aspnetcore.authentication.scheme", scheme)]);
        }
    }

    public void Dispose()
    {
        _meter.Dispose();
    }
}
