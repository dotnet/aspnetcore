// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Identity.Service
{
    public class LogoutResult
    {
        private LogoutResult(string logoutRedirectUri)
        {
            Status = logoutRedirectUri == null ? LogoutStatus.LocalLogoutPage : LogoutStatus.RedirectToLogoutUri;
            LogoutRedirect = logoutRedirectUri;
        }

        public string LogoutRedirect { get; }
        public LogoutStatus Status { get; }

        public static LogoutResult Redirect(string logoutRedirectUri) => new LogoutResult(logoutRedirectUri);
        public static LogoutResult RedirectToLocalLogoutPage() => new LogoutResult(null);
    }
}
