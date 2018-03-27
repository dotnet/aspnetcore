// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Identity.UI
{
    /// <summary>
    /// Provides an abstraction for instantiating the given user type.
    /// </summary>
    /// <typeparam name="TUser">The type of user.</typeparam>
    public interface IUserFactory<TUser> where TUser : class
    {
        /// <summary>
        /// Creates an instance of a user and assigns the provided values.
        /// </summary>
        /// <param name="email">Email address</param>
        /// <param name="userName">User name</param>
        /// <returns>Created user</returns>
        TUser CreateUser(string email, string userName);
    }
}
