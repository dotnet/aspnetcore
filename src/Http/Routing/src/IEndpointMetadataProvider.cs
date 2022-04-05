// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Indicates that a type provides a static method that provides <see cref="Endpoint"/> metadata when declared as a parameter type or the
/// returned type of an <see cref="Endpoint"/> route handler delegate.
/// </summary>
public interface IEndpointMetadataProvider
{
    /// <summary>
    /// Populates metadata for the related <see cref="Endpoint"/>.
    /// </summary>
    /// <param name="context">The <see cref="EndpointMetadataContext"/>.</param>
    static abstract void PopulateMetadata(EndpointMetadataContext context);
}
