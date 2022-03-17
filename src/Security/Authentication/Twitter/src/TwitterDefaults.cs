// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication.Twitter;

/// <summary>
/// Default values for the Twitter authentication handler.
/// </summary>
public static class TwitterDefaults
{
    /// <summary>
    /// The default scheme for Twitter authentication. The value is <c>Twitter</c>.
    /// </summary>
    public const string AuthenticationScheme = "Twitter";

    /// <summary>
    /// The default display name for Twitter authentication. Defaults to <c>Twitter</c>.
    /// </summary>
    public static readonly string DisplayName = "Twitter";

    // https://developer.twitter.com/en/docs/basics/authentication/api-reference/request_token
    internal const string RequestTokenEndpoint = "https://api.twitter.com/oauth/request_token";

    // https://developer.twitter.com/en/docs/basics/authentication/api-reference/authenticate
    internal const string AuthenticationEndpoint = "https://api.twitter.com/oauth/authenticate?oauth_token=";

    // https://developer.twitter.com/en/docs/basics/authentication/api-reference/access_token
    internal const string AccessTokenEndpoint = "https://api.twitter.com/oauth/access_token";
}
