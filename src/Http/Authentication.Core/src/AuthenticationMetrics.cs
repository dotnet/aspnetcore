// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication;

internal sealed class AuthenticationMetrics
{
    public const string MeterName = "Microsoft.AspNetCore.Authentication";

    private readonly Meter _meter;
    private readonly Histogram<double> _authenticatedRequestDuration;
    private readonly Counter<long> _challengeCount;
    private readonly Counter<long> _forbidCount;
    private readonly Counter<long> _signInCount;
    private readonly Counter<long> _signOutCount;

    public AuthenticationMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        _authenticatedRequestDuration = _meter.CreateHistogram<double>(
            "aspnetcore.authentication.authenticate.duration",
            unit: "s",
            description: "The authentication duration for a request.",
            advice: new() { HistogramBucketBoundaries = MetricsConstants.ShortSecondsBucketBoundaries });

        _challengeCount = _meter.CreateCounter<long>(
            "aspnetcore.authentication.challenges",
            unit: "{request}",
            description: "The total number of times a scheme is challenged.");

        _forbidCount = _meter.CreateCounter<long>(
            "aspnetcore.authentication.forbids",
            unit: "{request}",
            description: "The total number of times an authenticated user attempts to access a resource they are not permitted to access.");

        _signInCount = _meter.CreateCounter<long>(
            "aspnetcore.authentication.sign_ins",
            unit: "{request}",
            description: "The total number of times a principal is signed in.");

        _signOutCount = _meter.CreateCounter<long>(
            "aspnetcore.authentication.sign_outs",
            unit: "{request}",
            description: "The total number of times a scheme is signed out.");
    }

    public void AuthenticatedRequestSucceeded(string? scheme, AuthenticateResult result, long startTimestamp, long currentTimestamp)
    {
        if (_authenticatedRequestDuration.Enabled)
        {
            AuthenticatedRequestSucceededCore(scheme, result, startTimestamp, currentTimestamp);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void AuthenticatedRequestSucceededCore(string? scheme, AuthenticateResult result, long startTimestamp, long currentTimestamp)
    {
        var tags = new TagList();
        TryAddSchemeTag(ref tags, scheme);

        var resultTagValue = result switch
        {
            { None: true } => "none",
            { Succeeded: true } => "success",
            { Failure: not null } => "failure",
            _ => "_OTHER", // _OTHER is commonly used fallback for an extra or unexpected value. Shouldn't reach here.
        };
        tags.Add("aspnetcore.authentication.result", resultTagValue);

        var duration = Stopwatch.GetElapsedTime(startTimestamp, currentTimestamp);
        _authenticatedRequestDuration.Record(duration.TotalSeconds, tags);
    }

    public void AuthenticatedRequestFailed(string? scheme, Exception exception, long startTimestamp, long currentTimestamp)
    {
        if (_authenticatedRequestDuration.Enabled)
        {
            AuthenticatedRequestFailedCore(scheme, exception, startTimestamp, currentTimestamp);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void AuthenticatedRequestFailedCore(string? scheme, Exception exception, long startTimestamp, long currentTimestamp)
    {
        var tags = new TagList();
        TryAddSchemeTag(ref tags, scheme);
        AddErrorTag(ref tags, exception);

        var duration = Stopwatch.GetElapsedTime(startTimestamp, currentTimestamp);
        _authenticatedRequestDuration.Record(duration.TotalSeconds, tags);
    }

    public void ChallengeSucceeded(string? scheme)
    {
        if (_challengeCount.Enabled)
        {
            ChallengeSucceededCore(scheme);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ChallengeSucceededCore(string? scheme)
    {
        var tags = new TagList();
        TryAddSchemeTag(ref tags, scheme);

        _challengeCount.Add(1, tags);
    }

    public void ChallengeFailed(string? scheme, Exception exception)
    {
        if (_challengeCount.Enabled)
        {
            ChallengeFailedCore(scheme, exception);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ChallengeFailedCore(string? scheme, Exception exception)
    {
        var tags = new TagList();
        TryAddSchemeTag(ref tags, scheme);
        AddErrorTag(ref tags, exception);

        _challengeCount.Add(1, tags);
    }

    public void ForbidSucceeded(string? scheme)
    {
        if (_forbidCount.Enabled)
        {
            ForbidSucceededCore(scheme);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ForbidSucceededCore(string? scheme)
    {
        var tags = new TagList();
        TryAddSchemeTag(ref tags, scheme);
        _forbidCount.Add(1, tags);
    }

    public void ForbidFailed(string? scheme, Exception exception)
    {
        if (_forbidCount.Enabled)
        {
            ForbidFailedCore(scheme, exception);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ForbidFailedCore(string? scheme, Exception exception)
    {
        var tags = new TagList();
        TryAddSchemeTag(ref tags, scheme);
        AddErrorTag(ref tags, exception);

        _forbidCount.Add(1, tags);
    }

    public void SignInSucceeded(string? scheme)
    {
        if (_signInCount.Enabled)
        {
            SignInSucceededCore(scheme);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void SignInSucceededCore(string? scheme)
    {
        var tags = new TagList();
        TryAddSchemeTag(ref tags, scheme);
        _signInCount.Add(1, tags);
    }

    public void SignInFailed(string? scheme, Exception exception)
    {
        if (_signInCount.Enabled)
        {
            SignInFailedCore(scheme, exception);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void SignInFailedCore(string? scheme, Exception exception)
    {
        var tags = new TagList();
        TryAddSchemeTag(ref tags, scheme);
        AddErrorTag(ref tags, exception);

        _signInCount.Add(1, tags);
    }

    public void SignOutSucceeded(string? scheme)
    {
        if (_signOutCount.Enabled)
        {
            SignOutSucceededCore(scheme);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void SignOutSucceededCore(string? scheme)
    {
        var tags = new TagList();
        TryAddSchemeTag(ref tags, scheme);
        _signOutCount.Add(1, tags);
    }

    public void SignOutFailed(string? scheme, Exception exception)
    {
        if (_signOutCount.Enabled)
        {
            SignOutFailedCore(scheme, exception);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void SignOutFailedCore(string? scheme, Exception exception)
    {
        var tags = new TagList();
        TryAddSchemeTag(ref tags, scheme);
        AddErrorTag(ref tags, exception);

        _signOutCount.Add(1, tags);
    }

    private static void TryAddSchemeTag(ref TagList tags, string? scheme)
    {
        if (scheme is not null)
        {
            tags.Add("aspnetcore.authentication.scheme", scheme);
        }
    }

    private static void AddErrorTag(ref TagList tags, Exception exception)
    {
        tags.Add("error.type", exception.GetType().FullName);
    }
}
