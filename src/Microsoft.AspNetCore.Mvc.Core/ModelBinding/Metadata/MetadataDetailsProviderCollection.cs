// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata
{
    /// <summary>
    /// Represents a collection of metadata details providers.
    /// </summary>
    public class MetadataDetailsProviderCollection : Collection<IMetadataDetailsProvider>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataDetailsProviderCollection"/> class that is empty.
        /// </summary>
        public MetadataDetailsProviderCollection()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataDetailsProviderCollection"/> class
        /// as a wrapper for the specified list.
        /// </summary>
        /// <param name="metadataDetailsProviders">The list that is wrapped by the new collection.</param>
        public MetadataDetailsProviderCollection(IList<IMetadataDetailsProvider> metadataDetailsProviders)
            : base(metadataDetailsProviders)
        {
        }

        /// <summary>
        /// Removes all metadata details providers of the specified type.
        /// </summary>
        /// <typeparam name="TMetadataDetailsProvider">The type to remove.</typeparam>
        public void RemoveType<TMetadataDetailsProvider>() where TMetadataDetailsProvider : IMetadataDetailsProvider
        {
            RemoveType(typeof(TMetadataDetailsProvider));
        }

        /// <summary>
        /// Removes all metadata details providers of the specified type.
        /// </summary>
        /// <param name="metadataDetailsProviderType">The type to remove.</param>
        public void RemoveType(Type metadataDetailsProviderType)
        {
            for (var i = Count - 1; i >= 0; i--)
            {
                var metadataDetailsProvider = this[i];
                if (metadataDetailsProvider.GetType() == metadataDetailsProviderType)
                {
                    RemoveAt(i);
                }
            }
        }
    }
}
