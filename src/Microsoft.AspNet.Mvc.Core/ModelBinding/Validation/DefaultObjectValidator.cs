// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    /// <summary>
    /// The default implementation of <see cref="IObjectModelValidator"/>.
    /// </summary>
    public class DefaultObjectValidator : IObjectModelValidator
    {
        private readonly IModelMetadataProvider _modelMetadataProvider;

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultObjectValidator"/>.
        /// </summary>
        /// <param name="modelMetadataProvider">The <see cref="IModelMetadataProvider"/>.</param>
        public DefaultObjectValidator(
            IModelMetadataProvider modelMetadataProvider)
        {
            if (modelMetadataProvider == null)
            {
                throw new ArgumentNullException(nameof(modelMetadataProvider));
            }

            _modelMetadataProvider = modelMetadataProvider;
        }

        /// <inheritdoc />
        public void Validate(
            ActionContext actionContext,
            IModelValidatorProvider validatorProvider,
            ValidationStateDictionary validationState,
            string prefix,
            object model)
        {
            if (actionContext == null)
            {
                throw new ArgumentNullException(nameof(actionContext));
            }

            if (validatorProvider == null)
            {
                throw new ArgumentNullException(nameof(validatorProvider));
            }

            var visitor = new ValidationVisitor(
                actionContext,
                validatorProvider,
                _modelMetadataProvider,
                validationState);

            var metadata = model == null ? null : _modelMetadataProvider.GetMetadataForType(model.GetType());
            visitor.Validate(metadata, prefix, model);
        }
    }
}
