// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Identity
{
    /// <summary>
    /// Used for store specific options
    /// </summary>
    public class StoreOptions
    {
        /// <summary>
        /// If set to a positive number, the default OnModelCreating will use this value as the max length for any 
        /// properties used as keys, i.e. UserId, LoginProvider, ProviderKey.
        /// </summary>
        public int MaxLengthForKeys { get; set; }

        /// <summary>
        /// If set to true, the store must protect all personally identifying data for a user. 
        /// This will be enforced by requiring the store to implement <see cref="IProtectedUserStore{TUser}"/>.
        /// </summary>
        public bool ProtectPersonalData { get; set; }
    }
}