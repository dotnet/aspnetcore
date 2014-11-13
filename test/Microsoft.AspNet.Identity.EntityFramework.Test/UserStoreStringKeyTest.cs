// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNet.Identity.EntityFramework.Test
{
    [TestCaseOrderer("Microsoft.AspNet.Identity.Test.PriorityOrderer", "Microsoft.AspNet.Identity.EntityFramework.Test")]
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

    [TestCaseOrderer("Microsoft.AspNet.Identity.Test.PriorityOrderer", "Microsoft.AspNet.Identity.EntityFramework.Test")]
    public class UserStoreStringKeyTest : SqlStoreTestBase<StringUser, StringRole, string>
    {
        private readonly string _connectionString = @"Server=(localdb)\mssqllocaldb;Database=SqlUserStoreStringTest" + DateTime.Now.Month + "-" + DateTime.Now.Day + "-" + DateTime.Now.Year + ";Trusted_Connection=True;";

        public override string ConnectionString
        {
            get
            {
                return _connectionString;
            }
        }
    }
}