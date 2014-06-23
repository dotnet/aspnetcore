// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Testing;
using Xunit;

namespace Microsoft.AspNet.Identity.Test
{
    public abstract class UserManagerTestBase<TUser, TRole> where TUser: IdentityUser, new() where TRole: IdentityRole, new()
    {
        protected abstract UserManager<TUser> CreateManager();
        protected abstract RoleManager<TRole> CreateRoleManager();

        protected TUser CreateTestUser() {
            return new TUser() { UserName = Guid.NewGuid().ToString() };
        }

        protected TRole CreateRole(string name) {
            return new TRole() { Name = name };
        }


        [Fact]
        public async Task CanDeleteUser()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await manager.DeleteAsync(user));
            Assert.Null(await manager.FindByIdAsync(user.Id));
        }

        [Fact]
        public async Task CanUpdateUserName()
        {
            var manager = CreateManager();
            var user = new TUser() { UserName = "UpdateAsync" };
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            Assert.Null(await manager.FindByNameAsync("New"));
            user.UserName = "New";
            IdentityResultAssert.IsSuccess(await manager.UpdateAsync(user));
            Assert.NotNull(await manager.FindByNameAsync("New"));
            Assert.Null(await manager.FindByNameAsync("UpdateAsync"));
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
            user.PasswordHash = manager.PasswordHasher.HashPassword("New");
            IdentityResultAssert.IsSuccess(await manager.UpdateAsync(user));
            Assert.False(await manager.CheckPasswordAsync(user, "password"));
            Assert.True(await manager.CheckPasswordAsync(user, "New"));
        }

        [Fact]
        public async Task CanFindById()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            Assert.NotNull(await manager.FindByIdAsync(user.Id));
        }

        [Fact]
        public async Task UserValidatorCanBlockCreate()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            manager.UserValidator = new AlwaysBadValidator();
            IdentityResultAssert.IsFailure(await manager.CreateAsync(user), AlwaysBadValidator.ErrorMessage);
        }

        [Fact]
        public async Task UserValidatorCanBlockUpdate()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            manager.UserValidator = new AlwaysBadValidator();
            IdentityResultAssert.IsFailure(await manager.UpdateAsync(user), AlwaysBadValidator.ErrorMessage);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task UserValidatorBlocksShortEmailsWhenRequiresUniqueEmail(string email)
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            manager.Options.User.RequireUniqueEmail = true;
            IdentityResultAssert.IsFailure(await manager.CreateAsync(user), "Email cannot be null or empty.");
        }

#if NET45
        [Theory]
        [InlineData("@@afd")]
        [InlineData("bogus")]
        public async Task UserValidatorBlocksInvalidEmailsWhenRequiresUniqueEmail(string email)
        {
            var manager = CreateManager();
            var user = new TUser() { UserName = "UpdateBlocked", Email = email };
            manager.Options.User.RequireUniqueEmail = true;
            IdentityResultAssert.IsFailure(await manager.CreateAsync(user), "Email '" + email + "' is invalid.");
        }
