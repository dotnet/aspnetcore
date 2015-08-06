// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.Framework.Configuration;
using Microsoft.Framework.Configuration.Memory;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;
using Xunit;

namespace Microsoft.AspNet.Identity.Test
{
    public class IdentityOptionsTest
    {
        [Fact]
        public void VerifyDefaultOptions()
        {
            var options = new IdentityOptions();
            Assert.True(options.Lockout.AllowedForNewUsers);
            Assert.Equal(TimeSpan.FromMinutes(5), options.Lockout.DefaultLockoutTimeSpan);
            Assert.Equal(5, options.Lockout.MaxFailedAccessAttempts);

            Assert.True(options.Password.RequireDigit);
            Assert.True(options.Password.RequireLowercase);
            Assert.True(options.Password.RequireNonLetterOrDigit);
            Assert.True(options.Password.RequireUppercase);
            Assert.Equal(6, options.Password.RequiredLength);

            Assert.Equal("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@", options.User.AllowedUserNameCharacters);
            Assert.False(options.User.RequireUniqueEmail);

            Assert.Equal(ClaimTypes.Role, options.ClaimsIdentity.RoleClaimType);
            Assert.Equal(ClaimTypes.Name, options.ClaimsIdentity.UserNameClaimType);
            Assert.Equal(ClaimTypes.NameIdentifier, options.ClaimsIdentity.UserIdClaimType);
            Assert.Equal("AspNet.Identity.SecurityStamp", options.ClaimsIdentity.SecurityStampClaimType);
        }

        [Fact]
        public void IdentityOptionsFromConfig()
        {
            const string roleClaimType = "rolez";
            const string usernameClaimType = "namez";
            const string useridClaimType = "idz";
            const string securityStampClaimType = "stampz";

            var dic = new Dictionary<string, string>
            {
                {"identity:claimsidentity:roleclaimtype", roleClaimType},
                {"identity:claimsidentity:usernameclaimtype", usernameClaimType},
                {"identity:claimsidentity:useridclaimtype", useridClaimType},
                {"identity:claimsidentity:securitystampclaimtype", securityStampClaimType},
                {"identity:user:requireUniqueEmail", "true"},
                {"identity:password:RequiredLength", "10"},
                {"identity:password:RequireNonLetterOrDigit", "false"},
                {"identity:password:RequireUpperCase", "false"},
                {"identity:password:RequireDigit", "false"},
                {"identity:password:RequireLowerCase", "false"},
                {"identity:lockout:AllowedForNewUsers", "FALSe"},
                {"identity:lockout:MaxFailedAccessAttempts", "1000"}
            };
            var builder = new ConfigurationBuilder(new MemoryConfigurationSource(dic));
            var config = builder.Build();
            Assert.Equal(roleClaimType, config.Get("identity:claimsidentity:roleclaimtype"));

            var services = new ServiceCollection();
            services.AddIdentity<TestUser,TestRole>();
            services.ConfigureIdentity(config.GetConfigurationSection("identity"));
            var accessor = services.BuildServiceProvider().GetRequiredService<IOptions<IdentityOptions>>();
            Assert.NotNull(accessor);
            var options = accessor.Options;
            Assert.Equal(roleClaimType, options.ClaimsIdentity.RoleClaimType);
            Assert.Equal(useridClaimType, options.ClaimsIdentity.UserIdClaimType);
            Assert.Equal(usernameClaimType, options.ClaimsIdentity.UserNameClaimType);
            Assert.Equal(securityStampClaimType, options.ClaimsIdentity.SecurityStampClaimType);
            Assert.True(options.User.RequireUniqueEmail);
            Assert.Equal("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@", options.User.AllowedUserNameCharacters);
            Assert.False(options.Password.RequireDigit);
            Assert.False(options.Password.RequireLowercase);
            Assert.False(options.Password.RequireNonLetterOrDigit);
            Assert.False(options.Password.RequireUppercase);
            Assert.Equal(10, options.Password.RequiredLength);
            Assert.False(options.Lockout.AllowedForNewUsers);
            Assert.Equal(1000, options.Lockout.MaxFailedAccessAttempts);
        }

        [Fact]
        public void IdentityOptionsActionOverridesConfig()
        {
            var dic = new Dictionary<string, string>
            {
                {"identity:user:requireUniqueEmail", "true"},
                {"identity:lockout:MaxFailedAccessAttempts", "1000"}
            };
            var builder = new ConfigurationBuilder(new MemoryConfigurationSource(dic));
            var config = builder.Build();
            var services = new ServiceCollection();
            services.ConfigureIdentity(config.GetConfigurationSection("identity"));
            services.AddIdentity<TestUser, TestRole>(o => { o.User.RequireUniqueEmail = false; o.Lockout.MaxFailedAccessAttempts++; });
            var accessor = services.BuildServiceProvider().GetRequiredService<IOptions<IdentityOptions>>();
            Assert.NotNull(accessor);
            var options = accessor.Options;
            Assert.False(options.User.RequireUniqueEmail);
            Assert.Equal(1001, options.Lockout.MaxFailedAccessAttempts);
        }

        public class PasswordsNegativeLengthSetup : ConfigureOptions<IdentityOptions>
        {
            public PasswordsNegativeLengthSetup() 
                : base(options => options.Password.RequiredLength = -1)
            { }
        }

        [Fact]
        public void CanCustomizeIdentityOptions()
        {
            var services = new ServiceCollection()
                .ConfigureOptions<PasswordsNegativeLengthSetup>();
            services.AddIdentity<TestUser,TestRole>();
            var serviceProvider = services.BuildServiceProvider();

            var setup = serviceProvider.GetRequiredService<IConfigureOptions<IdentityOptions>>();
            Assert.IsType(typeof(PasswordsNegativeLengthSetup), setup);
            var optionsGetter = serviceProvider.GetRequiredService<IOptions<IdentityOptions>>();
            Assert.NotNull(optionsGetter);
            var myOptions = optionsGetter.Options;
            Assert.True(myOptions.Password.RequireLowercase);
            Assert.True(myOptions.Password.RequireDigit);
            Assert.True(myOptions.Password.RequireNonLetterOrDigit);
            Assert.True(myOptions.Password.RequireUppercase);
            Assert.Equal(-1, myOptions.Password.RequiredLength);
        }

        [Fact]
        public void CanSetupIdentityOptions()
        {
            var services = new ServiceCollection()
                .AddOptions()
                .ConfigureIdentity(options => options.User.RequireUniqueEmail = true);
            services.AddIdentity<TestUser,TestRole>();
            var serviceProvider = services.BuildServiceProvider();

            var optionsGetter = serviceProvider.GetRequiredService<IOptions<IdentityOptions>>();
            Assert.NotNull(optionsGetter);

            var myOptions = optionsGetter.Options;
            Assert.True(myOptions.User.RequireUniqueEmail);
        }
    }
}