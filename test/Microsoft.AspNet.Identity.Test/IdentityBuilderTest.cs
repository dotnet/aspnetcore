// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.OptionsModel;
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
            services.AddIdentity().AddUserValidator(validator);
            Assert.Equal(validator, services.BuildServiceProvider().GetRequiredService<IUserValidator<IdentityUser>>());
        }

        [Fact]
        public void CanSpecifyPasswordValidatorInstance()
        {
            var services = new ServiceCollection();
            var validator = new PasswordValidator<IdentityUser>();
            services.AddIdentity().AddPasswordValidator(validator);
            Assert.Equal(validator, services.BuildServiceProvider().GetRequiredService<IPasswordValidator<IdentityUser>>());
        }

        [Fact]
        public void CanSpecifyPasswordHasherInstance()
        {
            CanOverride<IPasswordHasher<IdentityUser>>(new PasswordHasher<IdentityUser>(new PasswordHasherOptionsAccessor()));
        }

        [Fact]
        public void EnsureDefaultServices()
        {
            var services = new ServiceCollection();
            services.AddIdentity();
            services.Add(OptionsServices.GetDefaultServices());

            var provider = services.BuildServiceProvider();
            var userValidator = provider.GetRequiredService<IUserValidator<IdentityUser>>() as UserValidator<IdentityUser>;
            Assert.NotNull(userValidator);

            var pwdValidator = provider.GetRequiredService<IPasswordValidator<IdentityUser>>() as PasswordValidator<IdentityUser>;
            Assert.NotNull(pwdValidator);

            var hasher = provider.GetRequiredService<IPasswordHasher<IdentityUser>>() as PasswordHasher<IdentityUser>;
            Assert.NotNull(hasher);
        }

        private static void CanOverride<TService>(TService instance)
            where TService : class
        {
            var services = new ServiceCollection();
            services.AddIdentity().AddInstance(instance);
            Assert.Equal(instance, services.BuildServiceProvider().GetRequiredService<TService>());
        }

    }
}