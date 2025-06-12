// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Validation;

/// <summary>
/// Represents the context of a validation error.
/// </summary>
/// <param name="name">The name of the property or parameter that caused the validation error.</param>
/// <param name="key">The full path from the root object to the property or parameter that caused the validation error.</param>
/// <param name="errors">The list of error messages associated with the validation error.</param>
/// <param name="container">The container object of the validated property.</param>
public readonly struct ValidationErrorContext(string name, string key, IReadOnlyList<string> errors, object? container)
{
    /// <summary>
    /// Gets the name of the property or parameter that caused the validation error.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the full path from the root object to the property or parameter that caused the validation error.
    /// </summary>
    public string Path { get; } = key;

    /// <summary>
    /// Gets the list of error messages associated with the validation error.
    /// </summary>
    public IReadOnlyList<string> Errors { get; } = errors;

    /// <summary>
    /// Gets a reference to the container object of the validated property.
    /// </summary>
    public object? Container { get; } = container;
}
