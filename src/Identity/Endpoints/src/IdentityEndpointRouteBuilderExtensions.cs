// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Endpoints.DTO;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Provides extension methods for <see cref="IEndpointRouteBuilder"/> to add identity endpoints.
/// </summary>
public static class IdentityEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Add endpoints for registering, logging in, and logging out using ASP.NET Core Identity.
    /// </summary>
    /// <typeparam name="TUser">The type describing the user. This should match the generic parameter in <see cref="UserManager{TUser}"/>.</typeparam>
    /// <param name="endpoints">
    /// The <see cref="IEndpointRouteBuilder"/> to add the identity endpoints to.
    /// Call <see cref="EndpointRouteBuilderExtensions.MapGroup(IEndpointRouteBuilder, string)"/> to add a prefix to all the endpoints.
    /// </param>
    /// <returns>An <see cref="IEndpointConventionBuilder"/> to further customize the added endpoints.</returns>
    public static IEndpointConventionBuilder MapIdentity<TUser>(this IEndpointRouteBuilder endpoints) where TUser : class, new()
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var v1 = endpoints.MapGroup("/v1");

        // NOTE: We cannot inject UserManager<TUser> directly because the TUser generic parameter is currently unsupported by RDG.
        // https://github.com/dotnet/aspnetcore/issues/47338
        v1.MapPost("/register", async Task<Results<Ok, ValidationProblem>>
            ([FromBody] RegisterRequest registration, [FromServices] IServiceProvider services) =>
        {
            var userManager = services.GetRequiredService<UserManager<TUser>>();

            var user = new TUser();
            await userManager.SetUserNameAsync(user, registration.Username);
            var result = await userManager.CreateAsync(user, registration.Password);

            if (result.Succeeded)
            {
                // TODO: Send email confirmation

                return TypedResults.Ok();
            }

            return TypedResults.ValidationProblem(result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description }));
        });

        v1.MapPost("/login", async Task<Results<UnauthorizedHttpResult, Ok<AccessTokenResponse>, SignInHttpResult>>
            ([FromBody] LoginRequest login, [FromServices] IServiceProvider services) =>
        {
            // TODO: Use SignInManager to checkout for email confirmation, lockout, etc...
            var userManager = services.GetRequiredService<UserManager<TUser>>();
            var user = await userManager.FindByNameAsync(login.Username);

            if (user is null || !await userManager.CheckPasswordAsync(user, login.Password))
            {
                return TypedResults.Unauthorized();
            }

            var scheme = login.CookieMode ? IdentityConstants.ApplicationScheme : IdentityConstants.BearerScheme;
            var claimsFactory = services.GetRequiredService<IUserClaimsPrincipalFactory<TUser>>();
            var claimsPrincipal = await claimsFactory.CreateAsync(user);

            return TypedResults.SignIn(claimsPrincipal, authenticationScheme: scheme);
        });

        return new IdentityEndpointConventionBuilder(v1);
    }

    // Wrap RouteGroupBuilder with a non-public type to avoid a potential future behavioral breaking change.
    private sealed class IdentityEndpointConventionBuilder(RouteGroupBuilder inner) : IEndpointConventionBuilder
    {
        private readonly IEndpointConventionBuilder _inner = inner;

        public void Add(Action<EndpointBuilder> convention) => _inner.Add(convention);
        public void Finally(Action<EndpointBuilder> finallyConvention) => _inner.Finally(finallyConvention);
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    private sealed class FromBodyAttribute : Attribute, IFromBodyMetadata
    {
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    private sealed class FromServicesAttribute : Attribute, IFromServiceMetadata
    {
    }
}
