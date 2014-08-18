// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Identity.Test;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using System;
using Xunit;

namespace Microsoft.AspNet.Identity.EntityFramework.Test
{
    public class GuidUser : IdentityUser<Guid>
    {
        public GuidUser()
        {
            Id = Guid.NewGuid();
            UserName = Id.ToString();
        }
    }

    public class GuidRole : IdentityRole<Guid>
    {
        public GuidRole()
        {
            Id = Guid.NewGuid();
            Name = Id.ToString();
        }
    }

    [TestCaseOrderer("Microsoft.AspNet.Identity.Test.PriorityOrderer", "Microsoft.AspNet.Identity.EntityFramework.Test")]
    public class UserStoreGuidTest : SqlStoreTestBase<GuidUser, GuidRole, Guid>
    {
        public override string ConnectionString
        {
            get
            {
                return @"Server=(localdb)\v11.0;Database=SqlUserStoreGuidTest;Trusted_Connection=True;";
            }
        }

        public class ApplicationUserStore : UserStore<GuidUser, GuidRole, ApplicationDbContext, Guid>
        {
            public ApplicationUserStore(ApplicationDbContext context) : base(context) { }

            public override Guid ConvertIdFromString(string userId)
            {
                return new Guid(userId);
            }
        }

        public class ApplicationRoleStore : RoleStore<GuidRole, ApplicationDbContext, Guid>
        {
            public ApplicationRoleStore(ApplicationDbContext context) : base(context) { }

            public override Guid ConvertIdFromString(string id)
            {
                return new Guid(id);
            }
        }

        protected override UserManager<GuidUser> CreateManager(object context)
        {
            if (context == null)
            {
                context = CreateTestContext();
            }
            return MockHelpers.CreateManager(() => new ApplicationUserStore((ApplicationDbContext)context));
        }

        protected override RoleManager<GuidRole> CreateRoleManager(object context)
        {
            if (context == null)
            {
                context = CreateTestContext();
            }
            var services = new ServiceCollection();
            services.AddIdentity<GuidUser, GuidRole>().AddRoleStore(() => new ApplicationRoleStore((ApplicationDbContext)context));
            return services.BuildServiceProvider().GetService<RoleManager<GuidRole>>();
        }
    }
}