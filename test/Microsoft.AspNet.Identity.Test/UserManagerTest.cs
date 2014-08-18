// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.OptionsModel;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Identity.Test
{
    public class UserManagerTest
    {
        private class TestManager : UserManager<TestUser>
        {
            public IUserStore<TestUser> StorePublic { get { return Store; } }

            public TestManager(IUserStore<TestUser> store, IOptionsAccessor<IdentityOptions> optionsAccessor,
                IPasswordHasher<TestUser> passwordHasher, IUserValidator<TestUser> userValidator,
                IPasswordValidator<TestUser> passwordValidator)
                : base(store, optionsAccessor, passwordHasher, userValidator, passwordValidator, null) { }
        }

        [Fact]
        public void EnsureDefaultServicesDefaultsWithStoreWorks()
        {
            var services = new ServiceCollection {IdentityServices.GetDefaultUserServices<TestUser>()};
            services.AddInstance<IOptionsAccessor<IdentityOptions>>(new OptionsAccessor<IdentityOptions>(null));
            services.AddTransient<IUserStore<TestUser>, NoopUserStore>();
            services.AddTransient<TestManager>();
            var manager = services.BuildServiceProvider().GetService<TestManager>();
            Assert.NotNull(manager.PasswordHasher);
            Assert.NotNull(manager.PasswordValidator);
            Assert.NotNull(manager.UserValidator);
            Assert.NotNull(manager.StorePublic);
            Assert.NotNull(manager.Options);
        }

        [Fact]
        public async Task CreateCallsStore()
        {
            // Setup
            var store = new Mock<IUserStore<TestUser>>();
            var user = new TestUser { UserName = "Foo" };
            store.Setup(s => s.CreateAsync(user, CancellationToken.None)).Returns(Task.FromResult(0)).Verifiable();
            var userManager = MockHelpers.TestUserManager<TestUser>(store.Object);

            // Act
            var result = await userManager.CreateAsync(user);

            // Assert
            Assert.True(result.Succeeded);
            store.VerifyAll();
        }

        [Fact]
        public async Task DeleteCallsStore()
        {
            // Setup
            var store = new Mock<IUserStore<TestUser>>();
            var user = new TestUser { UserName = "Foo" };
            store.Setup(s => s.DeleteAsync(user, CancellationToken.None)).Returns(Task.FromResult(0)).Verifiable();
            var userManager = MockHelpers.TestUserManager<TestUser>(store.Object);

            // Act
            var result = await userManager.DeleteAsync(user);

            // Assert
            Assert.True(result.Succeeded);
            store.VerifyAll();
        }

        [Fact]
        public async Task UpdateCallsStore()
        {
            // Setup
            var store = new Mock<IUserStore<TestUser>>();
            var user = new TestUser { UserName = "Foo" };
            store.Setup(s => s.UpdateAsync(user, CancellationToken.None)).Returns(Task.FromResult(0)).Verifiable();
            var userManager = MockHelpers.TestUserManager<TestUser>(store.Object);

            // Act
            var result = await userManager.UpdateAsync(user);

            // Assert
            Assert.True(result.Succeeded);
            store.VerifyAll();
        }

        [Fact]
        public async Task SetUserNameCallsStore()
        {
            // Setup
            var store = new Mock<IUserStore<TestUser>>();
            var user = new TestUser();
            store.Setup(s => s.SetUserNameAsync(user, It.IsAny<string>(), CancellationToken.None)).Returns(Task.FromResult(0)).Verifiable();
            var userManager = MockHelpers.TestUserManager<TestUser>(store.Object);

            // Act
            var result = await userManager.SetUserNameAsync(user, "foo");

            // Assert
            Assert.True(result.Succeeded);
            store.VerifyAll();
        }

        [Fact]
        public async Task FindByIdCallsStore()
        {
            // Setup
            var store = new Mock<IUserStore<TestUser>>();
            var user = new TestUser { UserName = "Foo" };
            store.Setup(s => s.FindByIdAsync(user.Id, CancellationToken.None)).Returns(Task.FromResult(user)).Verifiable();
            var userManager = MockHelpers.TestUserManager<TestUser>(store.Object);

            // Act
            var result = await userManager.FindByIdAsync(user.Id);

            // Assert
            Assert.Equal(user, result);
            store.VerifyAll();
        }

        [Fact]
        public async Task FindByNameCallsStoreWithNormalizedName()
        {
            // Setup
            var store = new Mock<IUserStore<TestUser>>();
            var user = new TestUser {UserName="Foo"};
            store.Setup(s => s.FindByNameAsync(user.UserName.ToUpperInvariant(), CancellationToken.None)).Returns(Task.FromResult(user)).Verifiable();
            var userManager = MockHelpers.TestUserManager<TestUser>(store.Object);

            // Act
            var result = await userManager.FindByNameAsync(user.UserName);

            // Assert
            Assert.Equal(user, result);
            store.VerifyAll();
        }

        [Fact]
        public async Task CanFindByNameCallsStoreWithoutNormalizedName()
        {
            // Setup
            var store = new Mock<IUserStore<TestUser>>();
            var user = new TestUser {UserName="Foo"};
            store.Setup(s => s.FindByNameAsync(user.UserName, CancellationToken.None)).Returns(Task.FromResult(user)).Verifiable();
            var userManager = MockHelpers.TestUserManager<TestUser>(store.Object);
            userManager.UserNameNormalizer = null;

            // Act
            var result = await userManager.FindByNameAsync(user.UserName);

            // Assert
            Assert.Equal(user, result);
            store.VerifyAll();
        }

        [Fact]
        public async Task UseUserNameAsEmailCallsStoreFindByName()
        {
            // Setup
            var store = new Mock<IUserStore<TestUser>>();
            var user = new TestUser {UserName="Foo"};
            store.Setup(s => s.FindByNameAsync(user.UserName, CancellationToken.None)).Returns(Task.FromResult(user)).Verifiable();
            var userManager = MockHelpers.TestUserManager(store.Object);
            userManager.Options.User.UseUserNameAsEmail = true;
            userManager.UserNameNormalizer = null;

            // Act
            var result = await userManager.FindByEmailAsync(user.UserName);

            // Assert
            Assert.Equal(user, result);
            store.VerifyAll();
        }

        [Fact]
        public async Task UseUserNameAsEmailReturnsName()
        {
            // Setup
            var store = new Mock<IUserStore<TestUser>>();
            var user = new TestUser { UserName = "Foo@email.com" };
            store.Setup(s => s.GetUserNameAsync(user, CancellationToken.None)).ReturnsAsync(user.UserName).Verifiable();
            var userManager = MockHelpers.TestUserManager(store.Object);
            userManager.Options.User.UseUserNameAsEmail = true;
            userManager.UserNameNormalizer = null;

            // Act
            var result = await userManager.GetEmailAsync(user);

            // Assert
            Assert.Equal(user.UserName, result);
            store.VerifyAll();
        }

        [Fact]
        public async Task UseUserNameAsEmailUpdatesNameAndEmail()
        {
            // Setup
            var store = new Mock<IUserEmailStore<TestUser>>();
            var user = new TestUser { UserName = "Foo" };
            var email = "foo@foo.com";
            store.Setup(s => s.SetUserNameAsync(user, email, CancellationToken.None)).Returns(Task.FromResult(0)).Verifiable();
            store.Setup(s => s.SetEmailAsync(user, email, CancellationToken.None)).Returns(Task.FromResult(0)).Verifiable();
            store.Setup(s => s.SetEmailConfirmedAsync(user, false, CancellationToken.None)).Returns(Task.FromResult(0)).Verifiable();
            var userManager = MockHelpers.TestUserManager(store.Object);
            userManager.Options.User.UseUserNameAsEmail = true;
            userManager.UserNameNormalizer = null;

            // Act
            IdentityResultAssert.IsSuccess(await userManager.SetEmailAsync(user, email));

            // Assert
            store.VerifyAll();
        }

        [Fact]
        public async Task AddToRolesCallsStore()
        {
            // Setup
            var store = new Mock<IUserRoleStore<TestUser>>();
            var user = new TestUser { UserName = "Foo" };
            var roles = new string[] {"A", "B", "C"};
            store.Setup(s => s.AddToRoleAsync(user, "A", CancellationToken.None))
                .Returns(Task.FromResult(0))
                .Verifiable();
            store.Setup(s => s.AddToRoleAsync(user, "B", CancellationToken.None))
                .Returns(Task.FromResult(0))
                .Verifiable();
            store.Setup(s => s.AddToRoleAsync(user, "C", CancellationToken.None))
                .Returns(Task.FromResult(0))
                .Verifiable();
            store.Setup(s => s.UpdateAsync(user, CancellationToken.None)).Returns(Task.FromResult(0)).Verifiable();
            store.Setup(s => s.GetRolesAsync(user, CancellationToken.None)).ReturnsAsync(new List<string>()).Verifiable();
            var userManager = MockHelpers.TestUserManager<TestUser>(store.Object);

            // Act
            var result = await userManager.AddToRolesAsync(user, roles);

            // Assert
            Assert.True(result.Succeeded);
            store.VerifyAll();
        }

        [Fact]
        public async Task AddToRolesFailsIfUserInRole()
        {
            // Setup
            var store = new Mock<IUserRoleStore<TestUser>>();
            var user = new TestUser { UserName = "Foo" };
            var roles = new[] { "A", "B", "C" };
            store.Setup(s => s.AddToRoleAsync(user, "A", CancellationToken.None))
                .Returns(Task.FromResult(0))
                .Verifiable();
            store.Setup(s => s.GetRolesAsync(user, CancellationToken.None)).ReturnsAsync(new List<string> { "B" }).Verifiable();
            var userManager = MockHelpers.TestUserManager<TestUser>(store.Object);

            // Act
            var result = await userManager.AddToRolesAsync(user, roles);

            // Assert
            IdentityResultAssert.IsFailure(result, "User already in role.");
            store.VerifyAll();
        }

        [Fact]
        public async Task RemoveFromRolesCallsStore()
        {
            // Setup
            var store = new Mock<IUserRoleStore<TestUser>>();
            var user = new TestUser { UserName = "Foo" };
            var roles = new[] { "A", "B", "C" };
            store.Setup(s => s.RemoveFromRoleAsync(user, "A", CancellationToken.None))
                .Returns(Task.FromResult(0))
                .Verifiable();
            store.Setup(s => s.RemoveFromRoleAsync(user, "B", CancellationToken.None))
                .Returns(Task.FromResult(0))
                .Verifiable();
            store.Setup(s => s.RemoveFromRoleAsync(user, "C", CancellationToken.None))
                .Returns(Task.FromResult(0))
                .Verifiable();
            store.Setup(s => s.UpdateAsync(user, CancellationToken.None)).Returns(Task.FromResult(0)).Verifiable();
            store.Setup(s => s.IsInRoleAsync(user, "A", CancellationToken.None))
                .Returns(Task.FromResult(true))
                .Verifiable();
            store.Setup(s => s.IsInRoleAsync(user, "B", CancellationToken.None))
                .Returns(Task.FromResult(true))
                .Verifiable();
            store.Setup(s => s.IsInRoleAsync(user, "C", CancellationToken.None))
                .Returns(Task.FromResult(true))
                .Verifiable();
            var userManager = MockHelpers.TestUserManager<TestUser>(store.Object);

            // Act
            var result = await userManager.RemoveFromRolesAsync(user, roles);

            // Assert
            Assert.True(result.Succeeded);
            store.VerifyAll();
        }

        [Fact]
        public async Task RemoveFromRolesFailsIfNotInRole()
        {
            // Setup
            var store = new Mock<IUserRoleStore<TestUser>>();
            var user = new TestUser { UserName = "Foo" };
            var roles = new string[] { "A", "B", "C" };
            store.Setup(s => s.RemoveFromRoleAsync(user, "A", CancellationToken.None))
                .Returns(Task.FromResult(0))
                .Verifiable();
            store.Setup(s => s.IsInRoleAsync(user, "A", CancellationToken.None))
                .Returns(Task.FromResult(true))
                .Verifiable();
            store.Setup(s => s.IsInRoleAsync(user, "B", CancellationToken.None))
                .Returns(Task.FromResult(false))
                .Verifiable();
            var userManager = MockHelpers.TestUserManager<TestUser>(store.Object);

            // Act
            var result = await userManager.RemoveFromRolesAsync(user, roles);

            // Assert
            IdentityResultAssert.IsFailure(result, "User is not in role.");
            store.VerifyAll();
        }

        [Fact]
        public async Task AddClaimsCallsStore()
        {
            // Setup
            var store = new Mock<IUserClaimStore<TestUser>>();
            var user = new TestUser { UserName = "Foo" };
            var claims = new Claim[] { new Claim("1", "1"), new Claim("2", "2"), new Claim("3", "3") };
            store.Setup(s => s.AddClaimsAsync(user, claims, CancellationToken.None))
                .Returns(Task.FromResult(0))
                .Verifiable();
            store.Setup(s => s.UpdateAsync(user, CancellationToken.None)).Returns(Task.FromResult(0)).Verifiable();
            var userManager = MockHelpers.TestUserManager<TestUser>(store.Object);

            // Act
            var result = await userManager.AddClaimsAsync(user, claims);

            // Assert
            Assert.True(result.Succeeded);
            store.VerifyAll();
        }

        [Fact]
        public async Task AddClaimCallsStore()
        {
            // Setup
            var store = new Mock<IUserClaimStore<TestUser>>();
            var user = new TestUser { UserName = "Foo" };
            var claim = new Claim("1", "1");
            store.Setup(s => s.AddClaimsAsync(user, It.IsAny<IEnumerable<Claim>>(), CancellationToken.None))
                .Returns(Task.FromResult(0))
                .Verifiable();
            store.Setup(s => s.UpdateAsync(user, CancellationToken.None)).Returns(Task.FromResult(0)).Verifiable();
            var userManager = MockHelpers.TestUserManager<TestUser>(store.Object);

            // Act
            var result = await userManager.AddClaimAsync(user, claim);

            // Assert
            Assert.True(result.Succeeded);
            store.VerifyAll();
        }

        [Fact]
        public async Task RemoveClaimsCallsStore()
        {
            // Setup
            var store = new Mock<IUserClaimStore<TestUser>>();
            var user = new TestUser { UserName = "Foo" };
            var claims = new Claim[] { new Claim("1", "1"), new Claim("2", "2"), new Claim("3", "3") };
            store.Setup(s => s.RemoveClaimsAsync(user, claims, CancellationToken.None))
                .Returns(Task.FromResult(0))
                .Verifiable();
            store.Setup(s => s.UpdateAsync(user, CancellationToken.None)).Returns(Task.FromResult(0)).Verifiable();
            var userManager = MockHelpers.TestUserManager<TestUser>(store.Object);

            // Act
            var result = await userManager.RemoveClaimsAsync(user, claims);

            // Assert
            Assert.True(result.Succeeded);
            store.VerifyAll();
        }

        [Fact]
        public async Task RemoveClaimCallsStore()
        {
            // Setup
            var store = new Mock<IUserClaimStore<TestUser>>();
            var user = new TestUser { UserName = "Foo" };
            var claim = new Claim("1", "1");
            store.Setup(s => s.RemoveClaimsAsync(user, It.IsAny<IEnumerable<Claim>>(), CancellationToken.None))
                .Returns(Task.FromResult(0))
                .Verifiable();
            store.Setup(s => s.UpdateAsync(user, CancellationToken.None)).Returns(Task.FromResult(0)).Verifiable();
            var userManager = MockHelpers.TestUserManager<TestUser>(store.Object);

            // Act
            var result = await userManager.RemoveClaimAsync(user, claim);

            // Assert
            Assert.True(result.Succeeded);
            store.VerifyAll();
        }

        [Fact]
        public async Task CheckPasswordWithNullUserReturnsFalse()
        {
            var manager = MockHelpers.TestUserManager(new EmptyStore());
            Assert.False(await manager.CheckPasswordAsync(null, "whatevs"));
        }

        [Fact]
        public async Task FindWithUnknownUserAndPasswordReturnsNull()
        {
            var manager = MockHelpers.TestUserManager(new EmptyStore());
            Assert.Null(await manager.FindByUserNamePasswordAsync("bogus", "whatevs"));
        }

        [Fact]
        public void UsersQueryableFailWhenStoreNotImplemented()
        {
            var manager = MockHelpers.TestUserManager(new NoopUserStore());
            Assert.False(manager.SupportsQueryableUsers);
            Assert.Throws<NotSupportedException>(() => manager.Users.Count());
        }

        [Fact]
        public async Task UsersEmailMethodsFailWhenStoreNotImplemented()
        {
            var manager = MockHelpers.TestUserManager(new NoopUserStore());
            Assert.False(manager.SupportsUserEmail);
            await Assert.ThrowsAsync<NotSupportedException>(() => manager.FindByEmailAsync(null));
            await Assert.ThrowsAsync<NotSupportedException>(() => manager.SetEmailAsync(null, null));
            await Assert.ThrowsAsync<NotSupportedException>(() => manager.GetEmailAsync(null));
            await Assert.ThrowsAsync<NotSupportedException>(() => manager.IsEmailConfirmedAsync(null));
            await Assert.ThrowsAsync<NotSupportedException>(() => manager.ConfirmEmailAsync(null, null));
        }

        [Fact]
        public async Task UsersPhoneNumberMethodsFailWhenStoreNotImplemented()
        {
            var manager = MockHelpers.TestUserManager(new NoopUserStore());
            Assert.False(manager.SupportsUserPhoneNumber);
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.SetPhoneNumberAsync(null, null));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.SetPhoneNumberAsync(null, null));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.GetPhoneNumberAsync(null));
        }

        [Fact]
        public async Task TokenMethodsThrowWithNoTokenProvider()
        {
            var manager = MockHelpers.TestUserManager(new NoopUserStore());
            await Assert.ThrowsAsync<NotSupportedException>(
                async () => await manager.GenerateUserTokenAsync(null, null));
            await Assert.ThrowsAsync<NotSupportedException>(
                async () => await manager.VerifyUserTokenAsync(null, null, null));
        }

        [Fact]
        public async Task PasswordMethodsFailWhenStoreNotImplemented()
        {
            var manager = MockHelpers.TestUserManager(new NoopUserStore());
            Assert.False(manager.SupportsUserPassword);
            await Assert.ThrowsAsync<NotSupportedException>(() => manager.CreateAsync(null, null));
            await Assert.ThrowsAsync<NotSupportedException>(() => manager.ChangePasswordAsync(null, null, null));
            await Assert.ThrowsAsync<NotSupportedException>(() => manager.AddPasswordAsync(null, null));
            await Assert.ThrowsAsync<NotSupportedException>(() => manager.RemovePasswordAsync(null));
            await Assert.ThrowsAsync<NotSupportedException>(() => manager.CheckPasswordAsync(null, null));
            await Assert.ThrowsAsync<NotSupportedException>(() => manager.HasPasswordAsync(null));
        }

        [Fact]
        public async Task SecurityStampMethodsFailWhenStoreNotImplemented()
        {
            var manager = MockHelpers.TestUserManager(new NoopUserStore());
            Assert.False(manager.SupportsUserSecurityStamp);
            await Assert.ThrowsAsync<NotSupportedException>(() => manager.UpdateSecurityStampAsync(null));
            await Assert.ThrowsAsync<NotSupportedException>(() => manager.GetSecurityStampAsync(null));
            await Assert.ThrowsAsync<NotSupportedException>(
                    () => manager.VerifyChangePhoneNumberTokenAsync(null, "1", "111-111-1111"));
            await Assert.ThrowsAsync<NotSupportedException>(
                    () => manager.GenerateChangePhoneNumberTokenAsync(null, "111-111-1111"));
        }

        [Fact]
        public async Task LoginMethodsFailWhenStoreNotImplemented()
        {
            var manager = MockHelpers.TestUserManager(new NoopUserStore());
            Assert.False(manager.SupportsUserLogin);
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.AddLoginAsync(null, null));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.RemoveLoginAsync(null, null, null));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.GetLoginsAsync(null));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.FindByLoginAsync(null, null));
        }

        [Fact]
        public async Task ClaimMethodsFailWhenStoreNotImplemented()
        {
            var manager = MockHelpers.TestUserManager(new NoopUserStore());
            Assert.False(manager.SupportsUserClaim);
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.AddClaimAsync(null, null));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.RemoveClaimAsync(null, null));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.GetClaimsAsync(null));
        }

        [Fact]
        public async Task TwoFactorStoreMethodsFailWhenStoreNotImplemented()
        {
            var manager = MockHelpers.TestUserManager(new NoopUserStore());
            Assert.False(manager.SupportsUserTwoFactor);
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.GetTwoFactorEnabledAsync(null));
            await
                Assert.ThrowsAsync<NotSupportedException>(async () => await manager.SetTwoFactorEnabledAsync(null, true));
        }

        [Fact]
        public async Task LockoutStoreMethodsFailWhenStoreNotImplemented()
        {
            var manager = MockHelpers.TestUserManager(new NoopUserStore());
            Assert.False(manager.SupportsUserLockout);
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.GetLockoutEnabledAsync(null));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.SetLockoutEnabledAsync(null, true));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.AccessFailedAsync(null));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.IsLockedOutAsync(null));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.ResetAccessFailedCountAsync(null));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.GetAccessFailedCountAsync(null));
        }

        [Fact]
        public async Task RoleMethodsFailWhenStoreNotImplemented()
        {
            var manager = MockHelpers.TestUserManager(new NoopUserStore());
            Assert.False(manager.SupportsUserRole);
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.AddToRoleAsync(null, "bogus"));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.AddToRolesAsync(null, null));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.GetRolesAsync(null));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.RemoveFromRoleAsync(null, "bogus"));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.RemoveFromRolesAsync(null, null));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.IsInRoleAsync(null, "bogus"));
        }

        [Fact]
        public void DisposeAfterDisposeDoesNotThrow()
        {
            var manager = MockHelpers.TestUserManager(new NoopUserStore());
            manager.Dispose();
            manager.Dispose();
        }

        [Fact]
        public async Task PasswordValidatorBlocksCreate()
        {
            // TODO: Can switch to Mock eventually
            var manager = MockHelpers.TestUserManager(new EmptyStore());
            manager.PasswordValidator = new BadPasswordValidator<TestUser>();
            IdentityResultAssert.IsFailure(await manager.CreateAsync(new TestUser(), "password"),
                BadPasswordValidator<TestUser>.ErrorMessage);
        }

        [Fact]
        public async Task ManagerPublicNullChecks()
        {
            var store = new NotImplementedStore();
            var optionsAccessor = new OptionsAccessor<IdentityOptions>(null);
            var passwordHasher = new PasswordHasher<TestUser>();
            var userValidator = new UserValidator<TestUser>();
            var passwordValidator = new PasswordValidator<TestUser>();

            Assert.Throws<ArgumentNullException>("store",
                () => new UserManager<TestUser>(null, null, null, null, null, null));
            Assert.Throws<ArgumentNullException>("optionsAccessor",
                () => new UserManager<TestUser>(store, null, null, null, null, null));
            Assert.Throws<ArgumentNullException>("passwordHasher",
                () => new UserManager<TestUser>(store, optionsAccessor, null, null, null, null));

            var manager = new UserManager<TestUser>(store, optionsAccessor, passwordHasher, userValidator, passwordValidator, null);

            Assert.Throws<ArgumentNullException>("value", () => manager.PasswordHasher = null);
            Assert.Throws<ArgumentNullException>("value", () => manager.Options = null);
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await manager.CreateAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await manager.CreateAsync(null, null));
            await
                Assert.ThrowsAsync<ArgumentNullException>("password",
                    async () => await manager.CreateAsync(new TestUser(), null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await manager.UpdateAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await manager.DeleteAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("claim", async () => await manager.AddClaimAsync(null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("claims", async () => await manager.AddClaimsAsync(null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("userName", async () => await manager.FindByNameAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("userName", async () => await manager.FindByUserNamePasswordAsync(null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("login", async () => await manager.AddLoginAsync(null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("loginProvider", 
                async () => await manager.RemoveLoginAsync(null, null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("providerKey",
                async () => await manager.RemoveLoginAsync(null, "", null));
            await Assert.ThrowsAsync<ArgumentNullException>("email", async () => await manager.FindByEmailAsync(null));
            Assert.Throws<ArgumentNullException>("twoFactorProvider",
                () => manager.RegisterTwoFactorProvider(null, null));
            Assert.Throws<ArgumentNullException>("provider", () => manager.RegisterTwoFactorProvider("bogus", null));
            await Assert.ThrowsAsync<ArgumentNullException>("roles", async () => await manager.AddToRolesAsync(new TestUser(), null));
            await Assert.ThrowsAsync<ArgumentNullException>("roles", async () => await manager.RemoveFromRolesAsync(new TestUser(), null));
        }

        [Fact]
        public async Task MethodsFailWithUnknownUserTest()
        {
            var manager = MockHelpers.TestUserManager(new EmptyStore());
            manager.UserTokenProvider = new NoOpTokenProvider();
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.GetUserNameAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.SetUserNameAsync(null, "bogus"));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.AddClaimAsync(null, new Claim("a", "b")));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.AddLoginAsync(null, new UserLoginInfo("","","")));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.AddPasswordAsync(null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.AddToRoleAsync(null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.AddToRolesAsync(null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.ChangePasswordAsync(null, null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.GetClaimsAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.GetLoginsAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.GetRolesAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.IsInRoleAsync(null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.RemoveClaimAsync(null, new Claim("a", "b")));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.RemoveLoginAsync(null, "", ""));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.RemovePasswordAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.RemoveFromRoleAsync(null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.RemoveFromRolesAsync(null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.UpdateSecurityStampAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.GetSecurityStampAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.HasPasswordAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.GeneratePasswordResetTokenAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.ResetPasswordAsync(null, null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.IsEmailConfirmedAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.GenerateEmailConfirmationTokenAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.ConfirmEmailAsync(null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.GetEmailAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.SetEmailAsync(null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.IsPhoneNumberConfirmedAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.ChangePhoneNumberAsync(null, null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.VerifyChangePhoneNumberTokenAsync(null, null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.GetPhoneNumberAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.SetPhoneNumberAsync(null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.GetTwoFactorEnabledAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.SetTwoFactorEnabledAsync(null, true));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.GenerateTwoFactorTokenAsync(null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.VerifyTwoFactorTokenAsync(null, null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.NotifyTwoFactorTokenAsync(null, null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.GetValidTwoFactorProvidersAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.VerifyUserTokenAsync(null, null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.AccessFailedAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.ResetAccessFailedCountAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.GetAccessFailedCountAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.GetLockoutEnabledAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.SetLockoutEnabledAsync(null, false));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.SetLockoutEndDateAsync(null, DateTimeOffset.UtcNow));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.GetLockoutEndDateAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.IsLockedOutAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.SendEmailAsync(null, null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.SendSmsAsync(null, null));
        }

        [Fact]
        public async Task MethodsThrowWhenDisposedTest()
        {
            var manager = MockHelpers.TestUserManager(new NoopUserStore());
            manager.Dispose();
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.AddClaimAsync(null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.AddClaimsAsync(null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.AddLoginAsync(null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.AddPasswordAsync(null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.AddToRoleAsync(null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.AddToRolesAsync(null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.ChangePasswordAsync(null, null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.GetClaimsAsync(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.GetLoginsAsync(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.GetRolesAsync(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.IsInRoleAsync(null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.RemoveClaimAsync(null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.RemoveClaimsAsync(null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.RemoveLoginAsync(null, null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.RemovePasswordAsync(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.RemoveFromRoleAsync(null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.RemoveFromRolesAsync(null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.FindByUserNamePasswordAsync(null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.FindByLoginAsync(null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.FindByIdAsync(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.FindByNameAsync(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.CreateAsync(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.CreateAsync(null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.UpdateAsync(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.DeleteAsync(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.UpdateSecurityStampAsync(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.GetSecurityStampAsync(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.GeneratePasswordResetTokenAsync(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.ResetPasswordAsync(null, null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.GenerateEmailConfirmationTokenAsync(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.IsEmailConfirmedAsync(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.ConfirmEmailAsync(null, null));
        }

        private class BadPasswordValidator<TUser> : IPasswordValidator<TUser> where TUser : class
        {
            public const string ErrorMessage = "I'm Bad.";

            public Task<IdentityResult> ValidateAsync(string password, UserManager<TUser> manager, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(IdentityResult.Failed(ErrorMessage));
            }
        }

        private class EmptyStore :
            IUserPasswordStore<TestUser>,
            IUserClaimStore<TestUser>,
            IUserLoginStore<TestUser>,
            IUserEmailStore<TestUser>,
            IUserPhoneNumberStore<TestUser>,
            IUserLockoutStore<TestUser>,
            IUserTwoFactorStore<TestUser>,
            IUserRoleStore<TestUser>,
            IUserSecurityStampStore<TestUser>
        {
            public Task<IList<Claim>> GetClaimsAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult<IList<Claim>>(new List<Claim>());
            }

            public Task AddClaimsAsync(TestUser user, IEnumerable<Claim> claim, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task RemoveClaimsAsync(TestUser user, IEnumerable<Claim> claim, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task SetEmailAsync(TestUser user, string email, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task<string> GetEmailAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult("");
            }

            public Task<bool> GetEmailConfirmedAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(false);
            }

            public Task SetEmailConfirmedAsync(TestUser user, bool confirmed, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task<TestUser> FindByEmailAsync(string email, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult<TestUser>(null);
            }

            public Task<DateTimeOffset> GetLockoutEndDateAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(DateTimeOffset.MinValue);
            }

            public Task SetLockoutEndDateAsync(TestUser user, DateTimeOffset lockoutEnd, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task<int> IncrementAccessFailedCountAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task ResetAccessFailedCountAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task<int> GetAccessFailedCountAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task<bool> GetLockoutEnabledAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(false);
            }

            public Task SetLockoutEnabledAsync(TestUser user, bool enabled, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task AddLoginAsync(TestUser user, UserLoginInfo login, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task RemoveLoginAsync(TestUser user, string loginProvider, string providerKey, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task<IList<UserLoginInfo>> GetLoginsAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult<IList<UserLoginInfo>>(new List<UserLoginInfo>());
            }

            public Task<TestUser> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult<TestUser>(null);
            }

            public void Dispose()
            {
            }

            public Task SetUserNameAsync(TestUser user, string userName, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task CreateAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task UpdateAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task DeleteAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task<TestUser> FindByIdAsync(string userId, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult<TestUser>(null);
            }

            public Task<TestUser> FindByNameAsync(string userName, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult<TestUser>(null);
            }

            public Task SetPasswordHashAsync(TestUser user, string passwordHash, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task<string> GetPasswordHashAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult<string>(null);
            }

            public Task<bool> HasPasswordAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(false);
            }

            public Task SetPhoneNumberAsync(TestUser user, string phoneNumber, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task<string> GetPhoneNumberAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult("");
            }

            public Task<bool> GetPhoneNumberConfirmedAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(false);
            }

            public Task SetPhoneNumberConfirmedAsync(TestUser user, bool confirmed, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task AddToRoleAsync(TestUser user, string roleName, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task RemoveFromRoleAsync(TestUser user, string roleName, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task<IList<string>> GetRolesAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult<IList<string>>(new List<string>());
            }

            public Task<bool> IsInRoleAsync(TestUser user, string roleName, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(false);
            }

            public Task SetSecurityStampAsync(TestUser user, string stamp, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task<string> GetSecurityStampAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult("");
            }

            public Task SetTwoFactorEnabledAsync(TestUser user, bool enabled, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task<bool> GetTwoFactorEnabledAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(false);
            }

            public Task<string> GetUserIdAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult<string>(null);
            }

            public Task<string> GetUserNameAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult<string>(null);
            }

            public Task<string> GetNormalizedUserNameAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult<string>(null);
            }

            public Task SetNormalizedUserNameAsync(TestUser user, string userName, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }
        }

        private class NoOpTokenProvider : IUserTokenProvider<TestUser>
        {
            public Task<string> GenerateAsync(string purpose, UserManager<TestUser> manager, TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult("Test");
            }

            public Task<bool> ValidateAsync(string purpose, string token, UserManager<TestUser> manager,
                TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(true);
            }

            public Task NotifyAsync(string token, UserManager<TestUser> manager, TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task<bool> IsValidProviderForUserAsync(UserManager<TestUser> manager, TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(true);
            }
        }

        private class NotImplementedStore :
            IUserPasswordStore<TestUser>,
            IUserClaimStore<TestUser>,
            IUserLoginStore<TestUser>,
            IUserRoleStore<TestUser>,
            IUserEmailStore<TestUser>,
            IUserPhoneNumberStore<TestUser>,
            IUserLockoutStore<TestUser>,
            IUserTwoFactorStore<TestUser>
        {
            public Task<IList<Claim>> GetClaimsAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task AddClaimsAsync(TestUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task RemoveClaimsAsync(TestUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task SetEmailAsync(TestUser user, string email, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<string> GetEmailAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<bool> GetEmailConfirmedAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task SetEmailConfirmedAsync(TestUser user, bool confirmed, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<TestUser> FindByEmailAsync(string email, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<DateTimeOffset> GetLockoutEndDateAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task SetLockoutEndDateAsync(TestUser user, DateTimeOffset lockoutEnd, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<int> IncrementAccessFailedCountAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task ResetAccessFailedCountAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<int> GetAccessFailedCountAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<bool> GetLockoutEnabledAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task SetLockoutEnabledAsync(TestUser user, bool enabled, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task AddLoginAsync(TestUser user, UserLoginInfo login, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task RemoveLoginAsync(TestUser user, string loginProvider, string providerKey, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<IList<UserLoginInfo>> GetLoginsAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<TestUser> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public void Dispose()
            {
                throw new NotImplementedException();
            }

            public Task<string> GetUserIdAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<string> GetUserNameAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task SetUserNameAsync(TestUser user, string userName, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task CreateAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task UpdateAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task DeleteAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<TestUser> FindByIdAsync(string userId, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<TestUser> FindByNameAsync(string userName, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task SetPasswordHashAsync(TestUser user, string passwordHash, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<string> GetPasswordHashAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<bool> HasPasswordAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task SetPhoneNumberAsync(TestUser user, string phoneNumber, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<string> GetPhoneNumberAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<bool> GetPhoneNumberConfirmedAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task SetPhoneNumberConfirmedAsync(TestUser user, bool confirmed, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task SetTwoFactorEnabledAsync(TestUser user, bool enabled, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<bool> GetTwoFactorEnabledAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task AddToRoleAsync(TestUser user, string roleName, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task RemoveFromRoleAsync(TestUser user, string roleName, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<IList<string>> GetRolesAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<bool> IsInRoleAsync(TestUser user, string roleName, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<string> GetNormalizedUserNameAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task SetNormalizedUserNameAsync(TestUser user, string userName, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }
        }
    }
}