// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication.Twitter;

/// <summary>
/// Default <see cref="TwitterEvents"/> implementation.
/// </summary>
public class TwitterEvents : RemoteAuthenticationEvents
{
    /// <summary>
    /// Gets or sets the function that is invoked when the Authenticated method is invoked.
    /// </summary>
    public Func<TwitterCreatingTicketContext, Task> OnCreatingTicket { get; set; } = context => Task.CompletedTask;

    /// <summary>
    /// Gets or sets the delegate that is invoked when the ApplyRedirect method is invoked.
    /// </summary>
    public Func<RedirectContext<TwitterOptions>, Task> OnRedirectToAuthorizationEndpoint { get; set; } = context =>
    {
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };

    /// <summary>
    /// Invoked whenever Twitter successfully authenticates a user
    /// </summary>
    /// <param name="context">Contains information about the login session as well as the user <see cref="System.Security.Claims.ClaimsIdentity"/>.</param>
    /// <returns>A <see cref="Task"/> representing the completed operation.</returns>
    public virtual Task CreatingTicket(TwitterCreatingTicketContext context) => OnCreatingTicket(context);

    /// <summary>
    /// Called when a Challenge causes a redirect to authorize endpoint in the Twitter handler
    /// </summary>
    /// <param name="context">Contains redirect URI and <see cref="AuthenticationProperties"/> of the challenge </param>
    public virtual Task RedirectToAuthorizationEndpoint(RedirectContext<TwitterOptions> context) => OnRedirectToAuthorizationEndpoint(context);
}
