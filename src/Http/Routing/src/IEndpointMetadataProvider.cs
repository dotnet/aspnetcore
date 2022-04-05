// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Indicates that a type provides a static method that returns <see cref="Endpoint"/> metadata when declared as a parameter type,
/// <see cref="Attribute"/> type, or the returned type of an <see cref="Endpoint"/> route handler delegate.
/// </summary>
public interface IEndpointMetadataProvider
{
    /// <summary>
    /// Supplies objects to apply as metadata to the related <see cref="Endpoint"/>.
    /// </summary>
    /// <param name="context">The <see cref="EndpointMetadataContext"/>.</param>
    static abstract void PopulateMetadata(EndpointMetadataContext context);
}
