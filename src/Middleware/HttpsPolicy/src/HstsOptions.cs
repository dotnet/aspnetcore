// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.HttpsPolicy;

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
    /// See: <see href="https://tools.ietf.org/html/rfc6797#section-6.1.1"/>
    /// </remarks>
    public TimeSpan MaxAge { get; set; } = TimeSpan.FromDays(30);

    /// <summary>
    /// Enables includeSubDomain parameter of the Strict-Transport-Security header.
    /// </summary>
    /// <remarks>
    /// See: <see href="https://tools.ietf.org/html/rfc6797#section-6.1.2"/>
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

    /// <summary>
    /// A list of host names that will not add the HSTS header.
    /// </summary>
    public IList<string> ExcludedHosts { get; } = new List<string>
        {
            "localhost",
            "127.0.0.1", // ipv4
            "[::1]" // ipv6
        };
}
