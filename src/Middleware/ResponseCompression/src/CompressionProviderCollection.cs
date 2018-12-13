// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;

namespace Microsoft.AspNetCore.ResponseCompression
{
    /// <summary>
    /// A Collection of ICompressionProvider's that also allows them to be instantiated from an <see cref="IServiceProvider" />.
    /// </summary>
    public class CompressionProviderCollection : Collection<ICompressionProvider>
    {
        /// <summary>
        /// Adds a type representing an <see cref="ICompressionProvider"/>.
        /// </summary>
        /// <remarks>
        /// Provider instances will be created using an <see cref="IServiceProvider" />.
        /// </remarks>
        public void Add<TCompressionProvider>() where TCompressionProvider : ICompressionProvider
        {
            Add(typeof(TCompressionProvider));
        }

        /// <summary>
        /// Adds a type representing an <see cref="ICompressionProvider"/>.
        /// </summary>
        /// <param name="providerType">Type representing an <see cref="ICompressionProvider"/>.</param>
        /// <remarks>
        /// Provider instances will be created using an <see cref="IServiceProvider" />.
        /// </remarks>
        public void Add(Type providerType)
        {
            if (providerType == null)
            {
                throw new ArgumentNullException(nameof(providerType));
            }

            if (!typeof(ICompressionProvider).IsAssignableFrom(providerType))
            {
                throw new ArgumentException($"The provider must implement {nameof(ICompressionProvider)}.", nameof(providerType));
            }

            var factory = new CompressionProviderFactory(providerType);
            Add(factory);
        }
    }
}
