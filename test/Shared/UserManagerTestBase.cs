// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Testing;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Xunit;
using System.Linq.Expressions;

namespace Microsoft.AspNet.Identity.Test
{
    // Common functionality tests that all verifies user manager functionality regardless of store implementation
    public abstract class UserManagerTestBase<TUser, TRole> : UserManagerTestBase<TUser, TRole, string>
        where TUser : class
        where TRole : class
    { }

    public abstract class UserManagerTestBase<TUser, TRole, TKey>
        where TUser : class
        where TRole : class
        where TKey : IEquatable<TKey>
    {
        private readonly IdentityErrorDescriber _errorDescriber = new IdentityErrorDescriber();

        protected virtual void SetupIdentityServices(IServiceCollection services, object context = null)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddIdentity<TUser, TRole>().AddDefaultTokenProviders();
            AddUserStore(services, context);
            AddRoleStore(services, context);
            services.AddLogging();
            services.AddInstance<ILogger<UserManager<TUser>>>(new TestLogger<UserManager<TUser>>());
            services.AddInstance<ILogger<RoleManager<TRole>>>(new TestLogger<RoleManager<TRole>>());
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

        protected abstract void SetUserPasswordHash(TUser user, string hashedPassword);

        protected abstract TUser CreateTestUser(string namePrefix = "", string email = "", string phoneNumber = "",
            bool lockoutEnabled = false, DateTimeOffset? lockoutEnd = null, bool useNamePrefixAsUserName = false);

        protected abstract TRole CreateTestRole(string roleNamePrefix = "", bool useRoleNamePrefixAsRoleName = false);

        protected abstract Expression<Func<TUser, bool>> UserNameEqualsPredicate(string userName);
        protected abstract Expression<Func<TUser, bool>> UserNameStartsWithPredicate(string userName);

        protected abstract Expression<Func<TRole, bool>> RoleNameEqualsPredicate(string roleName);
        protected abstract Expression<Func<TRole, bool>> RoleNameStartsWithPredicate(string roleName);

        [Fact]
        public async Task CanDeleteUser()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var userId = await manager.GetUserIdAsync(user);
            IdentityResultAssert.IsSuccess(await manager.DeleteAsync(user));
            Assert.Null(await manager.FindByIdAsync(userId));
        }

        [Fact]
        public async Task CanUpdateUserName()
        {
            var manager = CreateManager();
            var name = Guid.NewGuid().ToString();
            var user = CreateTestUser(name);
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var newName = Guid.NewGuid().ToString();
            Assert.Null(await manager.FindByNameAsync(newName));
            IdentityResultAssert.IsSuccess(await manager.SetUserNameAsync(user, newName));
            IdentityResultAssert.IsSuccess(await manager.UpdateAsync(user));
            Assert.NotNull(await manager.FindByNameAsync(newName));
            Assert.Null(await manager.FindByNameAsync(name));
        }

        [Fact]
        public async Task CheckSetUserNameValidatesUser()
        {
            var manager = CreateManager();
            var username = "UpdateAsync" + Guid.NewGuid().ToString();
            var newUsername = "New" + Guid.NewGuid().ToString();
            var user = CreateTestUser(username, useNamePrefixAsUserName: true);
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            Assert.Null(await manager.FindByNameAsync(newUsername));
            IdentityResultAssert.IsSuccess(await manager.SetUserNameAsync(user, newUsername));
            Assert.NotNull(await manager.FindByNameAsync(newUsername));
            Assert.Null(await manager.FindByNameAsync(username));

            var newUser = CreateTestUser(username, useNamePrefixAsUserName: true);
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(newUser));
            var error = _errorDescriber.InvalidUserName("");
            IdentityResultAssert.IsFailure(await manager.SetUserNameAsync(newUser, ""), error);
            IdentityResultAssert.VerifyLogMessage(manager.Logger, $"User {await manager.GetUserIdAsync(newUser)} validation failed: {error.Code}.");

            error = _errorDescriber.DuplicateUserName(newUsername);
            IdentityResultAssert.IsFailure(await manager.SetUserNameAsync(newUser, newUsername), error);
            IdentityResultAssert.VerifyLogMessage(manager.Logger, $"User {await manager.GetUserIdAsync(newUser)} validation failed: {error.Code}.");
        }

        [Fact]
        public async Task SetUserNameUpdatesSecurityStamp()
        {
            var manager = CreateManager();
            var username = "UpdateAsync" + Guid.NewGuid().ToString();
            var newUsername = "New" + Guid.NewGuid().ToString();
            var user = CreateTestUser(username, useNamePrefixAsUserName: true);
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var stamp = await manager.GetSecurityStampAsync(user);
            Assert.Null(await manager.FindByNameAsync(newUsername));
            IdentityResultAssert.IsSuccess(await manager.SetUserNameAsync(user, newUsername));
            Assert.NotEqual(stamp, await manager.GetSecurityStampAsync(user));
        }

        [Fact]
        public async Task CheckSetEmailValidatesUser()
        {
            var manager = CreateManager();
            manager.Options.User.RequireUniqueEmail = true;
            manager.UserValidators.Add(new UserValidator<TUser>());
            var random = new Random();
            var email = "foo" + random.Next() + "@example.com";
            var newEmail = "bar" + random.Next() + "@example.com";
            var user = CreateTestUser(email: email);
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await manager.SetEmailAsync(user, newEmail));

            var newUser = CreateTestUser(email: email);
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(newUser));
            IdentityResultAssert.IsFailure(await manager.SetEmailAsync(newUser, newEmail), _errorDescriber.DuplicateEmail(newEmail));
            IdentityResultAssert.IsFailure(await manager.SetEmailAsync(newUser, ""), _errorDescriber.InvalidEmail(""));
        }

        [Fact]
        public async Task CanUpdatePasswordUsingHasher()
        {
            var manager = CreateManager();
            var user = CreateTestUser("UpdatePassword");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, "password"));
            Assert.True(await manager.CheckPasswordAsync(user, "password"));
            var userId = await manager.GetUserIdAsync(user);

            SetUserPasswordHash(user, manager.PasswordHasher.HashPassword(user, "New"));
            IdentityResultAssert.IsSuccess(await manager.UpdateAsync(user));
            Assert.False(await manager.CheckPasswordAsync(user, "password"));
            IdentityResultAssert.VerifyLogMessage(manager.Logger, $"Invalid password for user {await manager.GetUserIdAsync(user)}.");
            Assert.True(await manager.CheckPasswordAsync(user, "New"));
        }

        [Fact]
        public async Task CanFindById()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            Assert.NotNull(await manager.FindByIdAsync(await manager.GetUserIdAsync(user)));
        }

        [Fact]
        public async Task UserValidatorCanBlockCreate()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            manager.UserValidators.Clear();
            manager.UserValidators.Add(new AlwaysBadValidator());
            IdentityResultAssert.IsFailure(await manager.CreateAsync(user), AlwaysBadValidator.ErrorMessage);
            IdentityResultAssert.VerifyLogMessage(manager.Logger, $"User {await manager.GetUserIdAsync(user)} validation failed: {AlwaysBadValidator.ErrorMessage.Code}.");
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
            IdentityResultAssert.VerifyLogMessage(manager.Logger, $"User {await manager.GetUserIdAsync(user)} validation failed: {AlwaysBadValidator.ErrorMessage.Code}.");
        }

        [Fact]
        public async Task CanChainUserValidators()
        {
            var manager = CreateManager();
            manager.UserValidators.Clear();
            var user = CreateTestUser();
            manager.UserValidators.Add(new AlwaysBadValidator());
            manager.UserValidators.Add(new AlwaysBadValidator());
            var result = await manager.CreateAsync(user);
            IdentityResultAssert.IsFailure(result, AlwaysBadValidator.ErrorMessage);
            IdentityResultAssert.VerifyLogMessage(manager.Logger, $"User {await manager.GetUserIdAsync(user)} validation failed: {AlwaysBadValidator.ErrorMessage.Code};{AlwaysBadValidator.ErrorMessage.Code}.");
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
            IdentityResultAssert.IsFailure(await manager.CreateAsync(user), _errorDescriber.InvalidEmail(email));
        }

