// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OpenApi.Extensions;

/// <summary>
/// An interface that lets users provide the OpenAPI document names.
/// This interface is only relevant for build-time OpenAPI generation.
/// If an implementation of this interface is registered, then the documentName provided to AddOpenApi will be ignored.
/// </summary>
public interface IOpenApiDocumentNamesOverrideProvider
{
    /// <summary>
    /// Gets the document names to generate at build-time.
    /// </summary>
    public IEnumerable<string> DocumentNames { get; }
}
