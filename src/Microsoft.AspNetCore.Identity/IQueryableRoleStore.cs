// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;

namespace Microsoft.AspNetCore.Identity
{
    /// <summary>
    /// Provides an abstraction for querying roles in a Role store.
    /// </summary>
    /// <typeparam name="TRole">The type encapsulating a role.</typeparam>
    public interface IQueryableRoleStore<TRole> : IRoleStore<TRole> where TRole : class
    {
        /// <summary>
        /// Returns an <see cref="IQueryable{T}"/> collection of roles.
        /// </summary>
        /// <value>An <see cref="IQueryable{T}"/> collection of roles.</value>
        IQueryable<TRole> Roles { get; }
    }
}