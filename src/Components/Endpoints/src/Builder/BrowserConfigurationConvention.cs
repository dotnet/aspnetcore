// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Components;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Convention that ensures every Razor component endpoint has a <see cref="BrowserConfiguration"/>
/// in its metadata when <see cref="RazorComponentsEndpointConventionBuilderExtensions.WithBrowserConfiguration"/>
/// has been called.
/// </summary>
/// <remarks>
/// Registered as a <c>Finally</c> convention so it runs after user conventions.
/// </remarks>
internal sealed class BrowserConfigurationConvention
{
    /// <summary>
    /// Applies the convention to an endpoint builder. Ensures a <see cref="BrowserConfiguration"/>
    /// exists in metadata if the user hasn't already added one.
    /// </summary>
    public static void ApplyConvention(EndpointBuilder endpointBuilder)
    {
        // Check if the user already added a BrowserConfiguration via WithBrowserConfiguration
        var existing = endpointBuilder.Metadata.OfType<BrowserConfiguration>().LastOrDefault();
        if (existing == null)
        {
            // No user configuration — add a default one
            endpointBuilder.Metadata.Add(new BrowserConfiguration());
        }
    }
}
