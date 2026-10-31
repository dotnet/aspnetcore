// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Provides an abstraction for a validating a role.
/// </summary>
/// <typeparam name="TRole">The type encapsulating a role.</typeparam>
public interface IRoleValidator<TRole> where TRole : class
{
    /// <summary>
    /// Validates a role as an asynchronous operation.
    /// </summary>
    /// <param name="manager">The <see cref="RoleManager{TRole}"/> managing the role store.</param>
    /// <param name="role">The role to validate.</param>
    /// <returns>A <see cref="Task{TResult}"/> that represents the <see cref="IdentityResult"/> of the asynchronous validation.</returns>
    Task<IdentityResult> ValidateAsync(RoleManager<TRole> manager, TRole role);
}
