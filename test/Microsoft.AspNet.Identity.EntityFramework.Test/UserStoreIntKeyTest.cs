// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

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

    [TestCaseOrderer("Microsoft.AspNet.Identity.Test.PriorityOrderer", "Microsoft.AspNet.Identity.EntityFramework.Test")]
    public class UserStoreIntTest : SqlStoreTestBase<IntUser, IntRole, int>
    {
        private readonly string _connectionString = @"Server=(localdb)\mssqllocaldb;Database=SqlUserStoreIntTest" + DateTime.Now.Month + "-" + DateTime.Now.Day + "-" + DateTime.Now.Year + ";Trusted_Connection=True;";

        public override string ConnectionString
        {
            get
            {
                return _connectionString;
            }
        }
    }
}