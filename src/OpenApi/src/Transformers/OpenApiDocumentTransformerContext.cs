// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace Microsoft.AspNetCore.OpenApi;

/// <summary>
/// Represents the context in which an OpenAPI document transformer is executed.
/// </summary>
public sealed class OpenApiDocumentTransformerContext
{
    /// <summary>
    /// Gets the name of the associated OpenAPI document.
    /// </summary>
    public required string DocumentName { get; init; }

    /// <summary>
    /// Gets the API description groups associated with current document.
    /// </summary>
    public required IReadOnlyList<ApiDescriptionGroup> DescriptionGroups { get; init; }

    /// <summary>
    /// Gets the application services associated with current document.
    /// </summary>
    public required IServiceProvider ApplicationServices { get; init; }
}
