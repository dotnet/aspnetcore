// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Metadata;

/// <summary>
/// Defines a contract used to specify an example for a parameter, request body, or response
/// associated with an <see cref="Endpoint"/>.
/// </summary>
public interface IExampleMetadata
{
    /// <summary>
    /// Gets the summary associated with the example.
    /// </summary>
    string Summary { get; }

    /// <summary>
    /// Gets the description associated with the example.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets an example value associated with an example.
    /// This property is mutually exclusibe with <see cref="ExternalValue"/>.
    /// </summary>
    object? Value { get; }

    /// <summary>
    /// Gets a reference to an external value associated with an example.
    /// This property is mutually exclusibe with <see cref="Value"/>.
    /// </summary>
    string? ExternalValue { get; }

    /// <summary>
    /// If the example targets a parameter, gets
    /// or sets the name of the parameter associated with the target.
    /// </summary>
    string? ParameterName { get; set; }
}
