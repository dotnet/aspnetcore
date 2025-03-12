// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.AspNetCore.Http.Validation;

/// <summary>
/// Represents the context for validating a validatable object.
/// </summary>
public sealed class ValidateContext
{
    /// <summary>
    /// Gets or sets the validation context used for validating objects that implement <see cref="IValidatableObject"/> or have <see cref="ValidationAttribute"/>.
    /// This context provides access to service provider and other validation metadata.
    /// </summary>
    public ValidationContext? ValidationContext { get; set; }

    /// <summary>
    /// Gets or sets the prefix used to identify the current object being validated in a complex object graph.
    /// This is used to build property paths in validation error messages (e.g., "Customer.Address.Street").
    /// </summary>
    public string CurrentValidationPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the validation options that control validation behavior,
    /// including validation depth limits and resolver registration.
    /// </summary>
    public required ValidationOptions ValidationOptions { get; set; }

    /// <summary>
    /// Gets or sets the dictionary of validation errors collected during validation.
    /// Keys are property names or paths, and values are arrays of error messages.
    /// In the default implementation, this dictionary is initialized when the first error is added.
    /// </summary>
    public Dictionary<string, string[]>? ValidationErrors { get; set; }

    /// <summary>
    /// Gets or sets the current depth in the validation hierarchy.
    /// This is used to prevent stack overflows from circular references.
    /// </summary>
    public int CurrentDepth { get; set; }

    internal void AddValidationError(string key, string[] error)
    {
        ValidationErrors ??= [];

        ValidationErrors[key] = error;
    }

    internal void AddOrExtendValidationErrors(string key, string[] errors)
    {
        ValidationErrors ??= [];

        if (ValidationErrors.TryGetValue(key, out var existingErrors))
        {
            var newErrors = new string[existingErrors.Length + errors.Length];
            existingErrors.CopyTo(newErrors, 0);
            errors.CopyTo(newErrors, existingErrors.Length);
            ValidationErrors[key] = newErrors;
        }
        else
        {
            ValidationErrors[key] = errors;
        }
    }

    internal void AddOrExtendValidationError(string key, string error)
    {
        ValidationErrors ??= [];

        if (ValidationErrors.TryGetValue(key, out var existingErrors) && !existingErrors.Contains(error))
        {
            ValidationErrors[key] = [.. existingErrors, error];
        }
        else
        {
            ValidationErrors[key] = [error];
        }
    }
}
