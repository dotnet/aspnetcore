// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Identity.UI;

/// <summary>
/// Static class that exposes logging event ids.
/// </summary>
public static class LoggerEventIds
{
    /// <summary>
    /// Event id when a user is created by an external provider.
    /// </summary>
    public static readonly EventId UserCreatedByExternalProvider = new EventId(1, "UserCreatedByExternalProvider");

    /// <summary>
    /// Event id when a user is logged in by an external provider.
    /// </summary>
    public static readonly EventId UserLoggedInByExternalProvider = new EventId(2, "UserLoggedInByExternalProvider");

    /// <summary>
    /// Event id when a user is logged in.
    /// </summary>
    public static readonly EventId UserLogin = new EventId(3, "UserLogin");

    /// <summary>
    /// Event id when a user is locked out.
    /// </summary>
    public static readonly EventId UserLockout = new EventId(4, "UserLockout");

    /// <summary>
    /// Event id when a user is logged in two factor authentication.
    /// </summary>
    public static readonly EventId UserLoginWith2FA = new EventId(5, "UserLoginWith2FA");

    /// <summary>
    /// Event id when a user has entered an invalid authenticator code.
    /// </summary>
    public static readonly EventId InvalidAuthenticatorCode = new EventId(6, "InvalidAuthenticatorCode");

    /// <summary>
    /// Event id when a user is logged in with recovey code.
    /// </summary>
    public static readonly EventId UserLoginWithRecoveryCode = new EventId(7, "UserLoginWithRecoveryCode");

    /// <summary>
    /// Event id when a user has entered an invalid recovey code.
    /// </summary>
    public static readonly EventId InvalidRecoveryCode = new EventId(8, "InvalidRecoveryCode");

    /// <summary>
    /// Event id when a user has changed the password.
    /// </summary>
    public static readonly EventId PasswordChanged = new EventId(9, "PasswordChanged");

    /// <summary>
    /// Event id when a user has been deleted.
    /// </summary>
    public static readonly EventId UserDeleted = new EventId(10, "UserDeleted");

    /// <summary>
    /// Event id when a user has disabled two factor authentication.
    /// </summary>
    public static readonly EventId TwoFADisabled = new EventId(11, "TwoFADisabled");

    /// <summary>
    /// Event id when a user has requested the personal data.
    /// </summary>
    public static readonly EventId PersonalDataRequested = new EventId(12, "PersonalDataRequested");

    /// <summary>
    /// Event id when a user has enabled two factor authentication.
    /// </summary>
    public static readonly EventId TwoFAEnabled = new EventId(13, "TwoFAEnabled");

    /// <summary>
    /// Event id when two factor authentication recovery code is generated.
    /// </summary>
    public static readonly EventId TwoFARecoveryGenerated = new EventId(14, "TwoFARecoveryGenerated");

    /// <summary>
    /// Event id when user has reset the authentication app key.
    /// </summary>
    public static readonly EventId AuthenticationAppKeyReset = new EventId(15, "AuthenticationAppKeyReset");

    /// <summary>
    /// Event id when a user is created.
    /// </summary>
    public static readonly EventId UserCreated = new EventId(16, "UserCreated");

    /// <summary>
    /// Event id when a user is logged out.
    /// </summary>
    public static readonly EventId UserLoggedOut = new EventId(17, "UserLoggedOut");
}
