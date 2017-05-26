// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore
{
    /// <summary>
    /// Represents a new instance of a persistence store for the specified user and role types.
    /// </summary>
    /// <typeparam name="TUser">The type representing a user.</typeparam>
    /// <typeparam name="TRole">The type representing a role.</typeparam>
    /// <typeparam name="TContext">The type of the data context class used to access the store.</typeparam>
    /// <typeparam name="TKey">The type of the primary key for a role.</typeparam>
    /// <typeparam name="TUserClaim">The type representing a claim.</typeparam>
    /// <typeparam name="TUserRole">The type representing a user role.</typeparam>
    /// <typeparam name="TUserLogin">The type representing a user external login.</typeparam>
    /// <typeparam name="TUserToken">The type representing a user token.</typeparam>
    /// <typeparam name="TRoleClaim">The type representing a role claim.</typeparam>
    public class UserStore<TUser, TRole, TContext, TKey, TUserClaim, TUserRole, TUserLogin, TUserToken, TRoleClaim> :
        UserStoreV2<TUser, TRole, TContext, TKey, TUserClaim, TUserRole, TUserLogin, TUserToken, TRoleClaim>
        where TUser : IdentityUser<TKey, TUserClaim, TUserRole, TUserLogin, TUserToken>
        where TRole : IdentityRole<TKey, TUserRole, TRoleClaim>
        where TContext : DbContext
        where TKey : IEquatable<TKey>
        where TUserClaim : IdentityUserClaim<TKey>, new()
        where TUserRole : IdentityUserRole<TKey>, new()
        where TUserLogin : IdentityUserLogin<TKey>, new()
        where TUserToken : IdentityUserToken<TKey>, new()
        where TRoleClaim : IdentityRoleClaim<TKey>, new()
    {
        /// <summary>
        /// Creates a new instance of the store.
        /// </summary>
        /// <param name="context">The context used to access the store.</param>
        /// <param name="describer">The <see cref="IdentityErrorDescriber"/> used to describe store errors.</param>
        public UserStore(TContext context, IdentityErrorDescriber describer) : base(context, describer)
        { }
    }
}
