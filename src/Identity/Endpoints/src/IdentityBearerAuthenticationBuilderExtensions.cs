// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Endpoints;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods to configure the identity bearer token authentication used by <see cref="IdentityEndpointRouteBuilderExtensions.MapIdentity{TUser}(IEndpointRouteBuilder)"/>.
/// </summary>
public static class IdentityBearerAuthenticationBuilderExtensions
{
    /// <summary>
    /// Adds identity bearer token authentication. The default scheme is specified by <see cref="IdentityConstants.BearerScheme"/>.
    /// <para>
    /// Identity bearer tokens can be obtained from endpoints added by <see cref="IdentityEndpointRouteBuilderExtensions.MapIdentity{TUser}(IEndpointRouteBuilder)"/>.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <param name="configure">Action used to configure the identity bearer token authentication options.</param>
    /// <returns>A reference to <paramref name="builder"/> after the operation has completed.</returns>
    public static AuthenticationBuilder AddIdentityBearer(this AuthenticationBuilder builder, Action<IdentityBearerOptions>? configure)
        => builder.AddScheme<IdentityBearerOptions, IdentityBearerAuthenticationHandler>(IdentityConstants.BearerScheme, configure);
}