#endif

        [Fact]
        public async Task PasswordValidatorCanBlockAddPassword()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            manager.PasswordValidator = new AlwaysBadValidator();
            IdentityResultAssert.IsFailure(await manager.AddPasswordAsync(user, "password"),
                AlwaysBadValidator.ErrorMessage);
        }

        [Fact]
        public async Task PasswordValidatorCanBlockChangePassword()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, "password"));
            manager.PasswordValidator = new AlwaysBadValidator();
            IdentityResultAssert.IsFailure(await manager.ChangePasswordAsync(user, "password", "new"),
                AlwaysBadValidator.ErrorMessage);
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
            const string userName = "CreateExternalUserTest";
            const string provider = "ZzAuth";
            const string providerKey = "HaoKey";
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(new TUser() { UserName = userName }));
            var user = await manager.FindByNameAsync(userName);
            var login = new UserLoginInfo(provider, providerKey);
            IdentityResultAssert.IsSuccess(await manager.AddLoginAsync(user, login));
            var logins = await manager.GetLoginsAsync(user);
            Assert.NotNull(logins);
            Assert.Equal(1, logins.Count());
            Assert.Equal(provider, logins.First().LoginProvider);
            Assert.Equal(providerKey, logins.First().ProviderKey);
        }

        [Fact]
        public async Task CanCreateUserLoginAndAddPassword()
        {
            var manager = CreateManager();
            var login = new UserLoginInfo("Provider", "key");
            var user = new TUser() { UserName = "CreateUserLoginAddPasswordTest" };
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await manager.AddLoginAsync(user, login));
            Assert.False(await manager.HasPasswordAsync(user));
            IdentityResultAssert.IsSuccess(await manager.AddPasswordAsync(user, "password"));
            Assert.True(await manager.HasPasswordAsync(user));
            var logins = await manager.GetLoginsAsync(user);
            Assert.NotNull(logins);
            Assert.Equal(1, logins.Count());
            Assert.Equal(user, await manager.FindByLoginAsync(login));
            Assert.Equal(user, await manager.FindByUserNamePasswordAsync(user.UserName, "password"));
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
        }

        [Fact]
        public async Task CanCreateUserAddRemoveLogin()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            var login = new UserLoginInfo("Provider", "key");
            var result = await manager.CreateAsync(user);
            Assert.NotNull(user);
            IdentityResultAssert.IsSuccess(result);
            IdentityResultAssert.IsSuccess(await manager.AddLoginAsync(user, login));
            Assert.Equal(user, await manager.FindByLoginAsync(login));
            var logins = await manager.GetLoginsAsync(user);
            Assert.NotNull(logins);
            Assert.Equal(1, logins.Count());
            Assert.Equal(login.LoginProvider, logins.Last().LoginProvider);
            Assert.Equal(login.ProviderKey, logins.Last().ProviderKey);
            var stamp = user.SecurityStamp;
            IdentityResultAssert.IsSuccess(await manager.RemoveLoginAsync(user, login));
            Assert.Null(await manager.FindByLoginAsync(login));
            logins = await manager.GetLoginsAsync(user);
            Assert.NotNull(logins);
            Assert.Equal(0, logins.Count());
            Assert.NotEqual(stamp, user.SecurityStamp);
        }

        [Fact]
        public async Task CanRemovePassword()
        {
            var manager = CreateManager();
            var user = new TUser() { UserName = "RemovePasswordTest" };
            const string password = "password";
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, password));
            var stamp = user.SecurityStamp;
            IdentityResultAssert.IsSuccess(await manager.RemovePasswordAsync(user));
            var u = await manager.FindByNameAsync(user.UserName);
            Assert.NotNull(u);
            Assert.Null(u.PasswordHash);
            Assert.NotEqual(stamp, user.SecurityStamp);
        }

        [Fact]
        public async Task CanChangePassword()
        {
            var manager = CreateManager();
            var user = new TUser() { UserName = "ChangePasswordTest" };
            const string password = "password";
            const string newPassword = "newpassword";
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, password));
            var stamp = user.SecurityStamp;
            Assert.NotNull(stamp);
            IdentityResultAssert.IsSuccess(await manager.ChangePasswordAsync(user, password, newPassword));
            Assert.Null(await manager.FindByUserNamePasswordAsync(user.UserName, password));
            Assert.Equal(user, await manager.FindByUserNamePasswordAsync(user.UserName, newPassword));
            Assert.NotEqual(stamp, user.SecurityStamp);
        }

        [Fact]
        public async Task CanAddRemoveUserClaim()
        {
            var manager = CreateManager();
            var user = new TUser() { UserName = "ClaimsAddRemove" };
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            Claim[] claims = { new Claim("c", "v"), new Claim("c2", "v2"), new Claim("c2", "v3") };
            foreach (Claim c in claims)
            {
                IdentityResultAssert.IsSuccess(await manager.AddClaimAsync(user, c));
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
        }

        [Fact]
        public async Task ChangePasswordFallsIfPasswordWrong()
        {
            var manager = CreateManager();
            var user = new TUser() { UserName = "user" };
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, "password"));
            var result = await manager.ChangePasswordAsync(user, "bogus", "newpassword");
            IdentityResultAssert.IsFailure(result, "Incorrect password.");
        }

        [Fact]
        public async Task AddDupeUserNameFails()
        {
            var manager = CreateManager();
            var user = new TUser() { UserName = "dupe" };
            var user2 = new TUser() { UserName = "dupe" };
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsFailure(await manager.CreateAsync(user2), "Name dupe is already taken.");
        }

        [Fact]
        public async Task AddDupeEmailAllowedByDefault()
        {
            var manager = CreateManager();
            var user = new TUser() { UserName = "dupe", Email = "yup@yup.com" };
            var user2 = new TUser() { UserName = "dupeEmail", Email = "yup@yup.com" };
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user2));
        }

        [Fact]
        public async Task AddDupeEmailFallsWhenUniqueEmailRequired()
        {
            var manager = CreateManager();
            manager.Options.User.RequireUniqueEmail = true;
            var user = new TUser() { UserName = "dupe", Email = "yup@yup.com" };
            var user2 = new TUser() { UserName = "dupeEmail", Email = "yup@yup.com" };
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsFailure(await manager.CreateAsync(user2), "Email 'yup@yup.com' is already taken.");
        }

        [Fact]
        public async Task UpdateSecurityStampActuallyChanges()
        {
            var manager = CreateManager();
            var user = new TUser() { UserName = "stampMe" };
            Assert.Null(user.SecurityStamp);
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var stamp = user.SecurityStamp;
            Assert.NotNull(stamp);
            IdentityResultAssert.IsSuccess(await manager.UpdateSecurityStampAsync(user));
            Assert.NotEqual(stamp, user.SecurityStamp);
        }

        [Fact]
        public async Task AddDupeLoginFails()
        {
            var manager = CreateManager();
            var user = new TUser() { UserName = "DupeLogin" };
            var login = new UserLoginInfo("provder", "key");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await manager.AddLoginAsync(user, login));
            var result = await manager.AddLoginAsync(user, login);
            IdentityResultAssert.IsFailure(result, "A user with that external login already exists.");
        }

        // Email tests
        [Fact]
        public async Task CanFindByEmail()
        {
            var manager = CreateManager();
            const string userName = "EmailTest";
            const string email = "email@test.com";
            var user = new TUser() { UserName = userName, Email = email };
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var fetch = await manager.FindByEmailAsync(email);
            Assert.Equal(user, fetch);
        }

        [Fact]
        public async Task CanFindUsersViaUserQuerable()
        {
            var mgr = CreateManager();
            var users = new[]
            {
                new TUser() { UserName = "user1" },
                new TUser() { UserName = "user2" },
                new TUser() { UserName = "user3" }
            };
            foreach (TUser u in users)
            {
                IdentityResultAssert.IsSuccess(await mgr.CreateAsync(u));
            }
            var usersQ = mgr.Users;
            Assert.Equal(3, usersQ.Count());
            Assert.NotNull(usersQ.FirstOrDefault(u => u.UserName == "user1"));
            Assert.NotNull(usersQ.FirstOrDefault(u => u.UserName == "user2"));
            Assert.NotNull(usersQ.FirstOrDefault(u => u.UserName == "user3"));
            Assert.Null(usersQ.FirstOrDefault(u => u.UserName == "bogus"));
        }

        [Fact]
        public async Task ClaimsIdentityCreatesExpectedClaims()
        {
            var manager = CreateManager();
            var role = CreateRoleManager();
            var user = new TUser() { UserName = "Hao" };
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await role.CreateAsync(CreateRole("Admin")));
            IdentityResultAssert.IsSuccess(await role.CreateAsync(CreateRole("Local")));
            IdentityResultAssert.IsSuccess(await manager.AddToRoleAsync(user, "Admin"));
            IdentityResultAssert.IsSuccess(await manager.AddToRoleAsync(user, "Local"));
            Claim[] userClaims =
            {
                new Claim("Whatever", "Value"),
                new Claim("Whatever2", "Value2")
            };
            foreach (var c in userClaims)
            {
                IdentityResultAssert.IsSuccess(await manager.AddClaimAsync(user, c));
            }

            var claimsFactory = new ClaimsIdentityFactory<TUser, TRole>(manager, role);
            var identity = await claimsFactory.CreateAsync(user, "test");
            var claims = identity.Claims.ToList();
            Assert.NotNull(claims);
            Assert.True(
                claims.Any(c => c.Type == manager.Options.ClaimType.UserName && c.Value == user.UserName));
            Assert.True(claims.Any(c => c.Type == manager.Options.ClaimType.UserId && c.Value == user.Id));
            Assert.True(claims.Any(c => c.Type == manager.Options.ClaimType.Role && c.Value == "Admin"));
            Assert.True(claims.Any(c => c.Type == manager.Options.ClaimType.Role && c.Value == "Local"));
            foreach (var cl in userClaims)
            {
                Assert.True(claims.Any(c => c.Type == cl.Type && c.Value == cl.Value));
            }
        }

        [Fact]
        public async Task ConfirmEmailFalseByDefaultTest()
        {
            var manager = CreateManager();
            var user = new TUser() { UserName = "test" };
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            Assert.False(await manager.IsEmailConfirmedAsync(user));
        }

        // TODO: No token provider implementations yet
        private class StaticTokenProvider : IUserTokenProvider<TUser>
        {
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

            public Task<bool> IsValidProviderForUserAsync(UserManager<TUser> manager, TUser user, CancellationToken cancellationToken = default(CancellationToken))
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
            manager.UserTokenProvider = new StaticTokenProvider();
            var user = new TUser() { UserName = "ResetPasswordTest" };
            const string password = "password";
            const string newPassword = "newpassword";
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, password));
            var stamp = user.SecurityStamp;
            Assert.NotNull(stamp);
            var token = await manager.GeneratePasswordResetTokenAsync(user);
            Assert.NotNull(token);
            IdentityResultAssert.IsSuccess(await manager.ResetPasswordAsync(user, token, newPassword));
            Assert.Null(await manager.FindByUserNamePasswordAsync(user.UserName, password));
            Assert.Equal(user, await manager.FindByUserNamePasswordAsync(user.UserName, newPassword));
            Assert.NotEqual(stamp, user.SecurityStamp);
        }

        [Fact]
        public async Task PasswordValidatorCanBlockResetPasswordWithStaticTokenProvider()
        {
            var manager = CreateManager();
            manager.UserTokenProvider = new StaticTokenProvider();
            var user = new TUser() { UserName = "ResetPasswordTest" };
            const string password = "password";
            const string newPassword = "newpassword";
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, password));
            var stamp = user.SecurityStamp;
            Assert.NotNull(stamp);
            var token = await manager.GeneratePasswordResetTokenAsync(user);
            Assert.NotNull(token);
            manager.PasswordValidator = new AlwaysBadValidator();
            IdentityResultAssert.IsFailure(await manager.ResetPasswordAsync(user, token, newPassword),
                AlwaysBadValidator.ErrorMessage);
            Assert.NotNull(await manager.FindByUserNamePasswordAsync(user.UserName, password));
            Assert.Equal(user, await manager.FindByUserNamePasswordAsync(user.UserName, password));
            Assert.Equal(stamp, user.SecurityStamp);
        }

        [Fact]
        public async Task ResetPasswordWithStaticTokenProviderFailsWithWrongToken()
        {
            var manager = CreateManager();
            manager.UserTokenProvider = new StaticTokenProvider();
            var user = new TUser() { UserName = "ResetPasswordTest" };
            const string password = "password";
            const string newPassword = "newpassword";
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, password));
            var stamp = user.SecurityStamp;
            Assert.NotNull(stamp);
            IdentityResultAssert.IsFailure(await manager.ResetPasswordAsync(user, "bogus", newPassword), "Invalid token.");
            Assert.NotNull(await manager.FindByUserNamePasswordAsync(user.UserName, password));
            Assert.Equal(user, await manager.FindByUserNamePasswordAsync(user.UserName, password));
            Assert.Equal(stamp, user.SecurityStamp);
        }

        [Fact]
        public async Task CanGenerateAndVerifyUserTokenWithStaticTokenProvider()
        {
            var manager = CreateManager();
            manager.UserTokenProvider = new StaticTokenProvider();
            var user = new TUser() { UserName = "UserTokenTest" };
            var user2 = new TUser() { UserName = "UserTokenTest2" };
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user2));
            var token = await manager.GenerateUserTokenAsync("test", user);
            Assert.True(await manager.VerifyUserTokenAsync(user, "test", token));
            Assert.False(await manager.VerifyUserTokenAsync(user, "test2", token));
            Assert.False(await manager.VerifyUserTokenAsync(user, "test", token + "a"));
            Assert.False(await manager.VerifyUserTokenAsync(user2, "test", token));
        }

        [Fact]
        public async Task CanConfirmEmailWithStaticToken()
        {
            var manager = CreateManager();
            manager.UserTokenProvider = new StaticTokenProvider();
            var user = new TUser() { UserName = "test" };
            Assert.False(user.EmailConfirmed);
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var token = await manager.GenerateEmailConfirmationTokenAsync(user);
            Assert.NotNull(token);
            IdentityResultAssert.IsSuccess(await manager.ConfirmEmailAsync(user, token));
            Assert.True(await manager.IsEmailConfirmedAsync(user));
            IdentityResultAssert.IsSuccess(await manager.SetEmailAsync(user, null));
            Assert.False(await manager.IsEmailConfirmedAsync(user));
        }

        [Fact]
        public async Task ConfirmEmailWithStaticTokenFailsWithWrongToken()
        {
            var manager = CreateManager();
            manager.UserTokenProvider = new StaticTokenProvider();
            var user = new TUser() { UserName = "test" };
            Assert.False(user.EmailConfirmed);
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsFailure(await manager.ConfirmEmailAsync(user, "bogus"), "Invalid token.");
            Assert.False(await manager.IsEmailConfirmedAsync(user));
        }

        // TODO: Can't reenable til we have a SecurityStamp linked token provider
        //[Fact]
        //public async Task ConfirmTokenFailsAfterPasswordChange()
        //{
        //    var manager = CreateManager();
        //    var user = new TUser() { UserName = "test" };
        //    Assert.False(user.EmailConfirmed);
        //    IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, "password"));
        //    var token = await manager.GenerateEmailConfirmationTokenAsync(user);
        //    Assert.NotNull(token);
        //    IdentityResultAssert.IsSuccess(await manager.ChangePasswordAsync(user, "password", "newpassword"));
        //    IdentityResultAssert.IsFailure(await manager.ConfirmEmailAsync(user, token), "Invalid token.");
        //    Assert.False(await manager.IsEmailConfirmedAsync(user));
        //}

        // Lockout tests

        [Fact]
        public async Task SingleFailureLockout()
        {
            var mgr = CreateManager();
            mgr.Options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromHours(1);
            mgr.Options.Lockout.EnabledByDefault = true;
            mgr.Options.Lockout.MaxFailedAccessAttempts = 0;
            var user = new TUser() { UserName = "fastLockout" };
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
            var user = new TUser() { UserName = "twoFailureLockout" };
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
            var user = new TUser() { UserName = "LockoutUtcNowTest", LockoutEnd = DateTimeOffset.UtcNow.AddSeconds(-1) };
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
            var user = new TUser() { UserName = "LockoutUtcNowTest", LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(5) };
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
            public const string ErrorMessage = "I'm Bad.";

            public Task<IdentityResult> ValidateAsync(string password, UserManager<TUser> manager, CancellationToken cancellationToken = default(CancellationToken))
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
            manager.RoleValidator = new AlwaysBadValidator();
            IdentityResultAssert.IsFailure(await manager.CreateAsync(CreateRole("blocked")),
                AlwaysBadValidator.ErrorMessage);
        }

        [Fact]
        public async Task BadValidatorBlocksRoleUpdate()
        {
            var manager = CreateRoleManager();
            var role = CreateRole("poorguy");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
            var error = AlwaysBadValidator.ErrorMessage;
            manager.RoleValidator = new AlwaysBadValidator();
            IdentityResultAssert.IsFailure(await manager.UpdateAsync(role), error);
        }

        [Fact]
        public async Task CanDeleteRoleTest()
        {
            var manager = CreateRoleManager();
            var role = CreateRole("delete");
            Assert.False(await manager.RoleExistsAsync(role.Name));
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
            IdentityResultAssert.IsSuccess(await manager.DeleteAsync(role));
            Assert.False(await manager.RoleExistsAsync(role.Name));
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
        }

        [Fact]
        public async Task CanRoleFindByIdTest()
        {
            var manager = CreateRoleManager();
            var role = CreateRole("FindByIdAsync");
            Assert.Null(await manager.FindByIdAsync(role.Id));
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
            Assert.Equal(role, await manager.FindByIdAsync(role.Id));
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
        public async Task CanUpdateRoleNameTest()
        {
            var manager = CreateRoleManager();
            var role = CreateRole("update");
            Assert.False(await manager.RoleExistsAsync(role.Name));
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
            Assert.True(await manager.RoleExistsAsync(role.Name));
            role.Name = "Changed";
            IdentityResultAssert.IsSuccess(await manager.UpdateAsync(role));
            Assert.False(await manager.RoleExistsAsync("update"));
            Assert.Equal(role, await manager.FindByNameAsync(role.Name));
        }

        [Fact]
        public async Task CanQuerableRolesTest()
        {
            var manager = CreateRoleManager();
            TRole[] roles =
            {
                CreateRole("r1"), CreateRole("r2"), CreateRole("r3"),
                CreateRole("r4")
            };
            foreach (var r in roles)
            {
                IdentityResultAssert.IsSuccess(await manager.CreateAsync(r));
            }
            Assert.Equal(roles.Length, manager.Roles.Count());
            var r1 = manager.Roles.FirstOrDefault(r => r.Name == "r1");
            Assert.Equal(roles[0], r1);
        }

        //[Fact]
        //public async Task DeleteRoleNonEmptySucceedsTest()
        //{
        //    // Need fail if not empty?
        //    var userMgr = CreateManager();
        //    var roleMgr = CreateRoleManager();
        //    var role = CreateRole("deleteNonEmpty");
        //    Assert.False(await roleMgr.RoleExistsAsync(role.Name));
        //    IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
        //    var user = new TUser() { UserName = "t");
        //    IdentityResultAssert.IsSuccess(await userMgr.CreateAsync(user));
        //    IdentityResultAssert.IsSuccess(await userMgr.AddToRoleAsync(user, role.Name));
        //    IdentityResultAssert.IsSuccess(await roleMgr.DeleteAsync(role));
        //    Assert.Null(await roleMgr.FindByNameAsync(role.Name));
        //    Assert.False(await roleMgr.RoleExistsAsync(role.Name));
        //    // REVIEW: We should throw if deleteing a non empty role?
        //    var roles = await userMgr.GetRolesAsync(user);

        //    // In memory this doesn't work since there's no concept of cascading deletes
        //    //Assert.Equal(0, roles.Count());
        //}

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
            Assert.True(await manager.RoleExistsAsync(role.Name));
            var role2 = CreateRole("dupeRole");
            IdentityResultAssert.IsFailure(await manager.CreateAsync(role2));
        }

        [Fact]
        public async Task CanAddUsersToRole()
        {
            var manager = CreateManager();
            var roleManager = CreateRoleManager();
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
            }
        }

        [Fact]
        public async Task CanGetRolesForUser()
        {
            var userManager = CreateManager();
            var roleManager = CreateRoleManager();
            TUser[] users =
            {
                new TUser() { UserName = "1"}, new TUser() { UserName = "2"}, new TUser() { UserName = "3"},
                new TUser() { UserName = "4"}
            };
            TRole[] roles =
            {
                CreateRole("r1"), CreateRole("r2"), CreateRole("r3"),
                CreateRole("r4")
            };
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
                Assert.Equal(roles.Length, rs.Count);
                foreach (var r in roles)
                {
                    Assert.True(rs.Any(role => role == r.Name));
                }
            }
        }

        [Fact]
        public async Task RemoveUserFromRoleWithMultipleRoles()
        {
            var userManager = CreateManager();
            var roleManager = CreateRoleManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await userManager.CreateAsync(user));
            TRole[] roles =
            {
                CreateRole("r1"), CreateRole("r2"), CreateRole("r3"),
                CreateRole("r4")
            };
            foreach (var r in roles)
            {
                IdentityResultAssert.IsSuccess(await roleManager.CreateAsync(r));
                IdentityResultAssert.IsSuccess(await userManager.AddToRoleAsync(user, r.Name));
                Assert.True(await userManager.IsInRoleAsync(user, r.Name));
            }
            IdentityResultAssert.IsSuccess(await userManager.RemoveFromRoleAsync(user, roles[2].Name));
            Assert.False(await userManager.IsInRoleAsync(user, roles[2].Name));
        }

        [Fact]
        public async Task CanRemoveUsersFromRole()
        {
            var userManager = CreateManager();
            var roleManager = CreateRoleManager();
            TUser[] users =
            {
                new TUser() { UserName = "1"}, new TUser() { UserName = "2"}, new TUser() { UserName = "3"},
                new TUser() { UserName = "4"}
            };
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
            var userMgr = CreateManager();
            var roleMgr = CreateRoleManager();
            var role = CreateRole("addUserDupeTest");
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await userMgr.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
            var result = await userMgr.RemoveFromRoleAsync(user, role.Name);
            IdentityResultAssert.IsFailure(result, "User is not in role.");
        }

        [Fact]
        public async Task AddUserToRoleFailsIfAlreadyInRole()
        {
            var userMgr = CreateManager();
            var roleMgr = CreateRoleManager();
            var role = CreateRole("addUserDupeTest");
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await userMgr.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
            IdentityResultAssert.IsSuccess(await userMgr.AddToRoleAsync(user, role.Name));
            Assert.True(await userMgr.IsInRoleAsync(user, role.Name));
            IdentityResultAssert.IsFailure(await userMgr.AddToRoleAsync(user, role.Name), "User already in role.");
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
            Assert.Equal(role.Name, (await roleMgr.FindByIdAsync(role.Id)).Name);
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

#if NET45
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
            var token2 = await manager.GenerateChangePhoneNumberTokenAsync(user, num2);
            Assert.NotEqual(token1, token2);
            Assert.True(await manager.VerifyChangePhoneNumberTokenAsync(user, token1, num1));
            Assert.True(await manager.VerifyChangePhoneNumberTokenAsync(user, token2, num2));
            Assert.False(await manager.VerifyChangePhoneNumberTokenAsync(user, token2, num1));
            Assert.False(await manager.VerifyChangePhoneNumberTokenAsync(user, token1, num2));
        }
