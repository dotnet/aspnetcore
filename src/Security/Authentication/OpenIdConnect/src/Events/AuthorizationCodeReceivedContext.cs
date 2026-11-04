// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Authentication.OpenIdConnect;

/// <summary>
/// This Context can be used to be informed when an 'AuthorizationCode' is received over the OpenIdConnect protocol.
/// </summary>
public class AuthorizationCodeReceivedContext : RemoteAuthenticationContext<OpenIdConnectOptions>
{
    /// <summary>
    /// Creates a <see cref="AuthorizationCodeReceivedContext"/>
    /// </summary>
    public AuthorizationCodeReceivedContext(
        HttpContext context,
        AuthenticationScheme scheme,
        OpenIdConnectOptions options,
        AuthenticationProperties properties)
        : base(context, scheme, options, properties) { }

    /// <summary>
    /// Gets or sets the <see cref="OpenIdConnectMessage"/>.
    /// </summary>
    public OpenIdConnectMessage ProtocolMessage { get; set; } = default!;

    /// <summary>
    /// Gets or sets the <see cref="JwtSecurityToken"/> that was received in the authentication response, if any.
    /// </summary>
    public JwtSecurityToken? JwtSecurityToken { get; set; }

    /// <summary>
    /// The request that will be sent to the token endpoint and is available for customization.
    /// </summary>
    public OpenIdConnectMessage? TokenEndpointRequest { get; set; }

    /// <summary>
    /// The configured communication channel to the identity provider for use when making custom requests to the token endpoint.
    /// </summary>
    public HttpClient Backchannel { get; internal set; } = default!;

    /// <summary>
    /// If the developer chooses to redeem the code themselves then they can provide the resulting tokens here. This is the
    /// same as calling HandleCodeRedemption. If set then the handler will not attempt to redeem the code. An IdToken
    /// is required if one had not been previously received in the authorization response. An access token is optional
    /// if the handler is to contact the user-info endpoint.
    /// </summary>
    public OpenIdConnectMessage? TokenEndpointResponse { get; set; }

    /// <summary>
    /// Indicates if the developer choose to handle (or skip) the code redemption. If true then the handler will not attempt
    /// to redeem the code. See HandleCodeRedemption and TokenEndpointResponse.
    /// </summary>
    public bool HandledCodeRedemption => TokenEndpointResponse != null;

    /// <summary>
    /// Tells the handler to skip the code redemption process. The developer may have redeemed the code themselves, or
    /// decided that the redemption was not required. If tokens were retrieved that are needed for further processing then
    /// call one of the overloads that allows providing tokens. An IdToken is required if one had not been previously received
    /// in the authorization response. An access token can optionally be provided for the handler to contact the
    /// user-info endpoint. Calling this is the same as setting TokenEndpointResponse.
    /// </summary>
    public void HandleCodeRedemption()
    {
        TokenEndpointResponse = new OpenIdConnectMessage();
    }

    /// <summary>
    /// Tells the handler to skip the code redemption process. The developer may have redeemed the code themselves, or
    /// decided that the redemption was not required. If tokens were retrieved that are needed for further processing then
    /// call one of the overloads that allows providing tokens. An IdToken is required if one had not been previously received
    /// in the authorization response. An access token can optionally be provided for the handler to contact the
    /// user-info endpoint. Calling this is the same as setting TokenEndpointResponse.
    /// </summary>
    public void HandleCodeRedemption(string accessToken, string idToken)
    {
        TokenEndpointResponse = new OpenIdConnectMessage() { AccessToken = accessToken, IdToken = idToken };
    }

    /// <summary>
    /// Tells the handler to skip the code redemption process. The developer may have redeemed the code themselves, or
    /// decided that the redemption was not required. If tokens were retrieved that are needed for further processing then
    /// call one of the overloads that allows providing tokens. An IdToken is required if one had not been previously received
    /// in the authorization response. An access token can optionally be provided for the handler to contact the
    /// user-info endpoint. Calling this is the same as setting TokenEndpointResponse.
    /// </summary>
    public void HandleCodeRedemption(OpenIdConnectMessage tokenEndpointResponse)
    {
        TokenEndpointResponse = tokenEndpointResponse;
    }
}
