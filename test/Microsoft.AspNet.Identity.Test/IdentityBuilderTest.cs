using System.Security.Claims;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.DependencyInjection.Fallback;
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
            services.AddIdentity<IdentityUser>(b => b.UseUserValidator(() => validator));
            Assert.Equal(validator, services.BuildServiceProvider().GetService<IUserValidator<IdentityUser>>());
        }

        [Fact]
        public void CanSpecifyPasswordValidatorInstance()
        {
            var services = new ServiceCollection();
            var validator = new PasswordValidator();
            services.AddIdentity<IdentityUser>(b => b.UsePasswordValidator(() => validator));
            Assert.Equal(validator, services.BuildServiceProvider().GetService<IPasswordValidator>());
        }

        [Fact]
        public void CanSpecifyLockoutPolicyInstance()
        {
            var services = new ServiceCollection();
            var policy = new LockoutPolicy();
            services.AddIdentity<IdentityUser>(b => b.UseLockoutPolicy(() => policy));
            Assert.Equal(policy, services.BuildServiceProvider().GetService<LockoutPolicy>());
        }

        [Fact]
        public void CanSpecifyPasswordHasherInstance()
        {
            CanOverride<IPasswordHasher, PasswordHasher>();
        }

        [Fact]
        public void CanSpecifyClaimsIdentityFactoryInstance()
        {
            CanOverride<IClaimsIdentityFactory<IdentityUser>, ClaimsIdentityFactory<IdentityUser>>();
        }

        [Fact]
        public void EnsureDefaultServices()
        {
            var services = new ServiceCollection();
            var builder = new IdentityBuilder<IdentityUser, IdentityRole>(services);
            builder.UseIdentity();

            var provider = services.BuildServiceProvider();
            var userValidator = provider.GetService<IUserValidator<IdentityUser>>() as UserValidator<IdentityUser>;
            Assert.NotNull(userValidator);
            Assert.True(userValidator.AllowOnlyAlphanumericUserNames);
            Assert.False(userValidator.RequireUniqueEmail);

            var pwdValidator = provider.GetService<IPasswordValidator>() as PasswordValidator;
            Assert.NotNull(userValidator);
            Assert.True(pwdValidator.RequireDigit);
            Assert.True(pwdValidator.RequireLowercase);
            Assert.True(pwdValidator.RequireNonLetterOrDigit);
            Assert.True(pwdValidator.RequireUppercase);
            Assert.Equal(6, pwdValidator.RequiredLength);

            var hasher = provider.GetService<IPasswordHasher>() as PasswordHasher;
            Assert.NotNull(hasher);

            var claimsFactory = provider.GetService<IClaimsIdentityFactory<IdentityUser>>() as ClaimsIdentityFactory<IdentityUser>;
            Assert.NotNull(claimsFactory);
            Assert.Equal(ClaimTypes.Role, claimsFactory.RoleClaimType);
            Assert.Equal(ClaimsIdentityFactory<IdentityUser>.DefaultSecurityStampClaimType, claimsFactory.SecurityStampClaimType);
            Assert.Equal(ClaimTypes.Name, claimsFactory.UserNameClaimType);
            Assert.Equal(ClaimTypes.NameIdentifier, claimsFactory.UserIdClaimType);
        }

        private static void CanOverride<TService, TImplementation>() where TImplementation : TService,new()
        {
            var services = new ServiceCollection();
            var instance = new TImplementation();
            services.AddIdentity<IdentityUser>(b => b.Use<TService>(() => instance));
            Assert.Equal(instance, services.BuildServiceProvider().GetService<TService>());
        }

    }
}