// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Identity.Test;

public static class MockHelpers
{
    public static StringBuilder LogMessage = new StringBuilder();

    public static Mock<UserManager<TUser>> MockUserManager<TUser>(
        IMeterFactory meterFactory = null,
        IPasskeyHandler<TUser> passkeyHandler = null)
        where TUser : class
    {
        var services = new ServiceCollection();
        if (meterFactory != null)
        {
            services.AddSingleton<SignInManagerMetrics>();
            services.AddSingleton(meterFactory);
        }
        if (passkeyHandler != null)
        {
            services.AddSingleton(passkeyHandler);
        }

        var store = new Mock<IUserStore<TUser>>();
        var mgr = new Mock<UserManager<TUser>>(store.Object, null, null, null, null, null, null, services.BuildServiceProvider(), null);
        mgr.Object.UserValidators.Add(new UserValidator<TUser>());
        mgr.Object.PasswordValidators.Add(new PasswordValidator<TUser>());
        return mgr;
    }

    public static Mock<RoleManager<TRole>> MockRoleManager<TRole>(IRoleStore<TRole> store = null) where TRole : class
    {
        store = store ?? new Mock<IRoleStore<TRole>>().Object;
        var roles = new List<IRoleValidator<TRole>>();
        roles.Add(new RoleValidator<TRole>());
        return new Mock<RoleManager<TRole>>(store, roles, MockLookupNormalizer(),
            new IdentityErrorDescriber(), null);
    }

    public static UserManager<TUser> TestUserManager<TUser>(IUserStore<TUser> store = null, IMeterFactory meterFactory = null) where TUser : class
    {
        store = store ?? new Mock<IUserStore<TUser>>().Object;
        var options = new Mock<IOptions<IdentityOptions>>();
        var idOptions = new IdentityOptions();
        idOptions.Lockout.AllowedForNewUsers = false;
        options.Setup(o => o.Value).Returns(idOptions);
        var userValidators = new List<IUserValidator<TUser>>();
        var validator = new Mock<IUserValidator<TUser>>();
        userValidators.Add(validator.Object);
        var pwdValidators = new List<PasswordValidator<TUser>>();
        pwdValidators.Add(new PasswordValidator<TUser>());
        var services = new ServiceCollection();
        if (meterFactory != null)
        {
            services.AddSingleton<UserManagerMetrics>();
            services.AddSingleton<SignInManagerMetrics>();
            services.AddSingleton(meterFactory);
        }
        var userManager = new UserManager<TUser>(store, options.Object, new PasswordHasher<TUser>(),
            userValidators, pwdValidators, MockLookupNormalizer(),
            new IdentityErrorDescriber(), services.BuildServiceProvider(),
            new Mock<ILogger<UserManager<TUser>>>().Object);
        validator.Setup(v => v.ValidateAsync(userManager, It.IsAny<TUser>()))
            .Returns(Task.FromResult(IdentityResult.Success)).Verifiable();
        return userManager;
    }

    public static RoleManager<TRole> TestRoleManager<TRole>(IRoleStore<TRole> store = null) where TRole : class
    {
        store = store ?? new Mock<IRoleStore<TRole>>().Object;
        var roles = new List<IRoleValidator<TRole>>();
        roles.Add(new RoleValidator<TRole>());
        return new RoleManager<TRole>(store, roles,
            MockLookupNormalizer(),
            new IdentityErrorDescriber(),
            null);
    }

    public static ILookupNormalizer MockLookupNormalizer()
    {
        var normalizerFunc = new Func<string, string>(i =>
        {
            if (i == null)
            {
                return null;
            }
            else
            {
                return Convert.ToBase64String(Encoding.UTF8.GetBytes(i)).ToUpperInvariant();
            }
        });
        var lookupNormalizer = new Mock<ILookupNormalizer>();
        lookupNormalizer.Setup(i => i.NormalizeName(It.IsAny<string>())).Returns(normalizerFunc);
        lookupNormalizer.Setup(i => i.NormalizeEmail(It.IsAny<string>())).Returns(normalizerFunc);
        return lookupNormalizer.Object;
    }
}
