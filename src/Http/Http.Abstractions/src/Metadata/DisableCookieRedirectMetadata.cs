// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Metadata;

/// <summary>
/// Metadata that indicates the endpoint should disable cookie-based authentication redirects.
/// When present, authentication handlers should prefer returning status codes over browser redirects.
/// </summary>
internal sealed class DisableCookieRedirectMetadata : IDisableCookieRedirectMetadata
{
    /// <summary>
    /// Singleton instance of <see cref="DisableCookieRedirectMetadata"/>.
    /// </summary>
    public static readonly DisableCookieRedirectMetadata Instance = new();

    private DisableCookieRedirectMetadata()
    {
    }
}
