// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

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
