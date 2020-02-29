// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Options used to create a new cookie.
    /// </summary>
    public class CookieOptions
    {
        /// <summary>
        /// Creates a default cookie with a path of '/'.
        /// </summary>
        public CookieOptions()
        {
            Path = "/";
        }

        /// <summary>
        /// Gets or sets the domain to associate the cookie with.
        /// </summary>
        /// <returns>The domain to associate the cookie with.</returns>
        public string Domain { get; set; }

        /// <summary>
        /// Gets or sets the cookie path.
        /// </summary>
        /// <returns>The cookie path.</returns>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the expiration date and time for the cookie.
        /// </summary>
        /// <returns>The expiration date and time for the cookie.</returns>
        public DateTimeOffset? Expires { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether to transmit the cookie using Secure Sockets Layer (SSL)--that is, over HTTPS only.
        /// </summary>
        /// <returns>true to transmit the cookie only over an SSL connection (HTTPS); otherwise, false.</returns>
        public bool Secure { get; set; }

        /// <summary>
        /// Gets or sets the value for the SameSite attribute of the cookie. The default value is <see cref="SameSiteMode.Unspecified"/>
        /// </summary>
        /// <returns>The <see cref="SameSiteMode"/> representing the enforcement mode of the cookie.</returns>
        public SameSiteMode SameSite { get; set; } = SameSiteMode.Unspecified;

        /// <summary>
        /// Gets or sets a value that indicates whether a cookie is accessible by client-side script.
        /// </summary>
        /// <returns>true if a cookie must not be accessible by client-side script; otherwise, false.</returns>
        public bool HttpOnly { get; set; }

        /// <summary>
        /// Gets or sets the max-age for the cookie.
        /// </summary>
        /// <returns>The max-age date and time for the cookie.</returns>
        public TimeSpan? MaxAge { get; set; }

        /// <summary>
        /// Indicates if this cookie is essential for the application to function correctly. If true then
        /// consent policy checks may be bypassed. The default value is false.
        /// </summary>
        public bool IsEssential { get; set; }
    }
}
