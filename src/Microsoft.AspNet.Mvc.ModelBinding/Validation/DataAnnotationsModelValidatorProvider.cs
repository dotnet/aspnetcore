// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    /// <summary>
    /// An implementation of <see cref="IModelValidatorProvider"/> which provides validators
    /// for attributes which derive from <see cref="ValidationAttribute"/>. It also provides
    /// a validator for types which implement <see cref="IValidatableObject"/>.
    /// </summary>
    public class DataAnnotationsModelValidatorProvider : IModelValidatorProvider
    {
        public void GetValidators(ModelValidatorProviderContext context)
        {
            foreach (var attribute in context.ValidatorMetadata.OfType<ValidationAttribute>())
            {
                context.Validators.Add(new DataAnnotationsModelValidator(attribute));
            }

            // Produce a validator if the type supports IValidatableObject
            if (typeof(IValidatableObject).IsAssignableFrom(context.ModelMetadata.ModelType))
            {
                context.Validators.Add(new ValidatableObjectAdapter());
            }
        }
    }
}
