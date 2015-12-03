// Copyright (c) Microsoft Corporation, Inc. All rights reserved.
// Licensed under the MIT License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Identity.EntityFramework;

namespace Microsoft.AspNet.Identity.Compat
{
    public class IdentityUser : IdentityUser<string, IdentityUserLogin, IdentityUserRole, IdentityUserClaim>
    {
        /// <summary>
        ///     Constructor which creates a new Guid for the Id
        /// </summary>
        public IdentityUser()
        {
            Id = Guid.NewGuid().ToString();
        }

        /// <summary>
        ///     Constructor that takes a userName
        /// </summary>
        /// <param name="userName"></param>
        public IdentityUser(string userName)
            : this()
        {
            UserName = userName;
            NormalizedUserName = userName.ToUpperInvariant();
        }
    }

    public class IdentityUser<TKey, TUserLogin, TUserRole, TUserClaim>
        : EntityFramework.IdentityUser<TKey, TUserLogin, TUserRole, TUserClaim>
        where TUserLogin : IdentityUserLogin<TKey>
        where TUserRole : IdentityUserRole<TKey>
        where TUserClaim : IdentityUserClaim<TKey>
    {
        public string NormalizedUserName { get; set; }

        /// <summary>
        ///     Normalized email
        /// </summary>
        public string NormalizedEmail { get; set; }

        /// <summary>
        ///     Concurrency stamp
        /// </summary>
        public virtual string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();
    }
}

