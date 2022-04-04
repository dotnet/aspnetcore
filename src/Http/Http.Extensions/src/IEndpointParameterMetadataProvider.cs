// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Indicates that a type provides a static method that returns <see cref="Endpoint"/> metadata when declared as the
/// parameter type of an <see cref="Endpoint"/> route handler delegate.
/// </summary>
public interface IEndpointParameterMetadataProvider
{
    /// <summary>
    /// Populates metadata for the related <see cref="Endpoint"/>.
    /// </summary>
    /// <param name="parameterContext">The <see cref="EndpointParameterMetadataContext"/>.</param>
    static abstract void PopulateMetadata(EndpointParameterMetadataContext parameterContext);
}
