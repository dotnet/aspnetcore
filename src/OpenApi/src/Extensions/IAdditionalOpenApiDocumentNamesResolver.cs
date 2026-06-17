// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OpenApi;

/// <summary>
/// An interface that lets users provide additional OpenAPI document names.
/// </summary>
public interface IAdditionalOpenApiDocumentNamesResolver
{
    /// <summary>
    /// Gets the additional document names.
    /// </summary>
    IEnumerable<string> ResolveDocumentNames();
}
