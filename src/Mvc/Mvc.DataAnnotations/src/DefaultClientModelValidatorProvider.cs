// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations
{
    /// <summary>
    /// A default implementation of <see cref="IClientModelValidatorProvider"/>.
    /// </summary>
    /// <remarks>
    /// The <see cref="DefaultClientModelValidatorProvider"/> provides validators from 
    /// <see cref="IClientModelValidator"/> instances in <see cref="ModelBinding.ModelMetadata.ValidatorMetadata"/>.
    /// </remarks>
    internal class DefaultClientModelValidatorProvider : IClientModelValidatorProvider
    {
        /// <inheritdoc />
        public void CreateValidators(ClientValidatorProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // Perf: Avoid allocations
            for (var i = 0; i < context.Results.Count; i++)
            {
                var validatorItem = context.Results[i];
                // Don't overwrite anything that was done by a previous provider.
                if (validatorItem.Validator != null)
                {
                    continue;
                }

                var validator = validatorItem.ValidatorMetadata as IClientModelValidator;
                if (validator != null)
                {
                    validatorItem.Validator = validator;
                    validatorItem.IsReusable = true;
                }
            }
        }
    }
}
