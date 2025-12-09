// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OpenApi;

internal static class TypeBasedTransformerExtensions
{
    /// <summary>
    /// Supports disposing of factory-based transformers that implement <see cref="IDisposable"/> or <see cref="IAsyncDisposable"/>
    /// after a given OpenAPI document generation request has been completed.
    /// </summary>
    /// <remarks>
    /// This method is intended to be invoked on <see cref="TypeBasedOpenApiOperationTransformer" /> and <see cref="TypeBasedOpenApiSchemaTransformer" />.
    /// instances which can be invoked multiple times within the same document generation request.
    /// </remarks>
    public static async Task FinalizeTransformer<ITransformer>(this ITransformer transformer)
    {
        if (transformer is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (transformer is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
