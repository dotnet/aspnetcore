// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding.Metadata
{
    /// <summary>
    /// A context for an <see cref="IBindingMetadataProvider"/>.
    /// </summary>
    public class BindingMetadataProviderContext
    {
        /// <summary>
        /// Creates a new <see cref="BindingMetadataProviderContext"/>.
        /// </summary>
        /// <param name="key">The <see cref="ModelMetadataIdentity"/> for the <see cref="ModelMetadata"/>.</param>
        /// <param name="attributes">The attributes for the <see cref="ModelMetadata"/>.</param>
        public BindingMetadataProviderContext(
            [NotNull] ModelMetadataIdentity key, 
            [NotNull] IReadOnlyList<object> attributes)
        {
            Key = key;
            Attributes = attributes;
            BindingMetadata = new BindingMetadata();
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
        /// Gets the <see cref="Metadata.BindingMetadata"/>.
        /// </summary>
        public BindingMetadata BindingMetadata { get; }
    }
}