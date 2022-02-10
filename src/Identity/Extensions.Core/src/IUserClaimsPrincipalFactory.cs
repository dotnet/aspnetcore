// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Provides an abstraction for a factory to create a <see cref="ClaimsPrincipal"/> from a user.
/// </summary>
/// <typeparam name="TUser">The type encapsulating a user.</typeparam>
public interface IUserClaimsPrincipalFactory<TUser>
    where TUser : class
{
    /// <summary>
    /// Creates a <see cref="ClaimsPrincipal"/> from an user asynchronously.
    /// </summary>
    /// <param name="user">The user to create a <see cref="ClaimsPrincipal"/> from.</param>
    /// <returns>The <see cref="Task"/> that represents the asynchronous creation operation, containing the created <see cref="ClaimsPrincipal"/>.</returns>
    Task<ClaimsPrincipal> CreateAsync(TUser user);
}
