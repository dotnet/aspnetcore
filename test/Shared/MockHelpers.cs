// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Framework.Logging;
using Moq;
using System.Text;

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
            return new Mock<RoleManager<TRole>>(store, roles, null, null,null);
        }

        public static Mock<ILogger> MockILogger(StringBuilder logStore = null)
        {
            logStore = logStore ?? LogMessage;
            var logger = new Mock<ILogger>();
            logger.Setup(x => x.Write(It.IsAny<LogLevel>(), It.IsAny<int>(), It.IsAny<object>(),
                It.IsAny<Exception>(), It.IsAny<Func<object, Exception, string>>()))
                .Callback((LogLevel logLevel, int eventId, object state, Exception exception, Func<object, Exception, string> formatter) =>
                            { logStore.Append(state.ToString()); });
            logger.Setup(x => x.IsEnabled(LogLevel.Information)).Returns(true);
            logger.Setup(x => x.IsEnabled(LogLevel.Warning)).Returns(true);

            return logger;
        }

        public static Mock<ILoggerFactory> MockILoggerFactory(ILogger logger = null)
        {
            logger = logger ?? MockILogger().Object;
            var loggerFactory = new Mock<ILoggerFactory>();
            loggerFactory.Setup(x => x.Create(It.IsAny<string>())).Returns(logger);
            return loggerFactory;
        }

        public static UserManager<TUser> UserManagerWithMockLogger<TUser>(ILoggerFactory loggerFactory = null) where TUser : class
        {
            var userstore = new Mock<IUserStore<TUser>>().Object;
            var userManager = new UserManager<TUser>(userstore, loggerFactory: loggerFactory ?? MockILoggerFactory().Object);

            return userManager;
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