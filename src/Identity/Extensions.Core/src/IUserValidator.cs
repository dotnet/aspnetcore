// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Provides an abstraction for user validation.
/// </summary>
/// <typeparam name="TUser">The type encapsulating a user.</typeparam>
public interface IUserValidator<TUser> where TUser : class
{
    /// <summary>
    /// Validates the specified <paramref name="user"/> as an asynchronous operation.
    /// </summary>
    /// <param name="manager">The <see cref="UserManager{TUser}"/> that can be used to retrieve user properties.</param>
    /// <param name="user">The user to validate.</param>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/> of the validation operation.</returns>
    Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user);
}
