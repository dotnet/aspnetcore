// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    /// <summary>
    /// An implementation of <see cref="IClientModelValidatorProvider"/> which provides client validators
    /// for specific numeric types.
    /// </summary>
    public class NumericClientModelValidatorProvider : IClientModelValidatorProvider
    {
        /// <inheritdoc />
        public void GetValidators(ClientValidatorProviderContext context)
        {
            var type = context.ModelMetadata.ModelType;
            var typeToValidate = Nullable.GetUnderlyingType(type) ?? type;

            // Check only the numeric types for which we set type='text'.
            if (typeToValidate == typeof(float) ||
                typeToValidate == typeof(double) ||
                typeToValidate == typeof(decimal))
            {
                context.Validators.Add(new NumericClientModelValidator());
            }
        }
    }
}
