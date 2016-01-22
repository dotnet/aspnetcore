// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.Test
{
    public class StringUser : IdentityUser
    {
        public StringUser()
        {
            Id = Guid.NewGuid().ToString();
            UserName = Id;
        }
    }

    public class StringRole : IdentityRole<string>
    {
        public StringRole()
        {
            Id = Guid.NewGuid().ToString();
            Name = Id;
        }
    }

    public class UserStoreStringKeyTest : SqlStoreTestBase<StringUser, StringRole, string>
    {
        public UserStoreStringKeyTest(ScratchDatabaseFixture fixture)
            : base(fixture)
        {
        }
    }
}