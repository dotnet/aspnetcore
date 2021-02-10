// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation
{
    /// <summary>
    /// A context object for <see cref="IModelValidator"/>.
    /// </summary>
    public class ModelValidationContext : ModelValidationContextBase
    {
        /// <summary>
        /// Create a new instance of <see cref="ModelValidationContext"/>.
        /// </summary>
        /// <param name="actionContext">The <see cref="ActionContext"/> for validation.</param>
        /// <param name="modelMetadata">The <see cref="ModelMetadata"/> for validation.</param>
        /// <param name="metadataProvider">The <see cref="IModelMetadataProvider"/> to be used in validation.</param>
        /// <param name="container">The model container.</param>
        /// <param name="model">The model to be validated.</param>
        public ModelValidationContext(
            ActionContext actionContext,
            ModelMetadata modelMetadata,
            IModelMetadataProvider metadataProvider,
            object? container,
            object? model)
            : base(actionContext, modelMetadata, metadataProvider)
        {
            Container = container;
            Model = model;
        }

        /// <summary>
        /// Gets the model object.
        /// </summary>
        public object? Model { get; }

        /// <summary>
        /// Gets the model container object.
        /// </summary>
        public object? Container { get; }
    }
}
