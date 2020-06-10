// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
        /// The expiration that should be used for entries in the MemoryCache, defaults to 2 minutes.
        /// This is a sliding expiration that will extend each time the certificate is used, so long as the certificate is valid (see X509Certificate2.NotAfter).
        /// </summary>
        public TimeSpan CacheEntryExpiration { get; set; } = TimeSpan.FromMinutes(2);

        /// <summary>
        /// How many validated certificate results to store in the cache, defaults to 1024.
        /// </summary>
        public int CacheSize { get; set; } = 1024;
    }
}
