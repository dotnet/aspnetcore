// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.AspNetCore.Http.Validation;

/// <summary>
/// Represents the context for validating a validatable object.
/// </summary>
public sealed class ValidatableContext(ValidationContext validationContext, string prefix, ValidationOptions validationOptions, Dictionary<string, string[]> validationErrors)
{
    public ValidationContext ValidationContext { get; } = validationContext;
    public string Prefix { get; set; } = prefix;
    public ValidationOptions ValidationOptions { get; } = validationOptions;
    public Dictionary<string, string[]> ValidationErrors { get; } = validationErrors;
}
