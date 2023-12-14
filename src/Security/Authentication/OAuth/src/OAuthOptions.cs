// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;

namespace Microsoft.AspNetCore.Authentication.OAuth;

/// <summary>
/// Configuration options OAuth.
/// </summary>
public class OAuthOptions : RemoteAuthenticationOptions
{
    /// <summary>
    /// Initializes a new instance of <see cref="OAuthOptions"/>.
    /// </summary>
    public OAuthOptions()
    {
        Events = new OAuthEvents();
    }

    /// <summary>
    /// Check that the options are valid. Should throw an exception if things are not ok.
    /// </summary>
    public override void Validate()
    {
        base.Validate();

        if (string.IsNullOrEmpty(ClientId))
        {
            throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Exception_OptionMustBeProvided, nameof(ClientId)), nameof(ClientId));
        }

        if (string.IsNullOrEmpty(ClientSecret))
        {
            throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Exception_OptionMustBeProvided, nameof(ClientSecret)), nameof(ClientSecret));
        }

        if (string.IsNullOrEmpty(AuthorizationEndpoint))
        {
            throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Exception_OptionMustBeProvided, nameof(AuthorizationEndpoint)), nameof(AuthorizationEndpoint));
        }

        if (string.IsNullOrEmpty(TokenEndpoint))
        {
            throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Exception_OptionMustBeProvided, nameof(TokenEndpoint)), nameof(TokenEndpoint));
        }

        if (!CallbackPath.HasValue)
        {
            throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Exception_OptionMustBeProvided, nameof(CallbackPath)), nameof(CallbackPath));
        }
    }

    /// <summary>
    /// Gets or sets the provider-assigned client id.
    /// </summary>
    public string ClientId { get; set; } = default!;

    /// <summary>
    /// Gets or sets the provider-assigned client secret.
    /// </summary>
    public string ClientSecret { get; set; } = default!;

    /// <summary>
    /// Gets or sets the URI where the client will be redirected to authenticate.
    /// </summary>
    public string AuthorizationEndpoint { get; set; } = default!;

    /// <summary>
    /// Gets or sets the URI the middleware will access to exchange the OAuth token.
    /// </summary>
    public string TokenEndpoint { get; set; } = default!;

    /// <summary>
    /// Gets or sets the URI the middleware will access to obtain the user information.
    /// This value is not used in the default implementation, it is for use in custom implementations of
    /// <see cref="OAuthEvents.OnCreatingTicket" />.
    /// </summary>
    public string UserInformationEndpoint { get; set; } = default!;

    /// <summary>
    /// Gets or sets the <see cref="OAuthEvents"/> used to handle authentication events.
    /// </summary>
    public new OAuthEvents Events
    {
        get { return (OAuthEvents)base.Events; }
        set { base.Events = value; }
    }

    /// <summary>
    /// A collection of claim actions used to select values from the json user data and create Claims.
    /// </summary>
    public ClaimActionCollection ClaimActions { get; } = new ClaimActionCollection();

    /// <summary>
    /// Gets the list of permissions to request.
    /// </summary>
    public ICollection<string> Scope { get; } = new HashSet<string>();

    /// <summary>
    /// Gets or sets the type used to secure data handled by the middleware.
    /// </summary>
    public ISecureDataFormat<AuthenticationProperties> StateDataFormat { get; set; } = default!;

    /// <summary>
    /// Enables or disables the use of the Proof Key for Code Exchange (PKCE) standard. See <see href="https://tools.ietf.org/html/rfc7636"/>.
    /// The default value is `false` but derived handlers should enable this if their provider supports it.
    /// </summary>
    public bool UsePkce { get; set; }
}
