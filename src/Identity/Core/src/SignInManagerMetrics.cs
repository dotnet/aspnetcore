// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Identity;

internal sealed class SignInManagerMetrics : IDisposable
{
    public const string MeterName = "Microsoft.AspNetCore.Identity";

    public const string AuthenticateDurationName = "aspnetcore.identity.sign_in.authenticate.duration";
    public const string RememberedTwoFactorCounterName = "aspnetcore.identity.sign_in.two_factor_clients_remembered";
    public const string ForgottenTwoFactorCounterName = "aspnetcore.identity.sign_in.two_factor_clients_forgotten";
    public const string CheckPasswordAttemptsCounterName = "aspnetcore.identity.sign_in.check_password_attempts";
    public const string SignInsCounterName = "aspnetcore.identity.sign_in.sign_ins";
    public const string SignOutsCounterName = "aspnetcore.identity.sign_in.sign_outs";

    private readonly Meter _meter;
    private readonly Histogram<double> _authenticateDuration;
    private readonly Counter<long> _rememberTwoFactorClientCounter;
    private readonly Counter<long> _forgetTwoFactorCounter;
    private readonly Counter<long> _checkPasswordCounter;
    private readonly Counter<long> _signInsCounter;
    private readonly Counter<long> _signOutsCounter;

    public SignInManagerMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        _authenticateDuration = _meter.CreateHistogram<double>(
            AuthenticateDurationName,
            unit: "s",
            description: "The duration of authenticate attempts. The authenticate metrics is recorded by sign in methods such as PasswordSignInAsync and TwoFactorSignInAsync.",
            advice: new() { HistogramBucketBoundaries = MetricsConstants.ShortSecondsBucketBoundaries });

        _rememberTwoFactorClientCounter = _meter.CreateCounter<long>(
            RememberedTwoFactorCounterName,
            unit: "{client}",
            description: "The total number of two factor clients remembered.");

        _forgetTwoFactorCounter = _meter.CreateCounter<long>(
            ForgottenTwoFactorCounterName,
            unit: "{client}",
            description: "The total number of two factor clients forgotten.");

        _checkPasswordCounter = _meter.CreateCounter<long>(
            CheckPasswordAttemptsCounterName,
            unit: "{attempt}",
            description: "The total number of check password attempts. Checks that the account is in a state that can log in and that the password is valid using the UserManager.CheckPasswordAsync method.");

        _signInsCounter = _meter.CreateCounter<long>(
            SignInsCounterName,
            unit: "{sign_in}",
            description: "The total number of calls to sign in user principals.");

        _signOutsCounter = _meter.CreateCounter<long>(
            SignOutsCounterName,
            unit: "{sign_out}",
            description: "The total number of calls to sign out user principals.");
    }

    internal void CheckPasswordSignIn(string userType, SignInResult? result, Exception? exception = null)
    {
        if (!_checkPasswordCounter.Enabled)
        {
            return;
        }

        var tags = new TagList
        {
            { "aspnetcore.identity.user_type", userType },
        };
        AddSignInResult(ref tags, result);
        AddErrorTag(ref tags, exception);

        _checkPasswordCounter.Add(1, tags);
    }

    internal void AuthenticateSignIn(string userType, string authenticationScheme, SignInResult? result, SignInType signInType, bool? isPersistent, long startTimestamp, Exception? exception = null)
    {
        if (!_authenticateDuration.Enabled)
        {
            return;
        }

        var tags = new TagList
        {
            { "aspnetcore.identity.user_type", userType },
            { "aspnetcore.authentication.scheme", authenticationScheme },
            { "aspnetcore.identity.sign_in.type", GetSignInType(signInType) },
        };
        AddIsPersistent(ref tags, isPersistent);
        AddSignInResult(ref tags, result);
        AddErrorTag(ref tags, exception);

        var duration = ValueStopwatch.GetElapsedTime(startTimestamp, Stopwatch.GetTimestamp());
        _authenticateDuration.Record(duration.TotalSeconds, tags);
    }

    internal void SignInUserPrincipal(string userType, string authenticationScheme, bool? isPersistent, Exception? exception = null)
    {
        if (!_signInsCounter.Enabled)
        {
            return;
        }

        var tags = new TagList
        {
            { "aspnetcore.identity.user_type", userType },
            { "aspnetcore.authentication.scheme", authenticationScheme },
        };
        AddIsPersistent(ref tags, isPersistent);
        AddErrorTag(ref tags, exception);

        _signInsCounter.Add(1, tags);
    }

    internal void SignOutUserPrincipal(string userType, string authenticationScheme, Exception? exception = null)
    {
        if (!_signOutsCounter.Enabled)
        {
            return;
        }

        var tags = new TagList
        {
            { "aspnetcore.identity.user_type", userType },
            { "aspnetcore.authentication.scheme", authenticationScheme },
        };
        AddErrorTag(ref tags, exception);

        _signOutsCounter.Add(1, tags);
    }

    internal void RememberTwoFactorClient(string userType, string authenticationScheme, Exception? exception = null)
    {
        if (!_rememberTwoFactorClientCounter.Enabled)
        {
            return;
        }

        var tags = new TagList
        {
            { "aspnetcore.identity.user_type", userType },
            { "aspnetcore.authentication.scheme", authenticationScheme }
        };
        AddErrorTag(ref tags, exception);

        _rememberTwoFactorClientCounter.Add(1, tags);
    }

    internal void ForgetTwoFactorClient(string userType, string authenticationScheme, Exception? exception = null)
    {
        if (!_forgetTwoFactorCounter.Enabled)
        {
            return;
        }

        var tags = new TagList
        {
            { "aspnetcore.identity.user_type", userType },
            { "aspnetcore.authentication.scheme", authenticationScheme }
        };
        AddErrorTag(ref tags, exception);

        _forgetTwoFactorCounter.Add(1, tags);
    }

    public void Dispose()
    {
        _meter.Dispose();
    }

    private static void AddIsPersistent(ref TagList tags, bool? isPersistent)
    {
        if (isPersistent != null)
        {
            tags.Add("aspnetcore.authentication.is_persistent", isPersistent.Value);
        }
    }

    private static void AddSignInResult(ref TagList tags, SignInResult? result)
    {
        if (result != null)
        {
            tags.Add("aspnetcore.identity.sign_in.result", GetSignInResult(result));
        }
    }

    private static void AddErrorTag(ref TagList tags, Exception? exception)
    {
        if (exception != null)
        {
            tags.Add("error.type", exception.GetType().FullName!);
        }
    }

    private static string GetSignInType(SignInType signInType)
    {
        return signInType switch
        {
            SignInType.Password => "password",
            SignInType.TwoFactorRecoveryCode => "two_factor_recovery_code",
            SignInType.TwoFactorAuthenticator => "two_factor_authenticator",
            SignInType.TwoFactor => "two_factor",
            SignInType.External => "external",
            SignInType.Passkey => "passkey",
            _ => "_OTHER"
        };
    }

    private static string GetSignInResult(SignInResult result)
    {
        return result switch
        {
            { Succeeded: true } => "success",
            { IsLockedOut: true } => "locked_out",
            { IsNotAllowed: true } => "not_allowed",
            { RequiresTwoFactor: true } => "requires_two_factor",
            _ => "failure"
        };
    }
}
