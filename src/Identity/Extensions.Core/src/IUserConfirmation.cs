// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Identity
{
    /// <summary>
    /// Provides an abstraction for confirmation of user accounts.
    /// </summary>
    /// <typeparam name="TUser">The type encapsulating a user.</typeparam>
    public interface IUserConfirmation<TUser> where TUser : class
    {
        /// <summary>
        /// Determines whether the specified <paramref name="user"/> is confirmed.
        /// </summary>
        /// <param name="manager">The <see cref="UserManager{TUser}"/> that can be used to retrieve user properties.</param>
        /// <param name="user">The user.</param>
        /// <returns>Whether the user is confirmed.</returns>
        Task<bool> IsConfirmedAsync(UserManager<TUser> manager, TUser user);
    }
}
