// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Extension methods on <see cref="HttpContext"/> for accessing <see cref="BrowserConfiguration"/>.
/// </summary>
public static class BrowserConfigurationHttpContextExtensions
{
    private static readonly object Key = new();

    /// <summary>
    /// Gets the <see cref="BrowserConfiguration"/> for the current request.
    /// If not already set, seeds from endpoint metadata or creates a new instance.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <returns>The <see cref="BrowserConfiguration"/> for the current request.</returns>
    public static BrowserConfiguration GetBrowserConfiguration(this HttpContext context)
    {
        if (!context.Items.TryGetValue(Key, out var result))
        {
            // Seed from endpoint metadata if available
            var metadataConfig = context.GetEndpoint()?.Metadata.GetMetadata<BrowserConfiguration>();
            var config = metadataConfig ?? new BrowserConfiguration();
            context.Items[Key] = config;
            return config;
        }

        return (BrowserConfiguration)result!;
    }
}
