// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Builds conventions that will be used for customization of <see cref="EndpointBuilder"/> instances.
/// </summary>
/// <remarks>
/// This interface is used at application startup to customize endpoints for the application.
/// </remarks>
public interface IEndpointConventionBuilder
{
    /// <summary>
    /// Adds the specified convention to the builder. Conventions are used to customize <see cref="EndpointBuilder"/> instances.
    /// </summary>
    /// <param name="convention">The convention to add to the builder.</param>
    void Add(Action<EndpointBuilder> convention);

    /// <summary>
    /// Registers the specified convention for execution after conventions registered
    /// via <see cref="Add(Action{EndpointBuilder})"/>
    /// </summary>
    /// <param name="finallyConvention">The convention to add to the builder.</param>
    void Finally(Action<EndpointBuilder> finallyConvention) => throw new NotImplementedException();
}
