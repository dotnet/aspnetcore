// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Default implementation of <see cref="IUserConfirmation{TUser}"/>.
/// </summary>
/// <typeparam name="TUser">The type encapsulating a user.</typeparam>
public class DefaultUserConfirmation<TUser> : IUserConfirmation<TUser> where TUser : class
{
    /// <summary>
    /// Determines whether the specified <paramref name="user"/> is confirmed.
    /// </summary>
    /// <param name="manager">The <see cref="UserManager{TUser}"/> that can be used to retrieve user properties.</param>
    /// <param name="user">The user.</param>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/> of the confirmation operation.</returns>
    public virtual async Task<bool> IsConfirmedAsync(UserManager<TUser> manager, TUser user)
    {
        return await manager.IsEmailConfirmedAsync(user).ConfigureAwait(false);
    }
}
