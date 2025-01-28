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

    public void AuthenticatedRequestCompleted(string? scheme, AuthenticateResult? result, Exception? exception, long startTimestamp, long currentTimestamp)
    {
        if (_authenticatedRequestDuration.Enabled)
        {
            AuthenticatedRequestCompletedCore(scheme, result, exception, startTimestamp, currentTimestamp);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void AuthenticatedRequestCompletedCore(string? scheme, AuthenticateResult? result, Exception? exception, long startTimestamp, long currentTimestamp)
    {
        var tags = new TagList();

        if (scheme is not null)
        {
            AddSchemeTag(ref tags, scheme);
        }

        if (result is not null)
        {
            tags.Add("aspnetcore.authentication.result", result switch
            {
                { None: true } => "none",
                { Succeeded: true } => "success",
                { Failure: not null } => "failure",
                _ => "_OTHER", // _OTHER is commonly used fallback for an extra or unexpected value. Shouldn't reach here.
            });
        }

        if (exception is not null)
        {
            AddErrorTag(ref tags, exception);
        }

        var duration = Stopwatch.GetElapsedTime(startTimestamp, currentTimestamp);
        _authenticatedRequestDuration.Record(duration.TotalSeconds, tags);
    }

    public void ChallengeCompleted(string? scheme, Exception? exception)
    {
        if (_challengeCount.Enabled)
        {
            ChallengeCompletedCore(scheme, exception);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ChallengeCompletedCore(string? scheme, Exception? exception)
    {
        var tags = new TagList();

        if (scheme is not null)
        {
            AddSchemeTag(ref tags, scheme);
        }

        if (exception is not null)
        {
            AddErrorTag(ref tags, exception);
        }

        _challengeCount.Add(1, tags);
    }

    public void ForbidCompleted(string? scheme, Exception? exception)
    {
        if (_forbidCount.Enabled)
        {
            ForbidCompletedCore(scheme, exception);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ForbidCompletedCore(string? scheme, Exception? exception)
    {
        var tags = new TagList();

        if (scheme is not null)
        {
            AddSchemeTag(ref tags, scheme);
        }

        if (exception is not null)
        {
            AddErrorTag(ref tags, exception);
        }

        _forbidCount.Add(1, tags);
    }

    public void SignInCompleted(string? scheme, Exception? exception)
    {
        if (_signInCount.Enabled)
        {
            SignInCompletedCore(scheme, exception);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void SignInCompletedCore(string? scheme, Exception? exception)
    {
        var tags = new TagList();

        if (scheme is not null)
        {
            AddSchemeTag(ref tags, scheme);
        }

        if (exception is not null)
        {
            AddErrorTag(ref tags, exception);
        }

        _signInCount.Add(1, tags);
    }

    public void SignOutCompleted(string? scheme, Exception? exception)
    {
        if (_signOutCount.Enabled)
        {
            SignOutCompletedCore(scheme, exception);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void SignOutCompletedCore(string? scheme, Exception? exception)
    {
        var tags = new TagList();

        if (scheme is not null)
        {
            AddSchemeTag(ref tags, scheme);
        }

        if (exception is not null)
        {
            AddErrorTag(ref tags, exception);
        }

        _signOutCount.Add(1, tags);
    }

    private static void AddSchemeTag(ref TagList tags, string scheme)
    {
        tags.Add("aspnetcore.authentication.scheme", scheme);
    }

    private static void AddErrorTag(ref TagList tags, Exception exception)
    {
        tags.Add("error.type", exception.GetType().FullName);
    }
}
