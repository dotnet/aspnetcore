// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Used to validate a user
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    public interface IUserValidator<TUser> where TUser : class
    {
        /// <summary>
        ///     ValidateAsync the user
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user,
            CancellationToken cancellationToken = default(CancellationToken));
    }
}