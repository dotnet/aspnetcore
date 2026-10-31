// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Authentication.Google;

/// <summary>
/// Authentication handler for Google's OAuth based authentication.
/// </summary>
public class GoogleHandler : OAuthHandler<GoogleOptions>
{
    /// <summary>
    /// Initializes a new instance of <see cref="GoogleHandler"/>.
    /// </summary>
    /// <inheritdoc />
    [Obsolete("ISystemClock is obsolete, use TimeProvider on AuthenticationSchemeOptions instead.")]
    public GoogleHandler(IOptionsMonitor<GoogleOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
        : base(options, logger, encoder, clock)
    { }

    /// <summary>
    /// Initializes a new instance of <see cref="GoogleHandler"/>.
    /// </summary>
    /// <inheritdoc />
    public GoogleHandler(IOptionsMonitor<GoogleOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    { }

    /// <inheritdoc />
    protected override async Task<AuthenticationTicket> CreateTicketAsync(
        ClaimsIdentity identity,
        AuthenticationProperties properties,
        OAuthTokenResponse tokens)
    {
        // Get the Google user
        var request = new HttpRequestMessage(HttpMethod.Get, Options.UserInformationEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await Backchannel.SendAsync(request, Context.RequestAborted);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"An error occurred when retrieving Google user information ({response.StatusCode}). Please check if the authentication information is correct.");
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
        // Google Identity Platform Manual:
        // https://developers.google.com/identity/protocols/OAuth2WebServer

        // Some query params and features (e.g. PKCE) are handled by the base class but some params have to be modified or added here
        var queryStrings = QueryHelpers.ParseQuery(new Uri(base.BuildChallengeUrl(properties, redirectUri)).Query);

        SetQueryParam(queryStrings, properties, GoogleChallengeProperties.ScopeKey, FormatScope, Options.Scope);
        SetQueryParam(queryStrings, properties, GoogleChallengeProperties.AccessTypeKey, Options.AccessType);
        SetQueryParam(queryStrings, properties, GoogleChallengeProperties.ApprovalPromptKey);
        SetQueryParam(queryStrings, properties, GoogleChallengeProperties.PromptParameterKey);
        SetQueryParam(queryStrings, properties, GoogleChallengeProperties.LoginHintKey);
        SetQueryParam(queryStrings, properties, GoogleChallengeProperties.IncludeGrantedScopesKey, v => v?.ToString(CultureInfo.InvariantCulture).ToLowerInvariant(), (bool?)null);

        // Some properties are removed when setting query params above, so the state has to be reset
        queryStrings["state"] = Options.StateDataFormat.Protect(properties);

        return QueryHelpers.AddQueryString(Options.AuthorizationEndpoint, queryStrings);
    }

    private static void SetQueryParam<T>(
        IDictionary<string, StringValues> queryStrings,
        AuthenticationProperties properties,
        string name,
        Func<T, string?> formatter,
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

    private static void SetQueryParam(
        IDictionary<string, StringValues> queryStrings,
        AuthenticationProperties properties,
        string name,
        string? defaultValue = null)
        => SetQueryParam(queryStrings, properties, name, x => x, defaultValue);
}
