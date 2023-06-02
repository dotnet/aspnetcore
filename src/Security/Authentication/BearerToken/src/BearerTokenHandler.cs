// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication.BearerToken.DTO;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Authentication.BearerToken;

internal sealed class BearerTokenHandler(
    IOptionsMonitor<BearerTokenOptions> optionsMonitor,
    ILoggerFactory loggerFactory,
    UrlEncoder urlEncoder,
    IDataProtectionProvider dataProtectionProvider)
    : SignInAuthenticationHandler<BearerTokenOptions>(optionsMonitor, loggerFactory, urlEncoder)
{
    private const string BearerTokenPurpose = "BearerToken";
    private const string RefreshTokenPurpose = "RefreshToken";

    private static readonly long OneSecondTicks = TimeSpan.FromSeconds(1).Ticks;

    private static readonly AuthenticateResult FailedUnprotectingToken = AuthenticateResult.Fail("Unprotected token failed");
    private static readonly AuthenticateResult TokenExpired = AuthenticateResult.Fail("Token expired");

    private ISecureDataFormat<AuthenticationTicket> TokenProtector
        => Options.TokenProtector ?? new TicketDataFormat(dataProtectionProvider.CreateProtector("Microsoft.AspNetCore.Authentication.BearerToken", Scheme.Name));

    private new BearerTokenEvents Events => (BearerTokenEvents)base.Events!;

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Give application opportunity to find from a different location, adjust, or reject token
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

        var ticket = TokenProtector.Unprotect(token, BearerTokenPurpose);

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
        properties.ExpiresUtc ??= utcNow + Options.BearerTokenExpiration;
        var isRefresh = properties.RefreshToken is not null;

        if (isRefresh)
        {
            var refreshTicket = TokenProtector.Unprotect(properties.RefreshToken, RefreshTokenPurpose);

            if (refreshTicket?.Properties?.ExpiresUtc is not { } expiresUtc || TimeProvider.GetUtcNow() >= expiresUtc)
            {
                await ChallengeAsync(properties);
                return;
            }

            user = refreshTicket.Principal;
        }

        var signingInContext = new SigningInContext(Context, Scheme, Options, user, properties);

        await Events.SigningInAsync(signingInContext);

        if (signingInContext.Principal?.Identity?.Name is null)
        {
            await ChallengeAsync(properties);
            return;
        }

        var response = new AccessTokenResponse
        {
            AccessToken = signingInContext.AccessToken ?? TokenProtector.Protect(CreateAccessTicket(signingInContext), BearerTokenPurpose),
            ExpiresInSeconds = CalculateExpiresInSeconds(utcNow, signingInContext.Properties.ExpiresUtc),
            RefreshToken = signingInContext.RefreshToken ?? TokenProtector.Protect(CreateRefreshTicket(user, utcNow), RefreshTokenPurpose),
        };

        await Context.Response.WriteAsJsonAsync(response, BearerTokenJsonSerializerContext.Default.AccessTokenResponse);

        if (isRefresh)
        {
            Logger.AuthenticationSchemeSignedInWithRefreshToken(Scheme.Name);
        }
        else
        {
            Logger.AuthenticationSchemeSignedIn(Scheme.Name);
        }
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

    private long CalculateExpiresInSeconds(DateTimeOffset utcNow, DateTimeOffset? expiresUtc)
    {
        static DateTimeOffset FloorSeconds(DateTimeOffset dateTimeOffset)
            => new(dateTimeOffset.Ticks / OneSecondTicks * OneSecondTicks, dateTimeOffset.Offset);

        // AuthenticationProperties floors ExpiresUtc. If this remains unchanged, we'll use BearerTokenExpiration directly
        // to produce a consistent ExpiresInTotalSeconds values. If ExpiresUtc was overridden, we just calculate the
        // the difference from utcNow and round even though this will likely result in unstable values.
        var expiresTimeSpan = Options.BearerTokenExpiration;
        var expectedExpiresUtc = FloorSeconds(utcNow + expiresTimeSpan);
        return (long)(expiresUtc switch
        {
            DateTimeOffset d when d == expectedExpiresUtc => expiresTimeSpan.TotalSeconds,
            DateTimeOffset d => (d - utcNow).TotalSeconds,
            _ => expiresTimeSpan.TotalSeconds,
        });
    }

    private static AuthenticationTicket CreateAccessTicket(SigningInContext context)
        => new(context.Principal!, context.Properties, context.Scheme.Name);

    private AuthenticationTicket CreateRefreshTicket(ClaimsPrincipal user, DateTimeOffset utcNow)
    {
        var refreshProperties = new AuthenticationProperties
        {
            ExpiresUtc = utcNow + Options.RefreshTokenExpiration
        };

        return new AuthenticationTicket(user, refreshProperties, $"{Scheme.Name}:{RefreshTokenPurpose}");
    }
}
