// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.Extensions.Validation;

/// <summary>
/// Represents a validation error.
/// </summary>
[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public sealed class ValidationError
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
    /// Gets the error message associated with the validation error.
    /// </summary>
    public required string ErrorMessage { get; init; }

    /// <summary>
    /// Gets a reference to the container object of the validated property.
    /// </summary>
    public required object? Container { get; init; }

    private string GetDebuggerDisplay()
    {
        return $"{Path}: {ErrorMessage}";
    }
}
