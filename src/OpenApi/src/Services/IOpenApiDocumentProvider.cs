// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
