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
    /// <remarks>
    /// Unless this event handles or skips the request, the handler signs out the local session. The request isn't
    /// authenticated by the handler. To reject requests that don't match the identity provider's expected origin,
    /// call <see cref="HandleRequestContext{WsFederationOptions}.HandleResponse"/> or
    /// <see cref="HandleRequestContext{WsFederationOptions}.SkipHandler"/>.
    /// The following example deliberately rejects a missing <c>Referer</c> header and uses a trailing slash to avoid
    /// matching look-alike host names:
    /// <example>
    /// <code>
    /// options.Events.OnRemoteSignOut = context =&gt;
    /// {
    ///     var referer = context.Request.Headers["Referer"].ToString();
    ///     if (!referer.StartsWith("https://adfs.contoso.com/", StringComparison.OrdinalIgnoreCase))
    ///     {
    ///         context.HandleResponse();
    ///     }
    ///
    ///     return Task.CompletedTask;
    /// };
    /// </code>
    /// </example>
    /// Before applying a filter, verify which headers the identity provider actually sends during sign-out, such as
    /// <c>Referer</c>, <c>Sec-Fetch-Dest</c>, and <c>Sec-Fetch-Site</c>, because delivery can vary by provider and
    /// deployment. These headers are request metadata, not cryptographic authentication, and non-browser clients can
    /// forge them. For iframe- or image-based cleanup, the authentication cookie generally needs
    /// <c>SameSite=None</c> (and <c>Secure</c>) to be sent by the browser.
    /// </remarks>
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
    /// <remarks>
    /// The default implementation invokes <see cref="OnRemoteSignOut"/>. If the event doesn't handle or skip the
    /// request, the handler signs out the local session. Call
    /// <see cref="HandleRequestContext{WsFederationOptions}.HandleResponse"/> or
    /// <see cref="HandleRequestContext{WsFederationOptions}.SkipHandler"/> to prevent the sign-out.
    /// </remarks>
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