#endif

        private class EmailTokenProvider : IUserTokenProvider<TUser>
        {
            public Task<string> GenerateAsync(string purpose, UserManager<TUser> manager, TUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(MakeToken(purpose));
            }

            public Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser> manager,
                TUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(token == MakeToken(purpose));
            }

            public Task NotifyAsync(string token, UserManager<TUser> manager, TUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return manager.SendEmailAsync(user, token, token);
            }

            public async Task<bool> IsValidProviderForUserAsync(UserManager<TUser> manager, TUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return !string.IsNullOrEmpty(await manager.GetEmailAsync(user));
            }

            private static string MakeToken(string purpose)
            {
                return "email:" + purpose;
            }
        }

        private class SmsTokenProvider : IUserTokenProvider<TUser>
        {
            public Task<string> GenerateAsync(string purpose, UserManager<TUser> manager, TUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(MakeToken(purpose));
            }

            public Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser> manager,
                TUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(token == MakeToken(purpose));
            }

            public Task NotifyAsync(string token, UserManager<TUser> manager, TUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return manager.SendSmsAsync(user, token, cancellationToken);
            }

            public async Task<bool> IsValidProviderForUserAsync(UserManager<TUser> manager, TUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return !string.IsNullOrEmpty(await manager.GetPhoneNumberAsync(user, cancellationToken));
            }

            private static string MakeToken(string purpose)
            {
                return "sms:" + purpose;
            }
        }

        [Fact]
        public async Task CanEmailTwoFactorToken()
        {
            var manager = CreateManager();
            var messageService = new TestMessageService();
            manager.EmailService = messageService;
            const string factorId = "EmailCode";
            manager.RegisterTwoFactorProvider(factorId, new EmailTokenProvider());
            var user = new TUser() { UserName = "EmailCodeTest", Email = "foo@foo.com" };
            const string password = "password";
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, password));
            var stamp = user.SecurityStamp;
            Assert.NotNull(stamp);
            var token = await manager.GenerateTwoFactorTokenAsync(user, factorId);
            Assert.NotNull(token);
            Assert.Null(messageService.Message);
            IdentityResultAssert.IsSuccess(await manager.NotifyTwoFactorTokenAsync(user, factorId, token));
            Assert.NotNull(messageService.Message);
            Assert.Equal(token, messageService.Message.Subject);
            Assert.Equal(token, messageService.Message.Body);
            Assert.True(await manager.VerifyTwoFactorTokenAsync(user, factorId, token));
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
                    "No IUserTwoFactorProvider for 'Bogus' is registered.");
        }


        //[Fact]
        //public async Task EmailTokenFactorWithFormatTest()
        //{
        //    var manager = CreateManager();
        //    var messageService = new TestMessageService();
        //    manager.EmailService = messageService;
        //    const string factorId = "EmailCode";
        //    manager.RegisterTwoFactorProvider(factorId, new EmailTokenProvider<TUser>
        //    {
        //        Subject = "Security Code",
        //        BodyFormat = "Your code is: {0}"
        //    });
        //    var user = new TUser() { UserName = "EmailCodeTest") { Email = "foo@foo.com" };
        //    const string password = "password";
        //    IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, password));
        //    var stamp = user.SecurityStamp;
        //    Assert.NotNull(stamp);
        //    var token = await manager.GenerateTwoFactorTokenAsync(user, factorId);
        //    Assert.NotNull(token);
        //    Assert.Null(messageService.Message);
        //    IdentityResultAssert.IsSuccess(await manager.NotifyTwoFactorTokenAsync(user, factorId, token));
        //    Assert.NotNull(messageService.Message);
        //    Assert.Equal("Security Code", messageService.Message.Subject);
        //    Assert.Equal("Your code is: " + token, messageService.Message.Body);
        //    Assert.True(await manager.VerifyTwoFactorTokenAsync(user, factorId, token));
        //}

        //[Fact]
        //public async Task EmailFactorFailsAfterSecurityStampChangeTest()
        //{
        //    var manager = CreateManager();
        //    const string factorId = "EmailCode";
        //    manager.RegisterTwoFactorProvider(factorId, new EmailTokenProvider<TUser>());
        //    var user = new TUser() { UserName = "EmailCodeTest") { Email = "foo@foo.com" };
        //    IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
        //    var stamp = user.SecurityStamp;
        //    Assert.NotNull(stamp);
        //    var token = await manager.GenerateTwoFactorTokenAsync(user, factorId);
        //    Assert.NotNull(token);
        //    IdentityResultAssert.IsSuccess(await manager.UpdateSecurityStampAsync(user));
        //    Assert.False(await manager.VerifyTwoFactorTokenAsync(user, factorId, token));
        //}

        [Fact]
        public async Task EnableTwoFactorChangesSecurityStamp()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var stamp = user.SecurityStamp;
            Assert.NotNull(stamp);
            IdentityResultAssert.IsSuccess(await manager.SetTwoFactorEnabledAsync(user, true));
            Assert.NotEqual(stamp, await manager.GetSecurityStampAsync(user));
            Assert.True(await manager.GetTwoFactorEnabledAsync(user));
        }

        [Fact]
        public async Task CanSendSms()
        {
            var manager = CreateManager();
            var messageService = new TestMessageService();
            manager.SmsService = messageService;
            var user = new TUser() { UserName = "SmsTest", PhoneNumber = "4251234567" };
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            await manager.SendSmsAsync(user, "Hi");
            Assert.NotNull(messageService.Message);
            Assert.Equal("Hi", messageService.Message.Body);
        }

        [Fact]
        public async Task CanSendEmail()
        {
            var manager = CreateManager();
            var messageService = new TestMessageService();
            manager.EmailService = messageService;
            var user = new TUser() { UserName = "EmailTest", Email = "foo@foo.com" };
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            await manager.SendEmailAsync(user, "Hi", "Body");
            Assert.NotNull(messageService.Message);
            Assert.Equal("Hi", messageService.Message.Subject);
            Assert.Equal("Body", messageService.Message.Body);
        }

        [Fact]
        public async Task CanSmsTwoFactorToken()
        {
            var manager = CreateManager();
            var messageService = new TestMessageService();
            manager.SmsService = messageService;
            const string factorId = "PhoneCode";
            manager.RegisterTwoFactorProvider(factorId, new SmsTokenProvider());
            var user = new TUser() { UserName = "PhoneCodeTest", PhoneNumber = "4251234567" };
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var stamp = user.SecurityStamp;
            Assert.NotNull(stamp);
            var token = await manager.GenerateTwoFactorTokenAsync(user, factorId);
            Assert.NotNull(token);
            Assert.Null(messageService.Message);
            IdentityResultAssert.IsSuccess(await manager.NotifyTwoFactorTokenAsync(user, factorId, token));
            Assert.NotNull(messageService.Message);
            Assert.Equal(token, messageService.Message.Body);
            Assert.True(await manager.VerifyTwoFactorTokenAsync(user, factorId, token));
        }

        //[Fact]
        //public async Task PhoneTokenFactorFormatTest()
        //{
        //    var manager = CreateManager();
        //    var messageService = new TestMessageService();
        //    manager.SmsService = messageService;
        //    const string factorId = "PhoneCode";
        //    manager.RegisterTwoFactorProvider(factorId, new PhoneNumberTokenProvider<TUser>
        //    {
        //        MessageFormat = "Your code is: {0}"
        //    });
        //    var user = new TUser() { UserName = "PhoneCodeTest", PhoneNumber = "4251234567" };
        //    IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
        //    var stamp = user.SecurityStamp;
        //    Assert.NotNull(stamp);
        //    var token = await manager.GenerateTwoFactorTokenAsync(user, factorId);
        //    Assert.NotNull(token);
        //    Assert.Null(messageService.Message);
        //    IdentityResultAssert.IsSuccess(await manager.NotifyTwoFactorTokenAsync(user, factorId, token));
        //    Assert.NotNull(messageService.Message);
        //    Assert.Equal("Your code is: " + token, messageService.Message.Body);
        //    Assert.True(await manager.VerifyTwoFactorTokenAsync(user, factorId, token));
        //}

        [Fact]
        public async Task GenerateTwoFactorWithUnknownFactorProviderWillThrow()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            const string error = "No IUserTwoFactorProvider for 'bogus' is registered.";
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
        public async Task GetValidTwoFactorTest()
        {
            var manager = CreateManager();
            manager.RegisterTwoFactorProvider("phone", new SmsTokenProvider());
            manager.RegisterTwoFactorProvider("email", new EmailTokenProvider());
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var factors = await manager.GetValidTwoFactorProvidersAsync(user);
            Assert.NotNull(factors);
            Assert.True(!factors.Any());
            IdentityResultAssert.IsSuccess(await manager.SetPhoneNumberAsync(user, "111-111-1111"));
            factors = await manager.GetValidTwoFactorProvidersAsync(user);
            Assert.NotNull(factors);
            Assert.True(factors.Count() == 1);
            Assert.Equal("phone", factors[0]);
            IdentityResultAssert.IsSuccess(await manager.SetEmailAsync(user, "test@test.com"));
            factors = await manager.GetValidTwoFactorProvidersAsync(user);
            Assert.NotNull(factors);
            Assert.True(factors.Count() == 2);
            IdentityResultAssert.IsSuccess(await manager.SetEmailAsync(user, null));
            factors = await manager.GetValidTwoFactorProvidersAsync(user);
            Assert.NotNull(factors);
            Assert.True(factors.Count() == 1);
            Assert.Equal("phone", factors[0]);
        }

        //[Fact]
        //public async Task PhoneFactorFailsAfterSecurityStampChangeTest()
        //{
        //    var manager = CreateManager();
        //    var factorId = "PhoneCode";
        //    manager.RegisterTwoFactorProvider(factorId, new PhoneNumberTokenProvider<TUser>());
        //    var user = new TUser() { UserName = "PhoneCodeTest");
        //    user.PhoneNumber = "4251234567";
        //    IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
        //    var stamp = user.SecurityStamp;
        //    Assert.NotNull(stamp);
        //    var token = await manager.GenerateTwoFactorTokenAsync(user, factorId);
        //    Assert.NotNull(token);
        //    IdentityResultAssert.IsSuccess(await manager.UpdateSecurityStampAsync(user));
        //    Assert.False(await manager.VerifyTwoFactorTokenAsync(user, factorId, token));
        //}

        [Fact]
        public async Task VerifyTokenFromWrongTokenProviderFails()
        {
            var manager = CreateManager();
            manager.RegisterTwoFactorProvider("PhoneCode", new SmsTokenProvider());
            manager.RegisterTwoFactorProvider("EmailCode", new EmailTokenProvider());
            var user = new TUser() { UserName = "WrongTokenProviderTest", PhoneNumber = "4251234567" };
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var token = await manager.GenerateTwoFactorTokenAsync(user, "PhoneCode");
            Assert.NotNull(token);
            Assert.False(await manager.VerifyTwoFactorTokenAsync(user, "EmailCode", token));
        }

        [Fact]
        public async Task VerifyWithWrongSmsTokenFails()
        {
            var manager = CreateManager();
            const string factorId = "PhoneCode";
            manager.RegisterTwoFactorProvider(factorId, new SmsTokenProvider());
            var user = new TUser() { UserName = "PhoneCodeTest", PhoneNumber = "4251234567" };
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            Assert.False(await manager.VerifyTwoFactorTokenAsync(user, factorId, "bogus"));
        }
    }
}