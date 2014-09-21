// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// Validates the body parameter of an action after the parameter
    /// has been read by the Input Formatters.
    /// </summary>
    public interface IBodyModelValidator
    {
        /// <summary>
        /// Determines whether the Model is valid
        /// and adds any validation errors to the <see cref="ModelStateDictionary"/>
        /// </summary>
        /// <param name="modelValidaitonContext">The validation context which contains the model, metadata
        /// and the validator providers.</param>
        /// <param name="keyPrefix">The <see cref="string"/> to append to the key for any validation errors.</param>
        /// <returns><c>true</c>if the model is valid, <c>false</c> otherwise.</returns>
        bool Validate(ModelValidationContext modelValidationContext, string keyPrefix);
    }
}