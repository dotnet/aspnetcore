// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata
{
    /// <summary>
    /// Provides <see cref="DisplayMetadata"/> for a <see cref="DefaultModelMetadata"/>.
    /// </summary>
    public interface IDisplayMetadataProvider : IMetadataDetailsProvider
    {
        /// <summary>
        /// Sets the values for properties of <see cref="DisplayMetadataProviderContext.DisplayMetadata"/>. 
        /// </summary>
        /// <param name="context">The <see cref="DisplayMetadataProviderContext"/>.</param>
        void CreateDisplayMetadata(DisplayMetadataProviderContext context);
    }
}
