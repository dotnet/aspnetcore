// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.AspNetCore.Authentication.OpenIdConnect;

/// <summary>
/// Configuration options for <see cref="OpenIdConnectHandler"/>
/// </summary>
public class OpenIdConnectOptions : RemoteAuthenticationOptions
{
    private CookieBuilder _nonceCookieBuilder;
    private readonly JwtSecurityTokenHandler _defaultHandler = new JwtSecurityTokenHandler();
    private readonly JsonWebTokenHandler _defaultTokenHandler = new JsonWebTokenHandler
    {
        MapInboundClaims = JwtSecurityTokenHandler.DefaultMapInboundClaims
    };

    private bool _mapInboundClaims = JwtSecurityTokenHandler.DefaultMapInboundClaims;

    /// <summary>
    /// Initializes a new <see cref="OpenIdConnectOptions"/>
    /// </summary>
    /// <remarks>
    /// Defaults:
    /// <para>AddNonceToRequest: true.</para>
    /// <para>BackchannelTimeout: 1 minute.</para>
    /// <para>ProtocolValidator: new <see cref="OpenIdConnectProtocolValidator"/>.</para>
    /// <para>RefreshOnIssuerKeyNotFound: true</para>
    /// <para>ResponseType: <see cref="OpenIdConnectResponseType.IdToken"/></para>
    /// <para>Scope: <see cref="OpenIdConnectScope.OpenIdProfile"/>.</para>
    /// <para>TokenValidationParameters: new <see cref="TokenValidationParameters"/> with AuthenticationScheme = authenticationScheme.</para>
    /// <para>UseTokenLifetime: false.</para>
    /// </remarks>
    public OpenIdConnectOptions()
    {
        CallbackPath = new PathString("/signin-oidc");
        SignedOutCallbackPath = new PathString("/signout-callback-oidc");
        RemoteSignOutPath = new PathString("/signout-oidc");
#pragma warning disable CS0618 // Type or member is obsolete
        SecurityTokenValidator = _defaultHandler;
#pragma warning restore CS0618 // Type or member is obsolete
        TokenHandler = _defaultTokenHandler;

        Events = new OpenIdConnectEvents();
        Scope.Add("openid");
        Scope.Add("profile");

        ClaimActions.DeleteClaim("nonce");
        ClaimActions.DeleteClaim("aud");
        ClaimActions.DeleteClaim("azp");
        ClaimActions.DeleteClaim("acr");
        ClaimActions.DeleteClaim("iss");
        ClaimActions.DeleteClaim("iat");
        ClaimActions.DeleteClaim("nbf");
        ClaimActions.DeleteClaim("exp");
        ClaimActions.DeleteClaim("at_hash");
        ClaimActions.DeleteClaim("c_hash");
        ClaimActions.DeleteClaim("ipaddr");
        ClaimActions.DeleteClaim("platf");
        ClaimActions.DeleteClaim("ver");

        // http://openid.net/specs/openid-connect-core-1_0.html#StandardClaims
        ClaimActions.MapUniqueJsonKey("sub", "sub");
        ClaimActions.MapUniqueJsonKey("name", "name");
        ClaimActions.MapUniqueJsonKey("given_name", "given_name");
        ClaimActions.MapUniqueJsonKey("family_name", "family_name");
        ClaimActions.MapUniqueJsonKey("profile", "profile");
        ClaimActions.MapUniqueJsonKey("email", "email");

        _nonceCookieBuilder = new OpenIdConnectNonceCookieBuilder(this)
        {
            Name = OpenIdConnectDefaults.CookieNoncePrefix,
            HttpOnly = true,
            SameSite = SameSiteMode.None,
            SecurePolicy = CookieSecurePolicy.Always,
            IsEssential = true,
        };
    }

