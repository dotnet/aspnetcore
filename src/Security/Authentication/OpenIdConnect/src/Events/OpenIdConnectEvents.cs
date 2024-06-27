// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication.OpenIdConnect;

/// <summary>
/// Specifies events which the <see cref="OpenIdConnectHandler" /> invokes to enable developer control over the authentication process.
/// </summary>
public class OpenIdConnectEvents : RemoteAuthenticationEvents
{
    /// <summary>
    /// Invoked if exceptions are thrown during request processing. The exceptions will be re-thrown after this event unless suppressed.
    /// </summary>
    public Func<AuthenticationFailedContext, Task> OnAuthenticationFailed { get; set; } = context => Task.CompletedTask;

    /// <summary>
    /// Invoked after security token validation if an authorization code is present in the protocol message.
    /// </summary>
    public Func<AuthorizationCodeReceivedContext, Task> OnAuthorizationCodeReceived { get; set; } = context => Task.CompletedTask;

    /// <summary>
    /// Invoked when a protocol message is first received.
    /// </summary>
    public Func<MessageReceivedContext, Task> OnMessageReceived { get; set; } = context => Task.CompletedTask;

    /// <summary>
    /// Invoked before redirecting to the identity provider to authenticate. This can be used to set ProtocolMessage.State
    /// that will be persisted through the authentication process. The ProtocolMessage can also be used to add or customize
    /// parameters sent to the identity provider.
    /// </summary>
    public Func<RedirectContext, Task> OnRedirectToIdentityProvider { get; set; } = context => Task.CompletedTask;

    /// <summary>
    /// Invoked before redirecting to the identity provider to sign out.
    /// </summary>
    public Func<RedirectContext, Task> OnRedirectToIdentityProviderForSignOut { get; set; } = context => Task.CompletedTask;

    /// <summary>
    /// Invoked before redirecting to the <see cref="OpenIdConnectOptions.SignedOutRedirectUri"/> at the end of a remote sign-out flow.
    /// </summary>
    public Func<RemoteSignOutContext, Task> OnSignedOutCallbackRedirect { get; set; } = context => Task.CompletedTask;

    /// <summary>
    /// Invoked when a request is received on the RemoteSignOutPath.
    /// </summary>
    public Func<RemoteSignOutContext, Task> OnRemoteSignOut { get; set; } = context => Task.CompletedTask;

    /// <summary>
    /// Invoked after "authorization code" is redeemed for tokens at the token endpoint.
    /// </summary>
    public Func<TokenResponseReceivedContext, Task> OnTokenResponseReceived { get; set; } = context => Task.CompletedTask;

    /// <summary>
    /// Invoked when an IdToken has been validated and produced an AuthenticationTicket. Note there are additional checks after this
    /// event that validate other aspects of the authentication flow like the nonce.
    /// </summary>
    public Func<TokenValidatedContext, Task> OnTokenValidated { get; set; } = context => Task.CompletedTask;

    /// <summary>
    /// Invoked when user information is retrieved from the UserInfoEndpoint.
    /// </summary>
    public Func<UserInformationReceivedContext, Task> OnUserInformationReceived { get; set; } = context => Task.CompletedTask;

    /// <summary>
    /// Invoked before authorization parameters are pushed using PAR.
    /// </summary>
    public Func<PushedAuthorizationContext, Task> OnPushAuthorization { get; set; } = context => Task.CompletedTask;

    /// <summary>
    /// Invoked if exceptions are thrown during request processing. The exceptions will be re-thrown after this event unless suppressed.
    /// </summary>
    public virtual Task AuthenticationFailed(AuthenticationFailedContext context) => OnAuthenticationFailed(context);

    /// <summary>
    /// Invoked if an authorization code is present in the protocol message.
    /// </summary>
    public virtual Task AuthorizationCodeReceived(AuthorizationCodeReceivedContext context) => OnAuthorizationCodeReceived(context);

    /// <summary>
    /// Invoked when a protocol message is first received.
    /// </summary>
    public virtual Task MessageReceived(MessageReceivedContext context) => OnMessageReceived(context);

    /// <summary>
    /// Invoked before redirecting to the identity provider to authenticate. This can be used to set ProtocolMessage.State
    /// that will be persisted through the authentication process. The ProtocolMessage can also be used to add or customize
    /// parameters sent to the identity provider.
    /// </summary>
    public virtual Task RedirectToIdentityProvider(RedirectContext context) => OnRedirectToIdentityProvider(context);

    /// <summary>
    /// Invoked before redirecting to the identity provider to sign out.
    /// </summary>
    public virtual Task RedirectToIdentityProviderForSignOut(RedirectContext context) => OnRedirectToIdentityProviderForSignOut(context);

    /// <summary>
    /// Invoked before redirecting to the <see cref="OpenIdConnectOptions.SignedOutRedirectUri"/> at the end of a remote sign-out flow.
    /// </summary>
    public virtual Task SignedOutCallbackRedirect(RemoteSignOutContext context) => OnSignedOutCallbackRedirect(context);

    /// <summary>
    /// Invoked when a request is received on the RemoteSignOutPath.
    /// </summary>
    public virtual Task RemoteSignOut(RemoteSignOutContext context) => OnRemoteSignOut(context);

    /// <summary>
    /// Invoked after an authorization code is redeemed for tokens at the token endpoint.
    /// </summary>
    public virtual Task TokenResponseReceived(TokenResponseReceivedContext context) => OnTokenResponseReceived(context);

    /// <summary>
    /// Invoked when an IdToken has been validated and produced an AuthenticationTicket. Note there are additional checks after this
    /// event that validate other aspects of the authentication flow like the nonce.
    /// </summary>
    public virtual Task TokenValidated(TokenValidatedContext context) => OnTokenValidated(context);

    /// <summary>
    /// Invoked when user information is retrieved from the UserInfoEndpoint.
    /// </summary>
    public virtual Task UserInformationReceived(UserInformationReceivedContext context) => OnUserInformationReceived(context);

    /// <summary>
    /// Invoked before authorization parameters are pushed during PAR.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public virtual Task PushAuthorization(PushedAuthorizationContext context) => OnPushAuthorization(context);
}
