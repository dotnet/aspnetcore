// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Identity.UI
{
    internal class UserFactory<TUser, TKey> : IUserFactory<TUser>
        where TUser : IdentityUser<TKey>, new()
        where TKey : IEquatable<TKey>
    {
        public TUser CreateUser(string email, string userName)
        {
            return new TUser() { Email = email, UserName = userName };
        }
    }
}
