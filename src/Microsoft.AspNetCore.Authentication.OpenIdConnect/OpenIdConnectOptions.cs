// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Configuration options for <see cref="OpenIdConnectMiddleware"/>
    /// </summary>
    public class OpenIdConnectOptions : RemoteAuthenticationOptions
    {
        /// <summary>
        /// Initializes a new <see cref="OpenIdConnectOptions"/>
        /// </summary>
        public OpenIdConnectOptions()
            : this(OpenIdConnectDefaults.AuthenticationScheme)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="OpenIdConnectOptions"/>
        /// </summary>
        /// <remarks>
        /// Defaults:
        /// <para>AddNonceToRequest: true.</para>
        /// <para>BackchannelTimeout: 1 minute.</para>
        /// <para>Caption: <see cref="OpenIdConnectDefaults.Caption"/>.</para>
        /// <para>ProtocolValidator: new <see cref="OpenIdConnectProtocolValidator"/>.</para>
        /// <para>RefreshOnIssuerKeyNotFound: true</para>
        /// <para>ResponseType: <see cref="OpenIdConnectResponseType.CodeIdToken"/></para>
        /// <para>Scope: <see cref="OpenIdConnectScope.OpenIdProfile"/>.</para>
        /// <para>TokenValidationParameters: new <see cref="TokenValidationParameters"/> with AuthenticationScheme = authenticationScheme.</para>
        /// <para>UseTokenLifetime: false.</para>
        /// </remarks>
        /// <param name="authenticationScheme"> will be used to when creating the <see cref="System.Security.Claims.ClaimsIdentity"/> for the AuthenticationScheme property.</param>
        public OpenIdConnectOptions(string authenticationScheme)
        {
            AuthenticationScheme = authenticationScheme;
            AutomaticChallenge = true;
            DisplayName = OpenIdConnectDefaults.Caption;
            CallbackPath = new PathString("/signin-oidc");
            SignedOutCallbackPath = new PathString("/signout-callback-oidc");
            RemoteSignOutPath = new PathString("/signout-oidc");

            Events = new OpenIdConnectEvents();
            Scope.Add("openid");
            Scope.Add("profile");
        }

        /// <summary>
        /// Gets or sets the Authority to use when making OpenIdConnect calls.
        /// </summary>
        public string Authority { get; set; }

        /// <summary>
        /// Gets or sets the 'client_id'.
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Gets or sets the 'client_secret'.
        /// </summary>
        public string ClientSecret { get; set; }

        /// <summary>
        /// Configuration provided directly by the developer. If provided, then MetadataAddress and the Backchannel properties
        /// will not be used. This information should not be updated during request processing.
        /// </summary>
        public OpenIdConnectConfiguration Configuration { get; set; }

        /// <summary>
        /// Responsible for retrieving, caching, and refreshing the configuration from metadata.
        /// If not provided, then one will be created using the MetadataAddress and Backchannel properties.
        /// </summary>
        public IConfigurationManager<OpenIdConnectConfiguration> ConfigurationManager { get; set; }

        /// <summary>
        /// Boolean to set whether the middleware should go to user info endpoint to retrieve additional claims or not after creating an identity from id_token received from token endpoint.
        /// </summary>
        public bool GetClaimsFromUserInfoEndpoint { get; set; }

        /// <summary>
        /// Gets or sets if HTTPS is required for the metadata address or authority.
        /// The default is true. This should be disabled only in development environments.
        /// </summary>
        public bool RequireHttpsMetadata { get; set; } = true;

        /// <summary>
        /// Gets or sets the discovery endpoint for obtaining metadata
        /// </summary>
        public string MetadataAddress { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IOpenIdConnectEvents"/> to notify when processing OpenIdConnect messages.
        /// </summary>
        public new IOpenIdConnectEvents Events
        {
            get { return (IOpenIdConnectEvents)base.Events; }
            set { base.Events = value; }
        }

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
        /// </summary>
        public PathString SignedOutCallbackPath { get; set; }

        /// <summary>
        /// The uri where the user agent will be returned to after application is signed out from the identity provider.
        /// The redirect will happen after the SignedOutCallbackPath is invoked.
        /// </summary>
        /// <remarks>This URI can be out of the application's domain. By default it points to the root.</remarks>
        public string PostLogoutRedirectUri { get; set; } = "/";

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
        public string Resource { get; set; }

        /// <summary>
        /// Gets or sets the 'response_mode'.
        /// </summary>
        public string ResponseMode { get; set; } = OpenIdConnectResponseMode.FormPost;

        /// <summary>
        /// Gets or sets the 'response_type'.
        /// </summary>
        public string ResponseType { get; set; } = OpenIdConnectResponseType.IdToken;

        /// <summary>
        /// Gets the list of permissions to request.
        /// </summary>
        public ICollection<string> Scope { get; } = new HashSet<string>();

        /// <summary>
        /// Requests received on this path will cause the middleware to invoke SignOut using the SignInScheme.
        /// </summary>
        public PathString RemoteSignOutPath { get; set; }

        /// <summary>
        /// The Authentication Scheme to use with SignOut on the SignOutPath. SignInScheme will be used if this
        /// is not set.
        /// </summary>
        public string SignOutScheme { get; set; }

        /// <summary>
        /// Gets or sets the type used to secure data handled by the middleware.
        /// </summary>
        public ISecureDataFormat<AuthenticationProperties> StateDataFormat { get; set; }

        /// <summary>
        /// Gets or sets the type used to secure strings used by the middleware.
        /// </summary>
        public ISecureDataFormat<string> StringDataFormat { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ISecurityTokenValidator"/> used to validate identity tokens.
        /// </summary>
        public ISecurityTokenValidator SecurityTokenValidator { get; set; } = new JwtSecurityTokenHandler();

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
        /// Indicates if requests to the CallbackPath may also be for other components. If enabled the middleware will pass
        /// requests through that do not contain OpenIdConnect authentication responses. Disabling this and setting the
        /// CallbackPath to a dedicated endpoint may provide better error handling.
        /// This is disabled by default.
        /// </summary>
        public bool SkipUnrecognizedRequests { get; set; } = false;
    }
}
