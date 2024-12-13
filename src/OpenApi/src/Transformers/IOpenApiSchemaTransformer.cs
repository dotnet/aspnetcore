// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.OpenApi.Models;

namespace Microsoft.AspNetCore.OpenApi;

/// <summary>
/// Represents a transformer that can be used to modify an OpenAPI schema.
/// </summary>
public interface IOpenApiSchemaTransformer
{
    /// <summary>
    /// Transforms the specified OpenAPI schema.
    /// </summary>
    /// <param name="schema">The <see cref="OpenApiSchema"/> to modify.</param>
    /// <param name="context">The <see cref="OpenApiSchemaTransformerContext"/> associated with the <see paramref="schema"/>.</param>
    /// <param name="cancellationToken">The cancellation token to use.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken);
}
