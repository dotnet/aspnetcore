// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using Microsoft.Framework.Logging;
using Moq;

namespace Microsoft.AspNet.Identity.Test
{
    public static class MockHelpers
    {
        public static StringBuilder LogMessage = new StringBuilder();

        public static Mock<UserManager<TUser>> MockUserManager<TUser>() where TUser : class
        {
            var store = new Mock<IUserStore<TUser>>();
            var mgr = new Mock<UserManager<TUser>>(store.Object, null, null, null, null, null, null, null, null, null);
            mgr.Object.UserValidators.Add(new UserValidator<TUser>());
            mgr.Object.PasswordValidators.Add(new PasswordValidator<TUser>());
            return mgr;
        }

        public static Mock<RoleManager<TRole>> MockRoleManager<TRole>(IRoleStore<TRole> store = null) where TRole : class
        {
            store = store ?? new Mock<IRoleStore<TRole>>().Object;
            var roles = new List<IRoleValidator<TRole>>();
            roles.Add(new RoleValidator<TRole>());
            return new Mock<RoleManager<TRole>>(store, roles, null, null, null);
        }

        public static Mock<ILogger<T>> MockILogger<T>(StringBuilder logStore = null) where T : class
        {
            logStore = logStore ?? LogMessage;
            var logger = new Mock<ILogger<T>>();
            logger.Setup(x => x.Write(It.IsAny<LogLevel>(), It.IsAny<int>(), It.IsAny<object>(),
                It.IsAny<Exception>(), It.IsAny<Func<object, Exception, string>>()))
                .Callback((LogLevel logLevel, int eventId, object state, Exception exception, Func<object, Exception, string> formatter) =>
                { logStore.Append(state.ToString()); });
            logger.Setup(x => x.IsEnabled(LogLevel.Information)).Returns(true);
            logger.Setup(x => x.IsEnabled(LogLevel.Warning)).Returns(true);

            return logger;
        }

        public static UserManager<TUser> TestUserManager<TUser>(IUserStore<TUser> store = null) where TUser : class
        {
            store = store ?? new Mock<IUserStore<TUser>>().Object;
            var validator = new Mock<IUserValidator<TUser>>();
            var userManager = new UserManager<TUser>(store);
            userManager.UserValidators.Add(validator.Object);
            userManager.PasswordValidators.Add(new PasswordValidator<TUser>());
            validator.Setup(v => v.ValidateAsync(userManager, It.IsAny<TUser>(), CancellationToken.None))
                .Returns(Task.FromResult(IdentityResult.Success)).Verifiable();
            return userManager;
        }

        public static RoleManager<TRole> TestRoleManager<TRole>(IRoleStore<TRole> store = null) where TRole : class
        {
            store = store ?? new Mock<IRoleStore<TRole>>().Object;
            var roles = new List<IRoleValidator<TRole>>();
            roles.Add(new RoleValidator<TRole>());
            return new RoleManager<TRole>(store, roles);
        }

    }
}