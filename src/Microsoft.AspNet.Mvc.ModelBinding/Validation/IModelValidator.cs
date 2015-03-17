// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    /// <summary>
    /// Validates a model value.
    /// </summary>
    public interface IModelValidator
    {
        /// <summary>
        /// Gets a value indicating whether or not this validator validates that a required value
        /// has been provided for the model.
        /// </summary>
        bool IsRequired { get; }

        /// <summary>
        /// Validates the model value.
        /// </summary>
        /// <param name="context">The <see cref="ModelValidationContext"/>.</param>
        /// <returns>
        /// A list of <see cref="ModelValidationResult"/> indicating the results of validating the model value.
        /// </returns>
        IEnumerable<ModelValidationResult> Validate(ModelValidationContext context);
    }
}
