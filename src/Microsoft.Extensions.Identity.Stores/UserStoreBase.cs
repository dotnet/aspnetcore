// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Identity
{
    /// <summary>
    /// Represents a new instance of a persistence store for the specified user and role types.
    /// This type will always support the most current schema.
    /// </summary>
    /// <typeparam name="TUser">The type representing a user.</typeparam>
    /// <typeparam name="TRole">The type representing a role.</typeparam>
    /// <typeparam name="TKey">The type of the primary key for a role.</typeparam>
    /// <typeparam name="TUserClaim">The type representing a claim.</typeparam>
    /// <typeparam name="TUserRole">The type representing a user role.</typeparam>
    /// <typeparam name="TUserLogin">The type representing a user external login.</typeparam>
    /// <typeparam name="TUserToken">The type representing a user token.</typeparam>
    /// <typeparam name="TRoleClaim">The type representing a role claim.</typeparam>
    public abstract class UserStoreBase<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TUserToken, TRoleClaim> :
        UserStoreBaseV2<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TUserToken, TRoleClaim>
        where TUser : IdentityUser<TKey, TUserClaim, TUserRole, TUserLogin, TUserToken>
        where TRole : IdentityRole<TKey, TUserRole, TRoleClaim>
        where TKey : IEquatable<TKey>
        where TUserClaim : IdentityUserClaim<TKey>, new()
        where TUserRole : IdentityUserRole<TKey>, new()
        where TUserLogin : IdentityUserLogin<TKey>, new()
        where TUserToken : IdentityUserToken<TKey>, new()
        where TRoleClaim : IdentityRoleClaim<TKey>, new()
    {
        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="describer">The <see cref="IdentityErrorDescriber"/> used to describe store errors.</param>
        public UserStoreBase(IdentityErrorDescriber describer) : base(describer) { }

    }
}
