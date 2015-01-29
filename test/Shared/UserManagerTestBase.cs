// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Testing;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.Logging;
using Xunit;

namespace Microsoft.AspNet.Identity.Test
{
    // Common functionality tests that all verifies user manager functionality regardless of store implementation
    public abstract class UserManagerTestBase<TUser, TRole> : UserManagerTestBase<TUser, TRole, string>
        where TUser : IdentityUser, new()
        where TRole : IdentityRole, new()
    { }

    public abstract class UserManagerTestBase<TUser, TRole, TKey>
        where TUser : IdentityUser<TKey>, new()
        where TRole : IdentityRole<TKey>, new()
        where TKey : IEquatable<TKey>
    {
        protected TestLoggerFactory loggerFactory;

        public UserManagerTestBase()
        {
            loggerFactory = new TestLoggerFactory();
        }

        protected virtual void SetupIdentityServices(IServiceCollection services, object context = null)
        {
            services.AddHosting();
            services.AddIdentity<TUser, TRole>().AddDefaultTokenProviders();
            AddUserStore(services, context);
            AddRoleStore(services, context);
            services.AddInstance<ILoggerFactory>(loggerFactory);
            services.ConfigureIdentity(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonLetterOrDigit = false;
                options.Password.RequireUppercase = false;
                options.User.UserNameValidationRegex = null;
            });
        }

        protected virtual UserManager<TUser> CreateManager(object context = null, IServiceCollection services = null)
        {
            if (services == null)
            {
                services = new ServiceCollection();
            }
            if (context == null)
            {
                context = CreateTestContext();
            }
            SetupIdentityServices(services, context);
            return services.BuildServiceProvider().GetService<UserManager<TUser>>();
        }

        protected RoleManager<TRole> CreateRoleManager(object context = null, IServiceCollection services = null)
        {
            if (services == null)
            {
                services = new ServiceCollection();
            }
            if (context == null)
            {
                context = CreateTestContext();
            }
            SetupIdentityServices(services, context);
            return services.BuildServiceProvider().GetService<RoleManager<TRole>>();
        }

        protected abstract object CreateTestContext();

        protected abstract void AddUserStore(IServiceCollection services, object context = null);
        protected abstract void AddRoleStore(IServiceCollection services, object context = null);

        protected TUser CreateTestUser(string namePrefix = "")
        {
            return new TUser() { UserName = namePrefix + Guid.NewGuid().ToString() };
        }

        protected TRole CreateRole(string namePrefix = "")
        {
            return new TRole() { Name = namePrefix + Guid.NewGuid().ToString() };
        }

        [Fact]
        public async Task CanDeleteUser()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.VerifyUserManagerSuccessLog(manager.Logger, "CreateAsync", user.Id.ToString());

            IdentityResultAssert.IsSuccess(await manager.DeleteAsync(user));
            IdentityResultAssert.VerifyUserManagerSuccessLog(manager.Logger, "DeleteAsync", user.Id.ToString());

            Assert.Null(await manager.FindByIdAsync(user.Id.ToString()));
        }

        [Fact]
        public async Task CanUpdateUserName()
        {
            var manager = CreateManager();
            var name = Guid.NewGuid().ToString();
            var user = new TUser() { UserName = name };
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var newName = Guid.NewGuid().ToString();
            Assert.Null(await manager.FindByNameAsync(newName));
            user.UserName = newName;
            IdentityResultAssert.IsSuccess(await manager.UpdateAsync(user));
            IdentityResultAssert.VerifyUserManagerSuccessLog(manager.Logger, "UpdateAsync", user.Id.ToString());
            Assert.NotNull(await manager.FindByNameAsync(newName));
            Assert.Null(await manager.FindByNameAsync(name));
        }

        [Fact]
        public async Task CanSetUserName()
        {
            var manager = CreateManager();
            var user = new TUser() { UserName = "UpdateAsync" };
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            Assert.Null(await manager.FindByNameAsync("New"));
            IdentityResultAssert.IsSuccess(await manager.SetUserNameAsync(user, "New"));
            Assert.NotNull(await manager.FindByNameAsync("New"));
            Assert.Null(await manager.FindByNameAsync("UpdateAsync"));
        }

        [Fact]
        public async Task CanUpdatePasswordUsingHasher()
        {
            var manager = CreateManager();
            var user = new TUser() { UserName = "UpdatePassword" };
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, "password"));
            Assert.True(await manager.CheckPasswordAsync(user, "password"));
            string expectedLog = string.Format("{0} for user: {1} : {2}", "CheckPasswordAsync", user.Id.ToString(), true.ToString());
            IdentityResultAssert.VerifyLogMessage(manager.Logger, expectedLog);

            user.PasswordHash = manager.PasswordHasher.HashPassword(user, "New");
            IdentityResultAssert.IsSuccess(await manager.UpdateAsync(user));
            Assert.False(await manager.CheckPasswordAsync(user, "password"));
            expectedLog = string.Format("{0} for user: {1} : {2}", "CheckPasswordAsync", user.Id.ToString(), false.ToString());
            IdentityResultAssert.VerifyLogMessage(manager.Logger, expectedLog);
            Assert.True(await manager.CheckPasswordAsync(user, "New"));
        }

        [Fact]
        public async Task CanFindById()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            Assert.NotNull(await manager.FindByIdAsync(user.Id.ToString()));
        }

        [Fact]
        public async Task UserValidatorCanBlockCreate()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            manager.UserValidators.Clear();
            manager.UserValidators.Add(new AlwaysBadValidator());
            IdentityResultAssert.IsFailure(await manager.CreateAsync(user), AlwaysBadValidator.ErrorMessage);
        }

        [Fact]
        public async Task UserValidatorCanBlockUpdate()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            manager.UserValidators.Clear();
            manager.UserValidators.Add(new AlwaysBadValidator());
            IdentityResultAssert.IsFailure(await manager.UpdateAsync(user), AlwaysBadValidator.ErrorMessage);
            IdentityResultAssert.VerifyUserManagerFailureLog(manager.Logger, "UpdateAsync", user.Id.ToString(), AlwaysBadValidator.ErrorMessage);
        }

        [Fact]
        public async Task CanChainUserValidators()
        {
            var manager = CreateManager();
            manager.UserValidators.Clear();
            manager.UserValidators.Add(new AlwaysBadValidator());
            manager.UserValidators.Add(new AlwaysBadValidator());
            var result = await manager.CreateAsync(CreateTestUser());
            IdentityResultAssert.IsFailure(result, AlwaysBadValidator.ErrorMessage);
            Assert.Equal(2, result.Errors.Count());
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task UserValidatorBlocksShortEmailsWhenRequiresUniqueEmail(string email)
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            manager.Options.User.RequireUniqueEmail = true;
            IdentityResultAssert.IsFailure(await manager.CreateAsync(user), IdentityErrorDescriber.Default.InvalidEmail(email));
        }

#if ASPNET50
        [Theory]
        [InlineData("@@afd")]
        [InlineData("bogus")]
        public async Task UserValidatorBlocksInvalidEmailsWhenRequiresUniqueEmail(string email)
        {
            var manager = CreateManager();
            var user = new TUser() { UserName = "UpdateBlocked", Email = email };
            manager.Options.User.RequireUniqueEmail = true;
            IdentityResultAssert.IsFailure(await manager.CreateAsync(user), IdentityErrorDescriber.Default.InvalidEmail(email));
        }
