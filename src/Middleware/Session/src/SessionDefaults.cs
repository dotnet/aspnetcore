// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Session
{
    /// <summary>
    /// Represents defaults for the Session.
    /// </summary>
    public static class SessionDefaults
    {
        /// <summary>
        /// Represent the default cookie name, which is ".AspNetCore.Session".
        /// </summary>
        public static readonly string CookieName = ".AspNetCore.Session";
        
        /// <summary>
        /// Represents the default path used to create the cookie, which is "/".
        /// </summary>
        public static readonly string CookiePath = "/";
    }
}