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
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
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
            services.AddIdentity<IdentityUser>(b => b.AddUserValidator(() => validator));
            Assert.Equal(validator, services.BuildServiceProvider().GetService<IUserValidator<IdentityUser>>());
        }

        [Fact]
        public void CanSpecifyPasswordValidatorInstance()
        {
            var services = new ServiceCollection();
            var validator = new PasswordValidator<IdentityUser>();
            services.AddIdentity<IdentityUser>(b => b.AddPasswordValidator(() => validator));
            Assert.Equal(validator, services.BuildServiceProvider().GetService<IPasswordValidator<IdentityUser>>());
        }

        [Fact]
        public void CanSpecifyPasswordHasherInstance()
        {
            CanOverride<IPasswordHasher>(new PasswordHasher());
        }

        [Fact]
        public void CanSpecifyClaimsIdentityFactoryInstance()
        {
            CanOverride<IClaimsIdentityFactory<IdentityUser>>(new ClaimsIdentityFactory<IdentityUser>());
        }

        [Fact]
        public void EnsureDefaultServices()
        {
            var services = new ServiceCollection();
            services.AddIdentity<IdentityUser>(identity => { });

            var provider = services.BuildServiceProvider();
            var userValidator = provider.GetService<IUserValidator<IdentityUser>>() as UserValidator<IdentityUser>;
            Assert.NotNull(userValidator);

            var pwdValidator = provider.GetService<IPasswordValidator<IdentityUser>>() as PasswordValidator<IdentityUser>;
            Assert.NotNull(pwdValidator);

            var hasher = provider.GetService<IPasswordHasher>() as PasswordHasher;
            Assert.NotNull(hasher);

            var claimsFactory = provider.GetService<IClaimsIdentityFactory<IdentityUser>>() as ClaimsIdentityFactory<IdentityUser>;
            Assert.NotNull(claimsFactory);
        }

        private static void CanOverride<TService>(TService instance)
        {
            var services = new ServiceCollection();
            services.AddIdentity<IdentityUser>(b => b.AddInstance<TService>(() => instance));
            Assert.Equal(instance, services.BuildServiceProvider().GetService<TService>());
        }

    }
}