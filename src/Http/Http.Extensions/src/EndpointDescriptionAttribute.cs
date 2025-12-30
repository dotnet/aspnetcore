// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Http.Metadata;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Specifies a description for the endpoint in <see cref="Endpoint.Metadata"/>.
/// </summary>
/// <remarks>
/// The OpenAPI specification supports a description attribute on operations and parameters that
/// can be used to annotate endpoints with detailed, multiline descriptors of their behavior.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
[DebuggerDisplay("{ToString(),nq}")]
public sealed class EndpointDescriptionAttribute : Attribute, IEndpointDescriptionMetadata
{
    /// <summary>
    /// Initializes an instance of the <see cref="EndpointDescriptionAttribute"/>.
    /// </summary>
    /// <param name="description">The description associated with the endpoint or parameter.</param>
    public EndpointDescriptionAttribute(string description)
    {
        Description = description;
    }

    /// <inheritdoc />
    public string Description { get; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"Description: {Description ?? "(null)"}";
    }
}
