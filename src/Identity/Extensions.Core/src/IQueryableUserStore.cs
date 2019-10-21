// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;

namespace Microsoft.AspNetCore.Identity
{
    /// <summary>
    /// Provides an abstraction for querying users in a User store.
    /// </summary>
    /// <typeparam name="TUser">The type encapsulating a user.</typeparam>
    public interface IQueryableUserStore<TUser> : IUserStore<TUser> where TUser : class
    {
        /// <summary>
        ///  Returns an <see cref="IQueryable{T}"/> collection of users as an asynchronous operation.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> that represents the result of the asynchronous query, a collection of users <see cref="IQueryable{T}"/>s.
        /// </returns>
        Task<IQueryable<TUser>> GetUsersAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}
