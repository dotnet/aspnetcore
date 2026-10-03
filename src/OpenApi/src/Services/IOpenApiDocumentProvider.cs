// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.OpenApi;

/// <summary>
/// Represents a provider for OpenAPI documents that can be used by consumers to
/// retrieve generated OpenAPI documents at runtime.
/// </summary>
public interface IOpenApiDocumentProvider
{
    /// <summary>
    /// Gets the OpenAPI document.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the OpenAPI document.</returns>
    /// <remarks>
    /// This method is typically used by consumers to retrieve the OpenAPI document. The generated document
    /// may not contain the appropriate servers information since it can be instantiated outside the context
    /// of an HTTP request. In these scenarios, the <see cref="OpenApiDocument"/> can be modified to
    /// include the appropriate servers information.
    /// </remarks>
    /// <remarks>
    /// Any OpenAPI transformers registered in the <see cref="OpenApiOptions"/> instance associated with
    /// this document will be applied to the document before it is returned.
    /// </remarks>
    Task<OpenApiDocument> GetOpenApiDocumentAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents an OpenAPI document provider that can target a requested OpenAPI specification version.
/// </summary>
[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public interface IOpenApiVersionedDocumentProvider : IOpenApiDocumentProvider
{
    /// <summary>
    /// Gets the OpenAPI document for the specified OpenAPI specification version.
    /// </summary>
    /// <param name="openApiVersion">The OpenAPI specification version to target while generating the document.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the OpenAPI document.</returns>
    /// <remarks>
    /// Any OpenAPI transformers registered in the <see cref="OpenApiOptions"/> instance associated with
    /// this document will be applied to the document before it is returned.
    /// </remarks>
    /// <remarks>
    /// Transformers can produce version-specific output based on the specified version. To target a different
    /// OpenAPI version, generate a new document with this method rather than serializing the returned document
    /// using a different version.
    /// </remarks>
    Task<OpenApiDocument> GetOpenApiDocumentForVersionAsync(OpenApiSpecVersion openApiVersion, CancellationToken cancellationToken = default);
}
