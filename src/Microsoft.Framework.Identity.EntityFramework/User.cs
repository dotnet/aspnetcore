// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Identity.Entity
{
    public class User
    {
        public User()
        {
            Id = Guid.NewGuid().ToString();
        }

        public User(string userName) : this()
        {
            UserName = userName;
        }

        public virtual string Id { get; set; }
        public virtual string UserName { get; set; }

        /// <summary>
        ///     The salted/hashed form of the user password
        /// </summary>
        public virtual string PasswordHash { get; set; }
    }
}

