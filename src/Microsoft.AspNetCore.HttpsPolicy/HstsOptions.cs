// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.HttpsPolicy
{
    /// <summary>
    /// Options for the Hsts Middleware
    /// </summary>
    public class HstsOptions
    {
        /// <summary>
        /// Sets the max-age parameter of the Strict-Transport-Security header.
        /// </summary>
        /// <remarks>
        /// Max-age is required; defaults to 30 days.
        /// See: https://tools.ietf.org/html/rfc6797#section-6.1.1
        /// </remarks>
        public TimeSpan MaxAge { get; set; } = TimeSpan.FromDays(30);

        /// <summary>
        /// Enables includeSubDomain parameter of the Strict-Transport-Security header.
        /// </summary>
        /// <remarks>
        /// See: https://tools.ietf.org/html/rfc6797#section-6.1.2
        /// </remarks>
        public bool IncludeSubDomains { get; set; }

        /// <summary>
        /// Sets the preload parameter of the Strict-Transport-Security header.
        /// </summary>
        /// <remarks>
        /// Preload is not part of the RFC specification, but is supported by web browsers
        /// to preload HSTS sites on fresh install. See https://hstspreload.org/.
        /// </remarks>
        public bool Preload { get; set; }
    }
}
