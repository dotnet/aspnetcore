// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity.Test;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Identity.InMemory.Test
{
    public class InMemoryStoreTest : IdentitySpecificationTestBase<PocoUser, PocoRole>
    {
        protected override object CreateTestContext()
        {
            return new InMemoryStore<PocoUser, PocoRole>();
        }

        protected override void AddUserStore(IServiceCollection services, object context = null)
        {
            services.AddSingleton<IUserStore<PocoUser>>((InMemoryStore<PocoUser, PocoRole>)context);
        }

        protected override void AddRoleStore(IServiceCollection services, object context = null)
        {
            services.AddSingleton<IRoleStore<PocoRole>>((InMemoryStore<PocoUser, PocoRole>)context);
        }

        protected override void SetUserPasswordHash(PocoUser user, string hashedPassword)
        {
            user.PasswordHash = hashedPassword;
        }

        protected override PocoUser CreateTestUser(string namePrefix = "", string email = "", string phoneNumber = "",
            bool lockoutEnabled = false, DateTimeOffset? lockoutEnd = default(DateTimeOffset?), bool useNamePrefixAsUserName = false)
        {
            return new PocoUser
            {
                UserName = useNamePrefixAsUserName ? namePrefix : string.Format("{0}{1}", namePrefix, Guid.NewGuid()),
                Email = email,
                PhoneNumber = phoneNumber,
                LockoutEnabled = lockoutEnabled,
                LockoutEnd = lockoutEnd
            };
        }

        protected override PocoRole CreateTestRole(string roleNamePrefix = "", bool useRoleNamePrefixAsRoleName = false)
        {
            var roleName = useRoleNamePrefixAsRoleName ? roleNamePrefix : string.Format("{0}{1}", roleNamePrefix, Guid.NewGuid());
            return new PocoRole(roleName);
        }

        protected override Expression<Func<PocoUser, bool>> UserNameEqualsPredicate(string userName) => u => u.UserName == userName;

        protected override Expression<Func<PocoRole, bool>> RoleNameEqualsPredicate(string roleName) => r => r.Name == roleName;

        protected override Expression<Func<PocoUser, bool>> UserNameStartsWithPredicate(string userName) => u => u.UserName.StartsWith(userName);

        protected override Expression<Func<PocoRole, bool>> RoleNameStartsWithPredicate(string roleName) => r => r.Name.StartsWith(roleName);
    }
}