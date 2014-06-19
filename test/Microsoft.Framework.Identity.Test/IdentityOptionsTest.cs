// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
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
            Assert.False(options.Lockout.EnabledByDefault);
            Assert.Equal(TimeSpan.FromMinutes(5), options.Lockout.DefaultLockoutTimeSpan);
            Assert.Equal(5, options.Lockout.MaxFailedAccessAttempts);

            Assert.True(options.Password.RequireDigit);
            Assert.True(options.Password.RequireLowercase);
            Assert.True(options.Password.RequireNonLetterOrDigit);
            Assert.True(options.Password.RequireUppercase);
            Assert.Equal(6, options.Password.RequiredLength);

            Assert.True(options.User.AllowOnlyAlphanumericNames);
            Assert.False(options.User.RequireUniqueEmail);

            Assert.Equal(ClaimTypes.Role, options.ClaimType.Role);
            Assert.Equal(ClaimTypes.Name, options.ClaimType.UserName);
            Assert.Equal(ClaimTypes.NameIdentifier, options.ClaimType.UserId);
            Assert.Equal(ClaimTypeOptions.DefaultSecurityStampClaimType, options.ClaimType.SecurityStamp);
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
                {"identity:claimtype:role", roleClaimType},
                {"identity:claimtype:username", usernameClaimType},
                {"identity:claimtype:userid", useridClaimType},
                {"identity:claimtype:securitystamp", securityStampClaimType},
                {"identity:user:requireUniqueEmail", "true"},
                {"identity:password:RequiredLength", "10"},
                {"identity:password:RequireNonLetterOrDigit", "false"},
                {"identity:password:RequireUpperCase", "false"},
                {"identity:password:RequireDigit", "false"},
                {"identity:password:RequireLowerCase", "false"},
                {"identity:lockout:EnabledByDefault", "TRUe"},
                {"identity:lockout:MaxFailedAccessAttempts", "1000"}
            };
            var config = new Configuration { new MemoryConfigurationSource(dic) };
            Assert.Equal(roleClaimType, config.Get("identity:claimtype:role"));

            var services = new ServiceCollection {OptionsServices.GetDefaultServices()};
            services.AddIdentity(config.GetSubKey("identity"));
            var accessor = services.BuildServiceProvider().GetService<IOptionsAccessor<IdentityOptions>>();
            Assert.NotNull(accessor);
            var options = accessor.Options;
            Assert.Equal(roleClaimType, options.ClaimType.Role);
            Assert.Equal(useridClaimType, options.ClaimType.UserId);
            Assert.Equal(usernameClaimType, options.ClaimType.UserName);
            Assert.Equal(securityStampClaimType, options.ClaimType.SecurityStamp);
            Assert.True(options.User.RequireUniqueEmail);
            Assert.True(options.User.AllowOnlyAlphanumericNames);
            Assert.True(options.User.AllowOnlyAlphanumericNames);
            Assert.False(options.Password.RequireDigit);
            Assert.False(options.Password.RequireLowercase);
            Assert.False(options.Password.RequireNonLetterOrDigit);
            Assert.False(options.Password.RequireUppercase);
            Assert.Equal(10, options.Password.RequiredLength);
            Assert.True(options.Lockout.EnabledByDefault);
            Assert.Equal(1000, options.Lockout.MaxFailedAccessAttempts);
        }

        public class PasswordsNegativeLengthSetup : IOptionsSetup<IdentityOptions>
        {
            public int Order { get { return 0; } }
            public void Setup(IdentityOptions options)
            {
                options.Password.RequiredLength = -1;
            }
        }

        [Fact]
        public void CanCustomizeIdentityOptions()
        {
            var builder = new Builder.Builder(new ServiceCollection().BuildServiceProvider());
            builder.UseServices(services =>
            {
                services.AddIdentity<IdentityUser>();
                services.AddSetup<PasswordsNegativeLengthSetup>();
            });

            var setup = builder.ApplicationServices.GetService<IOptionsSetup<IdentityOptions>>();
            Assert.IsType(typeof(PasswordsNegativeLengthSetup), setup);
            var optionsGetter = builder.ApplicationServices.GetService<IOptionsAccessor<IdentityOptions>>();
            Assert.NotNull(optionsGetter);
            setup.Setup(optionsGetter.Options);

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
            var app = new Builder.Builder(new ServiceCollection().BuildServiceProvider());
            app.UseServices(services =>
            {
                services.AddIdentity<IdentityUser>(identityServices => identityServices.SetupOptions(options => options.User.RequireUniqueEmail = true));
            });

            var optionsGetter = app.ApplicationServices.GetService<IOptionsAccessor<IdentityOptions>>();
            Assert.NotNull(optionsGetter);

            var myOptions = optionsGetter.Options;
            Assert.True(myOptions.User.RequireUniqueEmail);
        }

    }
}