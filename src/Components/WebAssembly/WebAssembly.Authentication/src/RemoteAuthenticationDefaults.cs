// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication;

/// <summary>
/// Represents default values for different configurable values used across the library.
/// </summary>
public class RemoteAuthenticationDefaults
{
    /// <summary>
    /// The default login path.
    /// </summary>
    public static readonly string LoginPath = "authentication/login";

    /// <summary>
    /// The default login callback path.
    /// </summary>
    public static readonly string LoginCallbackPath = "authentication/login-callback";

    /// <summary>
    /// The default login failed path.
    /// </summary>
    public static readonly string LoginFailedPath = "authentication/login-failed";

    /// <summary>
    /// The default logout path.
    /// </summary>
    public static readonly string LogoutPath = "authentication/logout";

    /// <summary>
    /// The default logout callback path.
    /// </summary>
    public static readonly string LogoutCallbackPath = "authentication/logout-callback";

    /// <summary>
    /// The default logout failed path.
    /// </summary>
    public static readonly string LogoutFailedPath = "authentication/logout-failed";

    /// <summary>
    /// The default logout succeeded path.
    /// </summary>
    public static readonly string LogoutSucceededPath = "authentication/logged-out";

    /// <summary>
    /// The default profile path.
    /// </summary>
    public static readonly string ProfilePath = "authentication/profile";

    /// <summary>
    /// The default register path.
    /// </summary>
    public static readonly string RegisterPath = "authentication/register";
}
