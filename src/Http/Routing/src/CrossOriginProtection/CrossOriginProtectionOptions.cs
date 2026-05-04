// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Antiforgery;

/// <summary>
/// Provides programmatic configuration for cross-origin request protection
/// based on Fetch Metadata headers (Sec-Fetch-Site) and Origin header validation.
/// </summary>
/// <remarks>
/// Cross-origin protection is registered by default and provides CSRF protection
/// for endpoints that have antiforgery metadata without requiring the antiforgery
/// middleware or DataProtection services. To disable cross-origin protection entirely,
/// remove the <see cref="ICrossOriginProtection"/> service from DI.
/// </remarks>
public class CrossOriginProtectionOptions
{
    /// <summary>
    /// Gets the collection of trusted origins that are allowed for cross-origin requests.
    /// Origins should be in the format "scheme://host[:port]" (e.g., "https://example.com").
    /// Comparison is case-insensitive.
    /// </summary>
    /// <remarks>
    /// Most applications don't need to configure this — same-origin requests are allowed
    /// automatically via Sec-Fetch-Site header validation. Only add origins here when you
    /// need to accept state-changing requests from a different origin.
    /// </remarks>
    public IList<string> TrustedOrigins { get; } = new List<string>();
}
