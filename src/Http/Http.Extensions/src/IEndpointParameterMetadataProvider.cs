// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Metadata;

/// <summary>
/// Indicates that a type provides a static method that provides <see cref="Endpoint"/> metadata when declared as the
/// parameter type of an <see cref="Endpoint"/> route handler delegate.
/// </summary>
public interface IEndpointParameterMetadataProvider
{
    /// <summary>
    /// Populates metadata for the related <see cref="Endpoint"/>.
    /// </summary>
    /// <remarks>
    /// This method is called by <see cref="RequestDelegateFactory"/> when creating a <see cref="RequestDelegate"/>.
    /// The <see cref="EndpointParameterMetadataContext.EndpointMetadata"/> property of <paramref name="parameterContext"/> will contain
    /// the initial metadata for the endpoint.<br />
    /// Add or remove objects on <see cref="EndpointParameterMetadataContext.EndpointMetadata"/> to affect the metadata of the endpoint.
    /// </remarks>
    /// <param name="parameterContext">The <see cref="EndpointParameterMetadataContext"/>.</param>
    static abstract void PopulateMetadata(EndpointParameterMetadataContext parameterContext);
}
