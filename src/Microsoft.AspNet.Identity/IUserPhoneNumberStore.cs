// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Stores a user's phoneNumber
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    public interface IUserPhoneNumberStore<TUser> : IUserStore<TUser> where TUser : class
    {
        /// <summary>
        ///     Set the user PhoneNumber
        /// </summary>
        /// <param name="user"></param>
        /// <param name="phoneNumber"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SetPhoneNumberAsync(TUser user, string phoneNumber,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Get the user phoneNumber
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<string> GetPhoneNumberAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Returns true if the user phone number is confirmed
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> GetPhoneNumberConfirmedAsync(TUser user,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Sets whether the user phone number is confirmed
        /// </summary>
        /// <param name="user"></param>
        /// <param name="confirmed"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SetPhoneNumberConfirmedAsync(TUser user, bool confirmed,
            CancellationToken cancellationToken = default(CancellationToken));
    }
}