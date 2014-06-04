// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// A <see cref="IModelValidator"/> to represent an error. This validator will always throw an exception regardless 
    /// of the actual model value.
    /// This is used to perform meta-validation - that is to verify the validation attributes make sense.
    /// </summary>
    public class ErrorModelValidator : IModelValidator
    {
        private readonly string _errorMessage;

        public ErrorModelValidator([NotNull] string errorMessage)
        {
            _errorMessage = errorMessage;
        }

        public bool IsRequired
        {
            get { return false; }
        }

        public IEnumerable<ModelValidationResult> Validate(ModelValidationContext context)
        {
            throw new InvalidOperationException(_errorMessage);
        }
    }
}
