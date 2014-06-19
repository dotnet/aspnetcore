// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Interface that exposes an IQueryable roles
    /// </summary>
    /// <typeparam name="TRole"></typeparam>
    public interface IQueryableRoleStore<TRole> : IRoleStore<TRole> where TRole : class
    {
        /// <summary>
        ///     IQueryable roles
        /// </summary>
        IQueryable<TRole> Roles { get; }
    }
}