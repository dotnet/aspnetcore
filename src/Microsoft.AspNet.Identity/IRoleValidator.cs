// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Used to validate a role
    /// </summary>
    /// <typeparam name="TRole"></typeparam>
    public interface IRoleValidator<TRole> where TRole : class
    {
        /// <summary>
        ///     ValidateAsync the user
        /// </summary>
        /// <param name="role"></param>
        /// <param name="manager"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IdentityResult> ValidateAsync(RoleManager<TRole> manager, TRole role,
            CancellationToken cancellationToken = default(CancellationToken));
    }
}