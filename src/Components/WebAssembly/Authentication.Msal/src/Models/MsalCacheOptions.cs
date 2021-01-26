// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Authentication.WebAssembly.Msal.Models
{
    /// <summary>
    /// Cache options for the msal.js cache.
    /// </summary>
    public class MsalCacheOptions
    {
        /// <summary>
        /// Gets or sets the cache location.
        /// </summary>
        /// <remarks>
        /// Valid values are <c>sessionStorage</c> and <c>localStorage</c>.
        /// </remarks>
        public string CacheLocation { get; set; }

        /// <summary>
        /// Gets or sets whether to store the authentication state in a cookie.
        /// </summary>
        public bool StoreAuthStateInCookie { get; set; }
    }
}