#if DNX451
        [Theory]
        [InlineData("@@afd")]
        [InlineData("bogus")]
        public async Task UserValidatorBlocksInvalidEmailsWhenRequiresUniqueEmail(string email)
        {
            var manager = CreateManager();
            var user = CreateTestUser("UpdateBlocked", email);
            manager.Options.User.RequireUniqueEmail = true;
            IdentityResultAssert.IsFailure(await manager.CreateAsync(user), _errorDescriber.InvalidEmail(email));
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
            IdentityResultAssert.VerifyLogMessage(manager.Logger, $"User {await manager.GetUserIdAsync(user)} password validation failed: {AlwaysBadValidator.ErrorMessage.Code}.");
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
            IdentityResultAssert.VerifyLogMessage(manager.Logger, $"User {await manager.GetUserIdAsync(user)} password validation failed: {AlwaysBadValidator.ErrorMessage.Code}.");
        }

        [Fact]
        public async Task PasswordValidatorCanBlockCreateUser()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            manager.PasswordValidators.Clear();
            manager.PasswordValidators.Add(new AlwaysBadValidator());
            IdentityResultAssert.IsFailure(await manager.CreateAsync(user, "password"), AlwaysBadValidator.ErrorMessage);
            IdentityResultAssert.VerifyLogMessage(manager.Logger, $"User {await manager.GetUserIdAsync(user)} password validation failed: {AlwaysBadValidator.ErrorMessage.Code}.");
        }

        [Fact]
        public async Task CanCreateUserNoPassword()
        {
            var manager = CreateManager();
            var username = "CreateUserTest" + Guid.NewGuid();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(CreateTestUser(username, useNamePrefixAsUserName: true)));
            var user = await manager.FindByNameAsync(username);
            Assert.NotNull(user);
            Assert.False(await manager.HasPasswordAsync(user));
            Assert.False(await manager.CheckPasswordAsync(user, "whatever"));
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
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var providerKey = await manager.GetUserIdAsync(user);
            IdentityResultAssert.IsSuccess(await manager.AddLoginAsync(user, new UserLoginInfo(provider, providerKey, display)));
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
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var userId = await manager.GetUserIdAsync(user);
            var login = new UserLoginInfo("Provider", userId, "display");
            IdentityResultAssert.IsSuccess(await manager.AddLoginAsync(user, login));
            Assert.False(await manager.HasPasswordAsync(user));
            IdentityResultAssert.IsSuccess(await manager.AddPasswordAsync(user, "password"));
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
            IdentityResultAssert.VerifyLogMessage(manager.Logger, $"User {await manager.GetUserIdAsync(user)} already has a password.");
        }

        [Fact]
        public async Task CanCreateUserAddRemoveLogin()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            var result = await manager.CreateAsync(user);
            Assert.NotNull(user);
            var userId = await manager.GetUserIdAsync(user);
            var login = new UserLoginInfo("Provider", userId, "display");
            IdentityResultAssert.IsSuccess(result);
            IdentityResultAssert.IsSuccess(await manager.AddLoginAsync(user, login));
            Assert.Equal(user, await manager.FindByLoginAsync(login.LoginProvider, login.ProviderKey));
            var logins = await manager.GetLoginsAsync(user);
            Assert.NotNull(logins);
            Assert.Equal(1, logins.Count());
            Assert.Equal(login.LoginProvider, logins.Last().LoginProvider);
            Assert.Equal(login.ProviderKey, logins.Last().ProviderKey);
            Assert.Equal(login.ProviderDisplayName, logins.Last().ProviderDisplayName);
            var stamp = await manager.GetSecurityStampAsync(user);
            IdentityResultAssert.IsSuccess(await manager.RemoveLoginAsync(user, login.LoginProvider, login.ProviderKey));
            Assert.Null(await manager.FindByLoginAsync(login.LoginProvider, login.ProviderKey));
            logins = await manager.GetLoginsAsync(user);
            Assert.NotNull(logins);
            Assert.Equal(0, logins.Count());
            Assert.NotEqual(stamp, await manager.GetSecurityStampAsync(user));
        }

        [Fact]
        public async Task CanRemovePassword()
        {
            var manager = CreateManager();
            var user = CreateTestUser("CanRemovePassword");
            const string password = "password";
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, password));
            var stamp = await manager.GetSecurityStampAsync(user);
            var username = await manager.GetUserNameAsync(user);
            IdentityResultAssert.IsSuccess(await manager.RemovePasswordAsync(user));
            var u = await manager.FindByNameAsync(username);
            Assert.NotNull(u);
            Assert.False(await manager.HasPasswordAsync(user));
            Assert.NotEqual(stamp, await manager.GetSecurityStampAsync(user));
        }

        [Fact]
        public async Task CanChangePassword()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            const string password = "password";
            const string newPassword = "newpassword";
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, password));
            var stamp = await manager.GetSecurityStampAsync(user);
            Assert.NotNull(stamp);
            IdentityResultAssert.IsSuccess(await manager.ChangePasswordAsync(user, password, newPassword));
            Assert.False(await manager.CheckPasswordAsync(user, password));
            Assert.True(await manager.CheckPasswordAsync(user, newPassword));
            Assert.NotEqual(stamp, await manager.GetSecurityStampAsync(user));
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
            var userId = await manager.GetUserIdAsync(user);
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
            IdentityResultAssert.VerifyLogMessage(manager.Logger, $"Change password failed for user {await manager.GetUserIdAsync(user)}.");
        }

        [Fact]
        public async Task AddDupeUserNameFails()
        {
            var manager = CreateManager();
            var username = "AddDupeUserNameFails" + Guid.NewGuid();
            var user = CreateTestUser(username, useNamePrefixAsUserName: true);
            var user2 = CreateTestUser(username, useNamePrefixAsUserName: true);
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsFailure(await manager.CreateAsync(user2), _errorDescriber.DuplicateUserName(username));
        }

        [Fact]
        public async Task AddDupeEmailAllowedByDefault()
        {
            var manager = CreateManager();
            var user = CreateTestUser(email: "yup@yup.com");
            var user2 = CreateTestUser(email: "yup@yup.com");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user2));
            IdentityResultAssert.IsSuccess(await manager.SetEmailAsync(user2, await manager.GetEmailAsync(user)));
        }

        [Fact]
        public async Task AddDupeEmailFailsWhenUniqueEmailRequired()
        {
            var manager = CreateManager();
            manager.Options.User.RequireUniqueEmail = true;
            var user = CreateTestUser(email: "FooUser@yup.com");
            var user2 = CreateTestUser(email: "FooUser@yup.com");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsFailure(await manager.CreateAsync(user2), _errorDescriber.DuplicateEmail("FooUser@yup.com"));
        }

        [Fact]
        public async Task UpdateSecurityStampActuallyChanges()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            Assert.Null(await manager.GetSecurityStampAsync(user));
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var stamp = await manager.GetSecurityStampAsync(user);
            Assert.NotNull(stamp);
            IdentityResultAssert.IsSuccess(await manager.UpdateSecurityStampAsync(user));
            Assert.NotEqual(stamp, await manager.GetSecurityStampAsync(user));
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
            IdentityResultAssert.IsFailure(result, _errorDescriber.LoginAlreadyAssociated());
            IdentityResultAssert.VerifyLogMessage(manager.Logger, $"AddLogin for user {await manager.GetUserIdAsync(user)} failed because it was already assocated with another user.");
        }

        // Email tests
        [Fact]
        public async Task CanFindByEmail()
        {
            var email = "foouser@test.com";
            var manager = CreateManager();
            var user = CreateTestUser(email: email);
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var fetch = await manager.FindByEmailAsync(email);
            Assert.Equal(user, fetch);
        }

        [Fact]
        public async Task CanFindUsersViaUserQuerable()
        {
            var mgr = CreateManager();
            if (mgr.SupportsQueryableUsers)
            {
                var users = GenerateUsers("CanFindUsersViaUserQuerable", 4);
                foreach (var u in users)
                {
                    IdentityResultAssert.IsSuccess(await mgr.CreateAsync(u));
                }
                Assert.Equal(users.Count, mgr.Users.Count(UserNameStartsWithPredicate("CanFindUsersViaUserQuerable")));
                Assert.Null(mgr.Users.FirstOrDefault(UserNameEqualsPredicate("bogus")));
            }
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

            public async Task<string> GenerateAsync(string purpose, UserManager<TUser> manager, TUser user)
            {
                return MakeToken(purpose, await manager.GetUserIdAsync(user));
            }

            public async Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser> manager, TUser user)
            {
                return token == MakeToken(purpose, await manager.GetUserIdAsync(user));
            }

            public Task NotifyAsync(string token, UserManager<TUser> manager, TUser user)
            {
                return Task.FromResult(0);
            }

            public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> manager, TUser user)
            {
                return Task.FromResult(true);
            }

            private static string MakeToken(string purpose, string userId)
            {
                return string.Join(":", userId, purpose, "ImmaToken");
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
            var stamp = await manager.GetSecurityStampAsync(user);
            Assert.NotNull(stamp);
            var token = await manager.GeneratePasswordResetTokenAsync(user);
            Assert.NotNull(token);
            var userId = await manager.GetUserIdAsync(user);
            IdentityResultAssert.IsSuccess(await manager.ResetPasswordAsync(user, token, newPassword));
            Assert.False(await manager.CheckPasswordAsync(user, password));
            Assert.True(await manager.CheckPasswordAsync(user, newPassword));
            Assert.NotEqual(stamp, await manager.GetSecurityStampAsync(user));
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
            var stamp = await manager.GetSecurityStampAsync(user);
            Assert.NotNull(stamp);
            var token = await manager.GeneratePasswordResetTokenAsync(user);
            Assert.NotNull(token);
            manager.PasswordValidators.Add(new AlwaysBadValidator());
            IdentityResultAssert.IsFailure(await manager.ResetPasswordAsync(user, token, newPassword),
                AlwaysBadValidator.ErrorMessage);
            IdentityResultAssert.VerifyLogMessage(manager.Logger, $"User {await manager.GetUserIdAsync(user)} password validation failed: {AlwaysBadValidator.ErrorMessage.Code}.");
            Assert.True(await manager.CheckPasswordAsync(user, password));
            Assert.Equal(stamp, await manager.GetSecurityStampAsync(user));
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
            var stamp = await manager.GetSecurityStampAsync(user);
            Assert.NotNull(stamp);
            IdentityResultAssert.IsFailure(await manager.ResetPasswordAsync(user, "bogus", newPassword), "Invalid token.");
            IdentityResultAssert.VerifyLogMessage(manager.Logger, $"VerifyUserTokenAsync() failed with purpose: ResetPassword for user { await manager.GetUserIdAsync(user)}.");
            Assert.True(await manager.CheckPasswordAsync(user, password));
            Assert.Equal(stamp, await manager.GetSecurityStampAsync(user));
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
            var userId = await manager.GetUserIdAsync(user);
            var token = await manager.GenerateUserTokenAsync(user, "Static", "test");

            Assert.True(await manager.VerifyUserTokenAsync(user, "Static", "test", token));

            Assert.False(await manager.VerifyUserTokenAsync(user, "Static", "test2", token));
            IdentityResultAssert.VerifyLogMessage(manager.Logger, $"VerifyUserTokenAsync() failed with purpose: test2 for user { await manager.GetUserIdAsync(user)}.");

            Assert.False(await manager.VerifyUserTokenAsync(user, "Static", "test", token + "a"));
            IdentityResultAssert.VerifyLogMessage(manager.Logger, $"VerifyUserTokenAsync() failed with purpose: test for user { await manager.GetUserIdAsync(user)}.");

            Assert.False(await manager.VerifyUserTokenAsync(user2, "Static", "test", token));
            IdentityResultAssert.VerifyLogMessage(manager.Logger, $"VerifyUserTokenAsync() failed with purpose: test for user { await manager.GetUserIdAsync(user2)}.");
        }

        [Fact]
        public async Task CanConfirmEmailWithStaticToken()
        {
            var manager = CreateManager();
            manager.RegisterTokenProvider(new StaticTokenProvider());
            manager.Options.EmailConfirmationTokenProvider = "Static";
            var user = CreateTestUser();
            Assert.False(await manager.IsEmailConfirmedAsync(user));
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var token = await manager.GenerateEmailConfirmationTokenAsync(user);
            Assert.NotNull(token);
            var userId = await manager.GetUserIdAsync(user);
            IdentityResultAssert.IsSuccess(await manager.ConfirmEmailAsync(user, token));
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
            Assert.False(await manager.IsEmailConfirmedAsync(user));
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsFailure(await manager.ConfirmEmailAsync(user, "bogus"), "Invalid token.");
            Assert.False(await manager.IsEmailConfirmedAsync(user));
            IdentityResultAssert.VerifyLogMessage(manager.Logger, $"VerifyUserTokenAsync() failed with purpose: EmailConfirmation for user { await manager.GetUserIdAsync(user)}.");
        }

        [Fact]
        public async Task ConfirmTokenFailsAfterPasswordChange()
        {
            var manager = CreateManager();
            var user = CreateTestUser(namePrefix: "Test");
            Assert.False(await manager.IsEmailConfirmedAsync(user));
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, "password"));
            var token = await manager.GenerateEmailConfirmationTokenAsync(user);
            Assert.NotNull(token);
            IdentityResultAssert.IsSuccess(await manager.ChangePasswordAsync(user, "password", "newpassword"));
            IdentityResultAssert.IsFailure(await manager.ConfirmEmailAsync(user, token), "Invalid token.");
            IdentityResultAssert.VerifyLogMessage(manager.Logger, $"VerifyUserTokenAsync() failed with purpose: EmailConfirmation for user { await manager.GetUserIdAsync(user)}.");
            Assert.False(await manager.IsEmailConfirmedAsync(user));
        }

        // Lockout tests

        [Fact]
        public async Task SingleFailureLockout()
        {
            var mgr = CreateManager();
            mgr.Options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromHours(1);
            mgr.Options.Lockout.MaxFailedAccessAttempts = 0;
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
            Assert.True(await mgr.GetLockoutEnabledAsync(user));
            Assert.False(await mgr.IsLockedOutAsync(user));
            IdentityResultAssert.IsSuccess(await mgr.AccessFailedAsync(user));
            Assert.True(await mgr.IsLockedOutAsync(user));
            Assert.True(await mgr.GetLockoutEndDateAsync(user) > DateTimeOffset.UtcNow.AddMinutes(55));
            IdentityResultAssert.VerifyLogMessage(mgr.Logger, $"User {await mgr.GetUserIdAsync(user)} is locked out.");
            
            Assert.Equal(0, await mgr.GetAccessFailedCountAsync(user));
        }

        [Fact]
        public async Task TwoFailureLockout()
        {
            var mgr = CreateManager();
            mgr.Options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromHours(1);
            mgr.Options.Lockout.MaxFailedAccessAttempts = 2;
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
            Assert.True(await mgr.GetLockoutEnabledAsync(user));
            Assert.False(await mgr.IsLockedOutAsync(user));
            IdentityResultAssert.IsSuccess(await mgr.AccessFailedAsync(user));
            Assert.False(await mgr.IsLockedOutAsync(user));
            Assert.False(await mgr.GetLockoutEndDateAsync(user) > DateTimeOffset.UtcNow.AddMinutes(55));
            Assert.Equal(1, await mgr.GetAccessFailedCountAsync(user));
            IdentityResultAssert.IsSuccess(await mgr.AccessFailedAsync(user));
            Assert.True(await mgr.IsLockedOutAsync(user));
            Assert.True(await mgr.GetLockoutEndDateAsync(user) > DateTimeOffset.UtcNow.AddMinutes(55));
            IdentityResultAssert.VerifyLogMessage(mgr.Logger, $"User {await mgr.GetUserIdAsync(user)} is locked out.");
            Assert.Equal(0, await mgr.GetAccessFailedCountAsync(user));
        }

        [Fact]
        public async Task ResetAccessCountPreventsLockout()
        {
            var mgr = CreateManager();
            mgr.Options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromHours(1);
            mgr.Options.Lockout.MaxFailedAccessAttempts = 2;
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
            Assert.True(await mgr.GetLockoutEnabledAsync(user));
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
            mgr.Options.Lockout.AllowedForNewUsers = false;
            mgr.Options.Lockout.MaxFailedAccessAttempts = 2;
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
            Assert.False(await mgr.GetLockoutEnabledAsync(user));
            IdentityResultAssert.IsSuccess(await mgr.SetLockoutEnabledAsync(user, true));
            Assert.True(await mgr.GetLockoutEnabledAsync(user));
            Assert.False(await mgr.IsLockedOutAsync(user));
            IdentityResultAssert.IsSuccess(await mgr.AccessFailedAsync(user));
            Assert.False(await mgr.IsLockedOutAsync(user));
            Assert.False(await mgr.GetLockoutEndDateAsync(user) > DateTimeOffset.UtcNow.AddMinutes(55));
            Assert.Equal(1, await mgr.GetAccessFailedCountAsync(user));
            IdentityResultAssert.IsSuccess(await mgr.AccessFailedAsync(user));
            Assert.True(await mgr.IsLockedOutAsync(user));
            Assert.True(await mgr.GetLockoutEndDateAsync(user) > DateTimeOffset.UtcNow.AddMinutes(55));
            IdentityResultAssert.VerifyLogMessage(mgr.Logger, $"User {await mgr.GetUserIdAsync(user)} is locked out.");
            Assert.Equal(0, await mgr.GetAccessFailedCountAsync(user));
        }

        [Fact]
        public async Task UserNotLockedOutWithNullDateTimeAndIsSetToNullDate()
        {
            var mgr = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
            Assert.True(await mgr.GetLockoutEnabledAsync(user));
            IdentityResultAssert.IsSuccess(await mgr.SetLockoutEndDateAsync(user, new DateTimeOffset()));
            Assert.False(await mgr.IsLockedOutAsync(user));
            Assert.Equal(new DateTimeOffset(), await mgr.GetLockoutEndDateAsync(user));
        }

        [Fact]
        public async Task LockoutFailsIfNotEnabled()
        {
            var mgr = CreateManager();
            mgr.Options.Lockout.AllowedForNewUsers = false;
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
            Assert.False(await mgr.GetLockoutEnabledAsync(user));
            IdentityResultAssert.IsFailure(await mgr.SetLockoutEndDateAsync(user, new DateTimeOffset()),
                "Lockout is not enabled for this user.");
            IdentityResultAssert.VerifyLogMessage(mgr.Logger, $"Lockout for user {await mgr.GetUserIdAsync(user)} failed because lockout is not enabled for this user.");
            Assert.False(await mgr.IsLockedOutAsync(user));
        }

        [Fact]
        public async Task LockoutEndToUtcNowMinus1SecInUserShouldNotBeLockedOut()
        {
            var mgr = CreateManager();
            var user = CreateTestUser(lockoutEnd: DateTimeOffset.UtcNow.AddSeconds(-1));
            IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
            Assert.True(await mgr.GetLockoutEnabledAsync(user));
            Assert.False(await mgr.IsLockedOutAsync(user));
        }

        [Fact]
        public async Task LockoutEndToUtcNowSubOneSecondWithManagerShouldNotBeLockedOut()
        {
            var mgr = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
            Assert.True(await mgr.GetLockoutEnabledAsync(user));
            IdentityResultAssert.IsSuccess(await mgr.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddSeconds(-1)));
            Assert.False(await mgr.IsLockedOutAsync(user));
        }

        [Fact]
        public async Task LockoutEndToUtcNowPlus5ShouldBeLockedOut()
        {
            var mgr = CreateManager();
            var lockoutEnd = DateTimeOffset.UtcNow.AddMinutes(5);
            var user = CreateTestUser(lockoutEnd: lockoutEnd);
            IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
            Assert.True(await mgr.GetLockoutEnabledAsync(user));
            Assert.True(await mgr.IsLockedOutAsync(user));
        }

        [Fact]
        public async Task UserLockedOutWithDateTimeLocalKindNowPlus30()
        {
            var mgr = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
            Assert.True(await mgr.GetLockoutEnabledAsync(user));
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
            var roleName = "create" + Guid.NewGuid().ToString();
            var role = CreateTestRole(roleName, useRoleNamePrefixAsRoleName: true);
            Assert.False(await manager.RoleExistsAsync(roleName));
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
            Assert.True(await manager.RoleExistsAsync(roleName));
        }

        private class AlwaysBadValidator : IUserValidator<TUser>, IRoleValidator<TRole>,
            IPasswordValidator<TUser>
        {
            public static readonly IdentityError ErrorMessage = new IdentityError { Description = "I'm Bad.", Code = "BadValidator" };

            public Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user, string password)
            {
                return Task.FromResult(IdentityResult.Failed(ErrorMessage));
            }

            public Task<IdentityResult> ValidateAsync(RoleManager<TRole> manager, TRole role)
            {
                return Task.FromResult(IdentityResult.Failed(ErrorMessage));
            }

            public Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user)
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
            var role = CreateTestRole("blocked");
            IdentityResultAssert.IsFailure(await manager.CreateAsync(role),
                AlwaysBadValidator.ErrorMessage);
            IdentityResultAssert.VerifyLogMessage(manager.Logger, $"Role {await manager.GetRoleIdAsync(role)} validation failed: {AlwaysBadValidator.ErrorMessage.Code}.");
        }

        [Fact]
        public async Task CanChainRoleValidators()
        {
            var manager = CreateRoleManager();
            manager.RoleValidators.Clear();
            manager.RoleValidators.Add(new AlwaysBadValidator());
            manager.RoleValidators.Add(new AlwaysBadValidator());
            var role = CreateTestRole("blocked");
            var result = await manager.CreateAsync(role);
            IdentityResultAssert.IsFailure(result, AlwaysBadValidator.ErrorMessage);
            IdentityResultAssert.VerifyLogMessage(manager.Logger, $"Role {await manager.GetRoleIdAsync(role)} validation failed: {AlwaysBadValidator.ErrorMessage.Code};{AlwaysBadValidator.ErrorMessage.Code}.");
            Assert.Equal(2, result.Errors.Count());
        }

        [Fact]
        public async Task BadValidatorBlocksRoleUpdate()
        {
            var manager = CreateRoleManager();
            var role = CreateTestRole("poorguy");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
            var error = AlwaysBadValidator.ErrorMessage;
            manager.RoleValidators.Clear();
            manager.RoleValidators.Add(new AlwaysBadValidator());
            IdentityResultAssert.IsFailure(await manager.UpdateAsync(role), error);
            IdentityResultAssert.VerifyLogMessage(manager.Logger, $"Role {await manager.GetRoleIdAsync(role)} validation failed: {AlwaysBadValidator.ErrorMessage.Code}.");
        }

        [Fact]
        public async Task CanDeleteRole()
        {
            var manager = CreateRoleManager();
            var roleName = "delete" + Guid.NewGuid().ToString();
            var role = CreateTestRole(roleName, useRoleNamePrefixAsRoleName: true);
            Assert.False(await manager.RoleExistsAsync(roleName));
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
            Assert.True(await manager.RoleExistsAsync(roleName));
            IdentityResultAssert.IsSuccess(await manager.DeleteAsync(role));
            Assert.False(await manager.RoleExistsAsync(roleName));
        }

        [Fact]
        public async Task CanAddRemoveRoleClaim()
        {
            var manager = CreateRoleManager();
            var role = CreateTestRole("ClaimsAddRemove");
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
        public async Task CanRoleFindById()
        {
            var manager = CreateRoleManager();
            var role = CreateTestRole("FindByIdAsync");
            Assert.Null(await manager.FindByIdAsync(await manager.GetRoleIdAsync(role)));
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
            Assert.Equal(role, await manager.FindByIdAsync(await manager.GetRoleIdAsync(role)));
        }

        [Fact]
        public async Task CanRoleFindByName()
        {
            var manager = CreateRoleManager();
            var roleName = "FindByNameAsync" + Guid.NewGuid().ToString();
            var role = CreateTestRole(roleName, useRoleNamePrefixAsRoleName: true);
            Assert.Null(await manager.FindByNameAsync(roleName));
            Assert.False(await manager.RoleExistsAsync(roleName));
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
            Assert.Equal(role, await manager.FindByNameAsync(roleName));
        }

        [Fact]
        public async Task CanUpdateRoleName()
        {
            var manager = CreateRoleManager();
            var roleName = "update" + Guid.NewGuid().ToString();
            var role = CreateTestRole(roleName, useRoleNamePrefixAsRoleName: true);
            Assert.False(await manager.RoleExistsAsync(roleName));
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
            Assert.True(await manager.RoleExistsAsync(roleName));
            IdentityResultAssert.IsSuccess(await manager.SetRoleNameAsync(role, "Changed"));
            IdentityResultAssert.IsSuccess(await manager.UpdateAsync(role));
            Assert.False(await manager.RoleExistsAsync("update"));
            Assert.Equal(role, await manager.FindByNameAsync("Changed"));
        }

        [Fact]
        public async Task CanQueryableRoles()
        {
            var manager = CreateRoleManager();
            if (manager.SupportsQueryableRoles)
            {
                var roles = GenerateRoles("CanQuerableRolesTest", 4);
                foreach (var r in roles)
                {
                    IdentityResultAssert.IsSuccess(await manager.CreateAsync(r));
                }
                Assert.Equal(roles.Count, manager.Roles.Count(RoleNameStartsWithPredicate("CanQuerableRolesTest")));
                Assert.Null(manager.Roles.FirstOrDefault(RoleNameEqualsPredicate("bogus")));
            }
        }

        // Enable when delete on cascade is supported in EF
        // [Fact]
        public async Task DeleteRoleNonEmptySucceedsTest()
        {
            // Need fail if not empty?
            var context = CreateTestContext();
            var userMgr = CreateManager(context);
            var roleMgr = CreateRoleManager(context);
            var roleName = "delete" + Guid.NewGuid().ToString();
            var role = CreateTestRole(roleName, useRoleNamePrefixAsRoleName: true);
            Assert.False(await roleMgr.RoleExistsAsync(roleName));
            IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await userMgr.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await userMgr.AddToRoleAsync(user, roleName));
            var roles = await userMgr.GetRolesAsync(user);
            Assert.Equal(1, roles.Count());
            IdentityResultAssert.IsSuccess(await roleMgr.DeleteAsync(role));
            Assert.Null(await roleMgr.FindByNameAsync(roleName));
            Assert.False(await roleMgr.RoleExistsAsync(roleName));
            // REVIEW: We should throw if deleteing a non empty role?
            roles = await userMgr.GetRolesAsync(user);

            // REVIEW: This depends on cascading deletes
            Assert.Equal(0, roles.Count());
        }

        // TODO: cascading deletes?  navigation properties not working
        ////[Fact]
        ////public async Task DeleteUserRemovesFromRoleTest()
        ////{
        ////    // Need fail if not empty?
        ////    var userMgr = CreateManager();
        ////    var roleMgr = CreateRoleManager();
        ////    var role = CreateRole("deleteNonEmpty");
        ////    Assert.False(await roleMgr.RoleExistsAsync(roleName));
        ////    IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
        ////    var user = new TUser() { UserName = "t");
        ////    IdentityResultAssert.IsSuccess(await userMgr.CreateAsync(user));
        ////    IdentityResultAssert.IsSuccess(await userMgr.AddToRoleAsync(user, roleName));
        ////    IdentityResultAssert.IsSuccess(await userMgr.DeleteAsync(user));
        ////    role = roleMgr.FindByIdAsync(role.Id);
        ////}

        [Fact]
        public async Task CreateRoleFailsIfExists()
        {
            var manager = CreateRoleManager();
            var roleName = "dupeRole" + Guid.NewGuid().ToString();
            var role = CreateTestRole(roleName, useRoleNamePrefixAsRoleName: true);
            Assert.False(await manager.RoleExistsAsync(roleName));
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
            Assert.True(await manager.RoleExistsAsync(roleName));
            var role2 = CreateTestRole(roleName, useRoleNamePrefixAsRoleName: true);
            IdentityResultAssert.IsFailure(await manager.CreateAsync(role2));
        }

        [Fact]
        public async Task CanAddUsersToRole()
        {
            var context = CreateTestContext();
            var manager = CreateManager(context);
            var roleManager = CreateRoleManager(context);
            var roleName = "AddUserTest" + Guid.NewGuid().ToString();
            var role = CreateTestRole(roleName, useRoleNamePrefixAsRoleName: true);
            IdentityResultAssert.IsSuccess(await roleManager.CreateAsync(role));
            TUser[] users =
            {
                CreateTestUser("1"),CreateTestUser("2"),CreateTestUser("3"),CreateTestUser("4"),
            };
            foreach (var u in users)
            {
                IdentityResultAssert.IsSuccess(await manager.CreateAsync(u));
                IdentityResultAssert.IsSuccess(await manager.AddToRoleAsync(u, roleName));
                Assert.True(await manager.IsInRoleAsync(u, roleName));
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
                    IdentityResultAssert.IsSuccess(await userManager.AddToRoleAsync(u, await roleManager.GetRoleNameAsync(r)));
                    Assert.True(await userManager.IsInRoleAsync(u, await roleManager.GetRoleNameAsync(r)));
                }
            }

            foreach (var u in users)
            {
                var rs = await userManager.GetRolesAsync(u);
                Assert.Equal(roles.Count, rs.Count);
                foreach (var r in roles)
                {
                    var expectedRoleName = await roleManager.GetRoleNameAsync(r);
                    Assert.True(rs.Any(role => role == expectedRoleName));
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
                IdentityResultAssert.IsSuccess(await userManager.AddToRoleAsync(user, await roleManager.GetRoleNameAsync(r)));
                Assert.True(await userManager.IsInRoleAsync(user, await roleManager.GetRoleNameAsync(r)));
            }
            IdentityResultAssert.IsSuccess(await userManager.RemoveFromRoleAsync(user, await roleManager.GetRoleNameAsync(roles[2])));
            Assert.False(await userManager.IsInRoleAsync(user, await roleManager.GetRoleNameAsync(roles[2])));
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
            var r = CreateTestRole("r1");
            IdentityResultAssert.IsSuccess(await roleManager.CreateAsync(r));
            foreach (var u in users)
            {
                IdentityResultAssert.IsSuccess(await userManager.AddToRoleAsync(u, await roleManager.GetRoleNameAsync(r)));
                Assert.True(await userManager.IsInRoleAsync(u, await roleManager.GetRoleNameAsync(r)));
            }
            foreach (var u in users)
            {
                IdentityResultAssert.IsSuccess(await userManager.RemoveFromRoleAsync(u, await roleManager.GetRoleNameAsync(r)));
                Assert.False(await userManager.IsInRoleAsync(u, await roleManager.GetRoleNameAsync(r)));
            }
        }

        [Fact]
        public async Task RemoveUserNotInRoleFails()
        {
            var context = CreateTestContext();
            var userMgr = CreateManager(context);
            var roleMgr = CreateRoleManager(context);
            var roleName = "addUserDupeTest" + Guid.NewGuid().ToString();
            var role = CreateTestRole(roleName, useRoleNamePrefixAsRoleName: true);
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await userMgr.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
            var result = await userMgr.RemoveFromRoleAsync(user, roleName);
            IdentityResultAssert.IsFailure(result, _errorDescriber.UserNotInRole(roleName));
            IdentityResultAssert.VerifyLogMessage(userMgr.Logger, $"User {await userMgr.GetUserIdAsync(user)} is not in role {roleName}.");
        }

        [Fact]
        public async Task AddUserToRoleFailsIfAlreadyInRole()
        {
            var context = CreateTestContext();
            var userMgr = CreateManager(context);
            var roleMgr = CreateRoleManager(context);
            var roleName = "addUserDupeTest" + Guid.NewGuid().ToString();
            var role = CreateTestRole(roleName, useRoleNamePrefixAsRoleName: true);
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await userMgr.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
            IdentityResultAssert.IsSuccess(await userMgr.AddToRoleAsync(user, roleName));
            Assert.True(await userMgr.IsInRoleAsync(user, roleName));
            IdentityResultAssert.IsFailure(await userMgr.AddToRoleAsync(user, roleName), _errorDescriber.UserAlreadyInRole(roleName));
            IdentityResultAssert.VerifyLogMessage(userMgr.Logger, $"User {await userMgr.GetUserIdAsync(user)} is already in role {roleName}.");
        }

        [Fact]
        public async Task CanFindRoleByNameWithManager()
        {
            var roleMgr = CreateRoleManager();
            var roleName = "findRoleByNameTest" + Guid.NewGuid().ToString();
            var role = CreateTestRole(roleName, useRoleNamePrefixAsRoleName: true);
            IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
            Assert.NotNull(await roleMgr.FindByNameAsync(roleName));
        }

        [Fact]
        public async Task CanFindRoleWithManager()
        {
            var roleMgr = CreateRoleManager();
            var roleName = "findRoleTest" + Guid.NewGuid().ToString();
            var role = CreateTestRole(roleName, useRoleNamePrefixAsRoleName: true);
            IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
            Assert.Equal(roleName, await roleMgr.GetRoleNameAsync(await roleMgr.FindByNameAsync(roleName)));
        }

        [Fact]
        public async Task SetPhoneNumberTest()
        {
            var manager = CreateManager();
            var user = CreateTestUser(phoneNumber: "123-456-7890");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var stamp = await manager.GetSecurityStampAsync(user);
            Assert.Equal(await manager.GetPhoneNumberAsync(user), "123-456-7890");
            IdentityResultAssert.IsSuccess(await manager.SetPhoneNumberAsync(user, "111-111-1111"));
            Assert.Equal(await manager.GetPhoneNumberAsync(user), "111-111-1111");
            Assert.NotEqual(stamp, await manager.GetSecurityStampAsync(user));
        }

        [Fact]
        public async Task CanChangePhoneNumber()
        {
            var manager = CreateManager();
            var user = CreateTestUser(phoneNumber: "123-456-7890");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            Assert.False(await manager.IsPhoneNumberConfirmedAsync(user));
            var stamp = await manager.GetSecurityStampAsync(user);
            var token1 = await manager.GenerateChangePhoneNumberTokenAsync(user, "111-111-1111");
            IdentityResultAssert.IsSuccess(await manager.ChangePhoneNumberAsync(user, "111-111-1111", token1));
            Assert.True(await manager.IsPhoneNumberConfirmedAsync(user));
            Assert.Equal(await manager.GetPhoneNumberAsync(user), "111-111-1111");
            Assert.NotEqual(stamp, await manager.GetSecurityStampAsync(user));
        }

        [Fact]
        public async Task ChangePhoneNumberFailsWithWrongToken()
        {
            var manager = CreateManager();
            var user = CreateTestUser(phoneNumber: "123-456-7890");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            Assert.False(await manager.IsPhoneNumberConfirmedAsync(user));
            var stamp = await manager.GetSecurityStampAsync(user);
            IdentityResultAssert.IsFailure(await manager.ChangePhoneNumberAsync(user, "111-111-1111", "bogus"),
                "Invalid token.");
            IdentityResultAssert.VerifyLogMessage(manager.Logger, $"VerifyChangePhoneNumberTokenAsync() failed for user {await manager.GetUserIdAsync(user)}.");
            Assert.False(await manager.IsPhoneNumberConfirmedAsync(user));
            Assert.Equal(await manager.GetPhoneNumberAsync(user), "123-456-7890");
            Assert.Equal(stamp, await manager.GetSecurityStampAsync(user));
        }

        [Fact]
        public async Task ChangePhoneNumberFailsWithWrongPhoneNumber()
        {
            var manager = CreateManager();
            var user = CreateTestUser(phoneNumber: "123-456-7890");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            Assert.False(await manager.IsPhoneNumberConfirmedAsync(user));
            var stamp = await manager.GetSecurityStampAsync(user);
            var token1 = await manager.GenerateChangePhoneNumberTokenAsync(user, "111-111-1111");
            IdentityResultAssert.IsFailure(await manager.ChangePhoneNumberAsync(user, "bogus", token1),
                "Invalid token.");
            Assert.False(await manager.IsPhoneNumberConfirmedAsync(user));
            Assert.Equal(await manager.GetPhoneNumberAsync(user), "123-456-7890");
            Assert.Equal(stamp, await manager.GetSecurityStampAsync(user));
        }

        [Fact]
        public async Task CanVerifyPhoneNumber()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            const string num1 = "111-123-4567";
            const string num2 = "111-111-1111";
            var userId = await manager.GetUserIdAsync(user);
            var token1 = await manager.GenerateChangePhoneNumberTokenAsync(user, num1);

            var token2 = await manager.GenerateChangePhoneNumberTokenAsync(user, num2);
            Assert.NotEqual(token1, token2);
            Assert.True(await manager.VerifyChangePhoneNumberTokenAsync(user, token1, num1));
            Assert.True(await manager.VerifyChangePhoneNumberTokenAsync(user, token2, num2));
            Assert.False(await manager.VerifyChangePhoneNumberTokenAsync(user, token2, num1));
            Assert.False(await manager.VerifyChangePhoneNumberTokenAsync(user, token1, num2));
            IdentityResultAssert.VerifyLogMessage(manager.Logger, $"VerifyChangePhoneNumberTokenAsync() failed for user {await manager.GetUserIdAsync(user)}.");
        }

        [Fact]
        public async Task CanChangeEmail()
        {
            var manager = CreateManager();
            var user = CreateTestUser("foouser");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var email = await manager.GetUserNameAsync(user) + "@diddly.bop";
            IdentityResultAssert.IsSuccess(await manager.SetEmailAsync(user, email));
            Assert.False(await manager.IsEmailConfirmedAsync(user));
            var stamp = await manager.GetSecurityStampAsync(user);
            var newEmail = await manager.GetUserNameAsync(user) + "@en.vec";
            var token1 = await manager.GenerateChangeEmailTokenAsync(user, newEmail);
            IdentityResultAssert.IsSuccess(await manager.ChangeEmailAsync(user, newEmail, token1));
            Assert.True(await manager.IsEmailConfirmedAsync(user));
            Assert.Equal(await manager.GetEmailAsync(user), newEmail);
            Assert.NotEqual(stamp, await manager.GetSecurityStampAsync(user));
        }

        [Fact]
        public async Task ChangeEmailFailsWithWrongToken()
        {
            var manager = CreateManager();
            var user = CreateTestUser("foouser");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var email = await manager.GetUserNameAsync(user) + "@diddly.bop";
            IdentityResultAssert.IsSuccess(await manager.SetEmailAsync(user, email));
            string oldEmail = email;
            Assert.False(await manager.IsEmailConfirmedAsync(user));
            var stamp = await manager.GetSecurityStampAsync(user);
            IdentityResultAssert.IsFailure(await manager.ChangeEmailAsync(user, "whatevah@foo.barf", "bogus"),
                "Invalid token.");
            IdentityResultAssert.VerifyLogMessage(manager.Logger, $"VerifyUserTokenAsync() failed with purpose: ChangeEmail:whatevah@foo.barf for user { await manager.GetUserIdAsync(user)}.");
            Assert.False(await manager.IsEmailConfirmedAsync(user));
            Assert.Equal(await manager.GetEmailAsync(user), oldEmail);
            Assert.Equal(stamp, await manager.GetSecurityStampAsync(user));
        }

        [Fact]
        public async Task ChangeEmailFailsWithEmail()
        {
            var manager = CreateManager();
            var user = CreateTestUser("foouser");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var email = await manager.GetUserNameAsync(user) + "@diddly.bop";
            IdentityResultAssert.IsSuccess(await manager.SetEmailAsync(user, email));
            string oldEmail = email;
            Assert.False(await manager.IsEmailConfirmedAsync(user));
            var stamp = await manager.GetSecurityStampAsync(user);
            var token1 = await manager.GenerateChangeEmailTokenAsync(user, "forgot@alrea.dy");
            IdentityResultAssert.IsFailure(await manager.ChangeEmailAsync(user, "oops@foo.barf", token1),
                "Invalid token.");
            IdentityResultAssert.VerifyLogMessage(manager.Logger, $"VerifyUserTokenAsync() failed with purpose: ChangeEmail:oops@foo.barf for user { await manager.GetUserIdAsync(user)}.");
            Assert.False(await manager.IsEmailConfirmedAsync(user));
            Assert.Equal(await manager.GetEmailAsync(user), oldEmail);
            Assert.Equal(stamp, await manager.GetSecurityStampAsync(user));
        }

        [Fact]
        public async Task EmailFactorFailsAfterSecurityStampChangeTest()
        {
            var manager = CreateManager();
            string factorId = "Email"; //default
            var user = CreateTestUser("foouser");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var email = await manager.GetUserNameAsync(user) + "@diddly.bop";
            IdentityResultAssert.IsSuccess(await manager.SetEmailAsync(user, email));
            var token = await manager.GenerateEmailConfirmationTokenAsync(user);
            await manager.ConfirmEmailAsync(user, token);

            var stamp = await manager.GetSecurityStampAsync(user);
            Assert.NotNull(stamp);
            token = await manager.GenerateTwoFactorTokenAsync(user, factorId);
            Assert.NotNull(token);
            IdentityResultAssert.IsSuccess(await manager.UpdateSecurityStampAsync(user));
            Assert.False(await manager.VerifyTwoFactorTokenAsync(user, factorId, token));
            IdentityResultAssert.VerifyLogMessage(manager.Logger, $"VerifyTwoFactorTokenAsync() failed for user {await manager.GetUserIdAsync(user)}.");
        }

        [Fact]
        public async Task EnableTwoFactorChangesSecurityStamp()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var stamp = await manager.GetSecurityStampAsync(user);
            Assert.NotNull(stamp);
            IdentityResultAssert.IsSuccess(await manager.SetTwoFactorEnabledAsync(user, true));
            Assert.NotEqual(stamp, await manager.GetSecurityStampAsync(user));
            Assert.True(await manager.GetTwoFactorEnabledAsync(user));
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
            var userId = await manager.GetUserIdAsync(user);
            var factors = await manager.GetValidTwoFactorProvidersAsync(user);
            Assert.NotNull(factors);
            Assert.False(factors.Any());
            IdentityResultAssert.IsSuccess(await manager.SetPhoneNumberAsync(user, "111-111-1111"));
            var token = await manager.GenerateChangePhoneNumberTokenAsync(user, "111-111-1111");
            IdentityResultAssert.IsSuccess(await manager.ChangePhoneNumberAsync(user, "111-111-1111", token));
            await manager.UpdateAsync(user);
            factors = await manager.GetValidTwoFactorProvidersAsync(user);
            Assert.NotNull(factors);
            Assert.Equal(1, factors.Count());
            Assert.Equal("Phone", factors[0]);
            IdentityResultAssert.IsSuccess(await manager.SetEmailAsync(user, "test@test.com"));
            token = await manager.GenerateEmailConfirmationTokenAsync(user);
            await manager.ConfirmEmailAsync(user, token);
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
            var user = CreateTestUser(phoneNumber: "4251234567");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var stamp = await manager.GetSecurityStampAsync(user);
            Assert.NotNull(stamp);
            var token = await manager.GenerateTwoFactorTokenAsync(user, factorId);
            Assert.NotNull(token);
            IdentityResultAssert.IsSuccess(await manager.UpdateSecurityStampAsync(user));
            Assert.False(await manager.VerifyTwoFactorTokenAsync(user, factorId, token));
            IdentityResultAssert.VerifyLogMessage(manager.Logger, $"VerifyTwoFactorTokenAsync() failed for user {await manager.GetUserIdAsync(user)}.");
        }

        [Fact]
        public async Task VerifyTokenFromWrongTokenProviderFails()
        {
            var manager = CreateManager();
            var user = CreateTestUser(phoneNumber: "4251234567");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var token = await manager.GenerateTwoFactorTokenAsync(user, "Phone");
            Assert.NotNull(token);
            Assert.False(await manager.VerifyTwoFactorTokenAsync(user, "Email", token));
            IdentityResultAssert.VerifyLogMessage(manager.Logger, $"VerifyTwoFactorTokenAsync() failed for user {await manager.GetUserIdAsync(user)}.");
        }

        [Fact]
        public async Task VerifyWithWrongSmsTokenFails()
        {
            var manager = CreateManager();
            var user = CreateTestUser(phoneNumber: "4251234567");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            Assert.False(await manager.VerifyTwoFactorTokenAsync(user, "Phone", "bogus"));
            IdentityResultAssert.VerifyLogMessage(manager.Logger, $"VerifyTwoFactorTokenAsync() failed for user {await manager.GetUserIdAsync(user)}.");
        }

        [Fact]
        public async Task NullableDateTimeOperationTest()
        {
            var userMgr = CreateManager();
            var user = CreateTestUser(lockoutEnabled: true);
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
            var roleNameList = new List<string>();

            foreach (var role in roles)
            {
                IdentityResultAssert.IsSuccess(await roleManager.CreateAsync(role));
                roleNameList.Add(await roleManager.GetRoleNameAsync(role));
            }

            for (int i = 0; i < 6; i++)
            {
                var user = CreateTestUser();
                IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));

                if ((i % 2) == 0)
                {
                    IdentityResultAssert.IsSuccess(await manager.AddToRolesAsync(user, roleNameList));
                }
            }

            foreach (var role in roles)
            {
                Assert.Equal(3, (await manager.GetUsersInRoleAsync(await roleManager.GetRoleNameAsync(role))).Count);
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
                roles.Add(CreateTestRole(namePrefix + i));
            }
            return roles;
        }
    }
}
