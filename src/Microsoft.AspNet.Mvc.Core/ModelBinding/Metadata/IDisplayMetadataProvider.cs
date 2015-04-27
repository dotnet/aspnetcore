// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding.Metadata
{
    /// <summary>
    /// Provides <see cref="DisplayMetadata"/> for a <see cref="DefaultModelMetadata"/>.
    /// </summary>
    public interface IDisplayMetadataProvider : IMetadataDetailsProvider
    {
        /// <summary>
        /// Gets the values for properties of <see cref="DisplayMetadata"/>. 
        /// </summary>
        /// <param name="context">The <see cref="DisplayMetadataProviderContext"/>.</param>
        void GetDisplayMetadata([NotNull] DisplayMetadataProviderContext context);
    }
}