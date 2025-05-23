// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading.Tasks;
using static Microsoft.AspNetCore.Identity.UserManagerMetrics;

namespace Microsoft.AspNetCore.Identity;

internal sealed class UserManagerMetrics : IDisposable
{
    public const string MeterName = "Microsoft.AspNetCore.Identity";

    public const string CreateCounterName = "aspnetcore.identity.user.create";
    public const string UpdateCounterName = "aspnetcore.identity.user.update";
    public const string DeleteCounterName = "aspnetcore.identity.user.delete";
    public const string CheckPasswordCounterName = "aspnetcore.identity.user.check_password";
    public const string VerifyPasswordCounterName = "aspnetcore.identity.user.verify_password";
    public const string VerifyTokenCounterName = "aspnetcore.identity.user.verify_token";
    public const string GenerateTokenCounterName = "aspnetcore.identity.user.generate_token";

    private readonly Meter _meter;
    private readonly Counter<long> _createCounter;
    private readonly Counter<long> _updateCounter;
    private readonly Counter<long> _deleteCounter;
    private readonly Counter<long> _checkPasswordCounter;
    private readonly Counter<long> _verifyPasswordCounter;
    private readonly Counter<long> _verifyTokenCounter;
    private readonly Counter<long> _generateTokenCounter;

