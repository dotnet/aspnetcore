// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication.WsFederation;

/// <summary>
/// Specifies events which the <see cref="WsFederationHandler"></see> invokes to enable developer control over the authentication process. />
/// </summary>
public class WsFederationEvents : RemoteAuthenticationEvents
{
    /// <summary>
    /// Invoked if exceptions are thrown during request processing. The exceptions will be re-thrown after this event unless suppressed.
    /// </summary>
    public Func<AuthenticationFailedContext, Task> OnAuthenticationFailed { get; set; } = context => Task.CompletedTask;

    /// <summary>
    /// Invoked when a protocol message is first received.
    /// </summary>
    public Func<MessageReceivedContext, Task> OnMessageReceived { get; set; } = context => Task.CompletedTask;

    /// <summary>
    /// Invoked to manipulate redirects to the identity provider for SignIn, SignOut, or Challenge.
    /// </summary>
    public Func<RedirectContext, Task> OnRedirectToIdentityProvider { get; set; } = context => Task.CompletedTask;

    /// <summary>
    /// Invoked when a wsignoutcleanup request is received at the RemoteSignOutPath endpoint.
    /// </summary>
    public Func<RemoteSignOutContext, Task> OnRemoteSignOut { get; set; } = context => Task.CompletedTask;

    /// <summary>
    /// Invoked with the security token that has been extracted from the protocol message.
    /// </summary>
    public Func<SecurityTokenReceivedContext, Task> OnSecurityTokenReceived { get; set; } = context => Task.CompletedTask;

    /// <summary>
    /// Invoked after the security token has passed validation and a ClaimsIdentity has been generated.
    /// </summary>
    public Func<SecurityTokenValidatedContext, Task> OnSecurityTokenValidated { get; set; } = context => Task.CompletedTask;

    /// <summary>
    /// Invoked if exceptions are thrown during request processing. The exceptions will be re-thrown after this event unless suppressed.
    /// </summary>
    public virtual Task AuthenticationFailed(AuthenticationFailedContext context) => OnAuthenticationFailed(context);

    /// <summary>
    /// Invoked when a protocol message is first received.
    /// </summary>
    public virtual Task MessageReceived(MessageReceivedContext context) => OnMessageReceived(context);

    /// <summary>
    /// Invoked to manipulate redirects to the identity provider for SignIn, SignOut, or Challenge.
    /// </summary>
    public virtual Task RedirectToIdentityProvider(RedirectContext context) => OnRedirectToIdentityProvider(context);

    /// <summary>
    /// Invoked when a wsignoutcleanup request is received at the RemoteSignOutPath endpoint.
    /// </summary>
    public virtual Task RemoteSignOut(RemoteSignOutContext context) => OnRemoteSignOut(context);

    /// <summary>
    /// Invoked with the security token that has been extracted from the protocol message.
    /// </summary>
    public virtual Task SecurityTokenReceived(SecurityTokenReceivedContext context) => OnSecurityTokenReceived(context);

    /// <summary>
    /// Invoked after the security token has passed validation and a ClaimsIdentity has been generated.
    /// </summary>
    public virtual Task SecurityTokenValidated(SecurityTokenValidatedContext context) => OnSecurityTokenValidated(context);
}
