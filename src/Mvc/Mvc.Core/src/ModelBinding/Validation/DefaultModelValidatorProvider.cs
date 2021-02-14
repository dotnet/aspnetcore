// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation
{
    /// <summary>
    /// A default <see cref="IModelValidatorProvider"/>.
    /// </summary>
    /// <remarks>
    /// The <see cref="DefaultModelValidatorProvider"/> provides validators from <see cref="IModelValidator"/>
    /// instances in <see cref="ModelBinding.ModelMetadata.ValidatorMetadata"/>.
    /// </remarks>
    internal class DefaultModelValidatorProvider : IMetadataBasedModelValidatorProvider
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

                if (validatorItem.ValidatorMetadata is IModelValidator validator)
                {
                    validatorItem.Validator = validator;
                    validatorItem.IsReusable = true;
                }
            }
        }

        public bool HasValidators(Type modelType, IList<object> validatorMetadata)
        {
            for (var i = 0; i < validatorMetadata.Count; i++)
            {
                if (validatorMetadata[i] is IModelValidator)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
