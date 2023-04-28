// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// <see cref="IdentityBuilder"/> extension methods to support <see cref="IdentityApiEndpointRouteBuilderExtensions.MapIdentityApi{TUser}(IEndpointRouteBuilder)"/>.
/// </summary>
public static class IdentityApiEndpointsIdentityBuilderExtensions
{
    /// <summary>
    /// Adds configuration ans services needed to support <see cref="IdentityApiEndpointRouteBuilderExtensions.MapIdentityApi{TUser}(IEndpointRouteBuilder)"/>
    /// but does not configure authentication. Call <see cref="BearerTokenExtensions.AddBearerToken(AuthenticationBuilder, Action{BearerTokenOptions}?)"/> and/or
    /// <see cref="IdentityCookieAuthenticationBuilderExtensions.AddIdentityCookies(AuthenticationBuilder)"/> to configure authentication separately.
    /// </summary>
    /// <param name="builder">The <see cref="IdentityBuilder"/>.</param>
    /// <returns>The <see cref="IdentityBuilder"/>.</returns>
    public static IdentityBuilder AddApiEndpoints(this IdentityBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddSignInManager();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<JsonOptions>, IdentityEndpointsJsonOptionsSetup>());
        return builder;
    }
}
