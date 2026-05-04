// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Antiforgery;

/// <summary>
/// Options for configuring cross-origin request protection.
/// </summary>
public class CrossOriginProtectionOptions
{
    /// <summary>
    /// Gets the list of trusted origins that are allowed to make cross-origin requests.
    /// Origins must be in the format "scheme://host[:port]" (e.g., "https://admin.example.com").
    /// Requests from these origins will be allowed even if they are cross-origin.
    /// </summary>
    /// <remarks>
    /// <para>
    /// By default, only same-origin requests are allowed. Adding origins to this list
    /// permits those origins to make state-changing (non-safe) requests.
    /// </para>
    /// <para>
    /// Origins are compared using scheme, host, and port. Default ports (80 for http, 443 for https)
    /// may be omitted. Host comparison is case-insensitive.
    /// </para>
    /// </remarks>
    public IList<string> TrustedOrigins { get; } = new List<string>();
}
