// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.WsFederation;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Tokens.Saml;
using Microsoft.IdentityModel.Tokens.Saml2;

namespace Microsoft.AspNetCore.Authentication.WsFederation;

/// <summary>
/// Configuration options for <see cref="WsFederationHandler"/>
/// </summary>
public class WsFederationOptions : RemoteAuthenticationOptions
{
    private ICollection<ISecurityTokenValidator> _securityTokenHandlers = new Collection<ISecurityTokenValidator>()
        {
            new Saml2SecurityTokenHandler(),
            new SamlSecurityTokenHandler(),
            new JwtSecurityTokenHandler()
        };

    private TokenValidationParameters _tokenValidationParameters = new TokenValidationParameters();

    /// <summary>
    /// Initializes a new <see cref="WsFederationOptions"/>
    /// </summary>
    public WsFederationOptions()
    {
        CallbackPath = "/signin-wsfed";
        // In ADFS the cleanup messages are sent to the same callback path as the initial login.
        // In AAD it sends the cleanup message to a random Reply Url and there's no deterministic way to configure it.
        //  If you manage to get it configured, then you can set RemoteSignOutPath accordingly.
        RemoteSignOutPath = "/signin-wsfed";
        Events = new WsFederationEvents();

        TokenHandlers = new Collection<TokenHandler>()
        {
            new Saml2SecurityTokenHandler(),
            new SamlSecurityTokenHandler(),
            new JsonWebTokenHandler{ MapInboundClaims = JwtSecurityTokenHandler.DefaultMapInboundClaims }
        };
    }

    /// <summary>
    /// Check that the options are valid.  Should throw an exception if things are not ok.
    /// </summary>
    public override void Validate()
    {
        base.Validate();

        if (ConfigurationManager == null)
        {
            throw new InvalidOperationException($"Provide {nameof(MetadataAddress)}, "
            + $"{nameof(Configuration)}, or {nameof(ConfigurationManager)} to {nameof(WsFederationOptions)}");
        }
    }

    /// <summary>
    /// Configuration provided directly by the developer. If provided, then MetadataAddress and the Backchannel properties
    /// will not be used. This information should not be updated during request processing.
    /// </summary>
    public WsFederationConfiguration? Configuration { get; set; }

    /// <summary>
    /// Gets or sets the address to retrieve the wsFederation metadata
    /// </summary>
    public string? MetadataAddress { get; set; }

    /// <summary>
    /// Responsible for retrieving, caching, and refreshing the configuration from metadata.
    /// If not provided, then one will be created using the MetadataAddress and Backchannel properties.
    /// </summary>
    public IConfigurationManager<WsFederationConfiguration> ConfigurationManager { get; set; } = default!;

    /// <summary>
    /// Gets or sets if a metadata refresh should be attempted after a SecurityTokenSignatureKeyNotFoundException. This allows for automatic
    /// recovery in the event of a signature key rollover. This is enabled by default.
    /// </summary>
    public bool RefreshOnIssuerKeyNotFound { get; set; } = true;

