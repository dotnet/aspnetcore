// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents login information and source for a user record.
/// </summary>
public class UserLoginInfo
{
    /// <summary>
    /// Creates a new instance of <see cref="UserLoginInfo"/>
    /// </summary>
    /// <param name="loginProvider">The provider associated with this login information.</param>
    /// <param name="providerKey">The unique identifier for this user provided by the login provider.</param>
    /// <param name="providerDisplayName">The display name for the login provider.</param>
    public UserLoginInfo(string loginProvider, string providerKey, string? providerDisplayName)
    {
        LoginProvider = loginProvider;
        ProviderKey = providerKey;
        ProviderDisplayName = providerDisplayName;
    }

    /// <summary>
    /// Gets or sets the provider for this instance of <see cref="UserLoginInfo"/>.
    /// </summary>
    /// <value>The provider for the this instance of <see cref="UserLoginInfo"/></value>
    /// <remarks>
    /// Examples of the provider may be Local, Facebook, Google, etc.
    /// </remarks>
    public string LoginProvider { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier for the user identity user provided by the login provider.
    /// </summary>
    /// <value>
    /// The unique identifier for the user identity user provided by the login provider.
    /// </value>
    /// <remarks>
    /// This would be unique per provider, examples may be @microsoft as a Twitter provider key.
    /// </remarks>
    public string ProviderKey { get; set; }

    /// <summary>
    /// Gets or sets the display name for the provider.
    /// </summary>
    /// <value>
    /// The display name for the provider.
    /// </value>
    /// <remarks>
    /// Examples of the display name may be local, FACEBOOK, Google, etc.
    /// </remarks>
    public string? ProviderDisplayName { get; set; }
}
