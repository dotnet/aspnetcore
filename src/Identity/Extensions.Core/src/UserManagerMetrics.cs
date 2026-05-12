// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Internal;
using static Microsoft.AspNetCore.Identity.UserManagerMetrics;

namespace Microsoft.AspNetCore.Identity;

internal sealed class UserManagerMetrics : IDisposable
{
    public const string MeterName = "Microsoft.AspNetCore.Identity";

    public const string CreateDurationName = "aspnetcore.identity.user.create.duration";
    public const string UpdateDurationName = "aspnetcore.identity.user.update.duration";
    public const string DeleteDurationName = "aspnetcore.identity.user.delete.duration";
    public const string CheckPasswordAttemptsCounterName = "aspnetcore.identity.user.check_password_attempts";
    public const string VerifyTokenAttemptsCounterName = "aspnetcore.identity.user.verify_token_attempts";
    public const string GenerateTokensCounterName = "aspnetcore.identity.user.generated_tokens";

    private readonly Meter _meter;
    private readonly Histogram<double> _createDuration;
    private readonly Histogram<double> _updateDuration;
    private readonly Histogram<double> _deleteDuration;
    private readonly Counter<long> _checkPasswordAttemptsCounter;
    private readonly Counter<long> _verifyTokenAttemptsCounter;
    private readonly Counter<long> _generateTokensCounter;

    public UserManagerMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        _createDuration = _meter.CreateHistogram<double>(
            CreateDurationName,
            unit: "s",
            description: "The duration of user creation operations.",
            advice: new() { HistogramBucketBoundaries = MetricsConstants.ShortSecondsBucketBoundaries });

        _updateDuration = _meter.CreateHistogram<double>(
            UpdateDurationName,
            unit: "s",
            description: "The duration of user update operations.",
            advice: new() { HistogramBucketBoundaries = MetricsConstants.ShortSecondsBucketBoundaries });

        _deleteDuration = _meter.CreateHistogram<double>(
            DeleteDurationName,
            unit: "s",
            description: "The duration of user deletion operations.",
            advice: new() { HistogramBucketBoundaries = MetricsConstants.ShortSecondsBucketBoundaries });

        _checkPasswordAttemptsCounter = _meter.CreateCounter<long>(
            CheckPasswordAttemptsCounterName,
            unit: "{attempt}",
            description: "The total number of check password attempts. Only checks whether the password is valid and not whether the user account is in a state that can log in.");

        _verifyTokenAttemptsCounter = _meter.CreateCounter<long>(
            VerifyTokenAttemptsCounterName,
            unit: "{attempt}",
            description: "The total number of token verification attempts.");

