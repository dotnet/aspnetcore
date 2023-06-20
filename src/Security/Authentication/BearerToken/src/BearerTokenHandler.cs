// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication.BearerToken.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Authentication.BearerToken;

internal sealed class BearerTokenHandler(IOptionsMonitor<BearerTokenOptions> optionsMonitor, ILoggerFactory loggerFactory, UrlEncoder urlEncoder)
    : SignInAuthenticationHandler<BearerTokenOptions>(optionsMonitor, loggerFactory, urlEncoder)
{
    private static readonly long OneSecondTicks = TimeSpan.FromSeconds(1).Ticks;

    private static readonly AuthenticateResult FailedUnprotectingToken = AuthenticateResult.Fail("Unprotected token failed");
    private static readonly AuthenticateResult TokenExpired = AuthenticateResult.Fail("Token expired");

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
        properties.ExpiresUtc ??= utcNow + Options.BearerTokenExpiration;

        var response = new AccessTokenResponse
        {
            AccessToken = Options.BearerTokenProtector.Protect(CreateBearerTicket(user, properties)),
            ExpiresInSeconds = CalculateExpiresInSeconds(utcNow, properties.ExpiresUtc),
            RefreshToken = Options.RefreshTokenProtector.Protect(CreateRefreshTicket(user, utcNow)),
        };

        Logger.AuthenticationSchemeSignedIn(Scheme.Name);

        await Context.Response.WriteAsJsonAsync(response, BearerTokenJsonSerializerContext.Default.AccessTokenResponse);
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
