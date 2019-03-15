// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Identity
{
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
        public async virtual Task<bool> IsConfirmedAsync(UserManager<TUser> manager, TUser user)
        {
            if (!await manager.IsEmailConfirmedAsync(user))
            {
                return false;
            }
            return true;
        }
    }
}