        _generateTokensCounter = _meter.CreateCounter<long>(
            GenerateTokensCounterName,
            unit: "{count}",
            description: "The total number of token generations.");
    }

    internal void CreateUser(string userType, IdentityResult? result, long startTimestamp, Exception? exception = null)
    {
        if (!_createDuration.Enabled)
        {
            return;
        }

        var tags = new TagList
        {
            { "aspnetcore.identity.user_type", userType }
        };
        AddIdentityResultTags(ref tags, result);
        AddErrorTag(ref tags, exception, result: result);

        var duration = GetElapsedTime(startTimestamp);
        _createDuration.Record(duration.TotalSeconds, tags);
    }

    internal void UpdateUser(string userType, IdentityResult? result, UserUpdateType updateType, long startTimestamp, Exception? exception = null)
    {
        if (!_updateDuration.Enabled)
        {
            return;
        }

        var tags = new TagList
        {
            { "aspnetcore.identity.user_type", userType },
            { "aspnetcore.identity.user.update_type", GetUpdateType(updateType) },
        };
        AddIdentityResultTags(ref tags, result);
        AddErrorTag(ref tags, exception, result: result);

        var duration = GetElapsedTime(startTimestamp);
        _updateDuration.Record(duration.TotalSeconds, tags);
    }

    internal void DeleteUser(string userType, IdentityResult? result, long startTimestamp, Exception? exception = null)
    {
        if (!_deleteDuration.Enabled)
        {
            return;
        }

        var tags = new TagList
        {
            { "aspnetcore.identity.user_type", userType }
        };
        AddIdentityResultTags(ref tags, result);
        AddErrorTag(ref tags, exception, result: result);

        var duration = GetElapsedTime(startTimestamp);
        _deleteDuration.Record(duration.TotalSeconds, tags);
    }

    internal void CheckPassword(string userType, bool? userMissing, PasswordVerificationResult? result, Exception? exception = null)
    {
        if (!_checkPasswordAttemptsCounter.Enabled)
        {
            return;
        }

        var tags = new TagList
        {
            { "aspnetcore.identity.user_type", userType },
        };
        if (userMissing != null || result != null)
        {
            tags.Add("aspnetcore.identity.password_check_result", GetPasswordResult(result, passwordMissing: null, userMissing));
        }
        AddErrorTag(ref tags, exception);

        _checkPasswordAttemptsCounter.Add(1, tags);
    }

    internal void VerifyToken(string userType, bool? result, string purpose, Exception? exception = null)
    {
        if (!_verifyTokenAttemptsCounter.Enabled)
        {
            return;
        }

        var tags = new TagList
        {
            { "aspnetcore.identity.user_type", userType },
            { "aspnetcore.identity.token_purpose", GetTokenPurpose(purpose) },
        };
        if (result != null)
        {
            tags.Add("aspnetcore.identity.token_verified", result == true ? "success" : "failure");
        }
        AddErrorTag(ref tags, exception);

        _verifyTokenAttemptsCounter.Add(1, tags);
    }

    internal void GenerateToken(string userType, string purpose, Exception? exception = null)
    {
        if (!_generateTokensCounter.Enabled)
        {
            return;
        }

        var tags = new TagList
        {
            { "aspnetcore.identity.user_type", userType },
            { "aspnetcore.identity.token_purpose", GetTokenPurpose(purpose) },
        };
        AddErrorTag(ref tags, exception);

        _generateTokensCounter.Add(1, tags);
    }

    private static TimeSpan GetElapsedTime(long startTimestamp)
    {
        return ValueStopwatch.GetElapsedTime(startTimestamp, Stopwatch.GetTimestamp());
    }

    private static string GetTokenPurpose(string purpose)
    {
        // Purpose could be any value and can't be used directly as a tag value. However, there are known purposes
        // on UserManager that we can detect and use as a tag value. Some could have a ':' in them followed by user data.
        // We need to trim them to content before ':' and then match to known values.
        ReadOnlySpan<char> trimmedPurpose = purpose;
        var colonIndex = purpose.IndexOf(':');
        if (colonIndex >= 0)
        {
            trimmedPurpose = purpose.AsSpan(0, colonIndex);
        }

        // These are known purposes that are specified in ASP.NET Core Identity.
        return trimmedPurpose switch
        {
            "ResetPassword" => "reset_password",
            "ChangePhoneNumber" => "change_phone_number",
            "EmailConfirmation" => "email_confirmation",
            "ChangeEmail" => "change_email",
            "TwoFactor" => "two_factor",
            _ => "_OTHER"
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
            tags.Add("aspnetcore.identity.error_code", code);
        }
    }

    private static void AddErrorTag(ref TagList tags, Exception? exception, IdentityResult? result = null)
    {
        var value = exception?.GetType().FullName ?? result?.Errors.FirstOrDefault()?.Code;
        if (value != null)
        {
            tags.Add("error.type", value);
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
            _ => "_OTHER"
        };
    }

    private static string GetUpdateType(UserUpdateType updateType)
    {
        return updateType switch
        {
            UserUpdateType.Update => "update",
            UserUpdateType.SetUserName => "set_user_name",
            UserUpdateType.AddPassword => "add_password",
            UserUpdateType.ChangePassword => "change_password",
            UserUpdateType.UpdateSecurityStamp => "update_security_stamp",
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
            UserUpdateType.IncrementAccessFailed => "increment_access_failed",
            UserUpdateType.ResetAccessFailedCount => "reset_access_failed_count",
            UserUpdateType.SetAuthenticationToken => "set_authentication_token",
            UserUpdateType.RemoveAuthenticationToken => "remove_authentication_token",
            UserUpdateType.ResetAuthenticatorKey => "reset_authenticator_key",
            UserUpdateType.GenerateNewTwoFactorRecoveryCodes => "generate_new_two_factor_recovery_codes",
            UserUpdateType.RedeemTwoFactorRecoveryCode => "redeem_two_factor_recovery_code",
            UserUpdateType.AddOrUpdatePasskey => "add_or_update_passkey",
            UserUpdateType.RemovePasskey => "remove_passkey",
            _ => "_OTHER"
        };
    }

    public void Dispose()
    {
        _meter.Dispose();
    }
}
