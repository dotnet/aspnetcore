// Copyright (c) .NET Foundation. All rights reserved.
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
            //Perf: Avoid allocations here
            for (var i = 0; i < context.ValidatorMetadata.Count; i++)
            {
                var validator = context.ValidatorMetadata[i] as IModelValidator;
                if (validator != null)
                {
                    context.Validators.Add(validator);
                }
            }
        }
    }
}