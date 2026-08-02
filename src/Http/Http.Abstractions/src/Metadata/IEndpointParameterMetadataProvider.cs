// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Http.Metadata;

/// <summary>
/// Indicates that a type provides a static method that provides <see cref="Endpoint"/> metadata when declared as the
/// parameter type of an <see cref="Endpoint"/> route handler delegate.
/// </summary>
public interface IEndpointParameterMetadataProvider
{
    /// <summary>
    /// Populates metadata for the related <see cref="Endpoint"/> and <see cref="ParameterInfo"/>.
    /// </summary>
    /// <remarks>
    /// This method is called by RequestDelegateFactory when creating a <see cref="RequestDelegate"/> and by MVC when creating endpoints for controller actions.
    /// This is called for each parameter of the route handler or action with a declared type implementing this interface.
    /// Add or remove objects on the <see cref="EndpointBuilder.Metadata"/> property of the <paramref name="builder"/> to modify the <see cref="Endpoint.Metadata"/> being built.
    /// </remarks>
    /// <param name="parameter">The <see cref="ParameterInfo"/> of the route handler delegate or MVC Action of the endpoint being created.</param>
    /// <param name="builder">The <see cref="EndpointBuilder"/> used to construct the endpoint for the given <paramref name="parameter"/>.</param>
    static abstract void PopulateMetadata(ParameterInfo parameter, EndpointBuilder builder);
}
