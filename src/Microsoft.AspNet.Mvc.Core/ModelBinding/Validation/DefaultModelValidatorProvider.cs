// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    /// <summary>
    /// A default <see cref="IModelValidatorProvider"/>.
    /// </summary>
    /// <remarks>
    /// The <see cref="DefaultModelValidatorProvider"/> provides validators from <see cref="IModelValidator"/>
    /// instances in <see cref="ModelMetadata.ValidatorMetadata"/>.
    /// </remarks>
    public class DefaultModelValidatorProvider : IModelValidatorProvider
    {
        /// <inheritdoc />
        public void GetValidators(ModelValidatorProviderContext context)
        {
            foreach (var metadata in context.ValidatorMetadata)
            {
                var validator = metadata as IModelValidator;
                if (validator != null)
                {
                    context.Validators.Add(validator);
                }
            }
        }
    }
}