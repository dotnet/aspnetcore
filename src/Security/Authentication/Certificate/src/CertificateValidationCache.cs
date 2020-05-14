// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

namespace Microsoft.AspNetCore.Authentication.Certificate
{
    /// <summary>
    /// MemoryCache based implementation used to store <see cref="AuthenticateResult"/> results after the certificate has been validated
    /// </summary>
    public class CertificateValidationCache : ICertificateValidationCache
    {
        private MemoryCache _cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 1024 });

        /// <summary>
        /// Get the <see cref="AuthenticateResult"/> for the connection and certificate.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="certificate">The certificate.</param>
        /// <returns>the <see cref="AuthenticateResult"/></returns>
        public AuthenticateResult Get(ConnectionInfo connection, X509Certificate certificate)
            => _cache.Get(ComputeKey(connection, certificate)) as AuthenticateResult;

        /// <summary>
        /// Store a <see cref="AuthenticateResult"/> for the connection and certificate
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="certificate">The certificate.</param>
        /// <param name="result">the <see cref="AuthenticateResult"/></param>
        public void Put(ConnectionInfo connection, X509Certificate certificate, AuthenticateResult result)
            =>  _cache.Set(ComputeKey(connection, certificate), result, new MemoryCacheEntryOptions().SetSize(1));

        private string ComputeKey(ConnectionInfo connection, X509Certificate certificate)
            => $"{certificate.GetCertHashString()}:{connection.Id}";
    }

}
