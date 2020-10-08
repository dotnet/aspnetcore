// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Identity.UI
{
    internal static class LoggerEventIds
    {
        public static readonly EventId UserCreatedByExternalProvider  = new EventId(1, "UserCreatedByExternalProvider");
        public static readonly EventId UserLoggedInByExternalProvider = new EventId(2, "UserLoggedInByExternalProvider");
        public static readonly EventId UserLogin = new EventId(3, "UserLogin");
        public static readonly EventId UserLockout = new EventId(4, "UserLockout");
        public static readonly EventId UserLoginWith2FA = new EventId(5, "UserLoginWith2FA");
        public static readonly EventId InvalidAuthenticatorCode = new EventId(6, "InvalidAuthenticatorCode");
        public static readonly EventId UserLoginWithRecoveryCode = new EventId(7, "UserLoginWithRecoveryCode");
        public static readonly EventId InvalidRecoveryCode = new EventId(8, "InvalidRecoveryCode");
        public static readonly EventId PasswordChanged = new EventId(9, "PasswordChanged");
        public static readonly EventId UserDeleted = new EventId(10, "UserDeleted");
        public static readonly EventId TwoFADisabled = new EventId(11, "TwoFADisabled");
        public static readonly EventId PersonalDataRequested = new EventId(12, "PersonalDataRequested");
        public static readonly EventId TwoFAEnabled = new EventId(13, "TwoFAEnabled");
        public static readonly EventId TwoFARecoveryGenerated = new EventId(14, "TwoFARecoveryGenerated");
        public static readonly EventId AuthenticationAppKeyReset = new EventId(15, "AuthenticationAppKeyReset");
        public static readonly EventId UserCreated = new EventId(16, "UserCreated");
        public static readonly EventId UserLoggedOut = new EventId(17, "UserLoggedOut");
 

    }
}
