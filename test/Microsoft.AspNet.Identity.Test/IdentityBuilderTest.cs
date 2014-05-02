// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System.Security.Claims;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.DependencyInjection.Fallback;
using Xunit;

namespace Microsoft.AspNet.Identity.Test
{
    public class IdentityBuilderTest
    {
        [Fact]
        public void CanSpecifyUserValidatorInstance()
        {
            var services = new ServiceCollection();
            var validator = new UserValidator<IdentityUser>();
            services.AddIdentity<IdentityUser>(b => b.UseUserValidator(() => validator));
            Assert.Equal(validator, services.BuildServiceProvider().GetService<IUserValidator<IdentityUser>>());
        }

        [Fact]
        public void CanSpecifyPasswordValidatorInstance()
        {
            var services = new ServiceCollection();
            var validator = new PasswordValidator();
            services.AddIdentity<IdentityUser>(b => b.UsePasswordValidator(() => validator));
            Assert.Equal(validator, services.BuildServiceProvider().GetService<IPasswordValidator>());
        }

        [Fact]
        public void CanSpecifyLockoutPolicyInstance()
        {
            var services = new ServiceCollection();
            var policy = new LockoutPolicy();
            services.AddIdentity<IdentityUser>(b => b.UseLockoutPolicy(() => policy));
            Assert.Equal(policy, services.BuildServiceProvider().GetService<LockoutPolicy>());
        }

        [Fact]
        public void CanSpecifyPasswordHasherInstance()
        {
            CanOverride<IPasswordHasher, PasswordHasher>();
        }

        [Fact]
        public void CanSpecifyClaimsIdentityFactoryInstance()
        {
            CanOverride<IClaimsIdentityFactory<IdentityUser>, ClaimsIdentityFactory<IdentityUser>>();
        }

        [Fact]
        public void EnsureDefaultServices()
        {
            var services = new ServiceCollection();
            var builder = new IdentityBuilder<IdentityUser, IdentityRole>(services);
            builder.UseIdentity();

            var provider = services.BuildServiceProvider();
            var userValidator = provider.GetService<IUserValidator<IdentityUser>>() as UserValidator<IdentityUser>;
            Assert.NotNull(userValidator);
            Assert.True(userValidator.AllowOnlyAlphanumericUserNames);
            Assert.False(userValidator.RequireUniqueEmail);

            var pwdValidator = provider.GetService<IPasswordValidator>() as PasswordValidator;
            Assert.NotNull(userValidator);
            Assert.True(pwdValidator.RequireDigit);
            Assert.True(pwdValidator.RequireLowercase);
            Assert.True(pwdValidator.RequireNonLetterOrDigit);
            Assert.True(pwdValidator.RequireUppercase);
            Assert.Equal(6, pwdValidator.RequiredLength);

            var hasher = provider.GetService<IPasswordHasher>() as PasswordHasher;
            Assert.NotNull(hasher);

            var claimsFactory = provider.GetService<IClaimsIdentityFactory<IdentityUser>>() as ClaimsIdentityFactory<IdentityUser>;
            Assert.NotNull(claimsFactory);
            Assert.Equal(ClaimTypes.Role, claimsFactory.RoleClaimType);
            Assert.Equal(ClaimsIdentityFactory<IdentityUser>.DefaultSecurityStampClaimType, claimsFactory.SecurityStampClaimType);
            Assert.Equal(ClaimTypes.Name, claimsFactory.UserNameClaimType);
            Assert.Equal(ClaimTypes.NameIdentifier, claimsFactory.UserIdClaimType);
        }

        private static void CanOverride<TService, TImplementation>() where TImplementation : TService,new()
        {
            var services = new ServiceCollection();
            var instance = new TImplementation();
            services.AddIdentity<IdentityUser>(b => b.Use<TService>(() => instance));
            Assert.Equal(instance, services.BuildServiceProvider().GetService<TService>());
        }

    }
}