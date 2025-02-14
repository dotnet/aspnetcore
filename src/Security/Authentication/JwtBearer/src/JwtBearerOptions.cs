// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.AspNetCore.Authentication.JwtBearer;

/// <summary>
/// Options class provides information needed to control Bearer Authentication handler behavior
/// </summary>
public class JwtBearerOptions : AuthenticationSchemeOptions
{
    private readonly JwtSecurityTokenHandler _defaultHandler = new JwtSecurityTokenHandler();
    private readonly JsonWebTokenHandler _defaultTokenHandler = new JsonWebTokenHandler
    {
        MapInboundClaims = JwtSecurityTokenHandler.DefaultMapInboundClaims
    };

    private bool _mapInboundClaims = JwtSecurityTokenHandler.DefaultMapInboundClaims;

    /// <summary>
    /// Initializes a new instance of <see cref="JwtBearerOptions"/>.
    /// </summary>
    public JwtBearerOptions()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        SecurityTokenValidators = new List<ISecurityTokenValidator> { _defaultHandler };
#pragma warning restore CS0618 // Type or member is obsolete
        TokenHandlers = new List<TokenHandler> { _defaultTokenHandler };
    }

    /// <summary>
    /// Gets or sets if HTTPS is required for the metadata address or authority.
    /// The default is true. This should be disabled only in development environments.
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>
    /// Gets or sets the discovery endpoint for obtaining metadata
    /// </summary>
    public string MetadataAddress { get; set; } = default!;

    /// <summary>
    /// Gets or sets the Authority to use when making OpenIdConnect calls.
    /// </summary>
    public string? Authority { get; set; }

    /// <summary>
    /// Gets or sets a single valid audience value for any received OpenIdConnect token.
    /// This value is passed into TokenValidationParameters.ValidAudience if that property is empty.
    /// </summary>
    /// <value>
    /// The expected audience for any received OpenIdConnect token.
    /// </value>
    public string? Audience { get; set; }

    /// <summary>
    /// Gets or sets the challenge to put in the "WWW-Authenticate" header.
    /// </summary>
    public string Challenge { get; set; } = JwtBearerDefaults.AuthenticationScheme;

    /// <summary>
    /// The object provided by the application to process events raised by the bearer authentication handler.
    /// The application may implement the interface fully, or it may create an instance of JwtBearerEvents
    /// and assign delegates only to the events it wants to process.
    /// </summary>
    public new JwtBearerEvents Events
    {
        get { return (JwtBearerEvents)base.Events!; }
        set { base.Events = value; }
    }

    /// <summary>
    /// The HttpMessageHandler used to retrieve metadata.
    /// This cannot be set at the same time as BackchannelCertificateValidator unless the value
    /// is a WebRequestHandler.
    /// </summary>
    public HttpMessageHandler? BackchannelHttpHandler { get; set; }

    /// <summary>
    /// The Backchannel used to retrieve metadata.
    /// </summary>
    public HttpClient Backchannel { get; set; } = default!;

    /// <summary>
    /// Gets or sets the timeout when using the backchannel to make an http call.
    /// </summary>
    public TimeSpan BackchannelTimeout { get; set; } = TimeSpan.FromMinutes(1);

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
    /// Gets or sets if a metadata refresh should be attempted after a SecurityTokenSignatureKeyNotFoundException. This allows for automatic
    /// recovery in the event of a signature key rollover. This is enabled by default.
    /// </summary>
    public bool RefreshOnIssuerKeyNotFound { get; set; } = true;

    /// <summary>
    /// Gets the ordered list of <see cref="ISecurityTokenValidator"/> used to validate access tokens.
    /// </summary>
    [Obsolete("SecurityTokenValidators is no longer used by default. Use TokenHandlers instead. To continue using SecurityTokenValidators, set UseSecurityTokenValidators to true. See https://aka.ms/aspnetcore8/security-token-changes")]
    public IList<ISecurityTokenValidator> SecurityTokenValidators { get; private set; }

    /// <summary>
    /// Gets the ordered list of <see cref="TokenHandler"/> used to validate access tokens.
    /// </summary>
    public IList<TokenHandler> TokenHandlers { get; private set; }

    /// <summary>
    /// Gets or sets the parameters used to validate identity tokens.
    /// </summary>
    /// <remarks>Contains the types and definitions required for validating a token.</remarks>
    /// <exception cref="ArgumentNullException">if 'value' is null.</exception>
    public TokenValidationParameters TokenValidationParameters { get; set; } = new TokenValidationParameters();

    /// <summary>
    /// Defines whether the bearer token should be stored in the
    /// <see cref="AuthenticationProperties"/> after a successful authorization.
    /// </summary>
    public bool SaveToken { get; set; } = true;

    /// <summary>
    /// Defines whether the token validation errors should be returned to the caller.
    /// Enabled by default, this option can be disabled to prevent the JWT handler
    /// from returning an error and an error_description in the WWW-Authenticate header.
    /// </summary>
    public bool IncludeErrorDetails { get; set; } = true;

    /// <summary>
    /// Gets or sets the <see cref="MapInboundClaims"/> property on the default instance of <see cref="JwtSecurityTokenHandler"/> in SecurityTokenValidators, or <see cref="JsonWebTokenHandler"/> in TokenHandlers, which is used when determining
    /// whether or not to map claim types that are extracted when validating a <see cref="JwtSecurityToken"/> or a <see cref="JsonWebToken"/>.
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
    /// Gets or sets whether <see cref="TokenHandlers"/> or <see cref="SecurityTokenValidators"/> will be used to validate the inbound token.
    /// </summary>
    /// <remarks>
    /// The advantages of using TokenHandlers are:
    /// <para>There is an Async model.</para>
    /// <para>The default token handler is a <see cref="JsonWebTokenHandler"/> which is faster than a <see cref="JwtSecurityTokenHandler"/>.</para>
    /// <para>There is an ability to make use of a Last-Known-Good model for metadata that protects applications when metadata is published with errors.</para>
    /// SecurityTokenValidators can be used when <see cref="TokenValidatedContext.SecurityToken"/> needs a <see cref="JwtSecurityToken"/>.
    /// When using TokenHandlers, <see cref="TokenValidatedContext.SecurityToken"/> will be a <see cref="JsonWebToken"/>. 
    /// </remarks>
    public bool UseSecurityTokenValidators { get; set; }
}
