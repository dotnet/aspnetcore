// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Specifies options for password hashing.
/// </summary>
public class PasswordHasherOptions
{
    private static readonly RandomNumberGenerator _defaultRng = RandomNumberGenerator.Create(); // secure PRNG

    /// <summary>
    /// Gets or sets the compatibility mode used when hashing passwords. Defaults to 'ASP.NET Identity version 3'.
    /// </summary>
    /// <value>
    /// The compatibility mode used when hashing passwords.
    /// </value>
    public PasswordHasherCompatibilityMode CompatibilityMode { get; set; } = PasswordHasherCompatibilityMode.IdentityV3;

    /// <summary>
    /// Gets or sets the number of iterations used when hashing passwords using PBKDF2. Default is 100,000.
    /// </summary>
    /// <value>
    /// The number of iterations used when hashing passwords using PBKDF2.
    /// </value>
    /// <remarks>
    /// This value is only used when the compatibility mode is set to 'V3'.
    /// The value must be a positive integer.
    /// </remarks>
    public int IterationCount { get; set; } = 100_000;

    // for unit testing
    internal RandomNumberGenerator Rng { get; set; } = _defaultRng;
}