    public UserManagerMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);
        _createCounter = _meter.CreateCounter<long>(CreateCounterName, "count", "The number of users created.");
        _updateCounter = _meter.CreateCounter<long>(UpdateCounterName, "count", "The number of user updates.");
        _deleteCounter = _meter.CreateCounter<long>(DeleteCounterName, "count", "The number of users deleted.");
        _checkPasswordCounter = _meter.CreateCounter<long>(CheckPasswordCounterName, "count", "The number of check password attempts.");
        _verifyPasswordCounter = _meter.CreateCounter<long>(VerifyPasswordCounterName, "count", "The number of password verification attempts.");
        _verifyTokenCounter = _meter.CreateCounter<long>(VerifyTokenCounterName, "count", "The number of token verification attempts.");
        _generateTokenCounter = _meter.CreateCounter<long>(GenerateTokenCounterName, "count", "The number of token generation attempts.");
    }

    internal void CreateUser(string userType, IdentityResult? result, Exception? exception = null)
    {
        if (_createCounter.Enabled)
        {
            var tags = new TagList
            {
                { "aspnetcore.identity.user_type", userType }
            };
            AddIdentityResultTags(ref tags, result);
            AddExceptionTags(ref tags, exception);

            _createCounter.Add(1, tags);
        }
    }

    private static void AddExceptionTags(ref TagList tags, Exception? exception)
    {
        if (exception != null)
        {
            tags.Add("error.type", exception.GetType().FullName!);
        }
    }

    internal void UpdateUser(string userType, IdentityResult? result, UserUpdateType updateType, Exception? exception = null)
    {
        if (_updateCounter.Enabled)
        {
            var tags = new TagList
            {
                { "aspnetcore.identity.user_type", userType },
                { "aspnetcore.identity.user.update_type", GetUpdateType(updateType) },
            };
            AddIdentityResultTags(ref tags, result);
            AddExceptionTags(ref tags, exception);

            _updateCounter.Add(1, tags);
        }
    }

    internal void DeleteUser(string userType, IdentityResult? result, Exception? exception = null)
    {
        if (_deleteCounter.Enabled)
        {
            var tags = new TagList
            {
                { "aspnetcore.identity.user_type", userType }
            };
            AddIdentityResultTags(ref tags, result);
            AddExceptionTags(ref tags, exception);

            _deleteCounter.Add(1, tags);
        }
    }

    internal void CheckPassword(string userType, bool? userMissing, PasswordVerificationResult? result, Exception? exception = null)
    {
        if (_checkPasswordCounter.Enabled)
        {
            var tags = new TagList
            {
                { "aspnetcore.identity.user_type", userType },
            };
            if (userMissing != null || result != null)
            {
                tags.Add("aspnetcore.identity.user.password_result", GetPasswordResult(result, passwordMissing: null, userMissing));
            }
            AddExceptionTags(ref tags, exception);

            _checkPasswordCounter.Add(1, tags);
        }
    }

    internal void VerifyPassword(string userType, bool passwordMissing, PasswordVerificationResult? result)
    {
        if (_verifyPasswordCounter.Enabled)
        {
            var tags = new TagList
            {
                { "aspnetcore.identity.user_type", userType },
                { "aspnetcore.identity.user.password_result", GetPasswordResult(result, passwordMissing, userMissing: null) },
            };

            _verifyPasswordCounter.Add(1, tags);
        }
    }

    internal void VerifyToken(string userType, bool? result, string purpose, Exception? exception = null)
    {
        var tags = new TagList
        {
            { "aspnetcore.identity.user_type", userType },
            { "aspnetcore.identity.token_purpose", GetTokenPurpose(purpose) },
        };
        if (result != null)
        {
            tags.Add("aspnetcore.identity.token_verified", result == true ? "success" : "failure");
        }
        AddExceptionTags(ref tags, exception);

        _verifyTokenCounter.Add(1, tags);
    }

    internal void GenerateToken(string userType, string purpose, Exception? exception = null)
    {
        var tags = new TagList
        {
            { "aspnetcore.identity.user_type", userType },
            { "aspnetcore.identity.token_purpose", GetTokenPurpose(purpose) },
        };
        AddExceptionTags(ref tags, exception);

        _generateTokenCounter.Add(1, tags);
    }

    private static string GetTokenPurpose(string purpose)
    {
        // Purpose could be any value and can't be used as a tag value. However, there are known purposes
        // on UserManager that we can detect and use as a tag value. Some could have a ':' in them followed by user data.
        // We need to trim them to content before ':' and then match to known values.
        var trimmedPurpose = purpose;
        var colonIndex = purpose.IndexOf(':');
        if (colonIndex >= 0)
        {
            trimmedPurpose = purpose.Substring(0, colonIndex);
        }
        
        return trimmedPurpose switch
        {
            "ResetPassword" => "reset_password",
            "ChangePhoneNumber" => "change_phone_number",
            "EmailConfirmation" => "email_confirmation",
            "ChangeEmail" => "change_email",
            "TwoFactor" => "two_factor",
            _ => "_UNKNOWN"
        };
    }

    private static void AddIdentityResultTags(ref TagList tags, IdentityResult? result)
    {
        if (result == null)
        {
            return;
        }

        tags.Add("aspnetcore.identity.result", result.Succeeded ? "success" : "failure");
        if (!result.Succeeded && result.Errors.FirstOrDefault()?.Code is { Length: > 0 } code)
        {
            tags.Add("aspnetcore.identity.result_error_code", code);
        }
    }

    private static string GetPasswordResult(PasswordVerificationResult? result, bool? passwordMissing, bool? userMissing)
    {
        return (result, passwordMissing ?? false, userMissing ?? false) switch
        {
            (PasswordVerificationResult.Success, false, false) => "success",
            (PasswordVerificationResult.SuccessRehashNeeded, false, false) => "success_rehash_needed",
            (PasswordVerificationResult.Failed, false, false) => "failure",
            (null, true, false) => "password_missing",
            (null, false, true) => "user_missing",
            _ => "_UNKNOWN"
        };
    }

    private static string GetUpdateType(UserUpdateType updateType)
    {
        return updateType switch
        {
            UserUpdateType.Update => "update",
            UserUpdateType.UserName => "user_name",
            UserUpdateType.AddPassword => "add_password",
            UserUpdateType.ChangePassword => "change_password",
            UserUpdateType.SecurityStamp => "security_stamp",
            UserUpdateType.ResetPassword => "reset_password",
            UserUpdateType.RemoveLogin => "remove_login",
            UserUpdateType.AddLogin => "add_login",
            UserUpdateType.AddClaims => "add_claims",
            UserUpdateType.ReplaceClaim => "replace_claim",
            UserUpdateType.RemoveClaims => "remove_claims",
            UserUpdateType.AddToRoles => "add_to_roles",
            UserUpdateType.RemoveFromRoles => "remove_from_roles",
            UserUpdateType.SetEmail => "set_email",
            UserUpdateType.ConfirmEmail => "confirm_email",
            UserUpdateType.PasswordRehash => "password_rehash",
            UserUpdateType.RemovePassword => "remove_password",
            UserUpdateType.ChangeEmail => "change_email",
            UserUpdateType.SetPhoneNumber => "set_phone_number",
            UserUpdateType.ChangePhoneNumber => "change_phone_number",
            UserUpdateType.SetTwoFactorEnabled => "set_two_factor_enabled",
            UserUpdateType.SetLockoutEnabled => "set_lockout_enabled",
            UserUpdateType.SetLockoutEndDate => "set_lockout_end_date",
            UserUpdateType.AccessFailed => "access_failed",
            UserUpdateType.ResetAccessFailedCount => "reset_access_failed_count",
            UserUpdateType.SetAuthenticationToken => "set_authentication_token",
            UserUpdateType.RemoveAuthenticationToken => "remove_authentication_token",
            UserUpdateType.ResetAuthenticatorKey => "reset_authenticator_key",
            _ => "_UNKNOWN"
        };
    }

    public void Dispose()
    {
        _meter.Dispose();
    }
}

internal enum UserUpdateType
{
    Update,
    UserName,
    AddPassword,
    ChangePassword,
    SecurityStamp,
    ResetPassword,
    RemoveLogin,
    AddLogin,
    AddClaims,
    ReplaceClaim,
    RemoveClaims,
    AddToRoles,
    RemoveFromRoles,
    SetEmail,
    ConfirmEmail,
    PasswordRehash,
    RemovePassword,
    ChangeEmail,
    SetPhoneNumber,
    ChangePhoneNumber,
    SetTwoFactorEnabled,
    SetLockoutEnabled,
    SetLockoutEndDate,
    AccessFailed,
    ResetAccessFailedCount,
    SetAuthenticationToken,
    RemoveAuthenticationToken,
    ResetAuthenticatorKey,
    GenerateNewTwoFactorRecoveryCodes,
    RedeemTwoFactorRecoveryCode,
}
