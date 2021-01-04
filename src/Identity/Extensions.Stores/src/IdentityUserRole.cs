// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Identity
{
    /// <summary>
    /// Represents the link between a user and a role.
    /// </summary>
    /// <typeparam name="TKey">The type of the primary key used for users and roles.</typeparam>
    public class IdentityUserRole<TKey> : IdentityUserRole<TKey, TKey> where TKey : IEquatable<TKey>
    {
    }

    /// <summary>
    /// Represents the link between a user and a role.
    /// </summary>
    /// <typeparam name="TKeyUser">The type of the primary key used for users.</typeparam>
    /// <typeparam name="TKeyRole">The type of the primary key used for roles.</typeparam>
    public class IdentityUserRole<TKeyUser, TKeyRole> where TKeyUser : IEquatable<TKeyUser> where TKeyRole : IEquatable<TKeyRole>
    {
        /// <summary>
        /// Gets or sets the primary key of the user that is linked to a role.
        /// </summary>
        public virtual TKeyUser UserId { get; set; }

        /// <summary>
        /// Gets or sets the primary key of the role that is linked to the user.
        /// </summary>
        public virtual TKeyRole RoleId { get; set; }
    }
}
