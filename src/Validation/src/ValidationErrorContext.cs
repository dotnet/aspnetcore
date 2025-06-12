// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Validation;

/// <summary>
/// Represents the context of a validation error.
/// </summary>
public readonly struct ValidationErrorContext
{
    /// <summary>
    /// Gets the name of the property or parameter that caused the validation error.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the full path from the root object to the property or parameter that caused the validation error.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets the list of error messages associated with the validation error.
    /// </summary>
    public required IReadOnlyList<string> Errors { get; init; }

    /// <summary>
    /// Gets a reference to the container object of the validated property.
    /// </summary>
    public required object? Container { get; init; }
}
