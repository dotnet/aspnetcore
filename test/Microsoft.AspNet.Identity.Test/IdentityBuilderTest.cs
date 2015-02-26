// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Framework.DependencyInjection;
using Xunit;

namespace Microsoft.AspNet.Identity.Test
{
    public class IdentityBuilderTest
    {

        [Fact]
        public void CanOverrideUserStore()
        {
            var services = new ServiceCollection();
            services.AddIdentity<TestUser,TestRole>().AddUserStore<MyUberThingy>();
            var thingy = services.BuildServiceProvider().GetRequiredService<IUserStore<TestUser>>() as MyUberThingy;
            Assert.NotNull(thingy);
        }

        [Fact]
        public void CanOverrideRoleStore()
        {
            var services = new ServiceCollection();
            services.AddIdentity<TestUser,TestRole>().AddRoleStore<MyUberThingy>();
            var thingy = services.BuildServiceProvider().GetRequiredService<IRoleStore<TestRole>>() as MyUberThingy;
            Assert.NotNull(thingy);
        }

        [Fact]
        public void CanOverrideRoleValidator()
        {
            var services = new ServiceCollection();
            services.AddIdentity<TestUser,TestRole>().AddRoleValidator<MyUberThingy>();
            var thingy = services.BuildServiceProvider().GetRequiredService<IRoleValidator<TestRole>>() as MyUberThingy;
            Assert.NotNull(thingy);
        }

        [Fact]
        public void CanOverrideUserValidator()
        {
            var services = new ServiceCollection();
            services.AddIdentity<TestUser,TestRole>().AddUserValidator<MyUberThingy>();
            var thingy = services.BuildServiceProvider().GetRequiredService<IUserValidator<TestUser>>() as MyUberThingy;
            Assert.NotNull(thingy);
        }

        [Fact]
        public void CanOverridePasswordValidator()
        {
            var services = new ServiceCollection();
            services.AddIdentity<TestUser,TestRole>().AddPasswordValidator<MyUberThingy>();
            var thingy = services.BuildServiceProvider().GetRequiredService<IPasswordValidator<TestUser>>() as MyUberThingy;
            Assert.NotNull(thingy);
        }

        [Fact]
        public void CanOverrideUserManager()
        {
            var services = new ServiceCollection();
            services.AddIdentity<TestUser, TestRole>()
                .AddUserStore<NoopUserStore>()
                .AddUserManager<MyUserManager>();
            var myUserManager = services.BuildServiceProvider().GetRequiredService(typeof(UserManager<TestUser>)) as MyUserManager;
            Assert.NotNull(myUserManager);
        }

        [Fact]
        public void CanOverrideRoleManager()
        {
            var services = new ServiceCollection();
            services.AddIdentity<TestUser, TestRole>()
                    .AddRoleStore<NoopRoleStore>()
                    .AddRoleManager<MyRoleManager>();
            var myRoleManager = services.BuildServiceProvider().GetRequiredService<RoleManager<TestRole>>() as MyRoleManager;
            Assert.NotNull(myRoleManager);
        }

        [Fact]
        public void EnsureDefaultServices()
        {
            var services = new ServiceCollection();
            services.AddIdentity<TestUser,TestRole>();

            var provider = services.BuildServiceProvider();
            var userValidator = provider.GetRequiredService<IUserValidator<TestUser>>() as UserValidator<TestUser>;
            Assert.NotNull(userValidator);

            var pwdValidator = provider.GetRequiredService<IPasswordValidator<TestUser>>() as PasswordValidator<TestUser>;
            Assert.NotNull(pwdValidator);

            var hasher = provider.GetRequiredService<IPasswordHasher<TestUser>>() as PasswordHasher<TestUser>;
            Assert.NotNull(hasher);
        }

        [Fact]
        public void EnsureDefaultTokenProviders()
        {
            var services = new ServiceCollection();
            services.AddIdentity<TestUser,TestRole>().AddDefaultTokenProviders();

            var provider = services.BuildServiceProvider();
            var tokenProviders = provider.GetRequiredService<IEnumerable<IUserTokenProvider<TestUser>>>();
            Assert.Equal(3, tokenProviders.Count());
        }

        private class MyUberThingy : IUserValidator<TestUser>, IPasswordValidator<TestUser>, IRoleValidator<TestRole>, IUserStore<TestUser>, IRoleStore<TestRole>
        {
            public Task<IdentityResult> CreateAsync(TestRole role, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<IdentityResult> CreateAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<IdentityResult> DeleteAsync(TestRole role, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<IdentityResult> DeleteAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public void Dispose()
            {
                throw new NotImplementedException();
            }

            public Task<TestUser> FindByIdAsync(string userId, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<TestUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<string> GetNormalizedRoleNameAsync(TestRole role, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<string> GetNormalizedUserNameAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<string> GetRoleIdAsync(TestRole role, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<string> GetRoleNameAsync(TestRole role, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<string> GetUserIdAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<string> GetUserNameAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task SetNormalizedRoleNameAsync(TestRole role, string normalizedName, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task SetNormalizedUserNameAsync(TestUser user, string normalizedName, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task SetRoleNameAsync(TestRole role, string roleName, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task SetUserNameAsync(TestUser user, string userName, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<IdentityResult> UpdateAsync(TestRole role, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<IdentityResult> UpdateAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<IdentityResult> ValidateAsync(RoleManager<TestRole> manager, TestRole role)
            {
                throw new NotImplementedException();
            }

            public Task<IdentityResult> ValidateAsync(UserManager<TestUser> manager, TestUser user)
            {
                throw new NotImplementedException();
            }

            public Task<IdentityResult> ValidateAsync(UserManager<TestUser> manager, TestUser user, string password)
            {
                throw new NotImplementedException();
            }

            Task<TestRole> IRoleStore<TestRole>.FindByIdAsync(string roleId, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            Task<TestRole> IRoleStore<TestRole>.FindByNameAsync(string roleName, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }

        private class MyUserManager : UserManager<TestUser>
        {
            public MyUserManager(IUserStore<TestUser> store) : base(store, null, null, null, null, null, null, null, null, null) { }
        }

        private class MyRoleManager : RoleManager<TestRole>
        {
            public MyRoleManager(IRoleStore<TestRole> store,
                IEnumerable<IRoleValidator<TestRole>> roleValidators) : base(store, null, null, null, null, null)
            {

            }
        }
    }
}