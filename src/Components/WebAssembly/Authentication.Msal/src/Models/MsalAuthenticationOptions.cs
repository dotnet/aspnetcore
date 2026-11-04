// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Authentication.WebAssembly.Msal;

/// <summary>
/// Authentication options for the underlying msal.js library handling the authentication.
/// </summary>
public class MsalAuthenticationOptions
{
    /// <summary>
    /// Gets or sets the client id for the application.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Gets or sets the authority for the Azure Active Directory or Azure Active Directory B2C instance.
    /// </summary>
    public string? Authority { get; set; }

    /// <summary>
    /// Gets or sets a value that indicates whether or not to validate the authority.
    /// </summary>
    /// <remarks>
    /// This value needs to be set to false when using Azure Active Directory B2C.
    /// </remarks>
    public bool ValidateAuthority { get; set; } = true;

    /// <summary>
    /// Gets or sets the redirect uri for the application.
    /// </summary>
    /// <remarks>
    /// It can be an absolute or base relative <see cref="Uri"/> and defaults to <c>authentication/login-callback.</c>
    /// </remarks>
    public string? RedirectUri { get; set; }

    /// <summary>
    /// Gets or sets the post logout redirect uri for the application.
    /// </summary>
    /// <remarks>
    /// It can be an absolute or base relative <see cref="Uri"/> and defaults to <c>authentication/logout-callback.</c>
    /// </remarks>
    public string? PostLogoutRedirectUri { get; set; }

    /// <summary>
    /// Gets or sets whether or not to navigate to the login request url after a successful login.
    /// </summary>
    public bool NavigateToLoginRequestUrl { get; set; }

    /// <summary>
    /// Gets or sets the set of known authority host names for the application.
    /// </summary>
    public IList<string> KnownAuthorities { get; set; } = new List<string>();
}
