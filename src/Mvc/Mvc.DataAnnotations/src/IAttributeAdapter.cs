// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations;

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
