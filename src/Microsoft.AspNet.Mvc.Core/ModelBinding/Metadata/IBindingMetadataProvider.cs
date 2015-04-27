// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding.Metadata
{
    /// <summary>
    /// Provides <see cref="BindingMetadata"/> for a <see cref="DefaultModelMetadata"/>.
    /// </summary>
    public interface IBindingMetadataProvider : IMetadataDetailsProvider
    {
        /// <summary>
        /// Gets the values for properties of <see cref="DisplayMetadata"/>. 
        /// </summary>
        /// <param name="context">The <see cref="BindingMetadataProviderContext"/>.</param>
        void GetBindingMetadata([NotNull] BindingMetadataProviderContext context);
    }
}