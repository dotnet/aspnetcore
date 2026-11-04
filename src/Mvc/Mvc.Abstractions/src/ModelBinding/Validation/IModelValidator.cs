// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

/// <summary>
/// Validates a model value.
/// </summary>
public interface IModelValidator
{
    /// <summary>
    /// Validates the model value.
    /// </summary>
    /// <param name="context">The <see cref="ModelValidationContext"/>.</param>
    /// <returns>
    /// A list of <see cref="ModelValidationResult"/> indicating the results of validating the model value.
    /// </returns>
    IEnumerable<ModelValidationResult> Validate(ModelValidationContext context);
}
