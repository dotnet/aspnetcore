// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.OptionsModel;
using System.Collections.Generic;
using Xunit;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Identity.Test
{
    public class IdentityBuilderTest
    {

        [Fact]
        public void CanOverrideUserStore()
        {
            var services = new ServiceCollection();
            services.AddIdentity().AddUserStore<MyUberThingy>();
            var thingy = services.BuildServiceProvider().GetRequiredService<IUserStore<IdentityUser>>() as MyUberThingy;
            Assert.NotNull(thingy);
        }

        [Fact]
        public void CanOverrideRoleStore()
        {
            var services = new ServiceCollection();
            services.AddIdentity().AddRoleStore<MyUberThingy>();
            var thingy = services.BuildServiceProvider().GetRequiredService<IRoleStore<IdentityRole>>() as MyUberThingy;
            Assert.NotNull(thingy);
        }

        [Fact]
        public void CanOverrideRoleValidator()
        {
            var services = new ServiceCollection();
            services.AddIdentity().AddRoleValidator<MyUberThingy>();
            var thingy = services.BuildServiceProvider().GetRequiredService<IRoleValidator<IdentityRole>>() as MyUberThingy;
            Assert.NotNull(thingy);
        }

        [Fact]
        public void CanOverrideUserValidator()
        {
            var services = new ServiceCollection();
            services.AddIdentity().AddUserValidator<MyUberThingy>();
            var thingy = services.BuildServiceProvider().GetRequiredService<IUserValidator<IdentityUser>>() as MyUberThingy;
            Assert.NotNull(thingy);
        }

        [Fact]
        public void CanOverridePasswordValidator()
        {
            var services = new ServiceCollection();
            services.AddIdentity().AddPasswordValidator<MyUberThingy>();
            var thingy = services.BuildServiceProvider().GetRequiredService<IPasswordValidator<IdentityUser>>() as MyUberThingy;
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
            services.AddIdentity();

            var provider = services.BuildServiceProvider();
            var userValidator = provider.GetRequiredService<IUserValidator<IdentityUser>>() as UserValidator<IdentityUser>;
            Assert.NotNull(userValidator);

            var pwdValidator = provider.GetRequiredService<IPasswordValidator<IdentityUser>>() as PasswordValidator<IdentityUser>;
            Assert.NotNull(pwdValidator);

            var hasher = provider.GetRequiredService<IPasswordHasher<IdentityUser>>() as PasswordHasher<IdentityUser>;
            Assert.NotNull(hasher);
        }

        [Fact]
        public void EnsureDefaultTokenProviders()
        {
            var services = new ServiceCollection();
            services.AddIdentity().AddDefaultTokenProviders();

            var provider = services.BuildServiceProvider();
            var tokenProviders = provider.GetRequiredService<IEnumerable<IUserTokenProvider<IdentityUser>>>();
            Assert.Equal(3, tokenProviders.Count());
        }

        private class MyUberThingy : IUserValidator<IdentityUser>, IPasswordValidator<IdentityUser>, IRoleValidator<IdentityRole>, IUserStore<IdentityUser>, IRoleStore<IdentityRole>
        {
            public Task<IdentityResult> CreateAsync(IdentityRole role, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<IdentityResult> CreateAsync(IdentityUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<IdentityResult> DeleteAsync(IdentityRole role, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<IdentityResult> DeleteAsync(IdentityUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public void Dispose()
            {
                throw new NotImplementedException();
            }

            public Task<IdentityUser> FindByIdAsync(string userId, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<IdentityUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<string> GetNormalizedRoleNameAsync(IdentityRole role, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<string> GetNormalizedUserNameAsync(IdentityUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<string> GetRoleIdAsync(IdentityRole role, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<string> GetRoleNameAsync(IdentityRole role, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<string> GetUserIdAsync(IdentityUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<string> GetUserNameAsync(IdentityUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task SetNormalizedRoleNameAsync(IdentityRole role, string normalizedName, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task SetNormalizedUserNameAsync(IdentityUser user, string normalizedName, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task SetRoleNameAsync(IdentityRole role, string roleName, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task SetUserNameAsync(IdentityUser user, string userName, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<IdentityResult> UpdateAsync(IdentityRole role, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<IdentityResult> UpdateAsync(IdentityUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<IdentityResult> ValidateAsync(RoleManager<IdentityRole> manager, IdentityRole role, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<IdentityResult> ValidateAsync(UserManager<IdentityUser> manager, IdentityUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<IdentityResult> ValidateAsync(UserManager<IdentityUser> manager, IdentityUser user, string password, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            Task<IdentityRole> IRoleStore<IdentityRole>.FindByIdAsync(string roleId, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            Task<IdentityRole> IRoleStore<IdentityRole>.FindByNameAsync(string roleName, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }

        private class MyUserManager : UserManager<TestUser>
        {
            public MyUserManager(IUserStore<TestUser> store) : base(store) { }
        }

        private class MyRoleManager : RoleManager<TestRole>
        {
            public MyRoleManager(IRoleStore<TestRole> store,
                IEnumerable<IRoleValidator<TestRole>> roleValidators) : base(store)
            {

            }
        }
    }
}