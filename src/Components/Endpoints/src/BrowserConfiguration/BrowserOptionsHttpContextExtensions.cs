// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Extension methods on <see cref="HttpContext"/> for accessing <see cref="BrowserOptions"/>.
/// </summary>
public static class BrowserOptionsHttpContextExtensions
{
    private static readonly object Key = new();

    /// <summary>
    /// Gets the <see cref="BrowserOptions"/> for the current request.
    /// If not already set, seeds from endpoint metadata or creates a new instance.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <returns>The <see cref="BrowserOptions"/> for the current request.</returns>
    public static BrowserOptions GetBrowserOptions(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.Items.TryGetValue(Key, out var result))
        {
            // Seed from endpoint metadata if available
            var metadataConfig = context.GetEndpoint()?.Metadata.GetMetadata<BrowserOptions>();
            var config = metadataConfig ?? new BrowserOptions();
            context.Items[Key] = config;
            return config;
        }

        return (BrowserOptions)result!;
    }
}
