// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Antiforgery;

/// <summary>
/// Provides programmatic configuration for cross-origin request protection
/// based on Fetch Metadata headers (Sec-Fetch-Site) and Origin header validation.
/// </summary>
/// <remarks>
/// Cross-origin protection is enabled by default and provides CSRF protection
/// for endpoints that have antiforgery metadata without requiring the antiforgery
/// middleware or DataProtection services.
/// </remarks>
public class CrossOriginProtectionOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether cross-origin protection is enabled.
    /// When enabled, requests to endpoints with antiforgery metadata are validated
    /// using Sec-Fetch-Site and Origin headers.
    /// </summary>
    /// <value>Defaults to <see langword="true"/>.</value>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets the collection of trusted origins that are allowed for cross-origin requests.
    /// Origins should be in the format "scheme://host[:port]" (e.g., "https://example.com").
    /// Comparison is case-insensitive.
    /// </summary>
    public IList<string> TrustedOrigins { get; } = new List<string>();
}
