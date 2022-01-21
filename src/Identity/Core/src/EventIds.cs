// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Identity;

internal static class EventIds
{
    public static EventId UserCannotSignInWithoutConfirmedEmail = new EventId(0, "UserCannotSignInWithoutConfirmedEmail");
    public static EventId SecurityStampValidationFailed = new EventId(0, "SecurityStampValidationFailed");
    public static EventId SecurityStampValidationFailedId4 = new EventId(4, "SecurityStampValidationFailed");
    public static EventId UserCannotSignInWithoutConfirmedPhoneNumber = new EventId(1, "UserCannotSignInWithoutConfirmedPhoneNumber");
    public static EventId InvalidPassword = new EventId(2, "InvalidPassword");
    public static EventId UserLockedOut = new EventId(3, "UserLockedOut");
    public static EventId UserCannotSignInWithoutConfirmedAccount = new EventId(4, "UserCannotSignInWithoutConfirmedAccount");
    public static EventId TwoFactorSecurityStampValidationFailed = new EventId(5, "TwoFactorSecurityStampValidationFailed");
}
