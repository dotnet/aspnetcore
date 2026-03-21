// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication.Facebook;

/// <summary>
/// Authentication handler for Facebook's OAuth based authentication.
/// </summary>
public class FacebookHandler : OAuthHandler<FacebookOptions>
{
    /// <summary>
    /// Initializes a new instance of <see cref="FacebookHandler"/>.
    /// </summary>
    /// <inheritdoc />
    [Obsolete("ISystemClock is obsolete, use TimeProvider on AuthenticationSchemeOptions instead.")]
    public FacebookHandler(IOptionsMonitor<FacebookOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
        : base(options, logger, encoder, clock)
    { }

    /// <summary>
    /// Initializes a new instance of <see cref="FacebookHandler"/>.
    /// </summary>
    /// <inheritdoc />
    public FacebookHandler(IOptionsMonitor<FacebookOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    { }

    /// <inheritdoc />
    protected override async Task<AuthenticationTicket> CreateTicketAsync(ClaimsIdentity identity, AuthenticationProperties properties, OAuthTokenResponse tokens)
    {
        var endpoint = QueryHelpers.AddQueryString(Options.UserInformationEndpoint, "access_token", tokens.AccessToken!);
        if (Options.SendAppSecretProof)
        {
            endpoint = QueryHelpers.AddQueryString(endpoint, "appsecret_proof", GenerateAppSecretProof(tokens.AccessToken!));
        }
        if (Options.Fields.Count > 0)
        {
            endpoint = QueryHelpers.AddQueryString(endpoint, "fields", string.Join(",", Options.Fields));
        }

        var response = await Backchannel.GetAsync(endpoint, Context.RequestAborted);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"An error occurred when retrieving Facebook user information ({response.StatusCode}). Please check if the authentication information is correct and the corresponding Facebook Graph API is enabled.");
        }

        using (var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Context.RequestAborted)))
        {
            var context = new OAuthCreatingTicketContext(new ClaimsPrincipal(identity), properties, Context, Scheme, Options, Backchannel, tokens, payload.RootElement);
            context.RunClaimActions();
            await Events.CreatingTicket(context);
            return new AuthenticationTicket(context.Principal!, context.Properties, Scheme.Name);
        }
    }

    private string GenerateAppSecretProof(string accessToken)
    {
        var key = Encoding.ASCII.GetBytes(Options.AppSecret);
        var tokenBytes = Encoding.ASCII.GetBytes(accessToken);
        var hash = HMACSHA256.HashData(key, tokenBytes);
        return Convert.ToHexStringLower(hash);
    }

    /// <inheritdoc />
    protected override string FormatScope(IEnumerable<string> scopes)
    {
        // Facebook deviates from the OAuth spec here. They require comma separated instead of space separated.
        // https://developers.facebook.com/docs/reference/dialogs/oauth
        // http://tools.ietf.org/html/rfc6749#section-3.3
        return string.Join(",", scopes);
    }

    /// <inheritdoc />
    protected override string FormatScope()
        => base.FormatScope();
}
