// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi;

namespace Microsoft.AspNetCore.OpenApi;

/// <summary>
/// Options to support the construction of OpenAPI documents.
/// </summary>
public class OpenApiOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OpenApiOptions"/> class
    /// with the default <see cref="ShouldInclude"/> predicate.
    /// </summary>
    public OpenApiOptions()
    {
        ShouldInclude = (description) => description.GroupName == null || description.GroupName == DocumentName;
    }

    /// <summary>
    /// The version of the OpenAPI specification to use. Defaults to <see cref="OpenApiSpecVersion.OpenApi3_0"/>.
    /// </summary>
    public OpenApiSpecVersion OpenApiVersion { get; set; } = OpenApiSpecVersion.OpenApi3_0;

    /// <summary>
    /// The name of the OpenAPI document this <see cref="OpenApiOptions"/> instance is associated with.
    /// </summary>
    public string DocumentName { get; internal set; } = OpenApiConstants.DefaultDocumentName;

    /// <summary>
    /// A delegate to determine whether a given <see cref="ApiDescription"/> should be included in the given OpenAPI document.
    /// </summary>
    public Func<ApiDescription, bool> ShouldInclude { get; set; }
}
