// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

internal enum UserUpdateType
{
    Update,
    SetUserName,
    AddPassword,
    ChangePassword,
    UpdateSecurityStamp,
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
    IncrementAccessFailed,
    ResetAccessFailedCount,
    SetAuthenticationToken,
    RemoveAuthenticationToken,
    ResetAuthenticatorKey,
    GenerateNewTwoFactorRecoveryCodes,
    RedeemTwoFactorRecoveryCode,
    AddOrUpdatePasskey,
    RemovePasskey
}
