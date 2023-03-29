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

internal sealed class IdentityBearerAuthenticationHandler : SignInAuthenticationHandler<IdentityBearerOptions>
{
    private const string BearerTokenPurpose = $"Microsoft.AspNetCore.Identity.Endpoints.IdentityBearerAuthenticationHandler:v1:BearerToken";

    private static readonly AuthenticateResult TokenMissing = AuthenticateResult.Fail("Token missing");
    private static readonly AuthenticateResult FailedUnprotectingToken = AuthenticateResult.Fail("Unprotected token failed");
    private static readonly AuthenticateResult TokenExpired = AuthenticateResult.Fail("Token expired");

    private readonly IDataProtectionProvider _fallbackDataProtectionProvider;

    public IdentityBearerAuthenticationHandler(
        IOptionsMonitor<IdentityBearerOptions> optionsMonitor,
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

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // If there's no bearer token, forward to cookie auth.
        if (await GetBearerTokenOrNullAsync() is not string token)
        {
            return Options.MissingBearerTokenFallbackScheme is string fallbackScheme
                ? await Context.AuthenticateAsync(fallbackScheme)
                : TokenMissing;
        }

        var ticket = BearerTokenProtector.Unprotect(token);

        if (ticket?.Properties?.ExpiresUtc is null)
        {
            return FailedUnprotectingToken;
        }

        if (Clock.UtcNow >= ticket.Properties.ExpiresUtc)
        {
            return TokenExpired;
        }

        return AuthenticateResult.Success(ticket);
    }

    protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        // If there's no bearer token, forward to cookie auth.
        if (await GetBearerTokenOrNullAsync() is null)
        {
            if (Options.MissingBearerTokenFallbackScheme is string fallbackScheme)
            {
                await Context.ForbidAsync(fallbackScheme);
                return;
            }
        }

        await base.HandleForbiddenAsync(properties);
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        // If there's no bearer token, forward to cookie auth.
        if (await GetBearerTokenOrNullAsync() is null)
        {
            if (Options.MissingBearerTokenFallbackScheme is string fallbackScheme)
            {
                await Context.ChallengeAsync(fallbackScheme);
                return;
            }
        }

        Response.Headers.Append(HeaderNames.WWWAuthenticate, "Bearer");
        await base.HandleChallengeAsync(properties);
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
If you want to delete cookies or clear a session, specify "{IdentityConstants.ApplicationScheme}" as the authentication scheme.
""");

    private async ValueTask<string?> GetBearerTokenOrNullAsync()
    {
        if (Options.ExtractBearerToken is not null &&
            await Options.ExtractBearerToken(Context) is string token)
        {
            return token;
        }

        var authorization = Request.Headers.Authorization.ToString();

        return authorization.StartsWith("Bearer ", StringComparison.Ordinal)
            ? authorization["Bearer ".Length..]
            : null;
    }
}
