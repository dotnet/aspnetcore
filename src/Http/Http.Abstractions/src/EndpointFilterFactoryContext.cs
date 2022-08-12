// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Represents the information accessible via the route handler filter
/// API when the user is constructing a new route handler.
/// </summary>
public sealed class EndpointFilterFactoryContext
{
    /// <summary>
    /// The <see cref="MethodInfo"/> associated with the current route handler, <see cref="RequestDelegate"/> or MVC action.
    /// </summary>
    /// <remarks>
    /// In the future this could support more endpoint types.
    /// </remarks>
    public required MethodInfo MethodInfo { get; init; }

    /// <summary>
    /// The <see cref="EndpointBuilder"/> associated with the current endpoint being filtered.
    /// </summary>
    public required EndpointBuilder EndpointBuilder { get; init; }
}
