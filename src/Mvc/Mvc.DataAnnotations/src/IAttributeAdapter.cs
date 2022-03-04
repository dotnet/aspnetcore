// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations
{
    /// <summary>
    /// Interface so that adapters provide their relevant values to error messages.
    /// </summary>
    public interface IAttributeAdapter : IClientModelValidator
    {
        /// <summary>
        /// Gets the error message.
        /// </summary>
        /// <param name="validationContext">The context to use in message creation.</param>
        /// <returns>The localized error message.</returns>
        string GetErrorMessage(ModelValidationContextBase validationContext);
    }
}
