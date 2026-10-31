// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;

namespace Microsoft.AspNetCore.Razor.Hosting;

/// <summary>
/// Extension methods for <see cref="RazorCompiledItem"/>.
/// </summary>
public static class RazorCompiledItemExtensions
{
    /// <summary>
    /// Gets the list of <see cref="IRazorSourceChecksumMetadata"/> associated with <paramref name="item"/>.
    /// </summary>
    /// <param name="item">The <see cref="RazorCompiledItem"/>.</param>
    /// <returns>A list of <see cref="IRazorSourceChecksumMetadata"/>.</returns>
    public static IReadOnlyList<IRazorSourceChecksumMetadata> GetChecksumMetadata(this RazorCompiledItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return item.Metadata.OfType<IRazorSourceChecksumMetadata>().ToArray();
    }
}
