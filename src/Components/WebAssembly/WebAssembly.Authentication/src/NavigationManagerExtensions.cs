// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication;

/// <summary>
/// Extensions for <see cref="NavigationManager"/>.
/// </summary>
public static class NavigationManagerExtensions
{
    internal const string LogoutNavigationState = "Logout";

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
            HistoryEntryState = new InteractiveAuthenticationRequest(
                InteractiveAuthenticationRequestType.Logout,
                returnUrl,
                Array.Empty<string>()).ToState()
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
    /// <param name="request">The <see cref="InteractiveAuthenticationRequest"/> containing the authorization details.</param>
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Serializes InteractiveAuthenticationRequest into the state entry.")]
    public static void NavigateToLogin(this NavigationManager manager, string loginPath, InteractiveAuthenticationRequest request)
    {
        manager.NavigateTo(loginPath, new NavigationOptions
        {
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
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Serializes InteractiveAuthenticationRequest into the state entry.")]
    public static void NavigateToLogin(this NavigationManager manager, string loginPath)
    {
        manager.NavigateToLogin(loginPath, new InteractiveAuthenticationRequest(InteractiveAuthenticationRequestType.Authenticate, manager.Uri, Array.Empty<string>()));
    }
}
