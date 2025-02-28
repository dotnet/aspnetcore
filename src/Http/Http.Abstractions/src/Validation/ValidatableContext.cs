// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.AspNetCore.Http.Validation;

/// <summary>
/// Represents the context for validating a validatable object.
/// </summary>
public sealed class ValidatableContext
{
    public ValidationContext? ValidationContext { get; set; }
    public string Prefix { get; set; } = string.Empty;
    public required ValidationOptions ValidationOptions { get; set; }
    public Dictionary<string, string[]>? ValidationErrors { get; set; }

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
            ValidationErrors[key] = new string[existingErrors.Length + errors.Length];
            existingErrors.CopyTo(ValidationErrors[key], 0);
            errors.CopyTo(ValidationErrors[key], existingErrors.Length);
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
