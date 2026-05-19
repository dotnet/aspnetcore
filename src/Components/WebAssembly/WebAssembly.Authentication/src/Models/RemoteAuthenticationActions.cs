// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication;

/// <summary>
/// Represents the list of authentication actions that can be performed by the <see cref="RemoteAuthenticatorViewCore{TAuthenticationState}"/>.
/// </summary>
public class RemoteAuthenticationActions
{
    /// <summary>
    /// The log in action.
    /// </summary>
    public const string LogIn = "login";

    /// <summary>
    /// The log in callback action.
    /// </summary>
    public const string LogInCallback = "login-callback";

    /// <summary>
    /// The log in failed action.
    /// </summary>
    public const string LogInFailed = "login-failed";

    /// <summary>
    /// The navigate to user profile action.
    /// </summary>
    public const string Profile = "profile";

    /// <summary>
    /// The navigate to register action.
    /// </summary>
    public const string Register = "register";

    /// <summary>
    /// The log out action.
    /// </summary>
    public const string LogOut = "logout";

    /// <summary>
    /// The log out callback action.
    /// </summary>
    public const string LogOutCallback = "logout-callback";

    /// <summary>
    /// The log out failed action.
    /// </summary>
    public const string LogOutFailed = "logout-failed";

    /// <summary>
    /// The log out succeeded action.
    /// </summary>
    public const string LogOutSucceeded = "logged-out";

    /// <summary>
    /// Whether or not a given <paramref name="candidate"/> represents a given <see cref="RemoteAuthenticationActions"/>.
    /// </summary>
    /// <param name="action">The <see cref="RemoteAuthenticationActions"/>.</param>
    /// <param name="candidate">The candidate.</param>
    /// <returns>Whether or not is the given <see cref="RemoteAuthenticationActions"/> action.</returns>
    public static bool IsAction(string action, string candidate) => action != null && string.Equals(action, candidate, System.StringComparison.OrdinalIgnoreCase);
}
