// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata
{
    /// <summary>
    /// Provides <see cref="BindingMetadata"/> for a <see cref="DefaultModelMetadata"/>.
    /// </summary>
    public interface IBindingMetadataProvider : IMetadataDetailsProvider
    {
        /// <summary>
        /// Sets the values for properties of <see cref="BindingMetadataProviderContext.BindingMetadata"/>. 
        /// </summary>
        /// <param name="context">The <see cref="BindingMetadataProviderContext"/>.</param>
        void CreateBindingMetadata(BindingMetadataProviderContext context);
    }
}
