// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

/// <summary>
/// Extension methods for <see cref="IMetadataDetailsProvider"/>.
/// </summary>
public static class MetadataDetailsProviderExtensions
{
    /// <summary>
    /// Removes all metadata details providers of the specified type.
    /// </summary>
    /// <param name="list">The list of <see cref="IMetadataDetailsProvider"/>s.</param>
    /// <typeparam name="TMetadataDetailsProvider">The type to remove.</typeparam>
    public static void RemoveType<TMetadataDetailsProvider>(this IList<IMetadataDetailsProvider> list) where TMetadataDetailsProvider : IMetadataDetailsProvider
    {
        ArgumentNullException.ThrowIfNull(list);

        RemoveType(list, typeof(TMetadataDetailsProvider));
    }

    /// <summary>
    /// Removes all metadata details providers of the specified type.
    /// </summary>
    /// <param name="list">The list of <see cref="IMetadataDetailsProvider"/>s.</param>
    /// <param name="type">The type to remove.</param>
    public static void RemoveType(this IList<IMetadataDetailsProvider> list, Type type)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(type);

        for (var i = list.Count - 1; i >= 0; i--)
        {
            var metadataDetailsProvider = list[i];
            if (metadataDetailsProvider.GetType() == type)
            {
                list.RemoveAt(i);
            }
        }
    }
}
