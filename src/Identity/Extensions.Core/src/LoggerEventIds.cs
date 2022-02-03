// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Identity.Core;

internal static class LoggerEventIds
{
    public static readonly EventId RoleValidationFailed = new EventId(0, "RoleValidationFailed");
    public static readonly EventId InvalidPassword = new EventId(0, "InvalidPassword");
    public static readonly EventId UserAlreadyHasPassword = new EventId(1, "UserAlreadyHasPassword");
    public static readonly EventId ChangePasswordFailed = new EventId(2, "ChangePasswordFailed");
    public static readonly EventId AddLoginFailed = new EventId(4, "AddLoginFailed");
    public static readonly EventId UserAlreadyInRole = new EventId(5, "UserAlreadyInRole");
    public static readonly EventId UserNotInRole = new EventId(6, "UserNotInRole");
    public static readonly EventId PhoneNumberChanged = new EventId(7, "PhoneNumberChanged");
    public static readonly EventId VerifyUserTokenFailed = new EventId(9, "VerifyUserTokenFailed");
    public static readonly EventId VerifyTwoFactorTokenFailed = new EventId(10, "VerifyTwoFactorTokenFailed");
    public static readonly EventId LockoutFailed = new EventId(11, "LockoutFailed");
    public static readonly EventId UserLockedOut = new EventId(12, "UserLockedOut");
    public static readonly EventId UserValidationFailed = new EventId(13, "UserValidationFailed");
    public static readonly EventId PasswordValidationFailed = new EventId(14, "PasswordValidationFailed");
    public static readonly EventId GetSecurityStampFailed = new EventId(15, "GetSecurityStampFailed");
}
