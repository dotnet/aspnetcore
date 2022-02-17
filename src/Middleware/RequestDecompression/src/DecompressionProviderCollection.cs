// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;

namespace Microsoft.AspNetCore.RequestDecompression;

/// <summary>
/// A collection of <see cref="IDecompressionProvider"/>'s that also allows them to instantiated from an <see cref="IServiceProvider"/>.
/// </summary>
public class DecompressionProviderCollection : Collection<IDecompressionProvider>
{
    /// <summary>
    /// Adds a type representing an <see cref="IDecompressionProvider"/>.
    /// </summary>
    /// <remarks>
    /// Provider instances will be created using an <see cref="IServiceProvider"/>.
    /// </remarks>
    public void Add<TDecompressionProvider>() where TDecompressionProvider : IDecompressionProvider
    {
        Add(typeof(TDecompressionProvider));
    }

    /// <summary>
    /// Adds a type representing a <see cref="IDecompressionProvider"/>.
    /// </summary>
    /// <param name="providerType">Type representing an <see cref="IDecompressionProvider"/>.</param>
    /// <remarks>
    /// Provider instance will be created using an <see cref="IServiceProvider"/>.
    /// </remarks>
    public void Add(Type providerType)
    {
        if (providerType == null)
        {
            throw new ArgumentNullException(nameof(providerType));
        }

        if (!typeof(IDecompressionProvider).IsAssignableFrom(providerType))
        {
            throw new ArgumentException($"The provider must implement {nameof(IDecompressionProvider)}.", nameof(providerType));
        }

        var factory = new DecompressionProviderFactory(providerType);
        Add(factory);
    }
}
