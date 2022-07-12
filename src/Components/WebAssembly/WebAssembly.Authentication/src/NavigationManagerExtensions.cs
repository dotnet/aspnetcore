// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    /// The navigation includes stated that is added to the browser history entry to
    /// prevent logout operations performed from different contexts.
    /// </remarks>
    /// <param name="manager">The <see cref="NavigationManager"/>.</param>
    /// <param name="logoutPath">The path to navigate too.</param>
    public static void NavigateToLogout(this NavigationManager manager, string logoutPath) =>
        manager.NavigateToLogout(logoutPath, null);

    /// <summary>
    /// Initiates a logout operation by navigating to the log out endpoint.
    /// </summary>
    /// <remarks>
    /// The navigation includes stated that is added to the browser history entry to
    /// prevent logout operations performed from different contexts.
    /// </remarks>
    /// <param name="manager">The <see cref="NavigationManager"/>.</param>
    /// <param name="logoutPath">The path to navigate too.</param>
    /// <param name="returnUrl">The url to redirect the user to after logging out.</param>
    public static void NavigateToLogout(this NavigationManager manager, string logoutPath, string returnUrl)
    {
        manager.NavigateTo(logoutPath, new NavigationOptions
        {
            HistoryEntryState = InteractiveRequestOptions.SignOut(returnUrl).ToState()
        });
    }

    /// <summary>
    /// Initiates a logout operation by navigating to the log out endpoint.
    /// </summary>
    /// <remarks>
    /// The navigation includes stated that is added to the browser history entry to
    /// prevent logout operations performed from different contexts.
    /// </remarks>
    /// <param name="manager">The <see cref="NavigationManager"/>.</param>
    /// <param name="loginPath">The path to the login url.</param>
    /// <param name="request">The <see cref="InteractiveRequestOptions"/> containing the authorization details.</param>
    public static void NavigateToLogin(this NavigationManager manager, string loginPath, InteractiveRequestOptions request)
    {
        manager.NavigateTo(loginPath, new NavigationOptions
        {
            ForceLoad = false,
            HistoryEntryState = request.ToState(),
        });
    }

    /// <summary>
    /// Initiates a logout operation by navigating to the log out endpoint.
    /// </summary>
    /// <remarks>
    /// The navigation includes stated that is added to the browser history entry to
    /// prevent logout operations performed from different contexts.
    /// </remarks>
    /// <param name="manager">The <see cref="NavigationManager"/>.</param>
    /// <param name="loginPath">The path to the login url.</param>
    public static void NavigateToLogin(this NavigationManager manager, string loginPath)
    {
        manager.NavigateToLogin(loginPath, InteractiveRequestOptions.SignIn(manager.Uri));
    }
}
