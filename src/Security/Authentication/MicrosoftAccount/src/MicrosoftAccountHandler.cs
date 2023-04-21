// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication.MicrosoftAccount;

/// <summary>
/// Authentication handler for Microsoft Account based authentication.
/// </summary>
public class MicrosoftAccountHandler : OAuthHandler<MicrosoftAccountOptions>
{
    /// <summary>
    /// Initializes a new instance of <see cref="MicrosoftAccountHandler"/>.
    /// </summary>
    /// <inheritdoc />
    [Obsolete("ISystemClock is obsolete, use TimeProvider on AuthenticationSchemeOptions instead.")]
    public MicrosoftAccountHandler(IOptionsMonitor<MicrosoftAccountOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
        : base(options, logger, encoder, clock)
    { }

    /// <summary>
    /// Initializes a new instance of <see cref="MicrosoftAccountHandler"/>.
    /// </summary>
    /// <inheritdoc />
    public MicrosoftAccountHandler(IOptionsMonitor<MicrosoftAccountOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    { }

    /// <inheritdoc />
    protected override async Task<AuthenticationTicket> CreateTicketAsync(ClaimsIdentity identity, AuthenticationProperties properties, OAuthTokenResponse tokens)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, Options.UserInformationEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await Backchannel.SendAsync(request, Context.RequestAborted);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"An error occurred when retrieving Microsoft user information ({response.StatusCode}). Please check if the authentication information is correct and the corresponding Microsoft Account API is enabled.");
        }

        using (var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Context.RequestAborted)))
        {
            var context = new OAuthCreatingTicketContext(new ClaimsPrincipal(identity), properties, Context, Scheme, Options, Backchannel, tokens, payload.RootElement);
            context.RunClaimActions();
            await Events.CreatingTicket(context);
            return new AuthenticationTicket(context.Principal!, context.Properties, Scheme.Name);
        }
    }

    /// <inheritdoc />
    protected override string BuildChallengeUrl(AuthenticationProperties properties, string redirectUri)
    {
        var queryStrings = new Dictionary<string, string>
            {
                { "client_id", Options.ClientId },
                { "response_type", "code" },
                { "redirect_uri", redirectUri }
            };

        AddQueryString(queryStrings, properties, MicrosoftChallengeProperties.ScopeKey, FormatScope, Options.Scope);
#pragma warning disable CS0618 // Type or member is obsolete
        AddQueryString(queryStrings, properties, MicrosoftChallengeProperties.ResponseModeKey);
#pragma warning restore CS0618 // Type or member is obsolete
        AddQueryString(queryStrings, properties, MicrosoftChallengeProperties.DomainHintKey);
        AddQueryString(queryStrings, properties, MicrosoftChallengeProperties.LoginHintKey);
        AddQueryString(queryStrings, properties, MicrosoftChallengeProperties.PromptKey);

        if (Options.UsePkce)
        {
            var bytes = new byte[32];
            RandomNumberGenerator.Fill(bytes);
            var codeVerifier = Base64UrlTextEncoder.Encode(bytes);

            // Store this for use during the code redemption.
            properties.Items.Add(OAuthConstants.CodeVerifierKey, codeVerifier);

            var challengeBytes = SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier));
            var codeChallenge = WebEncoders.Base64UrlEncode(challengeBytes);

            queryStrings[OAuthConstants.CodeChallengeKey] = codeChallenge;
            queryStrings[OAuthConstants.CodeChallengeMethodKey] = OAuthConstants.CodeChallengeMethodS256;
        }

        var state = Options.StateDataFormat.Protect(properties);
        queryStrings.Add("state", state);

        return QueryHelpers.AddQueryString(Options.AuthorizationEndpoint, queryStrings!);
    }

    private static void AddQueryString<T>(
       Dictionary<string, string> queryStrings,
       AuthenticationProperties properties,
       string name,
       Func<T, string> formatter,
       T defaultValue)
    {
        string? value;
        var parameterValue = properties.GetParameter<T>(name);
        if (parameterValue != null)
        {
            value = formatter(parameterValue);
        }
        else if (!properties.Items.TryGetValue(name, out value))
        {
            value = formatter(defaultValue);
        }

        // Remove the parameter from AuthenticationProperties so it won't be serialized into the state
        properties.Items.Remove(name);

        if (value != null)
        {
            queryStrings[name] = value;
        }
    }

    private static void AddQueryString(
        Dictionary<string, string> queryStrings,
        AuthenticationProperties properties,
        string name,
        string? defaultValue = null)
        => AddQueryString(queryStrings, properties, name, x => x!, defaultValue);
}
