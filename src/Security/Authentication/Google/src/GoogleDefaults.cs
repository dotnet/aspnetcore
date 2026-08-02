// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication.Google;

/// <summary>
/// Default values for Google authentication
/// </summary>
public static class GoogleDefaults
{
    /// <summary>
    /// The default scheme for Google authentication. Defaults to <c>Google</c>.
    /// </summary>
    public const string AuthenticationScheme = "Google";

    /// <summary>
    /// The default display name for Google authentication. Defaults to <c>Google</c>.
    /// </summary>
    public static readonly string DisplayName = "Google";

    /// <summary>
    /// The default endpoint used to perform Google authentication.
    /// </summary>
    /// <remarks>
    /// For more details about this endpoint, see <see href="https://developers.google.com/identity/protocols/oauth2/web-server#httprest"/>.
    /// </remarks>
    public static readonly string AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";

    /// <summary>
    /// The OAuth endpoint used to exchange access tokens.
    /// </summary>
    public static readonly string TokenEndpoint = "https://oauth2.googleapis.com/token";

    /// <summary>
    /// The Google endpoint that is used to gather additional user information.
    /// </summary>
    public static readonly string UserInformationEndpoint = "https://www.googleapis.com/oauth2/v3/userinfo";
}
