// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;

namespace Microsoft.AspNetCore.Identity;

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