#endif

        [Fact]
        public async Task PasswordValidatorCanBlockAddPassword()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            manager.PasswordValidators.Clear();
            manager.PasswordValidators.Add(new AlwaysBadValidator());
            IdentityResultAssert.IsFailure(await manager.AddPasswordAsync(user, "password"),
                AlwaysBadValidator.ErrorMessage);
            IdentityResultAssert.VerifyUserManagerFailureLog(manager.Logger, "AddPasswordAsync", user.Id.ToString(), AlwaysBadValidator.ErrorMessage);
        }

        [Fact]
        public async Task CanChainPasswordValidators()
        {
            var manager = CreateManager();
            manager.PasswordValidators.Clear();
            manager.PasswordValidators.Add(new AlwaysBadValidator());
            manager.PasswordValidators.Add(new AlwaysBadValidator());
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var result = await manager.AddPasswordAsync(user, "pwd");
            IdentityResultAssert.IsFailure(result, AlwaysBadValidator.ErrorMessage);
            Assert.Equal(2, result.Errors.Count());
        }

        [Fact]
        public async Task PasswordValidatorCanBlockChangePassword()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, "password"));
            manager.PasswordValidators.Clear();
            manager.PasswordValidators.Add(new AlwaysBadValidator());
            IdentityResultAssert.IsFailure(await manager.ChangePasswordAsync(user, "password", "new"),
                AlwaysBadValidator.ErrorMessage);
            IdentityResultAssert.VerifyUserManagerFailureLog(manager.Logger, "ChangePasswordAsync", user.Id.ToString(), AlwaysBadValidator.ErrorMessage);
        }

        [Fact]
        public async Task PasswordValidatorCanBlockCreateUser()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            manager.PasswordValidators.Clear();
            manager.PasswordValidators.Add(new AlwaysBadValidator());
            IdentityResultAssert.IsFailure(await manager.CreateAsync(user, "password"), AlwaysBadValidator.ErrorMessage);
        }

        [Fact]
        public async Task CanCreateUserNoPassword()
        {
            var manager = CreateManager();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(new TUser() { UserName = "CreateUserTest" }));
            var user = await manager.FindByNameAsync("CreateUserTest");
            Assert.NotNull(user);
            Assert.Null(user.PasswordHash);
            var logins = await manager.GetLoginsAsync(user);
            Assert.NotNull(logins);
            Assert.Equal(0, logins.Count());
        }

        [Fact]
        public async Task CanCreateUserAddLogin()
        {
            var manager = CreateManager();
            const string provider = "ZzAuth";
            const string display = "display";
            var user = CreateTestUser();
            var providerKey = user.Id.ToString();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            user = await manager.FindByNameAsync(user.UserName);
            IdentityResultAssert.IsSuccess(await manager.AddLoginAsync(user, new UserLoginInfo(provider, providerKey, display)));
            IdentityResultAssert.VerifyUserManagerSuccessLog(manager.Logger, "AddLoginAsync", user.Id.ToString());
            var logins = await manager.GetLoginsAsync(user);
            Assert.NotNull(logins);
            Assert.Equal(1, logins.Count());
            Assert.Equal(provider, logins.First().LoginProvider);
            Assert.Equal(providerKey, logins.First().ProviderKey);
            Assert.Equal(display, logins.First().ProviderDisplayName);
        }

        [Fact]
        public async Task CanCreateUserLoginAndAddPassword()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            var login = new UserLoginInfo("Provider", user.Id.ToString(), "display");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await manager.AddLoginAsync(user, login));
            Assert.False(await manager.HasPasswordAsync(user));
            IdentityResultAssert.IsSuccess(await manager.AddPasswordAsync(user, "password"));
            IdentityResultAssert.VerifyUserManagerSuccessLog(manager.Logger, "AddPasswordAsync", user.Id.ToString());
            Assert.True(await manager.HasPasswordAsync(user));
            var logins = await manager.GetLoginsAsync(user);
            Assert.NotNull(logins);
            Assert.Equal(1, logins.Count());
            Assert.Equal(user, await manager.FindByLoginAsync(login.LoginProvider, login.ProviderKey));
            Assert.True(await manager.CheckPasswordAsync(user, "password"));
        }

        [Fact]
        public async Task AddPasswordFailsIfAlreadyHave()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, "Password"));
            Assert.True(await manager.HasPasswordAsync(user));
            IdentityResultAssert.IsFailure(await manager.AddPasswordAsync(user, "password"),
                "User already has a password set.");
            IdentityResultAssert.VerifyUserManagerFailureLog(manager.Logger, "AddPasswordAsync", user.Id.ToString(), IdentityErrorDescriber.Default.UserAlreadyHasPassword());
        }

        [Fact]
        public async Task CanCreateUserAddRemoveLogin()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            var login = new UserLoginInfo("Provider", user.Id.ToString(), "display");
            var result = await manager.CreateAsync(user);
            Assert.NotNull(user);
            IdentityResultAssert.IsSuccess(result);
            IdentityResultAssert.IsSuccess(await manager.AddLoginAsync(user, login));
            Assert.Equal(user, await manager.FindByLoginAsync(login.LoginProvider, login.ProviderKey));
            var logins = await manager.GetLoginsAsync(user);
            Assert.NotNull(logins);
            Assert.Equal(1, logins.Count());
            Assert.Equal(login.LoginProvider, logins.Last().LoginProvider);
            Assert.Equal(login.ProviderKey, logins.Last().ProviderKey);
            Assert.Equal(login.ProviderDisplayName, logins.Last().ProviderDisplayName);
            var stamp = user.SecurityStamp;
            IdentityResultAssert.IsSuccess(await manager.RemoveLoginAsync(user, login.LoginProvider, login.ProviderKey));
            IdentityResultAssert.VerifyUserManagerSuccessLog(manager.Logger, "RemoveLoginAsync", user.Id.ToString());
            Assert.Null(await manager.FindByLoginAsync(login.LoginProvider, login.ProviderKey));
            logins = await manager.GetLoginsAsync(user);
            Assert.NotNull(logins);
            Assert.Equal(0, logins.Count());
            Assert.NotEqual(stamp, user.SecurityStamp);
        }

        [Fact]
        public async Task CanRemovePassword()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            const string password = "password";
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, password));
            var stamp = user.SecurityStamp;
            IdentityResultAssert.IsSuccess(await manager.RemovePasswordAsync(user));
            IdentityResultAssert.VerifyUserManagerSuccessLog(manager.Logger, "RemovePasswordAsync", user.Id.ToString());
            var u = await manager.FindByNameAsync(user.UserName);
            Assert.NotNull(u);
            Assert.Null(u.PasswordHash);
            Assert.NotEqual(stamp, user.SecurityStamp);
        }

        [Fact]
        public async Task CanChangePassword()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            const string password = "password";
            const string newPassword = "newpassword";
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, password));
            var stamp = user.SecurityStamp;
            Assert.NotNull(stamp);
            IdentityResultAssert.IsSuccess(await manager.ChangePasswordAsync(user, password, newPassword));
            Assert.False(await manager.CheckPasswordAsync(user, password));
            Assert.True(await manager.CheckPasswordAsync(user, newPassword));
            IdentityResultAssert.VerifyUserManagerSuccessLog(manager.Logger, "ChangePasswordAsync", user.Id.ToString());
            Assert.NotEqual(stamp, user.SecurityStamp);
        }

        [Fact]
        public async Task CanAddRemoveUserClaim()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            Claim[] claims = { new Claim("c", "v"), new Claim("c2", "v2"), new Claim("c2", "v3") };
            foreach (Claim c in claims)
            {
                IdentityResultAssert.IsSuccess(await manager.AddClaimAsync(user, c));
            }
            IdentityResultAssert.VerifyUserManagerSuccessLog(manager.Logger, "AddClaimsAsync", user.Id.ToString());
            var userClaims = await manager.GetClaimsAsync(user);
            Assert.Equal(3, userClaims.Count);
            IdentityResultAssert.IsSuccess(await manager.RemoveClaimAsync(user, claims[0]));
            IdentityResultAssert.VerifyUserManagerSuccessLog(manager.Logger, "RemoveClaimsAsync", user.Id.ToString());
            userClaims = await manager.GetClaimsAsync(user);
            Assert.Equal(2, userClaims.Count);
            IdentityResultAssert.IsSuccess(await manager.RemoveClaimAsync(user, claims[1]));
            userClaims = await manager.GetClaimsAsync(user);
            Assert.Equal(1, userClaims.Count);
            IdentityResultAssert.IsSuccess(await manager.RemoveClaimAsync(user, claims[2]));
            userClaims = await manager.GetClaimsAsync(user);
            Assert.Equal(0, userClaims.Count);
        }

        [Fact]
        public async Task RemoveClaimOnlyAffectsUser()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            var user2 = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user2));
            Claim[] claims = { new Claim("c", "v"), new Claim("c2", "v2"), new Claim("c2", "v3") };
            foreach (Claim c in claims)
            {
                IdentityResultAssert.IsSuccess(await manager.AddClaimAsync(user, c));
                IdentityResultAssert.IsSuccess(await manager.AddClaimAsync(user2, c));
            }
            var userClaims = await manager.GetClaimsAsync(user);
            Assert.Equal(3, userClaims.Count);
            IdentityResultAssert.IsSuccess(await manager.RemoveClaimAsync(user, claims[0]));
            userClaims = await manager.GetClaimsAsync(user);
            Assert.Equal(2, userClaims.Count);
            IdentityResultAssert.IsSuccess(await manager.RemoveClaimAsync(user, claims[1]));
            userClaims = await manager.GetClaimsAsync(user);
            Assert.Equal(1, userClaims.Count);
            IdentityResultAssert.IsSuccess(await manager.RemoveClaimAsync(user, claims[2]));
            userClaims = await manager.GetClaimsAsync(user);
            Assert.Equal(0, userClaims.Count);
            var userClaims2 = await manager.GetClaimsAsync(user2);
            Assert.Equal(3, userClaims2.Count);
        }

        [Fact]
        public async Task CanReplaceUserClaim()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await manager.AddClaimAsync(user, new Claim("c", "a")));
            var userClaims = await manager.GetClaimsAsync(user);
            Assert.Equal(1, userClaims.Count);
            Claim claim = new Claim("c", "b");
            Claim oldClaim = userClaims.FirstOrDefault();
            IdentityResultAssert.IsSuccess(await manager.ReplaceClaimAsync(user, oldClaim, claim));
            IdentityResultAssert.VerifyUserManagerSuccessLog(manager.Logger, "ReplaceClaimAsync", user.Id.ToString());
            var newUserClaims = await manager.GetClaimsAsync(user);
            Assert.Equal(1, newUserClaims.Count);
            Claim newClaim = newUserClaims.FirstOrDefault();
            Assert.Equal(claim.Type, newClaim.Type);
            Assert.Equal(claim.Value, newClaim.Value);
        }

        [Fact]
        public async Task ReplaceUserClaimOnlyAffectsUser()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            var user2 = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user2));
            IdentityResultAssert.IsSuccess(await manager.AddClaimAsync(user, new Claim("c", "a")));
            IdentityResultAssert.IsSuccess(await manager.AddClaimAsync(user2, new Claim("c", "a")));
            var userClaims = await manager.GetClaimsAsync(user);
            Assert.Equal(1, userClaims.Count);
            var userClaims2 = await manager.GetClaimsAsync(user);
            Assert.Equal(1, userClaims2.Count);
            Claim claim = new Claim("c", "b");
            Claim oldClaim = userClaims.FirstOrDefault();
            IdentityResultAssert.IsSuccess(await manager.ReplaceClaimAsync(user, oldClaim, claim));
            var newUserClaims = await manager.GetClaimsAsync(user);
            Assert.Equal(1, newUserClaims.Count);
            Claim newClaim = newUserClaims.FirstOrDefault();
            Assert.Equal(claim.Type, newClaim.Type);
            Assert.Equal(claim.Value, newClaim.Value);
            userClaims2 = await manager.GetClaimsAsync(user2);
            Assert.Equal(1, userClaims2.Count);
            Claim oldClaim2 = userClaims2.FirstOrDefault();
            Assert.Equal("c", oldClaim2.Type);
            Assert.Equal("a", oldClaim2.Value);
        }

        [Fact]
        public async Task ChangePasswordFallsIfPasswordWrong()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, "password"));
            var result = await manager.ChangePasswordAsync(user, "bogus", "newpassword");
            IdentityResultAssert.IsFailure(result, "Incorrect password.");
            IdentityResultAssert.VerifyUserManagerFailureLog(manager.Logger, "ChangePasswordAsync", user.Id.ToString(), IdentityErrorDescriber.Default.PasswordMismatch());
        }

        [Fact]
        public async Task AddDupeUserNameFails()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            var user2 = new TUser() { UserName = user.UserName };
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsFailure(await manager.CreateAsync(user2), IdentityErrorDescriber.Default.DuplicateUserName(user.UserName));
        }

        [Fact]
        public async Task AddDupeEmailAllowedByDefault()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            var user2 = CreateTestUser();
            user.Email = "yup@yup.com";
            user2.Email = "yup@yup.com";
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user2));
        }

        [Fact]
        public async Task AddDupeEmailFallsWhenUniqueEmailRequired()
        {
            var manager = CreateManager();
            manager.Options.User.RequireUniqueEmail = true;
            var user = CreateTestUser();
            var user2 = CreateTestUser();
            string email = user.UserName + "@yup.com";
            user.Email = email;
            user2.Email = email;
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsFailure(await manager.CreateAsync(user2), IdentityErrorDescriber.Default.DuplicateEmail(user.Email));
        }

        [Fact]
        public async Task UpdateSecurityStampActuallyChanges()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            Assert.Null(user.SecurityStamp);
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var stamp = user.SecurityStamp;
            Assert.NotNull(stamp);
            IdentityResultAssert.IsSuccess(await manager.UpdateSecurityStampAsync(user));
            IdentityResultAssert.VerifyUserManagerSuccessLog(manager.Logger, "UpdateSecurityStampAsync", user.Id.ToString());
            Assert.NotEqual(stamp, user.SecurityStamp);
        }

        [Fact]
        public async Task AddDupeLoginFails()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            var login = new UserLoginInfo("Provider", "key", "display");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await manager.AddLoginAsync(user, login));
            var result = await manager.AddLoginAsync(user, login);
            IdentityResultAssert.IsFailure(result, IdentityErrorDescriber.Default.LoginAlreadyAssociated());
            IdentityResultAssert.VerifyUserManagerFailureLog(manager.Logger, "AddLoginAsync", user.Id.ToString(), IdentityErrorDescriber.Default.LoginAlreadyAssociated());
        }

        // Email tests
        [Fact]
        public async Task CanFindByEmail()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            var email = user.UserName + "@test.com";
            user.Email = email;
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var fetch = await manager.FindByEmailAsync(email);
            Assert.Equal(user, fetch);
        }

        [Fact]
        public async Task CanFindUsersViaUserQuerable()
        {
            var mgr = CreateManager();
            var users = GenerateUsers("CanFindUsersViaUserQuerable", 3);
            foreach (var u in users)
            {
                IdentityResultAssert.IsSuccess(await mgr.CreateAsync(u));
            }
            var usersQ = mgr.Users.Where(u => u.UserName.StartsWith("CanFindUsersViaUserQuerable"));
            Assert.Null(mgr.Users.FirstOrDefault(u => u.UserName == "bogus"));
        }

        [Fact]
        public async Task ConfirmEmailFalseByDefaultTest()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            Assert.False(await manager.IsEmailConfirmedAsync(user));
        }

        private class StaticTokenProvider : IUserTokenProvider<TUser>
        {
            public string Name { get; } = "Static";

            public Task<string> GenerateAsync(string purpose, UserManager<TUser> manager,
                TUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(MakeToken(purpose, user));
            }

            public Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser> manager,
                TUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(token == MakeToken(purpose, user));
            }

            public Task NotifyAsync(string token, UserManager<TUser> manager, TUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> manager, TUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(true);
            }

            private static string MakeToken(string purpose, TUser user)
            {
                return string.Join(":", user.Id, purpose, "ImmaToken");
            }
        }

        [Fact]
        public async Task CanResetPasswordWithStaticTokenProvider()
        {
            var manager = CreateManager();
            manager.RegisterTokenProvider(new StaticTokenProvider());
            manager.Options.PasswordResetTokenProvider = "Static";
            var user = CreateTestUser();
            const string password = "password";
            const string newPassword = "newpassword";
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, password));
            var stamp = user.SecurityStamp;
            Assert.NotNull(stamp);
            var token = await manager.GeneratePasswordResetTokenAsync(user);
            Assert.NotNull(token);
            IdentityResultAssert.VerifyUserManagerSuccessLog(manager.Logger, "GeneratePasswordResetTokenAsync", user.Id.ToString());
            IdentityResultAssert.IsSuccess(await manager.ResetPasswordAsync(user, token, newPassword));
            IdentityResultAssert.VerifyUserManagerSuccessLog(manager.Logger, "ResetPasswordAsync", user.Id.ToString());
            Assert.False(await manager.CheckPasswordAsync(user, password));
            Assert.True(await manager.CheckPasswordAsync(user, newPassword));
            Assert.NotEqual(stamp, user.SecurityStamp);
        }

        [Fact]
        public async Task PasswordValidatorCanBlockResetPasswordWithStaticTokenProvider()
        {
            var manager = CreateManager();
            manager.RegisterTokenProvider(new StaticTokenProvider());
            manager.Options.PasswordResetTokenProvider = "Static";
            var user = CreateTestUser();
            const string password = "password";
            const string newPassword = "newpassword";
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, password));
            var stamp = user.SecurityStamp;
            Assert.NotNull(stamp);
            var token = await manager.GeneratePasswordResetTokenAsync(user);
            Assert.NotNull(token);
            manager.PasswordValidators.Add(new AlwaysBadValidator());
            IdentityResultAssert.IsFailure(await manager.ResetPasswordAsync(user, token, newPassword),
                AlwaysBadValidator.ErrorMessage);
            IdentityResultAssert.VerifyUserManagerFailureLog(manager.Logger, "ResetPasswordAsync", user.Id.ToString(), AlwaysBadValidator.ErrorMessage);
            Assert.True(await manager.CheckPasswordAsync(user, password));
            Assert.Equal(stamp, user.SecurityStamp);
        }

        [Fact]
        public async Task ResetPasswordWithStaticTokenProviderFailsWithWrongToken()
        {
            var manager = CreateManager();
            manager.RegisterTokenProvider(new StaticTokenProvider());
            manager.Options.PasswordResetTokenProvider = "Static";
            var user = CreateTestUser();
            const string password = "password";
            const string newPassword = "newpassword";
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, password));
            var stamp = user.SecurityStamp;
            Assert.NotNull(stamp);
            IdentityResultAssert.IsFailure(await manager.ResetPasswordAsync(user, "bogus", newPassword), "Invalid token.");
            IdentityResultAssert.VerifyUserManagerFailureLog(manager.Logger, "ResetPasswordAsync", user.Id.ToString(), IdentityErrorDescriber.Default.InvalidToken());
            Assert.True(await manager.CheckPasswordAsync(user, password));
            Assert.Equal(stamp, user.SecurityStamp);
        }

        [Fact]
        public async Task CanGenerateAndVerifyUserTokenWithStaticTokenProvider()
        {
            var manager = CreateManager();
            manager.RegisterTokenProvider(new StaticTokenProvider());
            var user = CreateTestUser();
            var user2 = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user2));
            var token = await manager.GenerateUserTokenAsync(user, "Static", "test");
            IdentityResultAssert.VerifyUserManagerSuccessLog(manager.Logger, "GenerateUserTokenAsync", user.Id.ToString());

            Assert.True(await manager.VerifyUserTokenAsync(user, "Static", "test", token));
            IdentityResultAssert.VerifyUserManagerSuccessLog(manager.Logger, "VerifyUserTokenAsync", user.Id.ToString());

            Assert.False(await manager.VerifyUserTokenAsync(user, "Static", "test2", token));
            IdentityResultAssert.VerifyUserManagerFailureLog(manager.Logger, "VerifyUserTokenAsync", user.Id.ToString(), IdentityErrorDescriber.Default.InvalidToken());

            Assert.False(await manager.VerifyUserTokenAsync(user, "Static", "test", token + "a"));
            Assert.False(await manager.VerifyUserTokenAsync(user2, "Static", "test", token));
        }

        [Fact]
        public async Task CanConfirmEmailWithStaticToken()
        {
            var manager = CreateManager();
            manager.RegisterTokenProvider(new StaticTokenProvider());
            manager.Options.EmailConfirmationTokenProvider = "Static";
            var user = CreateTestUser();
            Assert.False(user.EmailConfirmed);
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var token = await manager.GenerateEmailConfirmationTokenAsync(user);
            Assert.NotNull(token);
            IdentityResultAssert.VerifyUserManagerSuccessLog(manager.Logger, "GenerateEmailConfirmationTokenAsync", user.Id.ToString());
            IdentityResultAssert.IsSuccess(await manager.ConfirmEmailAsync(user, token));
            IdentityResultAssert.VerifyUserManagerSuccessLog(manager.Logger, "ConfirmEmailAsync", user.Id.ToString());
            Assert.True(await manager.IsEmailConfirmedAsync(user));
            IdentityResultAssert.IsSuccess(await manager.SetEmailAsync(user, null));
            Assert.False(await manager.IsEmailConfirmedAsync(user));
        }

        [Fact]
        public async Task ConfirmEmailWithStaticTokenFailsWithWrongToken()
        {
            var manager = CreateManager();
            manager.RegisterTokenProvider(new StaticTokenProvider());
            manager.Options.EmailConfirmationTokenProvider = "Static";
            var user = CreateTestUser();
            Assert.False(user.EmailConfirmed);
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsFailure(await manager.ConfirmEmailAsync(user, "bogus"), "Invalid token.");
            Assert.False(await manager.IsEmailConfirmedAsync(user));
            IdentityResultAssert.VerifyUserManagerFailureLog(manager.Logger, "ConfirmEmailAsync", user.Id.ToString(), IdentityErrorDescriber.Default.InvalidToken());
        }

        [Fact]
        public async Task ConfirmTokenFailsAfterPasswordChange()
        {
            var manager = CreateManager();
            var user = new TUser() { UserName = "test" };
            Assert.False(user.EmailConfirmed);
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, "password"));
            var token = await manager.GenerateEmailConfirmationTokenAsync(user);
            Assert.NotNull(token);
            IdentityResultAssert.IsSuccess(await manager.ChangePasswordAsync(user, "password", "newpassword"));
            IdentityResultAssert.IsFailure(await manager.ConfirmEmailAsync(user, token), "Invalid token.");
            Assert.False(await manager.IsEmailConfirmedAsync(user));
        }

        // Lockout tests

        [Fact]
        public async Task SingleFailureLockout()
        {
            var mgr = CreateManager();
            mgr.Options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromHours(1);
            mgr.Options.Lockout.EnabledByDefault = true;
            mgr.Options.Lockout.MaxFailedAccessAttempts = 0;
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
            Assert.True(await mgr.GetLockoutEnabledAsync(user));
            Assert.True(user.LockoutEnabled);
            Assert.False(await mgr.IsLockedOutAsync(user));
            IdentityResultAssert.IsSuccess(await mgr.AccessFailedAsync(user));
            Assert.True(await mgr.IsLockedOutAsync(user));
            Assert.True(await mgr.GetLockoutEndDateAsync(user) > DateTimeOffset.UtcNow.AddMinutes(55));
            Assert.Equal(0, await mgr.GetAccessFailedCountAsync(user));
        }

        [Fact]
        public async Task TwoFailureLockout()
        {
            var mgr = CreateManager();
            mgr.Options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromHours(1);
            mgr.Options.Lockout.EnabledByDefault = true;
            mgr.Options.Lockout.MaxFailedAccessAttempts = 2;
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
            Assert.True(await mgr.GetLockoutEnabledAsync(user));
            Assert.True(user.LockoutEnabled);
            Assert.False(await mgr.IsLockedOutAsync(user));
            IdentityResultAssert.IsSuccess(await mgr.AccessFailedAsync(user));
            Assert.False(await mgr.IsLockedOutAsync(user));
            Assert.False(await mgr.GetLockoutEndDateAsync(user) > DateTimeOffset.UtcNow.AddMinutes(55));
            Assert.Equal(1, await mgr.GetAccessFailedCountAsync(user));
            IdentityResultAssert.IsSuccess(await mgr.AccessFailedAsync(user));
            Assert.True(await mgr.IsLockedOutAsync(user));
            Assert.True(await mgr.GetLockoutEndDateAsync(user) > DateTimeOffset.UtcNow.AddMinutes(55));
            Assert.Equal(0, await mgr.GetAccessFailedCountAsync(user));
        }

        [Fact]
        public async Task ResetAccessCountPreventsLockout()
        {
            var mgr = CreateManager();
            mgr.Options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromHours(1);
            mgr.Options.Lockout.EnabledByDefault = true;
            mgr.Options.Lockout.MaxFailedAccessAttempts = 2;
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
            Assert.True(await mgr.GetLockoutEnabledAsync(user));
            Assert.True(user.LockoutEnabled);
            Assert.False(await mgr.IsLockedOutAsync(user));
            IdentityResultAssert.IsSuccess(await mgr.AccessFailedAsync(user));
            Assert.False(await mgr.IsLockedOutAsync(user));
            Assert.False(await mgr.GetLockoutEndDateAsync(user) > DateTimeOffset.UtcNow.AddMinutes(55));
            Assert.Equal(1, await mgr.GetAccessFailedCountAsync(user));
            IdentityResultAssert.IsSuccess(await mgr.ResetAccessFailedCountAsync(user));
            IdentityResultAssert.VerifyUserManagerSuccessLog(mgr.Logger, "ResetAccessFailedCountAsync", user.Id.ToString());
            Assert.Equal(0, await mgr.GetAccessFailedCountAsync(user));
            Assert.False(await mgr.IsLockedOutAsync(user));
            Assert.False(await mgr.GetLockoutEndDateAsync(user) > DateTimeOffset.UtcNow.AddMinutes(55));
            IdentityResultAssert.IsSuccess(await mgr.AccessFailedAsync(user));
            Assert.False(await mgr.IsLockedOutAsync(user));
            Assert.False(await mgr.GetLockoutEndDateAsync(user) > DateTimeOffset.UtcNow.AddMinutes(55));
            Assert.Equal(1, await mgr.GetAccessFailedCountAsync(user));
        }

        [Fact]
        public async Task CanEnableLockoutManuallyAndLockout()
        {
            var mgr = CreateManager();
            mgr.Options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromHours(1);
            mgr.Options.Lockout.MaxFailedAccessAttempts = 2;
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
            Assert.False(await mgr.GetLockoutEnabledAsync(user));
            Assert.False(user.LockoutEnabled);
            IdentityResultAssert.IsSuccess(await mgr.SetLockoutEnabledAsync(user, true));
            Assert.True(await mgr.GetLockoutEnabledAsync(user));
            Assert.True(user.LockoutEnabled);
            IdentityResultAssert.VerifyUserManagerSuccessLog(mgr.Logger, "SetLockoutEnabledAsync", user.Id.ToString());
            Assert.False(await mgr.IsLockedOutAsync(user));
            IdentityResultAssert.IsSuccess(await mgr.AccessFailedAsync(user));
            IdentityResultAssert.VerifyUserManagerSuccessLog(mgr.Logger, "AccessFailedAsync", user.Id.ToString());
            Assert.False(await mgr.IsLockedOutAsync(user));
            Assert.False(await mgr.GetLockoutEndDateAsync(user) > DateTimeOffset.UtcNow.AddMinutes(55));
            Assert.Equal(1, await mgr.GetAccessFailedCountAsync(user));
            IdentityResultAssert.IsSuccess(await mgr.AccessFailedAsync(user));
            Assert.True(await mgr.IsLockedOutAsync(user));
            Assert.True(await mgr.GetLockoutEndDateAsync(user) > DateTimeOffset.UtcNow.AddMinutes(55));
            Assert.Equal(0, await mgr.GetAccessFailedCountAsync(user));
        }

        [Fact]
        public async Task UserNotLockedOutWithNullDateTimeAndIsSetToNullDate()
        {
            var mgr = CreateManager();
            mgr.Options.Lockout.EnabledByDefault = true;
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
            Assert.True(await mgr.GetLockoutEnabledAsync(user));
            Assert.True(user.LockoutEnabled);
            IdentityResultAssert.IsSuccess(await mgr.SetLockoutEndDateAsync(user, new DateTimeOffset()));
            Assert.False(await mgr.IsLockedOutAsync(user));
            IdentityResultAssert.VerifyUserManagerSuccessLog(mgr.Logger, "SetLockoutEndDateAsync", user.Id.ToString());
            Assert.Equal(new DateTimeOffset(), await mgr.GetLockoutEndDateAsync(user));
            Assert.Equal(new DateTimeOffset(), user.LockoutEnd);
        }

        [Fact]
        public async Task LockoutFailsIfNotEnabled()
        {
            var mgr = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
            Assert.False(await mgr.GetLockoutEnabledAsync(user));
            Assert.False(user.LockoutEnabled);
            IdentityResultAssert.IsFailure(await mgr.SetLockoutEndDateAsync(user, new DateTimeOffset()),
                "Lockout is not enabled for this user.");
            Assert.False(await mgr.IsLockedOutAsync(user));
        }

        [Fact]
        public async Task LockoutEndToUtcNowMinus1SecInUserShouldNotBeLockedOut()
        {
            var mgr = CreateManager();
            mgr.Options.Lockout.EnabledByDefault = true;
            var user = CreateTestUser();
            user.LockoutEnd = DateTimeOffset.UtcNow.AddSeconds(-1);
            IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
            Assert.True(await mgr.GetLockoutEnabledAsync(user));
            Assert.True(user.LockoutEnabled);
            Assert.False(await mgr.IsLockedOutAsync(user));
        }

        [Fact]
        public async Task LockoutEndToUtcNowSubOneSecondWithManagerShouldNotBeLockedOut()
        {
            var mgr = CreateManager();
            mgr.Options.Lockout.EnabledByDefault = true;
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
            Assert.True(await mgr.GetLockoutEnabledAsync(user));
            Assert.True(user.LockoutEnabled);
            IdentityResultAssert.IsSuccess(await mgr.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddSeconds(-1)));
            Assert.False(await mgr.IsLockedOutAsync(user));
        }

        [Fact]
        public async Task LockoutEndToUtcNowPlus5ShouldBeLockedOut()
        {
            var mgr = CreateManager();
            mgr.Options.Lockout.EnabledByDefault = true;
            var user = CreateTestUser();
            user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(5);
            IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
            Assert.True(await mgr.GetLockoutEnabledAsync(user));
            Assert.True(user.LockoutEnabled);
            Assert.True(await mgr.IsLockedOutAsync(user));
        }

        [Fact]
        public async Task UserLockedOutWithDateTimeLocalKindNowPlus30()
        {
            var mgr = CreateManager();
            mgr.Options.Lockout.EnabledByDefault = true;
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
            Assert.True(await mgr.GetLockoutEnabledAsync(user));
            Assert.True(user.LockoutEnabled);
            var lockoutEnd = new DateTimeOffset(DateTime.Now.AddMinutes(30).ToLocalTime());
            IdentityResultAssert.IsSuccess(await mgr.SetLockoutEndDateAsync(user, lockoutEnd));
            Assert.True(await mgr.IsLockedOutAsync(user));
            var end = await mgr.GetLockoutEndDateAsync(user);
            Assert.Equal(lockoutEnd, end);
        }

        // Role Tests
        [Fact]
        public async Task CanCreateRoleTest()
        {
            var manager = CreateRoleManager();
            var role = CreateRole("create");
            Assert.False(await manager.RoleExistsAsync(role.Name));
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
            Assert.True(await manager.RoleExistsAsync(role.Name));
        }

        private class AlwaysBadValidator : IUserValidator<TUser>, IRoleValidator<TRole>,
            IPasswordValidator<TUser>
        {
            public static readonly IdentityError ErrorMessage = new IdentityError { Description = "I'm Bad.", Code = "BadValidator" };

            public Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user, string password, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(IdentityResult.Failed(ErrorMessage));
            }

            public Task<IdentityResult> ValidateAsync(RoleManager<TRole> manager, TRole role, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(IdentityResult.Failed(ErrorMessage));
            }

            public Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(IdentityResult.Failed(ErrorMessage));
            }
        }

        [Fact]
        public async Task BadValidatorBlocksCreateRole()
        {
            var manager = CreateRoleManager();
            manager.RoleValidators.Clear();
            manager.RoleValidators.Add(new AlwaysBadValidator());
            IdentityResultAssert.IsFailure(await manager.CreateAsync(CreateRole("blocked")),
                AlwaysBadValidator.ErrorMessage);
        }

        [Fact]
        public async Task CanChainRoleValidators()
        {
            var manager = CreateRoleManager();
            manager.RoleValidators.Clear();
            manager.RoleValidators.Add(new AlwaysBadValidator());
            manager.RoleValidators.Add(new AlwaysBadValidator());
            var result = await manager.CreateAsync(CreateRole("blocked"));
            IdentityResultAssert.IsFailure(result, AlwaysBadValidator.ErrorMessage);
            Assert.Equal(2, result.Errors.Count());
        }

        [Fact]
        public async Task BadValidatorBlocksRoleUpdate()
        {
            var manager = CreateRoleManager();
            var role = CreateRole("poorguy");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
            var error = AlwaysBadValidator.ErrorMessage;
            manager.RoleValidators.Clear();
            manager.RoleValidators.Add(new AlwaysBadValidator());
            IdentityResultAssert.IsFailure(await manager.UpdateAsync(role), error);
            IdentityResultAssert.VerifyRoleManagerFailureLog(manager.Logger, "UpdateAsync", role.Id.ToString(), AlwaysBadValidator.ErrorMessage);
        }

        [Fact]
        public async Task CanDeleteRole()
        {
            var manager = CreateRoleManager();
            var role = CreateRole("delete");
            Assert.False(await manager.RoleExistsAsync(role.Name));
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
            IdentityResultAssert.IsSuccess(await manager.DeleteAsync(role));
            Assert.False(await manager.RoleExistsAsync(role.Name));
            IdentityResultAssert.VerifyRoleManagerSuccessLog(manager.Logger, "DeleteAsync", role.Id.ToString());
        }

        [Fact]
        public async Task CanAddRemoveRoleClaim()
        {
            var manager = CreateRoleManager();
            var role = CreateRole("ClaimsAddRemove");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
            Claim[] claims = { new Claim("c", "v"), new Claim("c2", "v2"), new Claim("c2", "v3") };
            foreach (Claim c in claims)
            {
                IdentityResultAssert.IsSuccess(await manager.AddClaimAsync(role, c));
            }
            IdentityResultAssert.VerifyRoleManagerSuccessLog(manager.Logger, "AddClaimAsync", role.Id.ToString());
            var roleClaims = await manager.GetClaimsAsync(role);
            Assert.Equal(3, roleClaims.Count);
            IdentityResultAssert.IsSuccess(await manager.RemoveClaimAsync(role, claims[0]));
            roleClaims = await manager.GetClaimsAsync(role);
            Assert.Equal(2, roleClaims.Count);
            IdentityResultAssert.IsSuccess(await manager.RemoveClaimAsync(role, claims[1]));
            roleClaims = await manager.GetClaimsAsync(role);
            Assert.Equal(1, roleClaims.Count);
            IdentityResultAssert.IsSuccess(await manager.RemoveClaimAsync(role, claims[2]));
            roleClaims = await manager.GetClaimsAsync(role);
            Assert.Equal(0, roleClaims.Count);

            IdentityResultAssert.VerifyRoleManagerSuccessLog(manager.Logger, "RemoveClaimAsync", role.Id.ToString());
        }

        [Fact]
        public async Task CanRoleFindById()
        {
            var manager = CreateRoleManager();
            var role = CreateRole("FindByIdAsync");
            Assert.Null(await manager.FindByIdAsync(role.Id.ToString()));
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
            Assert.Equal(role, await manager.FindByIdAsync(role.Id.ToString()));
        }

        [Fact]
        public async Task CanRoleFindByName()
        {
            var manager = CreateRoleManager();
            var role = CreateRole("FindByNameAsync");
            Assert.Null(await manager.FindByNameAsync(role.Name));
            Assert.False(await manager.RoleExistsAsync(role.Name));
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
            Assert.Equal(role, await manager.FindByNameAsync(role.Name));
        }

        [Fact]
        public async Task CanUpdateRoleName()
        {
            var manager = CreateRoleManager();
            var role = CreateRole("update");
            Assert.False(await manager.RoleExistsAsync(role.Name));
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
            Assert.True(await manager.RoleExistsAsync(role.Name));
            IdentityResultAssert.IsSuccess(await manager.SetRoleNameAsync(role, "Changed"));
            IdentityResultAssert.IsSuccess(await manager.UpdateAsync(role));
            IdentityResultAssert.VerifyRoleManagerSuccessLog(manager.Logger, "UpdateAsync", role.Id.ToString());
            Assert.False(await manager.RoleExistsAsync("update"));
            Assert.Equal(role, await manager.FindByNameAsync("Changed"));
        }

        [Fact]
        public async Task CanQueryableRoles()
        {
            var manager = CreateRoleManager();
            var roles = GenerateRoles("CanQuerableRolesTest", 4);
            foreach (var r in roles)
            {
                IdentityResultAssert.IsSuccess(await manager.CreateAsync(r));
            }
            Assert.Equal(roles.Count, manager.Roles.Count(r => r.Name.StartsWith("CanQuerableRolesTest")));
            var r1 = manager.Roles.FirstOrDefault(r => r.Name.StartsWith("CanQuerableRolesTest1"));
            Assert.Equal(roles[1], r1);
        }

        [Fact]
        public async Task DeleteRoleNonEmptySucceedsTest()
        {
            // Need fail if not empty?
            var context = CreateTestContext();
            var userMgr = CreateManager(context);
            var roleMgr = CreateRoleManager(context);
            var role = CreateRole();
            Assert.False(await roleMgr.RoleExistsAsync(role.Name));
            IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await userMgr.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await userMgr.AddToRoleAsync(user, role.Name));
            var roles = await userMgr.GetRolesAsync(user);
            Assert.Equal(1, roles.Count());
            IdentityResultAssert.IsSuccess(await roleMgr.DeleteAsync(role));
            Assert.Null(await roleMgr.FindByNameAsync(role.Name));
            Assert.False(await roleMgr.RoleExistsAsync(role.Name));
            // REVIEW: We should throw if deleteing a non empty role?
            roles = await userMgr.GetRolesAsync(user);

            // REVIEW: This depends on cascading deletes
            //Assert.Equal(0, roles.Count());
        }

        // TODO: cascading deletes?  navigation properties not working
        ////[Fact]
        ////public async Task DeleteUserRemovesFromRoleTest()
        ////{
        ////    // Need fail if not empty?
        ////    var userMgr = CreateManager();
        ////    var roleMgr = CreateRoleManager();
        ////    var role = CreateRole("deleteNonEmpty");
        ////    Assert.False(await roleMgr.RoleExistsAsync(role.Name));
        ////    IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
        ////    var user = new TUser() { UserName = "t");
        ////    IdentityResultAssert.IsSuccess(await userMgr.CreateAsync(user));
        ////    IdentityResultAssert.IsSuccess(await userMgr.AddToRoleAsync(user, role.Name));
        ////    IdentityResultAssert.IsSuccess(await userMgr.DeleteAsync(user));
        ////    role = roleMgr.FindByIdAsync(role.Id);
        ////}

        [Fact]
        public async Task CreateRoleFailsIfExists()
        {
            var manager = CreateRoleManager();
            var role = CreateRole("dupeRole");
            Assert.False(await manager.RoleExistsAsync(role.Name));
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
            IdentityResultAssert.VerifyRoleManagerSuccessLog(manager.Logger, "CreateAsync", role.Id.ToString());
            Assert.True(await manager.RoleExistsAsync(role.Name));
            var role2 = CreateRole();
            role2.Name = role.Name;
            IdentityResultAssert.IsFailure(await manager.CreateAsync(role2));
        }

        [Fact]
        public async Task CanAddUsersToRole()
        {
            var context = CreateTestContext();
            var manager = CreateManager(context);
            var roleManager = CreateRoleManager(context);
            var role = CreateRole("addUserTest");
            IdentityResultAssert.IsSuccess(await roleManager.CreateAsync(role));
            TUser[] users =
            {
                new TUser() { UserName = "1"}, new TUser() { UserName = "2"}, new TUser() { UserName = "3"},
                new TUser() { UserName = "4"}
            };
            foreach (var u in users)
            {
                IdentityResultAssert.IsSuccess(await manager.CreateAsync(u));
                IdentityResultAssert.IsSuccess(await manager.AddToRoleAsync(u, role.Name));
                Assert.True(await manager.IsInRoleAsync(u, role.Name));
                IdentityResultAssert.VerifyUserManagerSuccessLog(manager.Logger, "AddToRoleAsync", u.Id.ToString());

            }
        }

        [Fact]
        public async Task CanGetRolesForUser()
        {
            var context = CreateTestContext();
            var userManager = CreateManager(context);
            var roleManager = CreateRoleManager(context);
            var users = GenerateUsers("CanGetRolesForUser", 4);
            var roles = GenerateRoles("CanGetRolesForUserRole", 4);
            foreach (var u in users)
            {
                IdentityResultAssert.IsSuccess(await userManager.CreateAsync(u));
            }
            foreach (var r in roles)
            {
                IdentityResultAssert.IsSuccess(await roleManager.CreateAsync(r));
                foreach (var u in users)
                {
                    IdentityResultAssert.IsSuccess(await userManager.AddToRoleAsync(u, r.Name));
                    Assert.True(await userManager.IsInRoleAsync(u, r.Name));
                }
            }

            foreach (var u in users)
            {
                var rs = await userManager.GetRolesAsync(u);
                Assert.Equal(roles.Count, rs.Count);
                foreach (var r in roles)
                {
                    Assert.True(rs.Any(role => role == r.Name));
                }
            }
        }

        [Fact]
        public async Task RemoveUserFromRoleWithMultipleRoles()
        {
            var context = CreateTestContext();
            var userManager = CreateManager(context);
            var roleManager = CreateRoleManager(context);
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await userManager.CreateAsync(user));
            var roles = GenerateRoles("RemoveUserFromRoleWithMultipleRoles", 4);
            foreach (var r in roles)
            {
                IdentityResultAssert.IsSuccess(await roleManager.CreateAsync(r));
                IdentityResultAssert.IsSuccess(await userManager.AddToRoleAsync(user, r.Name));
                Assert.True(await userManager.IsInRoleAsync(user, r.Name));
            }
            IdentityResultAssert.IsSuccess(await userManager.RemoveFromRoleAsync(user, roles[2].Name));
            IdentityResultAssert.VerifyUserManagerSuccessLog(userManager.Logger, "RemoveFromRoleAsync", user.Id.ToString());
            Assert.False(await userManager.IsInRoleAsync(user, roles[2].Name));
        }

        [Fact]
        public async Task CanRemoveUsersFromRole()
        {
            var context = CreateTestContext();
            var userManager = CreateManager(context);
            var roleManager = CreateRoleManager(context);
            var users = GenerateUsers("CanRemoveUsersFromRole", 4);
            foreach (var u in users)
            {
                IdentityResultAssert.IsSuccess(await userManager.CreateAsync(u));
            }
            var r = CreateRole("r1");
            IdentityResultAssert.IsSuccess(await roleManager.CreateAsync(r));
            foreach (var u in users)
            {
                IdentityResultAssert.IsSuccess(await userManager.AddToRoleAsync(u, r.Name));
                Assert.True(await userManager.IsInRoleAsync(u, r.Name));
            }
            foreach (var u in users)
            {
                IdentityResultAssert.IsSuccess(await userManager.RemoveFromRoleAsync(u, r.Name));
                Assert.False(await userManager.IsInRoleAsync(u, r.Name));
            }
        }

        [Fact]
        public async Task RemoveUserNotInRoleFails()
        {
            var context = CreateTestContext();
            var userMgr = CreateManager(context);
            var roleMgr = CreateRoleManager(context);
            var role = CreateRole("addUserDupeTest");
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await userMgr.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
            var result = await userMgr.RemoveFromRoleAsync(user, role.Name);
            IdentityResultAssert.IsFailure(result, IdentityErrorDescriber.Default.UserNotInRole(role.Name));
            IdentityResultAssert.VerifyUserManagerFailureLog(userMgr.Logger, "RemoveFromRoleAsync", user.Id.ToString(), IdentityErrorDescriber.Default.UserNotInRole(role.Name));
        }

        [Fact]
        public async Task AddUserToRoleFailsIfAlreadyInRole()
        {
            var context = CreateTestContext();
            var userMgr = CreateManager(context);
            var roleMgr = CreateRoleManager(context);
            var role = CreateRole("addUserDupeTest");
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await userMgr.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
            IdentityResultAssert.IsSuccess(await userMgr.AddToRoleAsync(user, role.Name));
            Assert.True(await userMgr.IsInRoleAsync(user, role.Name));
            IdentityResultAssert.IsFailure(await userMgr.AddToRoleAsync(user, role.Name), IdentityErrorDescriber.Default.UserAlreadyInRole(role.Name));
            IdentityResultAssert.VerifyUserManagerFailureLog(userMgr.Logger, "AddToRoleAsync", user.Id.ToString(), IdentityErrorDescriber.Default.UserAlreadyInRole(role.Name));
        }

        [Fact]
        public async Task CanFindRoleByNameWithManager()
        {
            var roleMgr = CreateRoleManager();
            var role = CreateRole("findRoleByNameTest");
            IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
            Assert.Equal(role.Id, (await roleMgr.FindByNameAsync(role.Name)).Id);
        }

        [Fact]
        public async Task CanFindRoleWithManager()
        {
            var roleMgr = CreateRoleManager();
            var role = CreateRole("findRoleTest");
            IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
            Assert.Equal(role.Name, (await roleMgr.FindByIdAsync(role.Id.ToString())).Name);
        }

        [Fact]
        public async Task SetPhoneNumberTest()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            user.PhoneNumber = "123-456-7890";
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var stamp = await manager.GetSecurityStampAsync(user);
            Assert.Equal(await manager.GetPhoneNumberAsync(user), "123-456-7890");
            IdentityResultAssert.IsSuccess(await manager.SetPhoneNumberAsync(user, "111-111-1111"));
            Assert.Equal(await manager.GetPhoneNumberAsync(user), "111-111-1111");
            Assert.NotEqual(stamp, user.SecurityStamp);
        }

        [Fact]
        public async Task CanChangePhoneNumber()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            user.PhoneNumber = "123-456-7890";
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            Assert.False(await manager.IsPhoneNumberConfirmedAsync(user));
            var stamp = await manager.GetSecurityStampAsync(user);
            var token1 = await manager.GenerateChangePhoneNumberTokenAsync(user, "111-111-1111");
            IdentityResultAssert.IsSuccess(await manager.ChangePhoneNumberAsync(user, "111-111-1111", token1));
            IdentityResultAssert.VerifyUserManagerSuccessLog(manager.Logger, "ChangePhoneNumberAsync", user.Id.ToString());
            Assert.True(await manager.IsPhoneNumberConfirmedAsync(user));
            Assert.Equal(await manager.GetPhoneNumberAsync(user), "111-111-1111");
            Assert.NotEqual(stamp, user.SecurityStamp);
        }

        [Fact]
        public async Task ChangePhoneNumberFailsWithWrongToken()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            user.PhoneNumber = "123-456-7890";
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            Assert.False(await manager.IsPhoneNumberConfirmedAsync(user));
            var stamp = await manager.GetSecurityStampAsync(user);
            IdentityResultAssert.IsFailure(await manager.ChangePhoneNumberAsync(user, "111-111-1111", "bogus"),
                "Invalid token.");
            IdentityResultAssert.VerifyUserManagerFailureLog(manager.Logger, "ChangePhoneNumberAsync", user.Id.ToString(), IdentityErrorDescriber.Default.InvalidToken());
            Assert.False(await manager.IsPhoneNumberConfirmedAsync(user));
            Assert.Equal(await manager.GetPhoneNumberAsync(user), "123-456-7890");
            Assert.Equal(stamp, user.SecurityStamp);
        }

        [Fact]
        public async Task ChangePhoneNumberFailsWithWrongPhoneNumber()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            user.PhoneNumber = "123-456-7890";
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            Assert.False(await manager.IsPhoneNumberConfirmedAsync(user));
            var stamp = await manager.GetSecurityStampAsync(user);
            var token1 = await manager.GenerateChangePhoneNumberTokenAsync(user, "111-111-1111");
            IdentityResultAssert.IsFailure(await manager.ChangePhoneNumberAsync(user, "bogus", token1),
                "Invalid token.");
            Assert.False(await manager.IsPhoneNumberConfirmedAsync(user));
            Assert.Equal(await manager.GetPhoneNumberAsync(user), "123-456-7890");
            Assert.Equal(stamp, user.SecurityStamp);
        }

        [Fact]
        public async Task CanVerifyPhoneNumber()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            const string num1 = "111-123-4567";
            const string num2 = "111-111-1111";
            var token1 = await manager.GenerateChangePhoneNumberTokenAsync(user, num1);
            IdentityResultAssert.VerifyUserManagerSuccessLog(manager.Logger, "GenerateChangePhoneNumberTokenAsync", user.Id.ToString());

            var token2 = await manager.GenerateChangePhoneNumberTokenAsync(user, num2);
            Assert.NotEqual(token1, token2);
            Assert.True(await manager.VerifyChangePhoneNumberTokenAsync(user, token1, num1));
            IdentityResultAssert.VerifyUserManagerSuccessLog(manager.Logger, "VerifyChangePhoneNumberTokenAsync", user.Id.ToString());
            Assert.True(await manager.VerifyChangePhoneNumberTokenAsync(user, token2, num2));
            Assert.False(await manager.VerifyChangePhoneNumberTokenAsync(user, token2, num1));
            Assert.False(await manager.VerifyChangePhoneNumberTokenAsync(user, token1, num2));
            IdentityResultAssert.VerifyUserManagerFailureLog(manager.Logger, "VerifyChangePhoneNumberTokenAsync", user.Id.ToString(), IdentityErrorDescriber.Default.InvalidToken());
        }

        [Fact]
        public async Task CanChangeEmail()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            user.Email = user.UserName + "@diddly.bop";
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            Assert.False(await manager.IsEmailConfirmedAsync(user));
            var stamp = await manager.GetSecurityStampAsync(user);
            string newEmail = user.UserName + "@en.vec";
            var token1 = await manager.GenerateChangeEmailTokenAsync(user, newEmail);
            IdentityResultAssert.IsSuccess(await manager.ChangeEmailAsync(user, newEmail, token1));
            IdentityResultAssert.VerifyUserManagerSuccessLog(manager.Logger, "ChangeEmailAsync", user.Id.ToString());
            Assert.True(await manager.IsEmailConfirmedAsync(user));
            Assert.Equal(await manager.GetEmailAsync(user), newEmail);
            Assert.NotEqual(stamp, user.SecurityStamp);
        }

        [Fact]
        public async Task ChangeEmailFailsWithWrongToken()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            user.Email = user.UserName + "@diddly.bop";
            string oldEmail = user.Email;
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            Assert.False(await manager.IsEmailConfirmedAsync(user));
            var stamp = await manager.GetSecurityStampAsync(user);
            IdentityResultAssert.IsFailure(await manager.ChangeEmailAsync(user, "whatevah@foo.barf", "bogus"),
                "Invalid token.");
            IdentityResultAssert.VerifyUserManagerFailureLog(manager.Logger, "ChangeEmailAsync", user.Id.ToString(), IdentityErrorDescriber.Default.InvalidToken());
            Assert.False(await manager.IsEmailConfirmedAsync(user));
            Assert.Equal(await manager.GetEmailAsync(user), oldEmail);
            Assert.Equal(stamp, user.SecurityStamp);
        }

        [Fact]
        public async Task ChangeEmailFailsWithEmail()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            user.Email = user.UserName + "@diddly.bop";
            string oldEmail = user.Email;
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            Assert.False(await manager.IsEmailConfirmedAsync(user));
            var stamp = await manager.GetSecurityStampAsync(user);
            var token1 = await manager.GenerateChangeEmailTokenAsync(user, "forgot@alrea.dy");
            IdentityResultAssert.IsFailure(await manager.ChangeEmailAsync(user, "oops@foo.barf", token1),
                "Invalid token.");
            Assert.False(await manager.IsEmailConfirmedAsync(user));
            Assert.Equal(await manager.GetEmailAsync(user), oldEmail);
            Assert.Equal(stamp, user.SecurityStamp);
        }

        [Fact]
        public async Task CanEmailTwoFactorToken()
        {
            var manager = CreateManager();
            var messageService = new TestMessageService<TUser> { Name = "Email" };
            manager.RegisterMessageProvider(messageService);
            const string factorId = "Email"; // default
            var user = new TUser() { UserName = "EmailCodeTest", Email = "foo@foo.com" };
            user.EmailConfirmed = true;
            const string password = "password";
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, password));
            var stamp = user.SecurityStamp;
            Assert.NotNull(stamp);
            var token = await manager.GenerateTwoFactorTokenAsync(user, factorId);
            Assert.NotNull(token);
            Assert.Null(messageService.Message);
            IdentityResultAssert.IsSuccess(await manager.NotifyTwoFactorTokenAsync(user, factorId, token));
            Assert.NotNull(messageService.Message);
            Assert.Equal("Your security code is: " + token, messageService.Message.Body);
            Assert.True(await manager.VerifyTwoFactorTokenAsync(user, factorId, token));
            IdentityResultAssert.VerifyUserManagerSuccessLog(manager.Logger, "VerifyTwoFactorTokenAsync", user.Id.ToString());
        }

        [Fact]
        public async Task NotifyWithUnknownProviderFails()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            await
                ExceptionAssert.ThrowsAsync<NotSupportedException>(
                    async () => await manager.NotifyTwoFactorTokenAsync(user, "Bogus", "token"),
                    "No IUserTokenProvider named 'Bogus' is registered.");
        }

        [Fact]
        public async Task EmailTokenFactorWithFormatTest()
        {
            // CONSIDER: do we want to support multiple email token options?
            const string factorId = "Email"; // default
            const string subject = "Custom subject";
            const string body = "Your code is {0}!";
            var services = new ServiceCollection();
            services.Configure<EmailTokenProviderOptions>(o =>
            {
                o.Name = factorId;
                o.Subject = subject;
                o.BodyFormat = body;
            });
            var manager = CreateManager(null, services);
            var messageService = new TestMessageService<TUser> { Name = "Email" };
            manager.RegisterMessageProvider(messageService);
            var user = CreateTestUser();
            user.Email = user.UserName + "@foo.com";
            const string password = "password";
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, password));
            var stamp = user.SecurityStamp;
            Assert.NotNull(stamp);
            var token = await manager.GenerateTwoFactorTokenAsync(user, factorId);
            Assert.NotNull(token);
            IdentityResultAssert.VerifyUserManagerSuccessLog(manager.Logger, "GenerateTwoFactorTokenAsync", user.Id.ToString());
            Assert.Null(messageService.Message);
            IdentityResultAssert.IsSuccess(await manager.NotifyTwoFactorTokenAsync(user, factorId, token));
            Assert.NotNull(messageService.Message);
            Assert.Equal(subject, messageService.Message.Subject);
            Assert.Equal(string.Format(body, token), messageService.Message.Body);
            IdentityResultAssert.VerifyUserManagerSuccessLog(manager.Logger, "NotifyTwoFactorTokenAsync", user.Id.ToString());
            Assert.True(await manager.VerifyTwoFactorTokenAsync(user, factorId, token));
        }

        [Fact]
        public async Task EmailFactorFailsAfterSecurityStampChangeTest()
        {
            var manager = CreateManager();
            string factorId = "Email"; //default
            var user = CreateTestUser();
            user.Email = user.UserName + "@foo.com";
            user.EmailConfirmed = true;
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var stamp = user.SecurityStamp;
            Assert.NotNull(stamp);
            var token = await manager.GenerateTwoFactorTokenAsync(user, factorId);
            Assert.NotNull(token);
            IdentityResultAssert.IsSuccess(await manager.UpdateSecurityStampAsync(user));
            Assert.False(await manager.VerifyTwoFactorTokenAsync(user, factorId, token));
        }

        [Fact]
        public async Task EnableTwoFactorChangesSecurityStamp()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var stamp = user.SecurityStamp;
            Assert.NotNull(stamp);
            IdentityResultAssert.IsSuccess(await manager.SetTwoFactorEnabledAsync(user, true));
            IdentityResultAssert.VerifyUserManagerSuccessLog(manager.Logger, "SetTwoFactorEnabledAsync", user.Id.ToString());
            Assert.NotEqual(stamp, await manager.GetSecurityStampAsync(user));
            Assert.True(await manager.GetTwoFactorEnabledAsync(user));
        }

        [Fact]
        public async Task CanSendMessage()
        {
            var manager = CreateManager();
            var messageService = new TestMessageService<TUser>();
            manager.RegisterMessageProvider(messageService);
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            await manager.SendMessageAsync(messageService.Name, new IdentityMessage { Destination = "foo", Subject = "Hi", Body = "Body" });
            Assert.NotNull(messageService.Message);
            Assert.Equal("foo", messageService.Message.Destination);
            Assert.Equal("Hi", messageService.Message.Subject);
            Assert.Equal("Body", messageService.Message.Body);
        }

        [Fact]
        public async Task CanSmsTwoFactorToken()
        {
            var manager = CreateManager();
            var messageService = new TestMessageService<TUser> { Name = "SMS" };
            manager.RegisterMessageProvider(messageService);
            const string factorId = "Phone"; // default
            var user = CreateTestUser();
            user.PhoneNumber = "4251234567";
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var stamp = user.SecurityStamp;
            Assert.NotNull(stamp);
            var token = await manager.GenerateTwoFactorTokenAsync(user, factorId);
            Assert.NotNull(token);
            Assert.Null(messageService.Message);
            IdentityResultAssert.IsSuccess(await manager.NotifyTwoFactorTokenAsync(user, factorId, token));
            Assert.NotNull(messageService.Message);
            Assert.Equal("Your security code is: " + token, messageService.Message.Body);
            Assert.True(await manager.VerifyTwoFactorTokenAsync(user, factorId, token));
        }

        [Fact]
        public async Task PhoneTokenFactorFormatTest()
        {
            const string factorId = "Phone"; // default
            var services = new ServiceCollection();
            services.Configure<PhoneNumberTokenProviderOptions>(o =>
            {
                o.Name = factorId;
                o.MessageFormat = "Your code is: {0}";
            });
            var manager = CreateManager(null, services);
            var messageService = new TestMessageService<TUser> { Name = "SMS" };
            manager.RegisterMessageProvider(messageService);
            var user = CreateTestUser();
            user.PhoneNumber = "4251234567";
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var stamp = user.SecurityStamp;
            Assert.NotNull(stamp);
            var token = await manager.GenerateTwoFactorTokenAsync(user, factorId);
            Assert.NotNull(token);
            Assert.Null(messageService.Message);
            IdentityResultAssert.IsSuccess(await manager.NotifyTwoFactorTokenAsync(user, factorId, token));
            Assert.NotNull(messageService.Message);
            Assert.Equal("Your code is: " + token, messageService.Message.Body);
            Assert.True(await manager.VerifyTwoFactorTokenAsync(user, factorId, token));
        }

        [Fact]
        public async Task GenerateTwoFactorWithUnknownFactorProviderWillThrow()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            const string error = "No IUserTokenProvider named 'bogus' is registered.";
            await
                ExceptionAssert.ThrowsAsync<NotSupportedException>(
                    () => manager.GenerateTwoFactorTokenAsync(user, "bogus"), error);
            await ExceptionAssert.ThrowsAsync<NotSupportedException>(
                () => manager.VerifyTwoFactorTokenAsync(user, "bogus", "bogus"), error);
        }

        [Fact]
        public async Task GetValidTwoFactorTestEmptyWithNoProviders()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var factors = await manager.GetValidTwoFactorProvidersAsync(user);
            Assert.NotNull(factors);
            Assert.True(!factors.Any());
        }

        [Fact]
        public async Task CanGetValidTwoFactor()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var factors = await manager.GetValidTwoFactorProvidersAsync(user);
            Assert.NotNull(factors);
            Assert.False(factors.Any());
            IdentityResultAssert.IsSuccess(await manager.SetPhoneNumberAsync(user, "111-111-1111"));
            IdentityResultAssert.VerifyUserManagerSuccessLog(manager.Logger, "SetPhoneNumberAsync", user.Id.ToString());
            user.PhoneNumberConfirmed = true;
            await manager.UpdateAsync(user);
            factors = await manager.GetValidTwoFactorProvidersAsync(user);
            Assert.NotNull(factors);
            Assert.Equal(1, factors.Count());
            Assert.Equal("Phone", factors[0]);
            IdentityResultAssert.IsSuccess(await manager.SetEmailAsync(user, "test@test.com"));
            IdentityResultAssert.VerifyUserManagerSuccessLog(manager.Logger, "SetEmailAsync", user.Id.ToString());
            user.EmailConfirmed = true;
            await manager.UpdateAsync(user);
            factors = await manager.GetValidTwoFactorProvidersAsync(user);
            Assert.NotNull(factors);
            Assert.Equal(2, factors.Count());
            IdentityResultAssert.IsSuccess(await manager.SetEmailAsync(user, null));
            factors = await manager.GetValidTwoFactorProvidersAsync(user);
            Assert.NotNull(factors);
            Assert.Equal(1, factors.Count());
            Assert.Equal("Phone", factors[0]);
        }

        [Fact]
        public async Task PhoneFactorFailsAfterSecurityStampChangeTest()
        {
            var manager = CreateManager();
            var factorId = "Phone"; // default
            var user = CreateTestUser();
            user.PhoneNumber = "4251234567";
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var stamp = user.SecurityStamp;
            Assert.NotNull(stamp);
            var token = await manager.GenerateTwoFactorTokenAsync(user, factorId);
            Assert.NotNull(token);
            IdentityResultAssert.IsSuccess(await manager.UpdateSecurityStampAsync(user));
            Assert.False(await manager.VerifyTwoFactorTokenAsync(user, factorId, token));
        }

        [Fact]
        public async Task VerifyTokenFromWrongTokenProviderFails()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            user.PhoneNumber = "4251234567";
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var token = await manager.GenerateTwoFactorTokenAsync(user, "Phone");
            Assert.NotNull(token);
            Assert.False(await manager.VerifyTwoFactorTokenAsync(user, "Email", token));
        }

        [Fact]
        public async Task VerifyWithWrongSmsTokenFails()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            user.PhoneNumber = "4251234567";
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            Assert.False(await manager.VerifyTwoFactorTokenAsync(user, "Phone", "bogus"));
            IdentityResultAssert.VerifyUserManagerFailureLog(manager.Logger, "VerifyTwoFactorTokenAsync", user.Id.ToString(), IdentityErrorDescriber.Default.InvalidToken());
        }

        [Fact]
        public async Task NullableDateTimeOperationTest()
        {
            var userMgr = CreateManager();
            var user = CreateTestUser();
            user.LockoutEnabled = true;
            IdentityResultAssert.IsSuccess(await userMgr.CreateAsync(user));

            Assert.Null(await userMgr.GetLockoutEndDateAsync(user));

            // set LockoutDateEndDate to null
            await userMgr.SetLockoutEndDateAsync(user, null);
            Assert.Null(await userMgr.GetLockoutEndDateAsync(user));

            // set to a valid value
            await userMgr.SetLockoutEndDateAsync(user, DateTimeOffset.Parse("01/01/2014"));
            Assert.Equal(DateTimeOffset.Parse("01/01/2014"), await userMgr.GetLockoutEndDateAsync(user));
        }

        [Fact]
        public async Task CanGetUsersWithClaims()
        {
            var manager = CreateManager();

            for (int i = 0; i < 6; i++)
            {
                var user = CreateTestUser();
                IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));

                if ((i % 2) == 0)
                {
                    IdentityResultAssert.IsSuccess(await manager.AddClaimAsync(user, new Claim("foo", "bar")));
                }
            }

            Assert.Equal(3, (await manager.GetUsersForClaimAsync(new Claim("foo", "bar"))).Count);

            Assert.Equal(0, (await manager.GetUsersForClaimAsync(new Claim("123", "456"))).Count);
        }

        [Fact]
        public async Task CanGetUsersInRole()
        {
            var context = CreateTestContext();
            var manager = CreateManager(context);
            var roleManager = CreateRoleManager(context);
            var roles = GenerateRoles("UsersInRole", 4);

            foreach (var role in roles)
            {
                IdentityResultAssert.IsSuccess(await roleManager.CreateAsync(role));
            }

            for (int i = 0; i < 6; i++)
            {
                var user = CreateTestUser();
                IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));

                if ((i % 2) == 0)
                {
                    IdentityResultAssert.IsSuccess(await manager.AddToRolesAsync(user, roles.Select(x => x.Name).AsEnumerable()));
                    IdentityResultAssert.VerifyUserManagerSuccessLog(manager.Logger, "AddToRolesAsync", user.Id.ToString());
                }
            }

            foreach (var role in roles)
            {
                Assert.Equal(3, (await manager.GetUsersInRoleAsync(role.Name)).Count);
            }

            Assert.Equal(0, (await manager.GetUsersInRoleAsync("123456")).Count);
        }

        public List<TUser> GenerateUsers(string userNamePrefix, int count)
        {
            var users = new List<TUser>(count);
            for (var i = 0; i < count; i++)
            {
                users.Add(CreateTestUser(userNamePrefix + i));
            }
            return users;
        }

        public List<TRole> GenerateRoles(string namePrefix, int count)
        {
            var roles = new List<TRole>(count);
            for (var i = 0; i < count; i++)
            {
                roles.Add(CreateRole(namePrefix + i));
            }
            return roles;
        }
    }
}
