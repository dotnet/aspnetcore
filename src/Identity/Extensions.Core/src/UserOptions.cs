// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Options for user validation.
/// </summary>
public class UserOptions
{
    /// <summary>
    /// Gets or sets the list of allowed characters in the username used to validate user names. Defaults to abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+
    /// </summary>
    /// <value>
    /// The list of allowed characters in the username used to validate user names.
    /// </value>
    public string AllowedUserNameCharacters { get; set; } = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

    /// <summary>
    /// Gets or sets a flag indicating whether the application requires unique emails for its users. Defaults to false.
    /// </summary>
    /// <value>
    /// True if the application requires each user to have their own, unique, not null, not blank email, otherwise false.
    /// </value>
    public bool RequireUniqueEmail { get; set; }
}