    /// <summary>
    /// Check that the options are valid.  Should throw an exception if things are not ok.
    /// </summary>
    public override void Validate()
    {
        base.Validate();

        if (MaxAge.HasValue && MaxAge.Value < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxAge), MaxAge.Value, "The value must not be a negative TimeSpan.");
        }

        ArgumentException.ThrowIfNullOrEmpty(ClientId);

        if (!CallbackPath.HasValue)
        {
            throw new ArgumentException("Options.CallbackPath must be provided.", nameof(CallbackPath));
        }

        if (ConfigurationManager == null)
        {
            throw new InvalidOperationException($"Provide {nameof(Authority)}, {nameof(MetadataAddress)}, "
            + $"{nameof(Configuration)}, or {nameof(ConfigurationManager)} to {nameof(OpenIdConnectOptions)}");
        }
    }

    /// <summary>
    /// Gets or sets the Authority to use when making OpenIdConnect calls.
    /// </summary>
    public string? Authority { get; set; }

    /// <summary>
    /// Gets or sets the 'client_id'.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Gets or sets the 'client_secret'.
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Configuration provided directly by the developer. If provided, then MetadataAddress and the Backchannel properties
    /// will not be used. This information should not be updated during request processing.
    /// </summary>
    public OpenIdConnectConfiguration? Configuration { get; set; }

    /// <summary>
    /// Responsible for retrieving, caching, and refreshing the configuration from metadata.
    /// If not provided, then one will be created using the MetadataAddress and Backchannel properties.
    /// </summary>
    public IConfigurationManager<OpenIdConnectConfiguration>? ConfigurationManager { get; set; }

    /// <summary>
    /// Boolean to set whether the handler should go to user info endpoint to retrieve additional claims or not after creating an identity from id_token received from token endpoint.
    /// The default is 'false'.
    /// </summary>
    public bool GetClaimsFromUserInfoEndpoint { get; set; }

    /// <summary>
    /// A collection of claim actions used to select values from the json user data and create Claims.
    /// </summary>
    public ClaimActionCollection ClaimActions { get; } = new ClaimActionCollection();

    /// <summary>
    /// Gets or sets if HTTPS is required for the metadata address or authority.
    /// The default is true. This should be disabled only in development environments.
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>
    /// Gets or sets the discovery endpoint for obtaining metadata
    /// </summary>
    public string? MetadataAddress { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="OpenIdConnectEvents"/> to notify when processing OpenIdConnect messages.
    /// </summary>
    public new OpenIdConnectEvents Events
    {
        get => (OpenIdConnectEvents)base.Events;
        set => base.Events = value;
    }

    /// <summary>
    /// Gets or sets the 'max_age'. If set the 'max_age' parameter will be sent with the authentication request. If the identity
    /// provider has not actively authenticated the user within the length of time specified, the user will be prompted to
    /// re-authenticate. By default no max_age is specified.
    /// </summary>
    public TimeSpan? MaxAge { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="OpenIdConnectProtocolValidator"/> that is used to ensure that the 'id_token' received
    /// is valid per: http://openid.net/specs/openid-connect-core-1_0.html#IDTokenValidation
    /// </summary>
    /// <exception cref="ArgumentNullException">if 'value' is null.</exception>
    public OpenIdConnectProtocolValidator ProtocolValidator { get; set; } = new OpenIdConnectProtocolValidator()
    {
        RequireStateValidation = false,
        NonceLifetime = TimeSpan.FromMinutes(15)
    };

    /// <summary>
    /// The request path within the application's base path where the user agent will be returned after sign out from the identity provider.
    /// See post_logout_redirect_uri from http://openid.net/specs/openid-connect-session-1_0.html#RedirectionAfterLogout.
    /// </summary>
    public PathString SignedOutCallbackPath { get; set; }

    /// <summary>
    /// The uri where the user agent will be redirected to after application is signed out from the identity provider.
    /// The redirect will happen after the SignedOutCallbackPath is invoked.
    /// </summary>
    /// <remarks>This URI can be out of the application's domain. By default it points to the root.</remarks>
    public string SignedOutRedirectUri { get; set; } = "/";

    /// <summary>
    /// Gets or sets if a metadata refresh should be attempted after a SecurityTokenSignatureKeyNotFoundException. This allows for automatic
    /// recovery in the event of a signature key rollover. This is enabled by default.
    /// </summary>
    public bool RefreshOnIssuerKeyNotFound { get; set; } = true;

    /// <summary>
    /// Gets or sets the method used to redirect the user agent to the identity provider.
    /// </summary>
    public OpenIdConnectRedirectBehavior AuthenticationMethod { get; set; } = OpenIdConnectRedirectBehavior.RedirectGet;

    /// <summary>
    /// Gets or sets the 'resource'.
    /// </summary>
    public string? Resource { get; set; }

    /// <summary>
    /// Gets or sets the 'response_mode'.
    /// </summary>
    public string ResponseMode { get; set; } = OpenIdConnectResponseMode.FormPost;

    /// <summary>
    /// Gets or sets the 'response_type'.
    /// </summary>
    public string ResponseType { get; set; } = OpenIdConnectResponseType.IdToken;

    /// <summary>
    /// Gets or sets the 'prompt'.
    /// </summary>
    public string? Prompt { get; set; }

    /// <summary>
    /// Gets the list of permissions to request.
    /// </summary>
    public ICollection<string> Scope { get; } = new HashSet<string>();

    /// <summary>
    /// Gets the additional parameters that will be included in the authorization request.
    /// </summary>
    /// <remarks>
    /// The additional parameters can be used to customize the authorization request,
    /// providing extra information or fulfilling specific requirements of the OpenIdConnect provider.
    /// These parameters are typically, but not always, appended to the query string.
    /// </remarks>
    public IDictionary<string, string> AdditionalAuthorizationParameters { get; } = new Dictionary<string, string>();

    /// <summary>
    /// Requests received on this path will cause the handler to invoke SignOut using the SignOutScheme.
    /// </summary>
    public PathString RemoteSignOutPath { get; set; }

    /// <summary>
    /// The Authentication Scheme to use with SignOut on the SignOutPath. SignInScheme will be used if this
    /// is not set.
    /// </summary>
    public string? SignOutScheme { get; set; }

    /// <summary>
    /// Gets or sets the type used to secure data handled by the handler.
    /// </summary>
    public ISecureDataFormat<AuthenticationProperties> StateDataFormat { get; set; } = default!;

    /// <summary>
    /// Gets or sets the type used to secure strings used by the handler.
    /// </summary>
    public ISecureDataFormat<string> StringDataFormat { get; set; } = default!;

    /// <summary>
    /// Gets or sets the <see cref="ISecurityTokenValidator"/> used to validate identity tokens.
    /// </summary>
    [Obsolete("SecurityTokenValidator is no longer used by default. Use TokenHandler instead. To continue using SecurityTokenValidator, set UseSecurityTokenValidator to true. See https://aka.ms/aspnetcore8/security-token-changes")]
    public ISecurityTokenValidator SecurityTokenValidator { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="TokenHandler"/> used to validate identity tokens.
    /// <para>
    /// This will be used instead of <see cref="SecurityTokenValidator"/> if <see cref="UseSecurityTokenValidator"/> is <see langword="false"/>.
    /// </para>
    /// </summary>
    public TokenHandler TokenHandler { get; set; }

    /// <summary>
    /// Gets or sets the parameters used to validate identity tokens.
    /// </summary>
    /// <remarks>Contains the types and definitions required for validating a token.</remarks>
    public TokenValidationParameters TokenValidationParameters { get; set; } = new TokenValidationParameters();

    /// <summary>
    /// Indicates that the authentication session lifetime (e.g. cookies) should match that of the authentication token.
    /// If the token does not provide lifetime information then normal session lifetimes will be used.
    /// This is disabled by default.
    /// </summary>
    public bool UseTokenLifetime { get; set; }

    /// <summary>
    /// Indicates if requests to the CallbackPath may also be for other components. If enabled the handler will pass
    /// requests through that do not contain OpenIdConnect authentication responses. Disabling this and setting the
    /// CallbackPath to a dedicated endpoint may provide better error handling.
    /// This is disabled by default.
    /// </summary>
    public bool SkipUnrecognizedRequests { get; set; }

    /// <summary>
    /// Indicates whether telemetry should be disabled. When this feature is enabled,
    /// the assembly version of the Microsoft IdentityModel packages is sent to the
    /// remote OpenID Connect provider as an authorization/logout request parameter.
    /// </summary>
    public bool DisableTelemetry { get; set; }

    /// <summary>
    /// Determines the settings used to create the nonce cookie before the
    /// cookie gets added to the response.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The value of <see cref="CookieBuilder.Name"/> is treated as the prefix to the cookie name, and defaults to <see cref="OpenIdConnectDefaults.CookieNoncePrefix"/>.
    /// </para>
    /// <list type="bullet">
    /// <item><description><see cref="CookieBuilder.SameSite"/> defaults to <see cref="SameSiteMode.None"/>.</description></item>
    /// <item><description><see cref="CookieBuilder.HttpOnly"/> defaults to <c>true</c>.</description></item>
    /// <item><description><see cref="CookieBuilder.IsEssential"/> defaults to <c>true</c>.</description></item>
    /// <item><description><see cref="CookieBuilder.SecurePolicy"/> defaults to <see cref="CookieSecurePolicy.Always"/>.</description></item>
    /// </list>
    /// </remarks>
    public CookieBuilder NonceCookie
    {
        get => _nonceCookieBuilder;
        set => _nonceCookieBuilder = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Enables or disables the use of the Proof Key for Code Exchange (PKCE) standard.
    /// This only applies when the <see cref="ResponseType"/> is set to <see cref="OpenIdConnectResponseType.Code"/>.
    /// See <see href="https://tools.ietf.org/html/rfc7636"/>.
    /// The default value is `true`.
    /// </summary>
    public bool UsePkce { get; set; } = true;

    private sealed class OpenIdConnectNonceCookieBuilder : RequestPathBaseCookieBuilder
    {
        private readonly OpenIdConnectOptions _options;

        public OpenIdConnectNonceCookieBuilder(OpenIdConnectOptions oidcOptions)
        {
            _options = oidcOptions;
        }

        protected override string AdditionalPath => _options.CallbackPath;

        public override CookieOptions Build(HttpContext context, DateTimeOffset expiresFrom)
        {
            var cookieOptions = base.Build(context, expiresFrom);

            if (!Expiration.HasValue || !cookieOptions.Expires.HasValue)
            {
                cookieOptions.Expires = expiresFrom.Add(_options.ProtocolValidator.NonceLifetime);
            }

            return cookieOptions;
        }
    }

    /// <summary>
    /// Gets or sets how often an automatic metadata refresh should occur.
    /// </summary>
    /// <value>
    /// Defaults to <see cref="ConfigurationManager{OpenIdConnectConfiguration}.DefaultAutomaticRefreshInterval" />.
    /// </value>
    public TimeSpan AutomaticRefreshInterval { get; set; } = ConfigurationManager<OpenIdConnectConfiguration>.DefaultAutomaticRefreshInterval;

    /// <summary>
    /// Gets or sets the minimum time between retrievals, in the event that a retrieval failed, or that a refresh was explicitly requested.
    /// </summary>
    /// <value>
    /// Defaults to <see cref="ConfigurationManager{OpenIdConnectConfiguration}.DefaultRefreshInterval" />.
    /// </value>
    public TimeSpan RefreshInterval { get; set; } = ConfigurationManager<OpenIdConnectConfiguration>.DefaultRefreshInterval;

    /// <summary>
    /// Gets or sets the <see cref="MapInboundClaims"/> property on the default instance of <see cref="JwtSecurityTokenHandler"/> in SecurityTokenValidator
    /// and default instance of <see cref="JsonWebTokenHandler"/> in TokenHandler, which is used when determining
    /// whether or not to map claim types that are extracted when validating a <see cref="JwtSecurityToken"/>.
    /// <para>If this is set to true, the Claim Type is set to the JSON claim 'name' after translating using this mapping. Otherwise, no mapping occurs.</para>
    /// <para>The default value is true.</para>
    /// </summary>
    public bool MapInboundClaims
    {
        get => _mapInboundClaims;
        set
        {
            _mapInboundClaims = value;
            _defaultHandler.MapInboundClaims = value;
            _defaultTokenHandler.MapInboundClaims = value;
        }
    }

    /// <summary>
    /// Gets or sets whether to use the <see cref="TokenHandler"/> or the <see cref="SecurityTokenValidator"/> for validating identity tokens.
    /// </summary>
    /// <remarks>
    /// The advantages of using TokenHandler are:
    /// <para>There is an Async model.</para>
    /// <para>The default token handler is a <see cref="JsonWebTokenHandler"/> which is faster than a <see cref="JwtSecurityTokenHandler"/>.</para>
    /// <para>There is an ability to make use of a Last-Known-Good model for metadata that protects applications when metadata is published with errors.</para>
    /// SecurityTokenValidator can be used when <see cref="TokenValidatedContext.SecurityToken"/> needs a <see cref="JwtSecurityToken"/>.
    /// When using TokenHandler, <see cref="TokenValidatedContext.SecurityToken"/> will be a <see cref="JsonWebToken"/>.
    /// </remarks>
    public bool UseSecurityTokenValidator { get; set; }

    /// <summary>
    /// Controls wether the handler should push authorization parameters on the
    /// backchannel before redirecting to the identity provider. See <see
    /// href="https://tools.ietf.org/html/9126"/>.
    /// </summary>
    /// <value>Defaults to <see
    /// cref="PushedAuthorizationBehavior.UseIfAvailable" />.</value>
    public PushedAuthorizationBehavior PushedAuthorizationBehavior { get; set; } = PushedAuthorizationBehavior.UseIfAvailable;
}
