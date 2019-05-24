// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata
{
    /// <summary>
    /// Provides <see cref="ValidationMetadata"/> for a <see cref="DefaultModelMetadata"/>.
    /// </summary>
    public interface IValidationMetadataProvider : IMetadataDetailsProvider
    {
        /// <summary>
        /// Gets the values for properties of <see cref="ValidationMetadata"/>. 
        /// </summary>
        /// <param name="context">The <see cref="ValidationMetadataProviderContext"/>.</param>
        void CreateValidationMetadata(ValidationMetadataProviderContext context);
    }
}