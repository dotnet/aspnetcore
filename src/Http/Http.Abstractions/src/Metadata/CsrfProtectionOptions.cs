// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Antiforgery;

/// <summary>
/// Options for configuring cross-origin CSRF protection.
/// </summary>
public class CsrfProtectionOptions
{
    /// <summary>
    /// Gets the collection of trusted origins that are allowed to make cross-origin requests.
    /// Origins should be in the format "scheme://host[:port]" (e.g., "https://example.com").
    /// </summary>
    public IList<string> TrustedOrigins { get; } = new List<string>();
}
