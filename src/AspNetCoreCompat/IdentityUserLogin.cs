// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Identity.CoreCompat
{
    public class IdentityUserLogin : IdentityUserLogin<string> { }

    /// <summary>
    /// Represents a login and its associated provider for a user.
    /// </summary>
    /// <typeparam name="TKey">The type of the primary key of the user associated with this login.</typeparam>
    public class IdentityUserLogin<TKey> : EntityFramework.IdentityUserLogin<TKey>
    {
        /// <summary>
        /// Gets or sets the friendly name used in a UI for this login.
        /// </summary>
        public virtual string ProviderDisplayName { get; set; }
    }
}