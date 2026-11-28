// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.IdentityModel.Tokens;

namespace Microsoft.AspNetCore.Authentication.OpenIdConnect;

/// <summary>
/// Represents the client registration information resolved for a single OpenID Connect authentication request.
/// This class groups the per-request client identity, credentials, and token validation settings used throughout
/// the authentication flow (challenge, token redemption, and protocol validation).
/// </summary>
/// <remarks>
/// <para>
/// Override <see cref="OpenIdConnectHandler.ResolveClientRegistrationAsync"/> to return a custom
/// <see cref="OpenIdConnectClientRegistration"/> per request. This enables multi-tenant scenarios where different
/// client registrations are used depending on the incoming request context.
/// </para>
/// <para>
/// This class is intentionally not sealed. Future versions may add additional properties
/// (such as per-request scopes or resource indicators) without introducing breaking changes.
/// </para>
/// </remarks>
public class OpenIdConnectClientRegistration
{
    /// <summary>
    /// Gets or sets the <c>client_id</c> to use for this authentication request.
    /// </summary>
    /// <remarks>
    /// This value is used as the <c>client_id</c> parameter in the authorization request, the token endpoint request,
    /// and for protocol validation of authentication and token responses. It defaults to
    /// <see cref="OpenIdConnectOptions.ClientId"/>.
    /// </remarks>
    public string ClientId { get; set; } = default!;

    /// <summary>
    /// Gets or sets the <c>client_secret</c> to use for this authentication request, or <see langword="null"/>
    /// if no shared secret is used (for example, when using <c>private_key_jwt</c> or federated credentials).
    /// </summary>
    /// <remarks>
    /// This value is used as the <c>client_secret</c> parameter in the token endpoint request and in pushed
    /// authorization requests. It defaults to <see cref="OpenIdConnectOptions.ClientSecret"/>.
    /// When set to <see langword="null"/> or empty, the handler will not include a <c>client_secret</c> parameter.
    /// For advanced client authentication scenarios (such as client assertions), use
    /// <see cref="AuthorizationCodeReceivedContext.HandleClientAuthentication"/> or
    /// <see cref="PushedAuthorizationContext.HandleClientAuthentication"/> to take full control.
    /// </remarks>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="TokenValidationParameters"/> used to validate tokens received during
    /// this authentication request.
    /// </summary>
    /// <remarks>
    /// This is typically a clone of <see cref="OpenIdConnectOptions.TokenValidationParameters"/> with per-request
    /// customizations applied, such as setting <see cref="TokenValidationParameters.ValidAudience"/> to match
    /// a dynamic <see cref="ClientId"/>.
    /// </remarks>
    public TokenValidationParameters TokenValidationParameters { get; set; } = default!;
}