    /// <summary>
    /// Indicates if requests to the CallbackPath may also be for other components. If enabled the handler will pass
    /// requests through that do not contain WsFederation authentication responses. Disabling this and setting the
    /// CallbackPath to a dedicated endpoint may provide better error handling.
    /// This is disabled by default.
    /// </summary>
    public bool SkipUnrecognizedRequests { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="WsFederationEvents"/> to call when processing WsFederation messages.
    /// </summary>
    public new WsFederationEvents Events
    {
        get => (WsFederationEvents)base.Events;
        set => base.Events = value;
    }

    /// <summary>
    /// Gets or sets the collection of <see cref="ISecurityTokenValidator"/> used to read and validate the <see cref="SecurityToken"/>s.
    /// </summary>
    [Obsolete("SecurityTokenHandlers is no longer used by default. Use TokenHandlers instead. To continue using SecurityTokenHandlers, set UseSecurityTokenHandlers to true. See https://aka.ms/aspnetcore8/security-token-changes")]
    public ICollection<ISecurityTokenValidator> SecurityTokenHandlers
    {
        get
        {
            return _securityTokenHandlers;
        }
        set
        {
            _securityTokenHandlers = value ?? throw new ArgumentNullException(nameof(SecurityTokenHandlers));
        }
    }

    /// <summary>
    /// Gets the collection of <see cref="ISecurityTokenValidator"/> used to read and validate the <see cref="SecurityToken"/>s.
    /// </summary>
    public ICollection<TokenHandler> TokenHandlers
    {
        get; private set;
    }

    /// <summary>
    /// Gets or sets the type used to secure data handled by the middleware.
    /// </summary>
    public ISecureDataFormat<AuthenticationProperties> StateDataFormat { get; set; } = default!;

    /// <summary>
    /// Gets or sets the <see cref="TokenValidationParameters"/>
    /// </summary>
    /// <exception cref="ArgumentNullException"> if 'TokenValidationParameters' is null.</exception>
    public TokenValidationParameters TokenValidationParameters
    {
        get
        {
            return _tokenValidationParameters;
        }
        set
        {
            _tokenValidationParameters = value ?? throw new ArgumentNullException(nameof(TokenValidationParameters));
        }
    }

    /// <summary>
    /// Gets or sets the 'wreply'. CallbackPath must be set to match or cleared so it can be generated dynamically.
    /// This field is optional. If not set then it will be generated from the current request and the CallbackPath.
    /// </summary>
    public string? Wreply { get; set; }

    /// <summary>
    /// Gets or sets the 'wreply' value used during sign-out.
    /// If none is specified then the value from the Wreply field is used.
    /// </summary>
    public string? SignOutWreply { get; set; }

    /// <summary>
    /// Gets or sets the 'wtrealm'.
    /// </summary>
    public string? Wtrealm { get; set; }

    /// <summary>
    /// Indicates that the authentication session lifetime (e.g. cookies) should match that of the authentication token.
    /// If the token does not provide lifetime information then normal session lifetimes will be used.
    /// This is enabled by default.
    /// </summary>
    public bool UseTokenLifetime { get; set; } = true;

    /// <summary>
    /// Gets or sets if HTTPS is required for the metadata address or authority.
    /// The default is true. This should be disabled only in development environments.
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>
    /// The Ws-Federation protocol allows the user to initiate logins without contacting the application for a Challenge first.
    /// However, that flow is susceptible to XSRF and other attacks so it is disabled here by default.
    /// </summary>
    public bool AllowUnsolicitedLogins { get; set; }

    /// <summary>
    /// Requests received on this path will cause the handler to invoke SignOut using the SignOutScheme.
    /// </summary>
    public PathString RemoteSignOutPath { get; set; }

    /// <summary>
    /// The Authentication Scheme to use with SignOutAsync from RemoteSignOutPath. SignInScheme will be used if this
    /// is not set.
    /// </summary>
    public string? SignOutScheme { get; set; }

    /// <summary>
    /// SaveTokens is not supported in WsFederation
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public new bool SaveTokens { get; set; }

    /// <summary>
    /// Gets or sets whether <see cref="TokenHandlers"/> or <see cref="SecurityTokenHandlers"/> will be used to validate the inbound token.
    /// </summary>
    /// <remarks>
    /// The advantages of using the TokenHandlers are:
    /// <para>There is an Async model.</para>
    /// <para>The default token handler for JsonWebTokens is a <see cref="JsonWebTokenHandler"/> which is faster than a <see cref="JwtSecurityTokenHandler"/>.</para>
    /// <para>There is an ability to make use of a Last-Known-Good model for metadata that protects applications when metadata is published with errors.</para>
    /// SecurityTokenHandlers can be used when <see cref="SecurityTokenValidatedContext.SecurityToken"/> needs a <see cref="JwtSecurityToken"/> when the security token is a JWT.
    /// When using TokenHandlers, <see cref="SecurityTokenValidatedContext.SecurityToken"/> will be a <see cref="JsonWebToken"/> when the security token is a JWT.
    /// </remarks>
    public bool UseSecurityTokenHandlers { get; set; }
}
