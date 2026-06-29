// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OpenApi;

/// <summary>
/// Represents an initializer that can be used to modify an OpenAPI document before paths, operations, and components are generated.
/// </summary>
public interface IOpenApiDocumentInitializer
{
    /// <summary>
    /// Initializes the specified OpenAPI document.
    /// </summary>
    /// <param name="document">The <see cref="OpenApiDocument"/> to modify.</param>
    /// <param name="context">The <see cref="OpenApiDocumentTransformerContext"/> associated with the <paramref name="document"/>.</param>
    /// <param name="cancellationToken">The cancellation token to use.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    Task InitializeAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken);
}
