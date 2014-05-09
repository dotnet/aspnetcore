// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Interface that exposes an IQueryable users
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    public interface IQueryableUserStore<TUser> : IUserStore<TUser> where TUser : class
    {
        /// <summary>
        ///     IQueryable users
        /// </summary>
        IQueryable<TUser> Users { get; }
    }
}