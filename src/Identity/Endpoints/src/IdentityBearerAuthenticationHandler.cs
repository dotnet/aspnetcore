// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Endpoints.DTO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Identity.Endpoints;

internal sealed class IdentityBearerAuthenticationHandler : SignInAuthenticationHandler<IdentityBearerAuthenticationOptions>
{
    private const string BearerTokenPurpose = $"Microsoft.AspNetCore.Identity.Endpoints.IdentityBearerAuthenticationHandler:v1:BearerToken";

    private static readonly Task<AuthenticateResult> TokenMissingTask = Task.FromResult(AuthenticateResult.Fail("Token missing"));
    private static readonly Task<AuthenticateResult> FailedUnprotectingTokenTask = Task.FromResult(AuthenticateResult.Fail("Unprotect token failed"));
    private static readonly Task<AuthenticateResult> TokenExpiredTask = Task.FromResult(AuthenticateResult.Fail("Token expired"));

    private readonly IDataProtectionProvider _fallbackDataProtectionProvider;

    public IdentityBearerAuthenticationHandler(
        IOptionsMonitor<IdentityBearerAuthenticationOptions> optionsMonitor,
        ILoggerFactory loggerFactory,
        UrlEncoder urlEncoder,
        ISystemClock clock,
        IDataProtectionProvider dataProtectionProvider)
        : base(optionsMonitor, loggerFactory, urlEncoder, clock)
    {
        _fallbackDataProtectionProvider = dataProtectionProvider;
    }

    private IDataProtectionProvider DataProtectionProvider
        => Options.DataProtectionProvider ?? _fallbackDataProtectionProvider;

    private ISecureDataFormat<AuthenticationTicket> BearerTokenProtector
        => Options.BearerTokenProtector ?? new TicketDataFormat(DataProtectionProvider.CreateProtector(BearerTokenPurpose));

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // If there's no bearer token, forward to cookie auth.
        if (GetBearerTokenOrNull() is not string token)
        {
            return Options.BearerTokenMissingFallbackScheme is string fallbackScheme
                ? Context.AuthenticateAsync(fallbackScheme)
                : TokenMissingTask;
        }

        var ticket = BearerTokenProtector.Unprotect(token);

        if (ticket?.Properties?.ExpiresUtc is null)
        {
            return FailedUnprotectingTokenTask;
        }

        if (Clock.UtcNow >= ticket.Properties.ExpiresUtc)
        {
            return TokenExpiredTask;
        }

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        // If there's no bearer token, forward to cookie auth.
        if (GetBearerTokenOrNull() is null)
        {
            return Options.BearerTokenMissingFallbackScheme is string fallbackScheme
                ? Context.AuthenticateAsync(fallbackScheme)
                : TokenMissingTask;
        }

        Response.Headers.Append(HeaderNames.WWWAuthenticate, "Bearer");
        return base.HandleChallengeAsync(properties);
    }

    protected override Task HandleSignInAsync(ClaimsPrincipal user, AuthenticationProperties? properties)
    {
        properties ??= new();
        properties.ExpiresUtc ??= Clock.UtcNow + Options.BearerTokenExpiration;

        var ticket = new AuthenticationTicket(user, properties, Scheme.Name);
        var accessTokenResponse = new AccessTokenResponse
        {
            AccessToken = BearerTokenProtector.Protect(ticket),
            ExpiresInTotalSeconds = Options.BearerTokenExpiration.TotalSeconds,
        };

        return Context.Response.WriteAsJsonAsync(accessTokenResponse);
    }

    protected override Task HandleSignOutAsync(AuthenticationProperties? properties)
        => throw new NotSupportedException($"""
Sign out is not currently supported by identity bearer tokens.
If you want to delete cookies or clear a session, specify "{Options.BearerTokenMissingFallbackScheme}" as the authentication scheme.
""");

    private string? GetBearerTokenOrNull()
    {
        var authorization = Request.Headers.Authorization.ToString();

        return authorization.StartsWith("Bearer ", StringComparison.Ordinal)
            ? authorization["Bearer ".Length..]
            : null;
    }
}
