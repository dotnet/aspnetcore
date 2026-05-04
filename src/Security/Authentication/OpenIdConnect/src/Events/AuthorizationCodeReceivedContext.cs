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

    /// <summary>
    /// Indicates if the <see cref="OpenIdConnectEvents.OnAuthorizationCodeReceived"/> event chose to handle client
    /// authentication for the token endpoint request. If <see langword="true"/>, the handler will not set
    /// client credentials (such as <c>client_secret</c>) on the <see cref="TokenEndpointRequest"/>,
    /// allowing the event to provide alternative authentication, such as <c>private_key_jwt</c> or
    /// a federated client assertion.
    /// </summary>
    public bool HandledClientAuthentication { get; private set; }

    /// <summary>
    /// Tells the handler to skip setting client authentication properties for the token endpoint request.
    /// The handler sets the <c>client_secret</c> from the <see cref="OpenIdConnectClientRegistration"/>
    /// returned by <see cref="OpenIdConnectHandler.ResolveClientRegistrationAsync"/> by default,
    /// but the <see cref="OpenIdConnectEvents.OnAuthorizationCodeReceived"/> event may replace that with an alternative
    /// authentication mode, such as <c>private_key_jwt</c> or a federated client assertion.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When this method is called, the handler will remove <c>client_secret</c> from the <see cref="TokenEndpointRequest"/>.
    /// The event handler is responsible for setting appropriate authentication parameters on the
    /// <see cref="TokenEndpointRequest"/>, such as <c>client_assertion</c> and <c>client_assertion_type</c>.
    /// This follows the same pattern as <see cref="PushedAuthorizationContext.HandledClientAuthentication"/>
    /// for pushed authorization requests, extending it to the token endpoint code redemption flow.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <see cref="HandleCodeRedemption()"/> has already been called, since both operations are mutually exclusive.
    /// </exception>
    public void HandleClientAuthentication()
    {
        if (HandledCodeRedemption)
        {
            throw new InvalidOperationException("HandleClientAuthentication cannot be called when HandleCodeRedemption has already been called.");
        }
        HandledClientAuthentication = true;
    }
}
