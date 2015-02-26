// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;
using Microsoft.AspNet.Identity.Test;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Identity.EntityFramework.InMemory.Test
{
    public class InMemoryEFUserStoreTest : UserManagerTestBase<IdentityUser, IdentityRole, string>
    {
        protected override object CreateTestContext()
        {
            return new InMemoryContext();
        }

        protected override void AddUserStore(IServiceCollection services, object context = null)
        {
            services.AddInstance<IUserStore<IdentityUser>>(new UserStore<IdentityUser>((InMemoryContext)context));
        }

        protected override void AddRoleStore(IServiceCollection services, object context = null)
        {
            var store = new RoleStore<IdentityRole, InMemoryContext>((InMemoryContext)context);
            services.AddInstance<IRoleStore<IdentityRole>>(store);
        }

        protected override IdentityUser CreateTestUser(string namePrefix = "", string email = "", string phoneNumber = "",
            bool lockoutEnabled = false, DateTimeOffset? lockoutEnd = default(DateTimeOffset?), bool useNamePrefixAsUserName = false)
        {
            return new IdentityUser
            {
                UserName = useNamePrefixAsUserName ? namePrefix : string.Format("{0}{1}", namePrefix, Guid.NewGuid()),
                Email = email,
                PhoneNumber = phoneNumber,
                LockoutEnabled = lockoutEnabled,
                LockoutEnd = lockoutEnd
            };
        }

        protected override IdentityRole CreateTestRole(string roleNamePrefix = "", bool useRoleNamePrefixAsRoleName = false)
        {
            var roleName = useRoleNamePrefixAsRoleName ? roleNamePrefix : string.Format("{0}{1}", roleNamePrefix, Guid.NewGuid());
            return new IdentityRole(roleName);
        }

        protected override void SetUserPasswordHash(IdentityUser user, string hashedPassword)
        {
            user.PasswordHash = hashedPassword;
        }

        protected override Expression<Func<IdentityUser, bool>> UserNameEqualsPredicate(string userName) => u => u.UserName == userName;

        protected override Expression<Func<IdentityRole, bool>> RoleNameEqualsPredicate(string roleName) => r => r.Name == roleName;

        protected override Expression<Func<IdentityUser, bool>> UserNameStartsWithPredicate(string userName) => u => u.UserName.StartsWith(userName);

        protected override Expression<Func<IdentityRole, bool>> RoleNameStartsWithPredicate(string roleName) => r => r.Name.StartsWith(roleName);
    }
}
