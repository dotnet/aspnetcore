// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Identity.Test
{
    public class IdentityBuilderTest
    {
        [Fact]
        public void AddRolesServicesAdded()
        {
            var services = new ServiceCollection();
            services.AddIdentityCore<PocoUser>(o => { })
                .AddRoles<PocoRole>()
                .AddUserStore<NoopUserStore>()
                .AddRoleStore<NoopRoleStore>();
            var sp = services.BuildServiceProvider();
            Assert.NotNull(sp.GetRequiredService<IRoleValidator<PocoRole>>());
            Assert.IsType<NoopRoleStore>(sp.GetRequiredService<IRoleStore<PocoRole>>());
            Assert.IsType<RoleManager<PocoRole>>(sp.GetRequiredService<RoleManager<PocoRole>>());
            Assert.NotNull(sp.GetRequiredService<RoleManager<PocoRole>>());
            Assert.IsType<UserClaimsPrincipalFactory<PocoUser, PocoRole>>(sp.GetRequiredService<IUserClaimsPrincipalFactory<PocoUser>>());
        }

        [Fact]
        public void AddRolesWithoutStoreWillError()
        {
            var services = new ServiceCollection();
            services.AddIdentityCore<PocoUser>(o => { })
                .AddRoles<PocoRole>();
            var sp = services.BuildServiceProvider();
            Assert.NotNull(sp.GetRequiredService<IRoleValidator<PocoRole>>());
            Assert.Null(sp.GetService<IRoleStore<PocoRole>>());
            Assert.Throws<InvalidOperationException>(() => sp.GetService<RoleManager<PocoRole>>());
        }


        [Fact]
        public void CanOverrideUserStore()
        {
            var services = new ServiceCollection()
               .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            services.AddIdentity<PocoUser,PocoRole>().AddUserStore<MyUberThingy>();
            var thingy = services.BuildServiceProvider().GetRequiredService<IUserStore<PocoUser>>() as MyUberThingy;
            Assert.NotNull(thingy);
        }

        [Fact]
        public void CanOverrideRoleStore()
        {
            var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            services.AddIdentity<PocoUser,PocoRole>().AddRoleStore<MyUberThingy>();
            var thingy = services.BuildServiceProvider().GetRequiredService<IRoleStore<PocoRole>>() as MyUberThingy;
            Assert.NotNull(thingy);
        }

        [Fact]
        public void CanOverridePrincipalFactory()
        {
            var services = new ServiceCollection()
                .AddLogging()
                .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            services.AddIdentity<PocoUser, PocoRole>()
                .AddClaimsPrincipalFactory<MyClaimsPrincipalFactory>()
                .AddUserManager<MyUserManager>()
                .AddUserStore<NoopUserStore>()
                .AddRoleStore<NoopRoleStore>();
            var thingy = services.BuildServiceProvider().GetRequiredService<IUserClaimsPrincipalFactory<PocoUser>>() as MyClaimsPrincipalFactory;
            Assert.NotNull(thingy);
        }

        [Fact]
        public void CanOverrideUserConfirmation()
        {
            var services = new ServiceCollection()
                .AddLogging()
                .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            services.AddIdentity<PocoUser, PocoRole>()
                .AddClaimsPrincipalFactory<MyClaimsPrincipalFactory>()
                .AddUserConfirmation<MyUserConfirmation>()
                .AddUserManager<MyUserManager>()
                .AddUserStore<NoopUserStore>()
                .AddRoleStore<NoopRoleStore>();
            var thingy = services.BuildServiceProvider().GetRequiredService<IUserConfirmation<PocoUser>>() as MyUserConfirmation;
            Assert.NotNull(thingy);
        }

        [Fact]
        public void CanOverrideRoleValidator()
        {
            var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            services.AddIdentity<PocoUser,PocoRole>().AddRoleValidator<MyUberThingy>();
            var thingy = services.BuildServiceProvider().GetRequiredService<IRoleValidator<PocoRole>>() as MyUberThingy;
            Assert.NotNull(thingy);
        }

        [Fact]
        public void CanOverrideUserValidator()
        {
            var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            services.AddIdentity<PocoUser,PocoRole>().AddUserValidator<MyUberThingy>();
            var thingy = services.BuildServiceProvider().GetRequiredService<IUserValidator<PocoUser>>() as MyUberThingy;
            Assert.NotNull(thingy);
        }

        [Fact]
        public void CanOverridePasswordValidator()
        {
            var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            services.AddIdentity<PocoUser,PocoRole>().AddPasswordValidator<MyUberThingy>();
            var thingy = services.BuildServiceProvider().GetRequiredService<IPasswordValidator<PocoUser>>() as MyUberThingy;
            Assert.NotNull(thingy);
        }

        [Fact]
        public void CanOverrideUserManager()
        {
            var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            services.AddIdentity<PocoUser, PocoRole>()
                .AddUserStore<NoopUserStore>()
                .AddUserManager<MyUserManager>();
            var myUserManager = services.BuildServiceProvider().GetRequiredService(typeof(UserManager<PocoUser>)) as MyUserManager;
            Assert.NotNull(myUserManager);
        }

        [Fact]
        public void CanOverrideRoleManager()
        {
            var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            services.AddIdentity<PocoUser, PocoRole>()
                    .AddRoleStore<NoopRoleStore>()
                    .AddRoleManager<MyRoleManager>();
            var myRoleManager = services.BuildServiceProvider().GetRequiredService<RoleManager<PocoRole>>() as MyRoleManager;
            Assert.NotNull(myRoleManager);
        }

        [Fact]
        public void CanOverrideSignInManager()
        {
            var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
                .AddHttpContextAccessor()
                .AddLogging();
            services.AddIdentity<PocoUser, PocoRole>()
                .AddUserStore<NoopUserStore>()
                .AddRoleStore<NoopRoleStore>()
                .AddUserManager<MyUserManager>()
                .AddClaimsPrincipalFactory<MyClaimsPrincipalFactory>()
                .AddSignInManager<MySignInManager>();
            var myUserManager = services.BuildServiceProvider().GetRequiredService(typeof(SignInManager<PocoUser>)) as MySignInManager;
            Assert.NotNull(myUserManager);
        }

        [Fact]
        public void EnsureDefaultServices()
        {
            var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            services.AddLogging()
                .AddIdentity<PocoUser,PocoRole>()
                .AddUserStore<NoopUserStore>()
                .AddRoleStore<NoopRoleStore>();

            var provider = services.BuildServiceProvider();
            var userValidator = provider.GetRequiredService<IUserValidator<PocoUser>>() as UserValidator<PocoUser>;
            Assert.NotNull(userValidator);

            var pwdValidator = provider.GetRequiredService<IPasswordValidator<PocoUser>>() as PasswordValidator<PocoUser>;
            Assert.NotNull(pwdValidator);

            var hasher = provider.GetRequiredService<IPasswordHasher<PocoUser>>() as PasswordHasher<PocoUser>;
            Assert.NotNull(hasher);

            Assert.IsType<RoleManager<PocoRole>>(provider.GetRequiredService<RoleManager<PocoRole>>());
            Assert.IsType<UserManager<PocoUser>>(provider.GetRequiredService<UserManager<PocoUser>>());
        }

        [Fact]
        public void EnsureDefaultTokenProviders()
        {
            var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            services.AddIdentity<PocoUser,PocoRole>().AddDefaultTokenProviders();

            var provider = services.BuildServiceProvider();
            var tokenProviders = provider.GetRequiredService<IOptions<IdentityOptions>>().Value.Tokens.ProviderMap.Values;
            Assert.Equal(4, tokenProviders.Count());
        }

        [Fact]
        public void AddManagerWithWrongTypesThrows()
        {
            var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            var builder = services.AddIdentity<PocoUser, PocoRole>();
            Assert.Throws<InvalidOperationException>(() => builder.AddUserManager<object>());
            Assert.Throws<InvalidOperationException>(() => builder.AddRoleManager<object>());
            Assert.Throws<InvalidOperationException>(() => builder.AddSignInManager<object>());
        }

        [Fact]
        public void AddTokenProviderWithWrongTypesThrows()
        {
            var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            var builder = services.AddIdentity<PocoUser, PocoRole>();
            Assert.Throws<InvalidOperationException>(() => builder.AddTokenProvider<object>("whatevs"));
            Assert.Throws<InvalidOperationException>(() => builder.AddTokenProvider("whatevs", typeof(object)));
        }

        private class MyUberThingy : IUserValidator<PocoUser>, IPasswordValidator<PocoUser>, IRoleValidator<PocoRole>, IUserStore<PocoUser>, IRoleStore<PocoRole>
        {
            public Task<IdentityResult> CreateAsync(PocoRole role, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<IdentityResult> CreateAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<IdentityResult> DeleteAsync(PocoRole role, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<IdentityResult> DeleteAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public void Dispose()
            {
                throw new NotImplementedException();
            }

            public Task<PocoUser> FindByIdAsync(string userId, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<PocoUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<string> GetNormalizedRoleNameAsync(PocoRole role, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<string> GetNormalizedUserNameAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<string> GetRoleIdAsync(PocoRole role, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<string> GetRoleNameAsync(PocoRole role, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<string> GetUserIdAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<string> GetUserNameAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task SetNormalizedRoleNameAsync(PocoRole role, string normalizedName, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task SetNormalizedUserNameAsync(PocoUser user, string normalizedName, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task SetRoleNameAsync(PocoRole role, string roleName, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task SetUserNameAsync(PocoUser user, string userName, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<IdentityResult> UpdateAsync(PocoRole role, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<IdentityResult> UpdateAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<IdentityResult> ValidateAsync(RoleManager<PocoRole> manager, PocoRole role)
            {
                throw new NotImplementedException();
            }

            public Task<IdentityResult> ValidateAsync(UserManager<PocoUser> manager, PocoUser user)
            {
                throw new NotImplementedException();
            }

            public Task<IdentityResult> ValidateAsync(UserManager<PocoUser> manager, PocoUser user, string password)
            {
                throw new NotImplementedException();
            }

            Task<PocoRole> IRoleStore<PocoRole>.FindByIdAsync(string roleId, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            Task<PocoRole> IRoleStore<PocoRole>.FindByNameAsync(string roleName, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }

        private class MySignInManager : SignInManager<PocoUser>
        {
            public MySignInManager(UserManager<PocoUser> manager, IHttpContextAccessor context, IUserClaimsPrincipalFactory<PocoUser> claimsFactory) : base(manager, context, claimsFactory, null, null, null, null) { }
        }

        private class MyUserManager : UserManager<PocoUser>
        {
            public MyUserManager(IUserStore<PocoUser> store) : base(store, null, null, null, null, null, null, null, null) { }
        }

        private class MyClaimsPrincipalFactory : UserClaimsPrincipalFactory<PocoUser, PocoRole>
        {
            public MyClaimsPrincipalFactory(UserManager<PocoUser> userManager, RoleManager<PocoRole> roleManager, IOptions<IdentityOptions> optionsAccessor) : base(userManager, roleManager, optionsAccessor)
            {
            }
        }

        private class MyRoleManager : RoleManager<PocoRole>
        {
            public MyRoleManager(IRoleStore<PocoRole> store,
                IEnumerable<IRoleValidator<PocoRole>> roleValidators) : base(store, null, null, null, null)
            {

            }
        }

        private class MyUserConfirmation : DefaultUserConfirmation<PocoUser>
        {
        }
    }
}
