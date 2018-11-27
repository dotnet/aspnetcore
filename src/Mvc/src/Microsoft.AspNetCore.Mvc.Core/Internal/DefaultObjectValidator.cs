// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    /// <summary>
    /// The default implementation of <see cref="IObjectModelValidator"/>.
    /// </summary>
    public class DefaultObjectValidator : ObjectModelValidator
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DefaultObjectValidator"/>.
        /// </summary>
        /// <param name="modelMetadataProvider">The <see cref="IModelMetadataProvider"/>.</param>
        /// <param name="validatorProviders">The list of <see cref="IModelValidatorProvider"/>.</param>
        public DefaultObjectValidator(
            IModelMetadataProvider modelMetadataProvider,
            IList<IModelValidatorProvider> validatorProviders)
            : base(modelMetadataProvider, validatorProviders)
        {
        }

        public override ValidationVisitor GetValidationVisitor(
            ActionContext actionContext,
            IModelValidatorProvider validatorProvider,
            ValidatorCache validatorCache,
            IModelMetadataProvider metadataProvider,
            ValidationStateDictionary validationState)
        {
            return new ValidationVisitor(
                actionContext,
                validatorProvider,
                validatorCache,
                metadataProvider,
                validationState);
        }
    }
}
