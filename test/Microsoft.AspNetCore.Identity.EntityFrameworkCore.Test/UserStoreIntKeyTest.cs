// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Identity.EntityFramework.Test
{
    public class IntUser : IdentityUser<int>
    {
        public IntUser()
        {
            UserName = Guid.NewGuid().ToString();
        }
    }

    public class IntRole : IdentityRole<int>
    {
        public IntRole()
        {
            Name = Guid.NewGuid().ToString();
        }
    }

    public class UserStoreIntTest : SqlStoreTestBase<IntUser, IntRole, int>
    {
        public UserStoreIntTest(ScratchDatabaseFixture fixture)
            : base(fixture)
        {
        }
    }
}