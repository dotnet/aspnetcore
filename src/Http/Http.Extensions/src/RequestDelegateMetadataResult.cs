// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// The metadata inferred by <see cref="RequestDelegateFactory.InferMetadata(System.Reflection.MethodInfo, Microsoft.AspNetCore.Http.RequestDelegateFactoryOptions?)"/>.
/// <see cref="RequestDelegateFactoryOptions.EndpointBuilder"/> will be automatically populated with this metadata if provided.
/// If this is passed to <see cref="RequestDelegateFactory.Create(Delegate, Microsoft.AspNetCore.Http.RequestDelegateFactoryOptions?, Microsoft.AspNetCore.Http.RequestDelegateMetadataResult?)"/>,
/// it will not repeat metadata inference. Any metadata that would be inferred should already be stored in the EndpointBuilder.
/// </summary>
public sealed class RequestDelegateMetadataResult
{
    /// <summary>
    /// Gets endpoint metadata inferred from creating the <see cref="RequestDelegate" />. If a non-null
    /// RequestDelegateFactoryOptions.EndpointMetadata list was passed in, this will be the same instance.
    /// </summary>
    public required IReadOnlyList<object> EndpointMetadata { get; init; }

    // This internal cached context avoids redoing unnecessary reflection in Create that was already done in InferMetadata.
    // InferMetadata currently does more work than it needs to building up expression trees, but the expectation is that InferMetadata will usually be followed by Create.
    // The property is typed as object to avoid having a dependency System.Linq.Expressions. The value is RequestDelegateFactoryContext.
    internal object? CachedFactoryContext { get; set; }
}
