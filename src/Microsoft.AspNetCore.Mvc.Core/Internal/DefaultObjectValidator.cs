// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    /// <summary>
    /// The default implementation of <see cref="IObjectModelValidator"/>.
    /// </summary>
    public class DefaultObjectValidator : IObjectModelValidator
    {
        private readonly IModelMetadataProvider _modelMetadataProvider;
        private readonly ValidatorCache _validatorCache;
        private readonly IModelValidatorProvider _validatorProvider;

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultObjectValidator"/>.
        /// </summary>
        /// <param name="modelMetadataProvider">The <see cref="IModelMetadataProvider"/>.</param>
        /// <param name="validatorProviders">The list of <see cref="IModelValidatorProvider"/>.</param>
        public DefaultObjectValidator(
            IModelMetadataProvider modelMetadataProvider,
            IList<IModelValidatorProvider> validatorProviders)
        {
            if (modelMetadataProvider == null)
            {
                throw new ArgumentNullException(nameof(modelMetadataProvider));
            }

            if (validatorProviders == null)
            {
                throw new ArgumentNullException(nameof(validatorProviders));
            }

            _modelMetadataProvider = modelMetadataProvider;
            _validatorCache = new ValidatorCache();

            _validatorProvider = new CompositeModelValidatorProvider(validatorProviders);
        }

        /// <inheritdoc />
        public void Validate(
            ActionContext actionContext,
            ValidationStateDictionary validationState,
            string prefix,
            object model)
        {
            if (actionContext == null)
            {
                throw new ArgumentNullException(nameof(actionContext));
            }

            var visitor = new ValidationVisitor(
                actionContext,
                _validatorProvider,
                _validatorCache,
                _modelMetadataProvider,
                validationState);

            var metadata = model == null ? null : _modelMetadataProvider.GetMetadataForType(model.GetType());
            visitor.Validate(metadata, prefix, model);
        }
    }
}
