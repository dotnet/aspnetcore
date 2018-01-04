// Copyright (c) Microsoft Corporation, Inc. All rights reserved.
// Licensed under the MIT License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Identity.EntityFramework;

namespace Microsoft.AspNet.Identity.CoreCompat
{
    public class IdentityRole : IdentityRole<string, IdentityUserRole>
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        public IdentityRole()
        {
            Id = Guid.NewGuid().ToString();
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="roleName"></param>
        public IdentityRole(string roleName)
            : this()
        {
            Name = roleName;
        }
    }

    public class IdentityRole<TKey, TUserRole> : Microsoft.AspNet.Identity.EntityFramework.IdentityRole<TKey, TUserRole>
        where TUserRole : IdentityUserRole<TKey>
    {
        /// <summary>
        ///     Normalized role name
        /// </summary>
        public virtual string NormalizedName { get; set; }

        /// <summary>
        ///     Concurrency stamp 
        /// </summary>
        public virtual string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        ///     Navigation property for claims in the role
        /// </summary>
        public virtual ICollection<IdentityRoleClaim<TKey>> Claims { get; } = new List<IdentityRoleClaim<TKey>>();
    }
}

