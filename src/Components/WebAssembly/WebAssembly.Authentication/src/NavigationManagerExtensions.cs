// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication;

/// <summary>
/// Extensions for <see cref="NavigationManager"/>.
/// </summary>
public static class NavigationManagerExtensions
{
    /// <summary>
    /// Initiates a logout operation by navigating to the log out endpoint.
    /// </summary>
    /// <remarks>
    /// The navigation includes state that is added to the browser history entry to
    /// prevent logout operations performed from different contexts.
    /// </remarks>
    /// <param name="manager">The <see cref="NavigationManager"/>.</param>
    /// <param name="logoutPath">The path to navigate to.</param>
    public static void NavigateToLogout(this NavigationManager manager, [StringSyntax(StringSyntaxAttribute.Uri, UriKind.Relative)] string logoutPath) =>
        manager.NavigateToLogout(logoutPath, null);

    /// <summary>
    /// Initiates a logout operation by navigating to the log out endpoint.
    /// </summary>
    /// <remarks>
    /// The navigation includes state that is added to the browser history entry to
    /// prevent logout operations performed from different contexts.
    /// </remarks>
    /// <param name="manager">The <see cref="NavigationManager"/>.</param>
    /// <param name="logoutPath">The path to navigate too.</param>
    /// <param name="returnUrl">The url to redirect the user to after logging out.</param>
    public static void NavigateToLogout(this NavigationManager manager, [StringSyntax(StringSyntaxAttribute.Uri, UriKind.Relative)] string logoutPath, [StringSyntax(StringSyntaxAttribute.Uri)] string? returnUrl)
    {
        manager.NavigateTo(logoutPath, new NavigationOptions
        {
            HistoryEntryState = new InteractiveRequestOptions
            {
                Interaction = InteractionType.SignOut,
                ReturnUrl = returnUrl!
            }.ToState()
        });
    }

    /// <summary>
    /// Initiates a login operation by navigating to the login endpoint.
    /// </summary>
    /// <remarks>
    /// The navigation includes state that is added to the browser history entry to
    /// prevent login operations performed from different contexts.
    /// </remarks>
    /// <param name="manager">The <see cref="NavigationManager"/>.</param>
    /// <param name="loginPath">The path to the login url.</param>
    /// <param name="request">The <see cref="InteractiveRequestOptions"/> containing the authorization details.</param>
    public static void NavigateToLogin(this NavigationManager manager, [StringSyntax(StringSyntaxAttribute.Uri, UriKind.Relative)] string loginPath, InteractiveRequestOptions request)
    {
        manager.NavigateTo(loginPath, new NavigationOptions
        {
            HistoryEntryState = request.ToState(),
        });
    }

    /// <summary>
    /// Initiates a login operation by navigating to the login endpoint.
    /// </summary>
    /// <remarks>
    /// The navigation includes state that is added to the browser history entry to
    /// prevent login operations performed from different contexts.
    /// </remarks>
    /// <param name="manager">The <see cref="NavigationManager"/>.</param>
    /// <param name="loginPath">The path to the login url.</param>
    public static void NavigateToLogin(this NavigationManager manager, [StringSyntax(StringSyntaxAttribute.Uri, UriKind.Relative)] string loginPath)
    {
        manager.NavigateToLogin(
            loginPath,
            new InteractiveRequestOptions
            {
                Interaction = InteractionType.SignIn,
                ReturnUrl = manager.Uri
            });
    }
}
