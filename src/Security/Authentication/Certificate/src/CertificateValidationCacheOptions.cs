// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Authentication.Certificate
{
    /// <summary>
    /// Configuration options for <see cref="CertificateValidationCache"/>
    /// </summary>
    public class CertificateValidationCacheOptions
    {
        /// <summary>
        /// Gets or sets the expiration that should be used for entries in the MemoryCache.
        /// This is a sliding expiration that will extend each time the certificate is used, so long as the certificate is valid (see X509Certificate2.NotAfter).
        /// </summary>
        /// <value>Defaults to 2 minutes.</value>
        public TimeSpan CacheEntryExpiration { get; set; } = TimeSpan.FromMinutes(2);

        /// <summary>
        /// Gets or sets the maximum number of validated certificate results that are allowed to cached.
        /// </summary>
        /// <value>
        /// Defaults to 1024.
        /// </value>
        public int CacheSize { get; set; } = 1024;
    }
}
