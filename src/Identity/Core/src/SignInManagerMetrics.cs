// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Microsoft.AspNetCore.Identity;

internal sealed class SignInManagerMetrics : IDisposable
{
    public const string MeterName = "Microsoft.AspNetCore.Identity";

    public const string AuthenticateCounterName = "aspnetcore.identity.sign_in.authenticate";
    public const string RememberTwoFactorCounterName = "aspnetcore.identity.sign_in.remember_two_factor";
    public const string ForgetTwoFactorCounterName = "aspnetcore.identity.sign_in.forget_two_factor";
    public const string CheckPasswordCounterName = "aspnetcore.identity.sign_in.check_password";
    public const string SignInUserPrincipalCounterName = "aspnetcore.identity.sign_in.sign_in_principal";
    public const string SignOutUserPrincipalCounterName = "aspnetcore.identity.sign_in.sign_out_principal";

    private readonly Meter _meter;
    private readonly Counter<long> _authenticateCounter;
    private readonly Counter<long> _rememberTwoFactorClientCounter;
    private readonly Counter<long> _forgetTwoFactorCounter;
    private readonly Counter<long> _checkPasswordCounter;
    private readonly Counter<long> _signInUserPrincipalCounter;
    private readonly Counter<long> _signOutUserPrincipalCounter;

    public SignInManagerMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        _authenticateCounter = _meter.CreateCounter<long>(AuthenticateCounterName, "count", "The number of authenticate attempts. The authenticate counter is incremented by sign in methods such as PasswordSignInAsync and TwoFactorSignInAsync.");
        _rememberTwoFactorClientCounter = _meter.CreateCounter<long>(RememberTwoFactorCounterName, "count", "The number of two factor clients remembered.");
        _forgetTwoFactorCounter = _meter.CreateCounter<long>(ForgetTwoFactorCounterName, "count", "The number of two factor clients forgotten.");
        _checkPasswordCounter = _meter.CreateCounter<long>(CheckPasswordCounterName, "count", "The number of check password attempts. Checks that the account is in a state that can log in and that the password is valid using the UserManager.CheckPasswordAsync method.");
        _signInUserPrincipalCounter = _meter.CreateCounter<long>(SignInUserPrincipalCounterName, "count", "The number of user principals signed in.");
        _signOutUserPrincipalCounter = _meter.CreateCounter<long>(SignOutUserPrincipalCounterName, "count", "The number of user principals signed out.");
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
        AddExceptionTags(ref tags, exception);

        _checkPasswordCounter.Add(1, tags);
    }

    internal void AuthenticateSignIn(string userType, string authenticationScheme, SignInResult? result, SignInType signInType, bool? isPersistent, Exception? exception = null)
    {
        if (!_authenticateCounter.Enabled)
        {
            return;
        }

        var tags = new TagList
        {
            { "aspnetcore.identity.user_type", userType },
            { "aspnetcore.identity.authentication_scheme", authenticationScheme },
            { "aspnetcore.identity.sign_in.type", GetSignInType(signInType) },
        };
        if (isPersistent != null)
        {
            tags.Add("aspnetcore.identity.sign_in.is_persistent", isPersistent.Value);
        }
        AddSignInResult(ref tags, result);
        AddExceptionTags(ref tags, exception);

        _authenticateCounter.Add(1, tags);
    }

    internal void SignInUserPrincipal(string userType, string authenticationScheme, Exception? exception = null)
    {
        if (!_signInUserPrincipalCounter.Enabled)
        {
            return;
        }

        var tags = new TagList
        {
            { "aspnetcore.identity.user_type", userType },
            { "aspnetcore.identity.authentication_scheme", authenticationScheme },
        };
        AddExceptionTags(ref tags, exception);

        _signInUserPrincipalCounter.Add(1, tags);
    }

    internal void SignOutUserPrincipal(string userType, string authenticationScheme, Exception? exception = null)
    {
        if (!_signOutUserPrincipalCounter.Enabled)
        {
            return;
        }

        var tags = new TagList
        {
            { "aspnetcore.identity.user_type", userType },
            { "aspnetcore.identity.authentication_scheme", authenticationScheme },
        };
        AddExceptionTags(ref tags, exception);

        _signOutUserPrincipalCounter.Add(1, tags);
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
            { "aspnetcore.identity.authentication_scheme", authenticationScheme }
        };
        AddExceptionTags(ref tags, exception);

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
            { "aspnetcore.identity.authentication_scheme", authenticationScheme }
        };
        AddExceptionTags(ref tags, exception);

        _forgetTwoFactorCounter.Add(1, tags);
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
            SignInType.Passkey => "passkey",
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
    Refresh,
    Password,
    TwoFactorRecoveryCode,
    TwoFactorAuthenticator,
    TwoFactor,
    External,
    Passkey
}
