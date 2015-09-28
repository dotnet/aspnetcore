// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    /// <summary>
    /// A default implementation of <see cref="IClientModelValidatorProvider"/>.
    /// </summary>
    /// <remarks>
    /// The <see cref="DefaultClientModelValidatorProvider"/> provides validators from 
    /// <see cref="IClientModelValidator"/> instances in <see cref="ModelMetadata.ValidatorMetadata"/>.
    /// </remarks>
    public class DefaultClientModelValidatorProvider : IClientModelValidatorProvider
    {
        /// <inheritdoc />
        public void GetValidators(ClientValidatorProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // Perf: Avoid allocations
            for (var i = 0; i < context.ValidatorMetadata.Count; i++)
            {
                var validator = context.ValidatorMetadata[i] as IClientModelValidator;
                if (validator != null)
                {
                    context.Validators.Add(validator);
                }
            }
        }
    }
}