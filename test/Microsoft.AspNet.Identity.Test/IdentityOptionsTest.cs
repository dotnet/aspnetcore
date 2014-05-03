using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNet.ConfigurationModel;
using Microsoft.AspNet.ConfigurationModel.Sources;
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
        public void CopyNullIsNoop()
        {
            var options = new IdentityOptions();
            options.Copy(null);
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
                {"identity:password:RequireLowerCase", "false"}
            };
            var config = new ConfigurationModel.Configuration { new MemoryConfigurationSource(dic) };
            Assert.Equal(roleClaimType, config.Get("identity:claimtype:role"));
            var options = new IdentityOptions(config);
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
        }

        [Fact]
        public void ClaimTypeOptionsFromConfig()
        {
            const string roleClaimType = "rolez";
            const string usernameClaimType = "namez";
            const string useridClaimType = "idz";
            const string securityStampClaimType = "stampz";

            var dic = new Dictionary<string, string>
            { 
                {"role", roleClaimType},
                {"username", usernameClaimType},
                {"userid", useridClaimType},
                {"securitystamp", securityStampClaimType}
            };
            var config = new ConfigurationModel.Configuration {new MemoryConfigurationSource(dic)};
            Assert.Equal(roleClaimType, config.Get("role"));
            var options = new ClaimTypeOptions(config);
            Assert.Equal(roleClaimType, options.Role);
            Assert.Equal(useridClaimType, options.UserId);
            Assert.Equal(usernameClaimType, options.UserName);
            Assert.Equal(securityStampClaimType, options.SecurityStamp);
        }

        [Fact]
        public void PasswordOptionsFromConfig()
        {
            var dic = new Dictionary<string, string>
            { 
                {"RequiredLength", "10"},
                {"RequireNonLetterOrDigit", "false"},
                {"RequireUpperCase", "false"},
                {"RequireDigit", "false"},
                {"RequireLowerCase", "false"}
            };
            var config = new ConfigurationModel.Configuration { new MemoryConfigurationSource(dic) };
            var options = new PasswordOptions(config);
            Assert.False(options.RequireDigit);
            Assert.False(options.RequireLowercase);
            Assert.False(options.RequireNonLetterOrDigit);
            Assert.False(options.RequireUppercase);
            Assert.Equal(10, options.RequiredLength);
        }

    }
}