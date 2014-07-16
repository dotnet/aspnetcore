// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Configuration for identity
    /// </summary>
    public class IdentityOptions
    {
        public IdentityOptions()
        {
            ClaimsIdentity = new ClaimsIdentityOptions();
            User = new UserOptions();
            Password = new PasswordOptions();
            Lockout = new LockoutOptions();
        }

        public ClaimsIdentityOptions ClaimsIdentity { get; set; }

        public UserOptions User { get; set; }

        public PasswordOptions Password { get; set; }

        public LockoutOptions Lockout { get; set; }
    }
}