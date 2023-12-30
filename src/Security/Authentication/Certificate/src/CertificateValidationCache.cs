// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication.Certificate;

/// <summary>
/// MemoryCache based implementation used to store <see cref="AuthenticateResult"/> results after the certificate has been validated
/// </summary>
public class CertificateValidationCache : ICertificateValidationCache
{
    private readonly MemoryCache _cache;
    private readonly CertificateValidationCacheOptions _options;
    private readonly TimeProvider _timeProvider;

    internal CertificateValidationCache(IOptions<CertificateValidationCacheOptions> options, TimeProvider timeProvider)
    {
        _options = options.Value;
        _cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = _options.CacheSize, Clock = new CachingClock(timeProvider) });
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="CertificateValidationCache"/>.
    /// </summary>
    /// <param name="options">An accessor to <see cref="CertificateValidationCacheOptions"/></param>
    public CertificateValidationCache(IOptions<CertificateValidationCacheOptions> options) : this(options, TimeProvider.System)
    { }

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
        // Never cache longer than 30 minutes
        var absoluteExpiration = _timeProvider.GetUtcNow().Add(TimeSpan.FromMinutes(30));
        var notAfter = certificate.NotAfter.ToUniversalTime();
        if (notAfter < absoluteExpiration)
        {
            absoluteExpiration = notAfter;
        }
        _cache.Set(ComputeKey(certificate), result.Clone(), new MemoryCacheEntryOptions()
            .SetSize(1)
            .SetSlidingExpiration(_options.CacheEntryExpiration)
            .SetAbsoluteExpiration(absoluteExpiration));
    }

    private static string ComputeKey(X509Certificate2 certificate)
        => certificate.GetCertHashString(HashAlgorithmName.SHA256);

    private sealed class CachingClock : Extensions.Internal.ISystemClock
    {
        private readonly TimeProvider _timeProvider;
        public CachingClock(TimeProvider timeProvider) => _timeProvider = timeProvider;
        public DateTimeOffset UtcNow => _timeProvider.GetUtcNow();
    }
}
