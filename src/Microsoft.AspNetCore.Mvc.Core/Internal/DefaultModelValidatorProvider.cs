// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    /// <summary>
    /// A default <see cref="IModelValidatorProvider"/>.
    /// </summary>
    /// <remarks>
    /// The <see cref="DefaultModelValidatorProvider"/> provides validators from <see cref="IModelValidator"/>
    /// instances in <see cref="ModelBinding.ModelMetadata.ValidatorMetadata"/>.
    /// </remarks>
    public class DefaultModelValidatorProvider : IModelValidatorProvider
    {
        /// <inheritdoc />
        public void CreateValidators(ModelValidatorProviderContext context)
        {
            //Perf: Avoid allocations here
            for (var i = 0; i < context.Results.Count; i++)
            {
                var validatorItem = context.Results[i];

                // Don't overwrite anything that was done by a previous provider.
                if (validatorItem.Validator != null)
                {
                    continue;
                }

                var validator = validatorItem.ValidatorMetadata as IModelValidator;
                if (validator != null)
                {
                    validatorItem.Validator = validator;
                    validatorItem.IsReusable = true;
                }
            }
        }
    }
}