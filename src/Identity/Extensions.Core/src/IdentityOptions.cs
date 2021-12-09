// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents all the options you can use to configure the identity system.
/// </summary>
public class IdentityOptions
{
    /// <summary>
    /// Gets or sets the <see cref="ClaimsIdentityOptions"/> for the identity system.
    /// </summary>
    /// <value>
    /// The <see cref="ClaimsIdentityOptions"/> for the identity system.
    /// </value>
    public ClaimsIdentityOptions ClaimsIdentity { get; set; } = new ClaimsIdentityOptions();

    /// <summary>
    /// Gets or sets the <see cref="UserOptions"/> for the identity system.
    /// </summary>
    /// <value>
    /// The <see cref="UserOptions"/> for the identity system.
    /// </value>
    public UserOptions User { get; set; } = new UserOptions();

    /// <summary>
    /// Gets or sets the <see cref="PasswordOptions"/> for the identity system.
    /// </summary>
    /// <value>
    /// The <see cref="PasswordOptions"/> for the identity system.
    /// </value>
    public PasswordOptions Password { get; set; } = new PasswordOptions();

    /// <summary>
    /// Gets or sets the <see cref="LockoutOptions"/> for the identity system.
    /// </summary>
    /// <value>
    /// The <see cref="LockoutOptions"/> for the identity system.
    /// </value>
    public LockoutOptions Lockout { get; set; } = new LockoutOptions();

    /// <summary>
    /// Gets or sets the <see cref="SignInOptions"/> for the identity system.
    /// </summary>
    /// <value>
    /// The <see cref="SignInOptions"/> for the identity system.
    /// </value>
    public SignInOptions SignIn { get; set; } = new SignInOptions();

    /// <summary>
    /// Gets or sets the <see cref="TokenOptions"/> for the identity system.
    /// </summary>
    /// <value>
    /// The <see cref="TokenOptions"/> for the identity system.
    /// </value>
    public TokenOptions Tokens { get; set; } = new TokenOptions();

    /// <summary>
    /// Gets or sets the <see cref="StoreOptions"/> for the identity system.
    /// </summary>
    /// <value>
    /// The <see cref="StoreOptions"/> for the identity system.
    /// </value>
    public StoreOptions Stores { get; set; } = new StoreOptions();
}
