// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Identity
{
    /// <summary>
    /// Creates a new instance of a persistence store for roles that always will match the latest     /// Creates a new instance of a persistence store for roles that matches <see cref="IdentityStoreOptions.Version"/>.
    /// </summary>
    /// <typeparam name="TRole">The type of the class representing a role.</typeparam>
    /// <typeparam name="TKey">The type of the primary key for a role.</typeparam>
    /// <typeparam name="TUserRole">The type of the class representing a user role.</typeparam>
    /// <typeparam name="TRoleClaim">The type of the class representing a role claim.</typeparam>
    public abstract class RoleStoreBase<TRole, TKey, TUserRole, TRoleClaim> :
        RoleStoreBaseV1<TRole, TKey, TUserRole, TRoleClaim>
        where TRole : IdentityRole<TKey, TUserRole, TRoleClaim>
        where TKey : IEquatable<TKey>
        where TUserRole : IdentityUserRole<TKey>, new()
        where TRoleClaim : IdentityRoleClaim<TKey>, new()
    {
        /// <summary>
        /// Constructs a new instance of <see cref="RoleStoreBase{TRole, TKey, TUserRole, TRoleClaim}"/>.
        /// </summary>
        /// <param name="describer">The <see cref="IdentityErrorDescriber"/>.</param>
        public RoleStoreBase(IdentityErrorDescriber describer) : base(describer) { }
    }
}
