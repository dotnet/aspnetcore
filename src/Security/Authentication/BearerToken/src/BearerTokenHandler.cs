// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Authentication.BearerToken;

internal sealed class BearerTokenHandler(IOptionsMonitor<BearerTokenOptions> optionsMonitor, ILoggerFactory loggerFactory, UrlEncoder urlEncoder)
    : SignInAuthenticationHandler<BearerTokenOptions>(optionsMonitor, loggerFactory, urlEncoder)
{
    private static readonly AuthenticateResult FailedUnprotectingToken = AuthenticateResult.Fail("Unprotected token failed");
    private static readonly AuthenticateResult TokenExpired = AuthenticateResult.Fail("Token expired");

    private new BearerTokenEvents Events => (BearerTokenEvents)base.Events!;

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Give application opportunity to find from a different location, adjust, or reject token.
        var messageReceivedContext = new MessageReceivedContext(Context, Scheme, Options);

        await Events.MessageReceivedAsync(messageReceivedContext);

        if (messageReceivedContext.Result is not null)
        {
            return messageReceivedContext.Result;
        }

        var token = messageReceivedContext.Token ?? GetBearerTokenOrNull();

        if (token is null)
        {
            return AuthenticateResult.NoResult();
        }

        var ticket = Options.BearerTokenProtector.Unprotect(token);

        if (ticket?.Properties?.ExpiresUtc is not { } expiresUtc)
        {
            return FailedUnprotectingToken;
        }

        if (TimeProvider.GetUtcNow() >= expiresUtc)
        {
            return TokenExpired;
        }

        return AuthenticateResult.Success(ticket);
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.Append(HeaderNames.WWWAuthenticate, "Bearer");
        return base.HandleChallengeAsync(properties);
    }

    protected override async Task HandleSignInAsync(ClaimsPrincipal user, AuthenticationProperties? properties)
    {
        var utcNow = TimeProvider.GetUtcNow();

        properties ??= new();
        properties.ExpiresUtc = utcNow + Options.BearerTokenExpiration;

        var response = new AccessTokenResponse
        {
            AccessToken = Options.BearerTokenProtector.Protect(CreateBearerTicket(user, properties)),
            ExpiresIn = (long)Options.BearerTokenExpiration.TotalSeconds,
            RefreshToken = Options.RefreshTokenProtector.Protect(CreateRefreshTicket(user, utcNow)),
        };

        Logger.AuthenticationSchemeSignedIn(Scheme.Name);

        await Context.Response.WriteAsJsonAsync(response, ResolveAccessTokenJsonTypeInfo(Context));
    }

    private static JsonTypeInfo<AccessTokenResponse> ResolveAccessTokenJsonTypeInfo(HttpContext httpContext)
    {
        // Attempt to resolve options from DI then fall back to static options
        var typeInfo = httpContext.RequestServices.GetService<IOptions<JsonOptions>>()
            ?.Value?.SerializerOptions?.GetTypeInfo(typeof(AccessTokenResponse)) as JsonTypeInfo<AccessTokenResponse>;
        return typeInfo ?? BearerTokenJsonSerializerContext.Default.AccessTokenResponse;
    }

    // No-op to avoid interfering with any mass sign-out logic.
    protected override Task HandleSignOutAsync(AuthenticationProperties? properties) => Task.CompletedTask;

    private string? GetBearerTokenOrNull()
    {
        var authorization = Request.Headers.Authorization.ToString();

        return authorization.StartsWith("Bearer ", StringComparison.Ordinal)
            ? authorization["Bearer ".Length..]
            : null;
    }

    private AuthenticationTicket CreateBearerTicket(ClaimsPrincipal user, AuthenticationProperties properties)
        => new(user, properties, $"{Scheme.Name}:AccessToken");

    private AuthenticationTicket CreateRefreshTicket(ClaimsPrincipal user, DateTimeOffset utcNow)
    {
        var refreshProperties = new AuthenticationProperties
        {
            ExpiresUtc = utcNow + Options.RefreshTokenExpiration
        };

        return new AuthenticationTicket(user, refreshProperties, $"{Scheme.Name}:RefreshToken");
    }
}
