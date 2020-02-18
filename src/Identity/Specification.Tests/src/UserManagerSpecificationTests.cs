// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Identity.Test
{
    /// <summary>
    /// Base class for tests that exercise basic identity functionality that all stores should support.
    /// </summary>
    /// <typeparam name="TUser">The type of the user.</typeparam>
    public abstract class UserManagerSpecificationTestBase<TUser> : UserManagerSpecificationTestBase<TUser, string> where TUser : class { }

    /// <summary>
    /// Base class for tests that exercise basic identity functionality that all stores should support.
    /// </summary>
    /// <typeparam name="TUser">The type of the user.</typeparam>
    /// <typeparam name="TKey">The primary key type.</typeparam>
    public abstract class UserManagerSpecificationTestBase<TUser, TKey>
        where TUser : class
        where TKey : IEquatable<TKey>
    {
        /// <summary>
        /// Null value.
        /// </summary>
        protected const string NullValue = "(null)";

        /// <summary>
        /// Error describer.
        /// </summary>
        protected readonly IdentityErrorDescriber _errorDescriber = new IdentityErrorDescriber();

        /// <summary>
        /// Configure the service collection used for tests.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="context"></param>
        protected virtual void SetupIdentityServices(IServiceCollection services, object context)
            => SetupBuilder(services, context);

        /// <summary>
        /// Configure the service collection used for tests.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="context"></param>
        protected virtual IdentityBuilder SetupBuilder(IServiceCollection services, object context)
        {
            services.AddHttpContextAccessor();
            services.AddDataProtection();
            var builder = services.AddIdentityCore<TUser>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.User.AllowedUserNameCharacters = null;
            }).AddDefaultTokenProviders();
            AddUserStore(services, context);
            services.AddLogging();
            services.AddSingleton<ILogger<UserManager<TUser>>>(new TestLogger<UserManager<TUser>>());
            return builder;
        }

        /// <summary>
        /// Creates the user manager used for tests.
        /// </summary>
        /// <param name="context">The context that will be passed into the store, typically a db context.</param>
        /// <param name="services">The service collection to use, optional.</param>
        /// <param name="configureServices">Delegate used to configure the services, optional.</param>
        /// <returns>The user manager to use for tests.</returns>
        protected virtual UserManager<TUser> CreateManager(object context = null, IServiceCollection services = null, Action<IServiceCollection> configureServices = null)
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
            configureServices?.Invoke(services);
            return services.BuildServiceProvider().GetService<UserManager<TUser>>();
        }

        /// <summary>
        /// Creates the context object for a test, typically a DbContext.
        /// </summary>
        /// <returns>The context object for a test, typically a DbContext.</returns>
        protected abstract object CreateTestContext();

        /// <summary>
        /// Adds an IUserStore to services for the test.
        /// </summary>
        /// <param name="services">The service collection to add to.</param>
        /// <param name="context">The context for the store to use, optional.</param>
        protected abstract void AddUserStore(IServiceCollection services, object context = null);

        /// <summary>
        /// Set the user's password hash.
        /// </summary>
        /// <param name="user">The user to set.</param>
        /// <param name="hashedPassword">The password hash to set.</param>
        protected abstract void SetUserPasswordHash(TUser user, string hashedPassword);

        /// <summary>
        /// Create a new test user instance.
        /// </summary>
        /// <param name="namePrefix">Optional name prefix, name will be randomized.</param>
        /// <param name="email">Optional email.</param>
        /// <param name="phoneNumber">Optional phone number.</param>
        /// <param name="lockoutEnabled">Optional lockout enabled.</param>
        /// <param name="lockoutEnd">Optional lockout end.</param>
        /// <param name="useNamePrefixAsUserName">If true, the prefix should be used as the username without a random pad.</param>
        /// <returns>The new test user instance.</returns>
        protected abstract TUser CreateTestUser(string namePrefix = "", string email = "", string phoneNumber = "",
            bool lockoutEnabled = false, DateTimeOffset? lockoutEnd = null, bool useNamePrefixAsUserName = false);

        /// <summary>
        /// Query used to do name equality checks.
        /// </summary>
        /// <param name="userName">The user name to match.</param>
        /// <returns>The query to use.</returns>
        protected abstract Expression<Func<TUser, bool>> UserNameEqualsPredicate(string userName);

        /// <summary>
        /// Query used to do user name prefix matching.
        /// </summary>
        /// <param name="userName">The user name to match.</param>
        /// <returns>The query to use.</returns>
        protected abstract Expression<Func<TUser, bool>> UserNameStartsWithPredicate(string userName);

        private class AlwaysBadValidator : IUserValidator<TUser>,
            IPasswordValidator<TUser>
        {
            public static readonly IdentityError ErrorMessage = new IdentityError { Description = "I'm Bad.", Code = "BadValidator" };

            public Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user, string password)
            {
                return Task.FromResult(IdentityResult.Failed(ErrorMessage));
            }

            public Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user)
            {
                return Task.FromResult(IdentityResult.Failed(ErrorMessage));
            }
        }

        private class EmptyBadValidator : IUserValidator<TUser>,
            IPasswordValidator<TUser>
        {
            public Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user, string password)
            {
                return Task.FromResult(IdentityResult.Failed());
            }

            public Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user)
            {
                return Task.FromResult(IdentityResult.Failed());
            }
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task PasswordValidatorWithNoErrorsCanBlockAddPassword()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            manager.PasswordValidators.Clear();
            manager.PasswordValidators.Add(new EmptyBadValidator());
            IdentityResultAssert.IsFailure(await manager.AddPasswordAsync(user, "password"));
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task CreateUserWillSetCreateDateOnlyIfSupported()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var userId = await manager.GetUserIdAsync(user);
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task ResetAuthenticatorKeyUpdatesSecurityStamp()
        {
            var manager = CreateManager();
            var username = "Create" + Guid.NewGuid().ToString();
            var user = CreateTestUser(username, useNamePrefixAsUserName: true);
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var stamp = await manager.GetSecurityStampAsync(user);
            IdentityResultAssert.IsSuccess(await manager.ResetAuthenticatorKeyAsync(user));
            Assert.NotEqual(stamp, await manager.GetSecurityStampAsync(user));
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task CanFindById()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            Assert.NotNull(await manager.FindByIdAsync(await manager.GetUserIdAsync(user)));
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task UserValidatorCanBlockCreate()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            manager.UserValidators.Clear();
            manager.UserValidators.Add(new AlwaysBadValidator());
            IdentityResultAssert.IsFailure(await manager.CreateAsync(user), AlwaysBadValidator.ErrorMessage);
            IdentityResultAssert.VerifyLogMessage(manager.Logger, $"User {await manager.GetUserIdAsync(user) ?? NullValue} validation failed: {AlwaysBadValidator.ErrorMessage.Code}.");
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task UserValidatorCanBlockUpdate()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            manager.UserValidators.Clear();
            manager.UserValidators.Add(new AlwaysBadValidator());
            IdentityResultAssert.IsFailure(await manager.UpdateAsync(user), AlwaysBadValidator.ErrorMessage);
            IdentityResultAssert.VerifyLogMessage(manager.Logger, $"User {await manager.GetUserIdAsync(user) ?? NullValue} validation failed: {AlwaysBadValidator.ErrorMessage.Code}.");
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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
            IdentityResultAssert.VerifyLogMessage(manager.Logger, $"User {await manager.GetUserIdAsync(user) ?? NullValue} validation failed: {AlwaysBadValidator.ErrorMessage.Code};{AlwaysBadValidator.ErrorMessage.Code}.");
            Assert.Equal(2, result.Errors.Count());
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task CanChainPasswordValidators()
        {
            var manager = CreateManager();
            manager.PasswordValidators.Clear();
            manager.PasswordValidators.Add(new EmptyBadValidator());
            manager.PasswordValidators.Add(new AlwaysBadValidator());
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var result = await manager.AddPasswordAsync(user, "pwd");
            IdentityResultAssert.IsFailure(result, AlwaysBadValidator.ErrorMessage);
            Assert.Single(result.Errors);
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task PasswordValidatorWithNoErrorsCanBlockChangePassword()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, "password"));
            manager.PasswordValidators.Clear();
            manager.PasswordValidators.Add(new AlwaysBadValidator());
            IdentityResultAssert.IsFailure(await manager.ChangePasswordAsync(user, "password", "new"));
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task PasswordValidatorWithNoErrorsCanBlockCreateUser()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            manager.PasswordValidators.Clear();
            manager.PasswordValidators.Add(new AlwaysBadValidator());
            IdentityResultAssert.IsFailure(await manager.CreateAsync(user, "password"));
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task PasswordValidatorWithNoErrorsCanBlockResetPasswordWithStaticTokenProvider()
        {
            var manager = CreateManager();
            manager.RegisterTokenProvider("Static", new StaticTokenProvider());
            manager.Options.Tokens.PasswordResetTokenProvider = "Static";
            var user = CreateTestUser();
            const string password = "password";
            const string newPassword = "newpassword";
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, password));
            var stamp = await manager.GetSecurityStampAsync(user);
            Assert.NotNull(stamp);
            var token = await manager.GeneratePasswordResetTokenAsync(user);
            Assert.NotNull(token);
            manager.PasswordValidators.Add(new AlwaysBadValidator());
            IdentityResultAssert.IsFailure(await manager.ResetPasswordAsync(user, token, newPassword));
            Assert.True(await manager.CheckPasswordAsync(user, password));
            Assert.Equal(stamp, await manager.GetSecurityStampAsync(user));
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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
            IdentityResultAssert.VerifyLogMessage(manager.Logger, $"User {await manager.GetUserIdAsync(user) ?? NullValue} password validation failed: {AlwaysBadValidator.ErrorMessage.Code}.");
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task PasswordValidatorCanBlockCreateUser()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            manager.PasswordValidators.Clear();
            manager.PasswordValidators.Add(new AlwaysBadValidator());
            IdentityResultAssert.IsFailure(await manager.CreateAsync(user, "password"), AlwaysBadValidator.ErrorMessage);
            IdentityResultAssert.VerifyLogMessage(manager.Logger, $"User {await manager.GetUserIdAsync(user) ?? NullValue} password validation failed: {AlwaysBadValidator.ErrorMessage.Code}.");
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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
            Assert.Empty(logins);
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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
            Assert.Single(logins);
            Assert.Equal(provider, logins.First().LoginProvider);
            Assert.Equal(providerKey, logins.First().ProviderKey);
            Assert.Equal(display, logins.First().ProviderDisplayName);
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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
            Assert.Single(logins);
            Assert.Equal(user, await manager.FindByLoginAsync(login.LoginProvider, login.ProviderKey));
            Assert.True(await manager.CheckPasswordAsync(user, "password"));
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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
            Assert.Single(logins);
            Assert.Equal(login.LoginProvider, logins.Last().LoginProvider);
            Assert.Equal(login.ProviderKey, logins.Last().ProviderKey);
            Assert.Equal(login.ProviderDisplayName, logins.Last().ProviderDisplayName);
            var stamp = await manager.GetSecurityStampAsync(user);
            IdentityResultAssert.IsSuccess(await manager.RemoveLoginAsync(user, login.LoginProvider, login.ProviderKey));
            Assert.Null(await manager.FindByLoginAsync(login.LoginProvider, login.ProviderKey));
            logins = await manager.GetLoginsAsync(user);
            Assert.NotNull(logins);
            Assert.Empty(logins);
            Assert.NotEqual(stamp, await manager.GetSecurityStampAsync(user));
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task UpdateSecurityStampActuallyChanges()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var stamp = await manager.GetSecurityStampAsync(user);
            IdentityResultAssert.IsSuccess(await manager.UpdateSecurityStampAsync(user));
            Assert.NotEqual(stamp, await manager.GetSecurityStampAsync(user));
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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
            IdentityResultAssert.VerifyLogMessage(manager.Logger, $"AddLogin for user {await manager.GetUserIdAsync(user)} failed because it was already associated with another user.");
        }

        // Email tests

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async virtual Task CanFindUsersViaUserQuerable()
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

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task ConfirmEmailFalseByDefaultTest()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            Assert.False(await manager.IsEmailConfirmedAsync(user));
        }

        private class StaticTokenProvider : IUserTwoFactorTokenProvider<TUser>
        {
            public async Task<string> GenerateAsync(string purpose, UserManager<TUser> manager, TUser user)
            {
                return MakeToken(purpose, await manager.GetUserIdAsync(user));
            }

            public async Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser> manager, TUser user)
            {
                return token == MakeToken(purpose, await manager.GetUserIdAsync(user));
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

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task CanResetPasswordWithStaticTokenProvider()
        {
            var manager = CreateManager();
            manager.RegisterTokenProvider("Static", new StaticTokenProvider());
            manager.Options.Tokens.PasswordResetTokenProvider = "Static";
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

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task PasswordValidatorCanBlockResetPasswordWithStaticTokenProvider()
        {
            var manager = CreateManager();
            manager.RegisterTokenProvider("Static", new StaticTokenProvider());
            manager.Options.Tokens.PasswordResetTokenProvider = "Static";
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

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task ResetPasswordWithStaticTokenProviderFailsWithWrongToken()
        {
            var manager = CreateManager();
            manager.RegisterTokenProvider("Static", new StaticTokenProvider());
            manager.Options.Tokens.PasswordResetTokenProvider = "Static";
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

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task CanGenerateAndVerifyUserTokenWithStaticTokenProvider()
        {
            var manager = CreateManager();
            manager.RegisterTokenProvider("Static", new StaticTokenProvider());
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

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task CanConfirmEmailWithStaticToken()
        {
            var manager = CreateManager();
            manager.RegisterTokenProvider("Static", new StaticTokenProvider());
            manager.Options.Tokens.EmailConfirmationTokenProvider = "Static";
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

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task ConfirmEmailWithStaticTokenFailsWithWrongToken()
        {
            var manager = CreateManager();
            manager.RegisterTokenProvider("Static", new StaticTokenProvider());
            manager.Options.Tokens.EmailConfirmationTokenProvider = "Static";
            var user = CreateTestUser();
            Assert.False(await manager.IsEmailConfirmedAsync(user));
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsFailure(await manager.ConfirmEmailAsync(user, "bogus"), "Invalid token.");
            Assert.False(await manager.IsEmailConfirmedAsync(user));
            IdentityResultAssert.VerifyLogMessage(manager.Logger, $"VerifyUserTokenAsync() failed with purpose: EmailConfirmation for user { await manager.GetUserIdAsync(user)}.");
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task LockoutEndToUtcNowMinus1SecInUserShouldNotBeLockedOut()
        {
            var mgr = CreateManager();
            var user = CreateTestUser(lockoutEnd: DateTimeOffset.UtcNow.AddSeconds(-1));
            IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
            Assert.True(await mgr.GetLockoutEnabledAsync(user));
            Assert.False(await mgr.IsLockedOutAsync(user));
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task SetPhoneNumberTest()
        {
            var manager = CreateManager();
            var user = CreateTestUser(phoneNumber: "123-456-7890");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var stamp = await manager.GetSecurityStampAsync(user);
            Assert.Equal("123-456-7890", await manager.GetPhoneNumberAsync(user));
            IdentityResultAssert.IsSuccess(await manager.SetPhoneNumberAsync(user, "111-111-1111"));
            Assert.Equal("111-111-1111", await manager.GetPhoneNumberAsync(user));
            Assert.NotEqual(stamp, await manager.GetSecurityStampAsync(user));
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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
            Assert.Equal("111-111-1111", await manager.GetPhoneNumberAsync(user));
            Assert.NotEqual(stamp, await manager.GetSecurityStampAsync(user));
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task ChangePhoneNumberTokenIsInt()
        {
            var manager = CreateManager();
            var user = CreateTestUser(phoneNumber: "123-456-7890");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var token1 = await manager.GenerateChangePhoneNumberTokenAsync(user, "111-111-1111");
            Assert.True(int.TryParse(token1, out var ignored));
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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
            IdentityResultAssert.VerifyLogMessage(manager.Logger, $"VerifyUserTokenAsync() failed with purpose: ChangePhoneNumber:111-111-1111 for user {await manager.GetUserIdAsync(user)}.");
            Assert.False(await manager.IsPhoneNumberConfirmedAsync(user));
            Assert.Equal("123-456-7890", await manager.GetPhoneNumberAsync(user));
            Assert.Equal(stamp, await manager.GetSecurityStampAsync(user));
        }

        private class YesPhoneNumberProvider : IUserTwoFactorTokenProvider<TUser>
        {
            public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> manager, TUser user)
                => Task.FromResult(true);

            public Task<string> GenerateAsync(string purpose, UserManager<TUser> manager, TUser user)
                => Task.FromResult(purpose);

            public Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser> manager, TUser user)
                => Task.FromResult(true);
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task ChangePhoneNumberWithCustomProvider()
        {
            var manager = CreateManager();
            manager.RegisterTokenProvider("Yes", new YesPhoneNumberProvider());
            manager.Options.Tokens.ChangePhoneNumberTokenProvider = "Yes";
            var user = CreateTestUser(phoneNumber: "123-456-7890");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            Assert.False(await manager.IsPhoneNumberConfirmedAsync(user));
            var stamp = await manager.GetSecurityStampAsync(user);
            IdentityResultAssert.IsSuccess(await manager.ChangePhoneNumberAsync(user, "111-111-1111", "whatever"));
            Assert.True(await manager.IsPhoneNumberConfirmedAsync(user));
            Assert.Equal("111-111-1111", await manager.GetPhoneNumberAsync(user));
            Assert.NotEqual(stamp, await manager.GetSecurityStampAsync(user));
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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
            Assert.Equal("123-456-7890", await manager.GetPhoneNumberAsync(user));
            Assert.Equal(stamp, await manager.GetSecurityStampAsync(user));
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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
            IdentityResultAssert.VerifyLogMessage(manager.Logger, $"VerifyUserTokenAsync() failed with purpose: ChangePhoneNumber:{num1} for user {await manager.GetUserIdAsync(user)}.");
            IdentityResultAssert.VerifyLogMessage(manager.Logger, $"VerifyUserTokenAsync() failed with purpose: ChangePhoneNumber:{num2} for user {await manager.GetUserIdAsync(user)}.");
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task CanChangeEmailOnlyIfEmailSame()
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
            var token2 = await manager.GenerateChangeEmailTokenAsync(user, "should@fail.com");
            IdentityResultAssert.IsSuccess(await manager.ChangeEmailAsync(user, newEmail, token1));
            Assert.True(await manager.IsEmailConfirmedAsync(user));
            Assert.Equal(await manager.GetEmailAsync(user), newEmail);
            Assert.NotEqual(stamp, await manager.GetSecurityStampAsync(user));
            IdentityResultAssert.IsFailure(await manager.ChangeEmailAsync(user, "should@fail.com", token2));
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task CanChangeEmailWithDifferentTokenProvider()
        {
            var manager = CreateManager(context: null, services: null,
                configureServices: s => s.Configure<IdentityOptions>(
                    o => o.Tokens.ProviderMap["NewProvider2"] = new TokenProviderDescriptor(typeof(EmailTokenProvider<TUser>))));
            manager.Options.Tokens.ChangeEmailTokenProvider = "NewProvider2";
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

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task ChangeEmailTokensFailsAfterEmailChanged()
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
            IdentityResultAssert.IsSuccess(await manager.SetEmailAsync(user, "another@email.com"));
            Assert.NotEqual(stamp, await manager.GetSecurityStampAsync(user));
            IdentityResultAssert.IsFailure(await manager.ChangeEmailAsync(user, newEmail, token1));
            Assert.False(await manager.IsEmailConfirmedAsync(user));
            Assert.Equal("another@email.com", await manager.GetEmailAsync(user));
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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
            IdentityResultAssert.IsFailure(await manager.ChangeEmailAsync(user, "whatevah@foo.boop", "bogus"),
                "Invalid token.");
            IdentityResultAssert.VerifyLogMessage(manager.Logger, $"VerifyUserTokenAsync() failed with purpose: ChangeEmail:whatevah@foo.boop for user { await manager.GetUserIdAsync(user)}.");
            Assert.False(await manager.IsEmailConfirmedAsync(user));
            Assert.Equal(await manager.GetEmailAsync(user), oldEmail);
            Assert.Equal(stamp, await manager.GetSecurityStampAsync(user));
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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
            IdentityResultAssert.IsFailure(await manager.ChangeEmailAsync(user, "oops@foo.boop", token1),
                "Invalid token.");
            IdentityResultAssert.VerifyLogMessage(manager.Logger, $"VerifyUserTokenAsync() failed with purpose: ChangeEmail:oops@foo.boop for user { await manager.GetUserIdAsync(user)}.");
            Assert.False(await manager.IsEmailConfirmedAsync(user));
            Assert.Equal(await manager.GetEmailAsync(user), oldEmail);
            Assert.Equal(stamp, await manager.GetSecurityStampAsync(user));
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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
            var token2 = await manager.GenerateTwoFactorTokenAsync(user, factorId);
            if (token == token2) // Guard against the rare case there's a totp collision once
            {
                IdentityResultAssert.IsSuccess(await manager.UpdateSecurityStampAsync(user));
            }
            Assert.False(await manager.VerifyTwoFactorTokenAsync(user, factorId, token));
            IdentityResultAssert.VerifyLogMessage(manager.Logger, $"VerifyTwoFactorTokenAsync() failed for user {await manager.GetUserIdAsync(user)}.");
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task GenerateTwoFactorWithUnknownFactorProviderWillThrow()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var error = $"No IUserTwoFactorTokenProvider<{nameof(TUser)}> named 'bogus' is registered.";
            var ex = await Assert.ThrowsAsync<NotSupportedException>(
                () => manager.GenerateTwoFactorTokenAsync(user, "bogus"));
            Assert.Equal(error, ex.Message);
            ex = await Assert.ThrowsAsync<NotSupportedException>(
                () => manager.VerifyTwoFactorTokenAsync(user, "bogus", "bogus"));
            Assert.Equal(error, ex.Message);
            ex = await Assert.ThrowsAsync<NotSupportedException>(
                () => manager.VerifyUserTokenAsync(user, "bogus", "bogus", "bogus"));
            Assert.Equal(error, ex.Message);
            ex = await Assert.ThrowsAsync<NotSupportedException>(
                () => manager.GenerateUserTokenAsync(user, "bogus", "bogus"));
            Assert.Equal(error, ex.Message);
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task CanGetSetUpdateAndRemoveUserToken()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            Assert.Null(await manager.GetAuthenticationTokenAsync(user, "provider", "name"));
            IdentityResultAssert.IsSuccess(await manager.SetAuthenticationTokenAsync(user, "provider", "name", "value"));
            Assert.Equal("value", await manager.GetAuthenticationTokenAsync(user, "provider", "name"));

            IdentityResultAssert.IsSuccess(await manager.SetAuthenticationTokenAsync(user, "provider", "name", "value2"));
            Assert.Equal("value2", await manager.GetAuthenticationTokenAsync(user, "provider", "name"));

            IdentityResultAssert.IsSuccess(await manager.RemoveAuthenticationTokenAsync(user, "whatevs", "name"));
            Assert.Equal("value2", await manager.GetAuthenticationTokenAsync(user, "provider", "name"));

            IdentityResultAssert.IsSuccess(await manager.RemoveAuthenticationTokenAsync(user, "provider", "name"));
            Assert.Null(await manager.GetAuthenticationTokenAsync(user, "provider", "name"));
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task CanRedeemRecoveryCodeOnlyOnce()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));

            var numCodes = 15;
            var newCodes = await manager.GenerateNewTwoFactorRecoveryCodesAsync(user, numCodes);
            Assert.Equal(numCodes, newCodes.Count());

            foreach (var code in newCodes)
            {
                IdentityResultAssert.IsSuccess(await manager.RedeemTwoFactorRecoveryCodeAsync(user, code));
                IdentityResultAssert.IsFailure(await manager.RedeemTwoFactorRecoveryCodeAsync(user, code));
                Assert.Equal(--numCodes, await manager.CountRecoveryCodesAsync(user));
            }
            // One last time to be sure
            foreach (var code in newCodes)
            {
                IdentityResultAssert.IsFailure(await manager.RedeemTwoFactorRecoveryCodeAsync(user, code));
            }
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task RecoveryCodesInvalidAfterReplace()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));

            var numCodes = 15;
            var newCodes = await manager.GenerateNewTwoFactorRecoveryCodesAsync(user, numCodes);
            Assert.Equal(numCodes, newCodes.Count());
            var realCodes = await manager.GenerateNewTwoFactorRecoveryCodesAsync(user, numCodes);
            Assert.Equal(numCodes, realCodes.Count());

            foreach (var code in newCodes)
            {
                IdentityResultAssert.IsFailure(await manager.RedeemTwoFactorRecoveryCodeAsync(user, code));
            }

            foreach (var code in realCodes)
            {
                IdentityResultAssert.IsSuccess(await manager.RedeemTwoFactorRecoveryCodeAsync(user, code));
            }
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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
            Assert.Single(factors);
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
            Assert.Single(factors);
            Assert.Equal("Phone", factors[0]);
            IdentityResultAssert.IsSuccess(await manager.ResetAuthenticatorKeyAsync(user));
            factors = await manager.GetValidTwoFactorProvidersAsync(user);
            Assert.NotNull(factors);
            Assert.Equal(2, factors.Count());
            Assert.Equal("Authenticator", factors[1]);
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
        [Fact]
        public async Task VerifyWithWrongSmsTokenFails()
        {
            var manager = CreateManager();
            var user = CreateTestUser(phoneNumber: "4251234567");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            Assert.False(await manager.VerifyTwoFactorTokenAsync(user, "Phone", "bogus"));
            IdentityResultAssert.VerifyLogMessage(manager.Logger, $"VerifyTwoFactorTokenAsync() failed for user {await manager.GetUserIdAsync(user)}.");
        }

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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

        /// <summary>
        /// Test.
        /// </summary>
        /// <returns>Task</returns>
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

        /// <summary>
        /// Generate count users with a name prefix.
        /// </summary>
        /// <param name="userNamePrefix"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        protected List<TUser> GenerateUsers(string userNamePrefix, int count)
        {
            var users = new List<TUser>(count);
            for (var i = 0; i < count; i++)
            {
                users.Add(CreateTestUser(userNamePrefix + i));
            }
            return users;
        }
    }
}
