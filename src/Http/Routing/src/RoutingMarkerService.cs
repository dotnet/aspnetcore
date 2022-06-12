// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// A marker class used to determine if all the routing services were added
/// to the <see cref="IServiceCollection"/> before routing is configured.
/// </summary>
internal sealed class RoutingMarkerService
{
}
