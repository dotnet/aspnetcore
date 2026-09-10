// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Identity;

internal static class EventIds
{
    public static readonly EventId UserCannotSignInWithoutConfirmedEmail = new(0, "UserCannotSignInWithoutConfirmedEmail");
    public static readonly EventId SecurityStampValidationFailed = new(0, "SecurityStampValidationFailed");
    public static readonly EventId SecurityStampValidationFailedId4 = new(4, "SecurityStampValidationFailed");
    public static readonly EventId UserCannotSignInWithoutConfirmedPhoneNumber = new(1, "UserCannotSignInWithoutConfirmedPhoneNumber");
    public static readonly EventId InvalidPassword = new(2, "InvalidPassword");
    public static readonly EventId UserLockedOut = new(3, "UserLockedOut");
    public static readonly EventId UserCannotSignInWithoutConfirmedAccount = new(4, "UserCannotSignInWithoutConfirmedAccount");
    public static readonly EventId TwoFactorSecurityStampValidationFailed = new(5, "TwoFactorSecurityStampValidationFailed");
    public static readonly EventId NoPasskeyCreationOptions = new(6, "NoPasskeyCreationOptions");
    public static readonly EventId UserDoesNotMatchPasskeyCreationOptions = new(7, "UserDoesNotMatchPasskeyCreationOptions");
    public static readonly EventId PasskeyAttestationFailed = new(8, "PasskeyAttestationFailed");
    public static readonly EventId PasskeyAssertionFailed = new(9, "PasskeyAssertionFailed");
}
