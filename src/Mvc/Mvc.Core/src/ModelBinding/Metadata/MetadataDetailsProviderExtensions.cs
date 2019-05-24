// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata
{
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
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }

            RemoveType(list, typeof(TMetadataDetailsProvider));
        }

        /// <summary>
        /// Removes all metadata details providers of the specified type.
        /// </summary>
        /// <param name="list">The list of <see cref="IMetadataDetailsProvider"/>s.</param>
        /// <param name="type">The type to remove.</param>
        public static void RemoveType(this IList<IMetadataDetailsProvider> list, Type type)
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

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
}
