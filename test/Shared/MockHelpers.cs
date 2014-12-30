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
            var mgr = new Mock<UserManager<TUser>>(store.Object, null, null, null, null, null, null, null, null);
            mgr.Object.UserValidators.Add(new UserValidator<TUser>());
            mgr.Object.PasswordValidators.Add(new PasswordValidator<TUser>());
            return mgr;
        }

        public static Mock<RoleManager<TRole>> MockRoleManager<TRole>() where TRole : class
        {
            var store = new Mock<IRoleStore<TRole>>();
            var roles = new List<IRoleValidator<TRole>>();
            roles.Add(new RoleValidator<TRole>());
            return new Mock<RoleManager<TRole>>(store.Object, roles, null);
        }

        public static UserManager<TUser> TestUserManager<TUser>() where TUser : class
        {
            return TestUserManager(new Mock<IUserStore<TUser>>().Object);
        }

        public static UserManager<TUser> TestUserManager<TUser>(IUserStore<TUser> store) where TUser : class
        {
            var validator = new Mock<IUserValidator<TUser>>();
            var userManager = new UserManager<TUser>(store);
            userManager.UserValidators.Add(validator.Object);
            userManager.PasswordValidators.Add(new PasswordValidator<TUser>());
            validator.Setup(v => v.ValidateAsync(userManager, It.IsAny<TUser>(), CancellationToken.None))
                .Returns(Task.FromResult(IdentityResult.Success)).Verifiable();
            return userManager;
        }
    }
}