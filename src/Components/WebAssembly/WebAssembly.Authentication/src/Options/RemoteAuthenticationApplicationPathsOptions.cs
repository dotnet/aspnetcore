// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication;

/// <summary>
/// Represents the options for the paths used by the application for authentication operations. These paths are relative to the base.
/// </summary>
public class RemoteAuthenticationApplicationPathsOptions
{
    /// <summary>
    /// Gets or sets the path to the endpoint for registering new users.
    /// </summary>
    public string RegisterPath { get; set; } = RemoteAuthenticationDefaults.RegisterPath;

    /// <summary>
    /// Gets or sets the remote path to the remote endpoint for registering new users.
    /// It might be absolute and point outside of the application.
    /// </summary>
    public string? RemoteRegisterPath { get; set; }

    /// <summary>
    /// Gets or sets the path to the endpoint for modifying the settings for the user profile.
    /// </summary>
    public string ProfilePath { get; set; } = RemoteAuthenticationDefaults.ProfilePath;

    /// <summary>
    /// Gets or sets the path to the remote endpoint for modifying the settings for the user profile.
    /// It might be absolute and point outside of the application.
    /// </summary>
    public string? RemoteProfilePath { get; set; }

    /// <summary>
    /// Gets or sets the path to the login page.
    /// </summary>
    public string LogInPath { get; set; } = RemoteAuthenticationDefaults.LoginPath;

    /// <summary>
    /// Gets or sets the path to the login callback page.
    /// </summary>
    public string LogInCallbackPath { get; set; } = RemoteAuthenticationDefaults.LoginCallbackPath;

    /// <summary>
    /// Gets or sets the path to the login failed page.
    /// </summary>
    public string LogInFailedPath { get; set; } = RemoteAuthenticationDefaults.LoginFailedPath;

    /// <summary>
    /// Gets or sets the path to the logout page.
    /// </summary>
    public string LogOutPath { get; set; } = RemoteAuthenticationDefaults.LogoutPath;

    /// <summary>
    /// Gets or sets the path to the logout callback page.
    /// </summary>
    public string LogOutCallbackPath { get; set; } = RemoteAuthenticationDefaults.LogoutCallbackPath;

    /// <summary>
    /// Gets or sets the path to the logout failed page.
    /// </summary>
    public string LogOutFailedPath { get; set; } = RemoteAuthenticationDefaults.LogoutFailedPath;

    /// <summary>
    /// Gets or sets the path to the logout succeeded page.
    /// </summary>
    public string LogOutSucceededPath { get; set; } = RemoteAuthenticationDefaults.LogoutSucceededPath;
}
