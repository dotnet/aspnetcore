// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication.Certificate
{
    /// <summary>
    /// MemoryCache based implementation used to store <see cref="AuthenticateResult"/> results after the certificate has been validated
    /// </summary>
    public class CertificateValidationCache : ICertificateValidationCache
    {
        private readonly MemoryCache _cache;
        private readonly CertificateValidationCacheOptions _options;

        /// <summary>
        /// Initializes a new instance of <see cref="CertificateValidationCache"/>.
        /// </summary>
        /// <param name="options">An accessor to <see cref="CertificateValidationCacheOptions"/></param>
        public CertificateValidationCache(IOptions<CertificateValidationCacheOptions> options)
        {
            _options = options.Value;
            _cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = _options.CacheSize });
        }

        /// <summary>
        /// Get the <see cref="AuthenticateResult"/> for the connection and certificate.
        /// </summary>
        /// <param name="context">The HttpContext.</param>
        /// <param name="certificate">The certificate.</param>
        /// <returns>the <see cref="AuthenticateResult"/></returns>
        public AuthenticateResult? Get(HttpContext context, X509Certificate2 certificate)
            => _cache.Get<AuthenticateResult>(ComputeKey(certificate))?.Clone();

        /// <summary>
        /// Store a <see cref="AuthenticateResult"/> for the connection and certificate
        /// </summary>
        /// <param name="context">The HttpContext.</param>
        /// <param name="certificate">The certificate.</param>
        /// <param name="result">the <see cref="AuthenticateResult"/></param>
        public void Put(HttpContext context, X509Certificate2 certificate, AuthenticateResult result)
        {
            // Cache expired certs for little while too
            var absExpiration = (certificate.NotAfter < DateTime.Now) ? DateTime.Now + _options.CacheEntryExpiration : certificate.NotAfter;
            _cache.Set(ComputeKey(certificate), result.Clone(), new MemoryCacheEntryOptions()
                .SetSize(1).SetSlidingExpiration(_options.CacheEntryExpiration).SetAbsoluteExpiration(absExpiration));
        }

        private string ComputeKey(X509Certificate2 certificate)
            => certificate.GetCertHashString(HashAlgorithmName.SHA256);
    }
}
