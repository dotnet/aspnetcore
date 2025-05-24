// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Microsoft.AspNetCore.Identity;

internal sealed class SignInManagerMetrics : IDisposable
{
    public const string MeterName = "Microsoft.AspNetCore.Identity";

    private readonly Meter _meter;
    private readonly Counter<long> _signInCounter;
    private readonly Counter<long> _rememberTwoFactorClientCounter;
    private readonly Counter<long> _forgetTwoFactorClientCounter;
    private readonly Counter<long> _refreshSignInCounter;
    private readonly Counter<long> _checkPasswordSignInCounter;

    public SignInManagerMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        _signInCounter = _meter.CreateCounter<long>("aspnetcore.identity.sign_in", "count", "The number of sign-in attempts.");
        _rememberTwoFactorClientCounter = _meter.CreateCounter<long>("aspnetcore.identity.remember_two_factor_client", "count", "The number of remember two factor client attempts.");
        _forgetTwoFactorClientCounter = _meter.CreateCounter<long>("aspnetcore.identity.forget_two_factor_client", "count", "The number of forget two factor client attempts.");
        _refreshSignInCounter = _meter.CreateCounter<long>("aspnetcore.identity.refresh_sign_in", "count", "The number of refresh sign-in attempts.");
        _checkPasswordSignInCounter = _meter.CreateCounter<long>("aspnetcore.identity.check_password_sign_in", "count", "The number of check password attempts.");
    }

    internal void CheckPasswordSignIn(string userType, SignInResult? result, Exception? exception = null)
    {
        var tags = new TagList
        {
            { "aspnetcore.identity.user_type", userType },
        };
        AddSignInResult(ref tags, result);
        AddExceptionTags(ref tags, exception);

        _checkPasswordSignInCounter.Add(1, tags);
    }

    internal void SignIn(string userType, string authenticationScheme, SignInResult? result, SignInType signInType, bool isPersistent, Exception? exception = null)
    {
        var tags = new TagList
        {
            { "aspnetcore.identity.user_type", userType },
            { "aspnetcore.identity.authentication_scheme", authenticationScheme },
            { "aspnetcore.identity.sign_in.type", GetSignInType(signInType) },
            { "aspnetcore.identity.sign_in.is_persistent", isPersistent },
        };
        if (result != null)
        {
            tags.Add("aspnetcore.identity.sign_in.result", GetSignInResult(result));
        }
        AddExceptionTags(ref tags, exception);

        _signInCounter.Add(1, tags);
    }

    internal void RememberTwoFactorClient(string userType, string authenticationScheme, Exception? exception = null)
    {
        var tags = new TagList
        {
            { "aspnetcore.identity.user_type", userType },
            { "aspnetcore.identity.authentication_scheme", authenticationScheme }
        };
        AddExceptionTags(ref tags, exception);

        _rememberTwoFactorClientCounter.Add(1, tags);
    }

    internal void ForgetTwoFactorClient(string userType, string authenticationScheme, Exception? exception = null)
    {
        var tags = new TagList
        {
            { "aspnetcore.identity.user_type", userType },
            { "aspnetcore.identity.authentication_scheme", authenticationScheme }
        };
        AddExceptionTags(ref tags, exception);

        _forgetTwoFactorClientCounter.Add(1, tags);
    }

    internal void RefreshSignIn(string userType, string authenticationScheme, bool? success, bool? isPersistent, Exception? exception = null)
    {
        var tags = new TagList
        {
            { "aspnetcore.identity.user_type", userType },
            { "aspnetcore.identity.authentication_scheme", authenticationScheme },
            { "aspnetcore.identity.sign_in.result", success.GetValueOrDefault() ? "success" : "failure" }
        };
        if (isPersistent != null)
        {
            tags.Add("aspnetcore.identity.sign_in.is_persistent", isPersistent.Value);
        }
        AddExceptionTags(ref tags, exception);

        _refreshSignInCounter.Add(1, tags);
    }

    public void Dispose()
    {
        _meter.Dispose();
    }

    private static void AddSignInResult(ref TagList tags, SignInResult? result)
    {
        if (result != null)
        {
            tags.Add("aspnetcore.identity.sign_in.result", GetSignInResult(result));
        }
    }

    private static void AddExceptionTags(ref TagList tags, Exception? exception)
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
            _ => "_UNKNOWN"
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

internal enum SignInType
{
    Password,
    TwoFactorRecoveryCode,
    TwoFactorAuthenticator,
    TwoFactor,
    External
}
