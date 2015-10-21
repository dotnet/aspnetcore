// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    /// Helpful constants for working with the authentication cookie compatibility shim.
    /// </summary>
    public static class DefaultCompatibilityConstants
    {
        /// <summary>
        /// The default authentication type for application authentication cookies.
        /// </summary>
        public const string ApplicationCookieAuthenticationType = "Microsoft.AspNet.Identity.Application.AuthType";

        /// <summary>
        /// The default cookie name for application authentication cookies.
        /// Used by <see cref="CookieAuthenticationOptions.CookieName"/>.
        /// </summary>
        public const string CookieName = ".AspNet.Microsoft.AspNet.Identity.Application";
    }
}