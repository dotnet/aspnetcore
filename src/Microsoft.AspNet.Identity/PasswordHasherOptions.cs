// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Cryptography;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    /// Allows configuring how passwords are hashed.
    /// </summary>
    public class PasswordHasherOptions
    {
        private static readonly RandomNumberGenerator _defaultRng = RandomNumberGenerator.Create(); // secure PRNG

        /// <summary>
        /// Specifies the compatibility mode to use when hashing passwords.
        /// </summary>
        /// <remarks>
        /// The default compatibility mode is 'ASP.NET Identity version 3'.
        /// </remarks>
        public PasswordHasherCompatibilityMode CompatibilityMode { get; set; } = PasswordHasherCompatibilityMode.IdentityV3;

        // for unit testing
        internal RandomNumberGenerator Rng { get; set; } = _defaultRng;
    }
}