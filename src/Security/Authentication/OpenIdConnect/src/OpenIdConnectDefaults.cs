// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication.OpenIdConnect;

/// <summary>
/// Default values related to OpenIdConnect authentication handler
/// </summary>
public static class OpenIdConnectDefaults
{
    /// <summary>
    /// Constant used to identify state in openIdConnect protocol message.
    /// </summary>
    public static readonly string AuthenticationPropertiesKey = "OpenIdConnect.AuthenticationProperties";

    /// <summary>
    /// The default value used for OpenIdConnectOptions.AuthenticationScheme.
    /// </summary>
    public const string AuthenticationScheme = "OpenIdConnect";

    /// <summary>
    /// The default value for the display name.
    /// </summary>
    public static readonly string DisplayName = "OpenIdConnect";

    /// <summary>
    /// The prefix used to for the nonce in the cookie.
    /// </summary>
    public static readonly string CookieNoncePrefix = ".AspNetCore.OpenIdConnect.Nonce.";

    /// <summary>
    /// The property for the RedirectUri that was used when asking for a 'authorizationCode'.
    /// </summary>
    public static readonly string RedirectUriForCodePropertiesKey = "OpenIdConnect.Code.RedirectUri";

    /// <summary>
    /// Constant used to identify userstate inside AuthenticationProperties that have been serialized in the 'state' parameter.
    /// </summary>
    public static readonly string UserstatePropertiesKey = "OpenIdConnect.Userstate";
}
