// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    /// <summary>
    /// The context for client-side model validation.
    /// </summary>
    public class ClientModelValidationContext : ModelValidationContextBase
    {
        /// <summary>
        /// Create a new instance of <see cref="ClientModelValidationContext"/>.
        /// </summary>
        /// <param name="actionContext">The <see cref="ActionContext"/> for validation.</param>
        /// <param name="metadata">The <see cref="ModelMetadata"/> for validation.</param>
        /// <param name="metadataProvider">The <see cref="IModelMetadataProvider"/> to be used in validation.</param>
        public ClientModelValidationContext(
            ActionContext actionContext,
            ModelMetadata metadata,
            IModelMetadataProvider metadataProvider)
            : base(actionContext, metadata, metadataProvider)
        {
        }
    }
}
