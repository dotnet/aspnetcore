// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation
{
    /// <summary>
    /// The default implementation of <see cref="IObjectModelValidator"/>.
    /// </summary>
    internal class DefaultObjectValidator : ObjectModelValidator
    {
        private readonly MvcOptions _mvcOptions;

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultObjectValidator"/>.
        /// </summary>
        /// <param name="modelMetadataProvider">The <see cref="IModelMetadataProvider"/>.</param>
        /// <param name="validatorProviders">The list of <see cref="IModelValidatorProvider"/>.</param>
        /// <param name="mvcOptions">Accessor to <see cref="MvcOptions"/>.</param>
        public DefaultObjectValidator(
            IModelMetadataProvider modelMetadataProvider,
            IList<IModelValidatorProvider> validatorProviders,
            MvcOptions mvcOptions)
            : base(modelMetadataProvider, validatorProviders)
        {
            _mvcOptions = mvcOptions;
        }

        public override ValidationVisitor GetValidationVisitor(
            ActionContext actionContext,
            IModelValidatorProvider validatorProvider,
            ValidatorCache validatorCache,
            IModelMetadataProvider metadataProvider,
            ValidationStateDictionary validationState)
        {
            var visitor = new ValidationVisitor(
                actionContext,
                validatorProvider,
                validatorCache,
                metadataProvider,
                validationState)
            {
                MaxValidationDepth = _mvcOptions.MaxValidationDepth,
            };

            return visitor;
        }
    }
}
