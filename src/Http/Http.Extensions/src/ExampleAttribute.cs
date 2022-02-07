// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Metadata;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Specifies an example associated with a parameter, request body, or response of an <see cref="Endpoint"/>.
/// </summary>
/// <remarks>
/// The OpenAPI specification supports an examples property that can be used to annotate
/// request bodies, parameters, and responses with examples of the data type associated
/// with each element.
/// </remarks>
[AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
public sealed class ExampleAttribute : Attribute, IExampleMetadata
{
    /// <summary>
    /// Initializes an instance of the <see cref="ExampleAttribute"/> given
    /// a <see cref="Value"/>.
    /// </summary>
    public ExampleAttribute(string summary, string description, object value)
    {
        Summary = summary;
        Description = description;
        Value = value;
    }

    /// <summary>
    /// Initializes an instance of the <see cref="ExampleAttribute"/> given
    /// an <see cref="ExternalValue"/>.
    /// </summary>
    public ExampleAttribute(string summary, string description, string externalValue)
    {
        Summary = summary;
        Description = description;
        ExternalValue = externalValue;
    }

    /// <inheritdoc />
    public string Description { get; }

    /// <inheritdoc />
    public string Summary { get; }

    /// <inheritdoc />
    public object? Value { get; }

    /// <inheritdoc />
    public string? ExternalValue { get; }

    /// <inheritdoc />
    public string? ParameterName { get; set; }
}
