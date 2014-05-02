// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Interface to generate user tokens
    /// </summary>
    public interface IUserTokenProvider<TUser> where TUser : class
    {
        /// <summary>
        ///     Generate a token for a user
        /// </summary>
        /// <param name="purpose"></param>
        /// <param name="manager"></param>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<string> GenerateAsync(string purpose, UserManager<TUser> manager, TUser user, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     ValidateAsync and unprotect a token, returns null if invalid
        /// </summary>
        /// <param name="purpose"></param>
        /// <param name="token"></param>
        /// <param name="manager"></param>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser> manager, TUser user, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Notifies the user that a token has been generated, i.e. via email or sms, or can no-op
        /// </summary>
        /// <param name="token"></param>
        /// <param name="manager"></param>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task NotifyAsync(string token, UserManager<TUser> manager, TUser user, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Returns true if provider can be used for this user, i.e. could require a user to have an email
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> IsValidProviderForUserAsync(UserManager<TUser> manager, TUser user, CancellationToken cancellationToken = default(CancellationToken));
    }
}