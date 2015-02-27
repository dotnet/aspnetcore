// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding.Metadata
{
    /// <summary>
    /// A context for and <see cref="IDisplayMetadataProvider"/>.
    /// </summary>
    public class DisplayMetadataProviderContext
    {
        /// <summary>
        /// Creates a new <see cref="DisplayMetadataProviderContext"/>.
        /// </summary>
        /// <param name="key">The <see cref="ModelMetadataIdentity"/> for the <see cref="ModelMetadata"/>.</param>
        /// <param name="attributes">The attributes for the <see cref="ModelMetadata"/>.</param>
        public DisplayMetadataProviderContext(
            [NotNull] ModelMetadataIdentity key, 
            [NotNull] IReadOnlyList<object> attributes)
        {
            Key = key;
            Attributes = attributes;
            DisplayMetadata = new DisplayMetadata();
        }

        /// <summary>
        /// Gets the attributes.
        /// </summary>
        public IReadOnlyList<object> Attributes { get; }

        /// <summary>
        /// Gets the <see cref="ModelMetadataIdentity"/>.
        /// </summary>
        public ModelMetadataIdentity Key { get; }

        /// <summary>
        /// Gets the <see cref="Metadata.DisplayMetadata"/>.
        /// </summary>
        public DisplayMetadata DisplayMetadata { get; }
    }
}