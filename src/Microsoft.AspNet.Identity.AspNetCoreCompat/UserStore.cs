// Copyright (c) Microsoft Corporation, Inc. All rights reserved.
// Licensed under the MIT License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Data.Entity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace Microsoft.AspNet.Identity.CoreCompat
{
    public class UserStore<TUser> :
        UserStore<TUser, IdentityRole, string, IdentityUserLogin, IdentityUserRole, IdentityUserClaim>, IUserStore<TUser>
        where TUser : IdentityUser
    {
        /// <summary>
        ///     Default constuctor which uses a new instance of a default EntityDbContext.
        /// </summary>
        public UserStore()
            : this(new IdentityDbContext<TUser>())
        {
            DisposeContext = true;
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="context"></param>
        public UserStore(DbContext context)
            : base(context)
        {
        }
    }

    public class UserStore<TUser, TRole, TKey, TUserLogin, TUserRole, TUserClaim>
        : EntityFramework.UserStore<TUser, TRole, TKey, TUserLogin, TUserRole, TUserClaim>
        where TKey : IEquatable<TKey>
        where TUser : IdentityUser<TKey, TUserLogin, TUserRole, TUserClaim>
        where TRole : IdentityRole<TKey, TUserRole>
        where TUserLogin : IdentityUserLogin<TKey>, new()
        where TUserRole : IdentityUserRole<TKey>, new()
        where TUserClaim : IdentityUserClaim<TKey>, new()
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="context"></param>
        public UserStore(DbContext context) : base(context) { }

    }
}

