// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Framework.OptionsModel;
using Moq;

namespace Microsoft.AspNet.Identity.Test
{
    public static class MockHelpers
    {
        public static Mock<UserManager<TUser>> MockUserManager<TUser>() where TUser : class
        {
            var store = new Mock<IUserStore<TUser>>();
            var options = new OptionsManager<IdentityOptions>(null);
            var userValidators = new List<IUserValidator<TUser>>();
            userValidators.Add(new UserValidator<TUser>());
            var pwdValidators = new List<IPasswordValidator<TUser>>();
            pwdValidators.Add(new PasswordValidator<TUser>());
            return new Mock<UserManager<TUser>>(
                store.Object,
                options,
                new PasswordHasher<TUser>(new PasswordHasherOptionsAccessor()),
                userValidators,
                pwdValidators,
                new UpperInvariantUserNameNormalizer(),
                new List<IUserTokenProvider<TUser>>(),
                new List<IIdentityMessageProvider>());
        }

        public static Mock<RoleManager<TRole>> MockRoleManager<TRole>() where TRole : class
        {
            var store = new Mock<IRoleStore<TRole>>();
            var roles = new List<IRoleValidator<TRole>>();
            roles.Add(new RoleValidator<TRole>());
            return new Mock<RoleManager<TRole>>(store.Object, roles);
        }

        public static UserManager<TUser> TestUserManager<TUser>() where TUser : class
        {
            return TestUserManager(new Mock<IUserStore<TUser>>().Object);
        }

        public static UserManager<TUser> TestUserManager<TUser>(IUserStore<TUser> store) where TUser : class
        {
            var options = new OptionsManager<IdentityOptions>(null);
            var validator = new Mock<UserValidator<TUser>>();
            var userManager = new UserManager<TUser>(store, options, new PasswordHasher<TUser>(new PasswordHasherOptionsAccessor()), 
                null, null, new UpperInvariantUserNameNormalizer(), null, null);
            userManager.UserValidators.Add(validator.Object);
            userManager.PasswordValidators.Add(new PasswordValidator<TUser>());
            validator.Setup(v => v.ValidateAsync(userManager, It.IsAny<TUser>(), CancellationToken.None))
                .Returns(Task.FromResult(IdentityResult.Success)).Verifiable();
            return userManager;
        }
    }
}