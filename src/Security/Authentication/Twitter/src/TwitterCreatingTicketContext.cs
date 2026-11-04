// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.Twitter;

/// <summary>
/// Contains information about the login session as well as the user <see cref="System.Security.Claims.ClaimsIdentity"/>.
/// </summary>
public class TwitterCreatingTicketContext : ResultContext<TwitterOptions>
{
    /// <summary>
    /// Initializes a <see cref="TwitterCreatingTicketContext"/>
    /// </summary>
    /// <param name="context">The HTTP environment</param>
    /// <param name="scheme">The scheme data</param>
    /// <param name="options">The options for Twitter</param>
    /// <param name="principal">The <see cref="ClaimsPrincipal"/>.</param>
    /// <param name="properties">The <see cref="AuthenticationProperties"/>.</param>
    /// <param name="userId">Twitter user ID</param>
    /// <param name="screenName">Twitter screen name</param>
    /// <param name="accessToken">Twitter access token</param>
    /// <param name="accessTokenSecret">Twitter access token secret</param>
    /// <param name="user">User details</param>
    public TwitterCreatingTicketContext(
        HttpContext context,
        AuthenticationScheme scheme,
        TwitterOptions options,
        ClaimsPrincipal principal,
        AuthenticationProperties properties,
        string userId,
        string screenName,
        string accessToken,
        string accessTokenSecret,
        JsonElement user)
        : base(context, scheme, options)
    {
        UserId = userId;
        ScreenName = screenName;
        AccessToken = accessToken;
        AccessTokenSecret = accessTokenSecret;
        User = user;
        Principal = principal;
        Properties = properties;
    }

    /// <summary>
    /// Gets the Twitter user ID
    /// </summary>
    public string UserId { get; }

    /// <summary>
    /// Gets the Twitter screen name
    /// </summary>
    public string ScreenName { get; }

    /// <summary>
    /// Gets the Twitter access token
    /// </summary>
    public string AccessToken { get; }

    /// <summary>
    /// Gets the Twitter access token secret
    /// </summary>
    public string AccessTokenSecret { get; }

    /// <summary>
    /// Gets the JSON-serialized user or an empty
    /// <see cref="JsonElement"/> if it is not available.
    /// </summary>
    public JsonElement User { get; }
}
