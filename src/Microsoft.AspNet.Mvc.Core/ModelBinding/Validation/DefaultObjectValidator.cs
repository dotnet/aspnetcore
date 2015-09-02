// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    /// <summary>
    /// The default implementation of <see cref="IObjectModelValidator"/>.
    /// </summary>
    public class DefaultObjectValidator : IObjectModelValidator
    {
        private readonly IList<IExcludeTypeValidationFilter> _excludeFilters;
        private readonly IModelMetadataProvider _modelMetadataProvider;

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultObjectValidator"/>.
        /// </summary>
        /// <param name="excludeFilters"><see cref="IExcludeTypeValidationFilter"/>s that determine
        /// types to exclude from validation.</param>
        /// <param name="modelMetadataProvider">The <see cref="IModelMetadataProvider"/>.</param>
        public DefaultObjectValidator(
            IList<IExcludeTypeValidationFilter> excludeFilters,
            IModelMetadataProvider modelMetadataProvider)
        {
            if (excludeFilters == null)
            {
                throw new ArgumentNullException(nameof(excludeFilters));
            }

            if (modelMetadataProvider == null)
            {
                throw new ArgumentNullException(nameof(modelMetadataProvider));
            }

            _modelMetadataProvider = modelMetadataProvider;
            _excludeFilters = excludeFilters;
        }

        /// <inheritdoc />
        public void Validate(
            IModelValidatorProvider validatorProvider,
            ModelStateDictionary modelState,
            ValidationStateDictionary validationState,
            string prefix,
            object model)
        {
            if (validatorProvider == null)
            {
                throw new ArgumentNullException(nameof(validatorProvider));
            }

            if (modelState == null)
            {
                throw new ArgumentNullException(nameof(modelState));
            }

            var visitor = new ValidationVisitor(
                validatorProvider,
                _excludeFilters,
                modelState,
                validationState);

            var metadata = model == null ? null : _modelMetadataProvider.GetMetadataForType(model.GetType());
            visitor.Validate(metadata, prefix, model);
        }
    }
}
