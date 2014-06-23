// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
        }

        private static void CanOverride<TService>(TService instance)
        {
            var services = new ServiceCollection();
            services.AddIdentity<IdentityUser>(b => b.AddInstance<TService>(() => instance));
            Assert.Equal(instance, services.BuildServiceProvider().GetService<TService>());
        }

    }
}