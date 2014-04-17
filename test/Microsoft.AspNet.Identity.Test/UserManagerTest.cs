using Microsoft.AspNet.DependencyInjection.Fallback;
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

            public TestManager(IServiceProvider provider) : base(provider) { }
        }

        [Fact]
        public void ServiceProviderWireupTest()
        {
            var manager = new TestManager(TestServices.DefaultServices<TestUser, string>().BuildServiceProvider());
            Assert.NotNull(manager.PasswordHasher);
            Assert.NotNull(manager.PasswordValidator);
            Assert.NotNull(manager.UserValidator);
            Assert.NotNull(manager.StorePublic);
        }

#if NET45
        //TODO: Mock fails in K (this works fine in net45)
        [Fact]
        public async Task CreateCallsStore()
        {
            // Setup
            var store = new Mock<IUserStore<TestUser>>();
            var user = new TestUser { UserName = "Foo" };
            store.Setup(s => s.CreateAsync(user, CancellationToken.None)).Returns(Task.FromResult(0)).Verifiable();
            var validator = new Mock<UserValidator<TestUser>>();
            var userManager = new UserManager<TestUser>(store.Object);
            validator.Setup(v => v.ValidateAsync(userManager, user, CancellationToken.None)).Returns(Task.FromResult(IdentityResult.Success)).Verifiable();
            userManager.UserValidator = validator.Object;

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
            var userManager = new UserManager<TestUser>(store.Object);

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
            var validator = new Mock<UserValidator<TestUser>>();
            var userManager = new UserManager<TestUser>(store.Object);
            validator.Setup(v => v.ValidateAsync(userManager, user, CancellationToken.None)).Returns(Task.FromResult(IdentityResult.Success)).Verifiable();
            userManager.UserValidator = validator.Object;

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
            var validator = new Mock<UserValidator<TestUser>>();
            var userManager = new UserManager<TestUser>(store.Object);
            validator.Setup(v => v.ValidateAsync(userManager, user, CancellationToken.None)).Returns(Task.FromResult(IdentityResult.Success)).Verifiable();
            userManager.UserValidator = validator.Object;

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
            var userManager = new UserManager<TestUser>(store.Object);

            // Act
            var result = await userManager.FindByIdAsync(user.Id);

            // Assert
            Assert.Equal(user, result);
            store.VerifyAll();
        }

        [Fact]
        public async Task FindByNameCallsStore()
        {
            // Setup
            var store = new Mock<IUserStore<TestUser>>();
            var user = new TestUser {UserName="Foo"};
            store.Setup(s => s.FindByNameAsync(user.UserName, CancellationToken.None)).Returns(Task.FromResult(user)).Verifiable();
            var userManager = new UserManager<TestUser>(store.Object);

            // Act
            var result = await userManager.FindByNameAsync(user.UserName);

            // Assert
            Assert.Equal(user, result);
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
            var userManager = new UserManager<TestUser>(store.Object) {UserValidator = null};

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
            var roles = new string[] { "A", "B", "C" };
            store.Setup(s => s.AddToRoleAsync(user, "A", CancellationToken.None))
                .Returns(Task.FromResult(0))
                .Verifiable();
            store.Setup(s => s.GetRolesAsync(user, CancellationToken.None)).ReturnsAsync(new List<string> { "B" }).Verifiable();
            var userManager = new UserManager<TestUser>(store.Object) { UserValidator = null };

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
            var roles = new string[] { "A", "B", "C" };
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
            var userManager = new UserManager<TestUser>(store.Object) { UserValidator = null };

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
            var userManager = new UserManager<TestUser>(store.Object) { UserValidator = null };

            // Act
            var result = await userManager.RemoveFromRolesAsync(user, roles);

            // Assert
            IdentityResultAssert.IsFailure(result, "User is not in role.");
            store.VerifyAll();
        }

#endif

        [Fact]
        public async Task CheckPasswordWithNullUserReturnsFalse()
        {
            var manager = new UserManager<TestUser>(new EmptyStore());
            Assert.False(await manager.CheckPasswordAsync(null, "whatevs"));
        }

        [Fact]
        public async Task FindWithUnknownUserAndPasswordReturnsNull()
        {
            var manager = new UserManager<TestUser>(new EmptyStore());
            Assert.Null(await manager.FindByUserNamePasswordAsync("bogus", "whatevs"));
        }

        [Fact]
        public void UsersQueryableFailWhenStoreNotImplemented()
        {
            var manager = new UserManager<TestUser>(new NoopUserStore());
            Assert.False(manager.SupportsQueryableUsers);
            Assert.Throws<NotSupportedException>(() => manager.Users.Count());
        }

        [Fact]
        public async Task UsersEmailMethodsFailWhenStoreNotImplemented()
        {
            var manager = new UserManager<TestUser>(new NoopUserStore());
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
            var manager = new UserManager<TestUser>(new NoopUserStore());
            Assert.False(manager.SupportsUserPhoneNumber);
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.SetPhoneNumberAsync(null, null));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.SetPhoneNumberAsync(null, null));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.GetPhoneNumberAsync(null));
        }

        [Fact]
        public async Task TokenMethodsThrowWithNoTokenProvider()
        {
            var manager = new UserManager<TestUser>(new NoopUserStore());
            await Assert.ThrowsAsync<NotSupportedException>(
                async () => await manager.GenerateUserTokenAsync(null, null));
            await Assert.ThrowsAsync<NotSupportedException>(
                async () => await manager.VerifyUserTokenAsync(null, null, null));
        }

        [Fact]
        public async Task PasswordMethodsFailWhenStoreNotImplemented()
        {
            var manager = new UserManager<TestUser>(new NoopUserStore());
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
            var manager = new UserManager<TestUser>(new NoopUserStore());
            Assert.False(manager.SupportsUserSecurityStamp);
            await Assert.ThrowsAsync<NotSupportedException>(() => manager.UpdateSecurityStampAsync(null));
            await Assert.ThrowsAsync<NotSupportedException>(() => manager.GetSecurityStampAsync(null));
#if NET45
            await
                Assert.ThrowsAsync<NotSupportedException>(
                    () => manager.VerifyChangePhoneNumberTokenAsync(null, "1", "111-111-1111"));
            await
                Assert.ThrowsAsync<NotSupportedException>(
                    () => manager.GenerateChangePhoneNumberTokenAsync(null, "111-111-1111"));
#endif
        }

        [Fact]
        public async Task LoginMethodsFailWhenStoreNotImplemented()
        {
            var manager = new UserManager<TestUser>(new NoopUserStore());
            Assert.False(manager.SupportsUserLogin);
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.AddLoginAsync(null, null));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.RemoveLoginAsync(null, null));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.GetLoginsAsync(null));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.FindByLoginAsync(null));
        }

        [Fact]
        public async Task ClaimMethodsFailWhenStoreNotImplemented()
        {
            var manager = new UserManager<TestUser>(new NoopUserStore());
            Assert.False(manager.SupportsUserClaim);
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.AddClaimAsync(null, null));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.RemoveClaimAsync(null, null));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.GetClaimsAsync(null));
        }

        [Fact]
        public async Task TwoFactorStoreMethodsFailWhenStoreNotImplemented()
        {
            var manager = new UserManager<TestUser>(new NoopUserStore());
            Assert.False(manager.SupportsUserTwoFactor);
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.GetTwoFactorEnabledAsync(null));
            await
                Assert.ThrowsAsync<NotSupportedException>(async () => await manager.SetTwoFactorEnabledAsync(null, true));
        }

        [Fact]
        public async Task LockoutStoreMethodsFailWhenStoreNotImplemented()
        {
            var manager = new UserManager<TestUser>(new NoopUserStore());
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
            var manager = new UserManager<TestUser>(new NoopUserStore());
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
            var manager = new UserManager<TestUser>(new NoopUserStore());
            manager.Dispose();
            manager.Dispose();
        }

        [Fact]
        public async Task PasswordValidatorBlocksCreate()
        {
            // TODO: Can switch to Mock eventually
            var manager = new UserManager<TestUser>(new EmptyStore())
            {
                PasswordValidator = new BadPasswordValidtor()
            };
            IdentityResultAssert.IsFailure(await manager.CreateAsync(new TestUser(), "password"),
                BadPasswordValidtor.ErrorMessage);
        }

        [Fact]
        public async Task ManagerPublicNullChecks()
        {
            Assert.Throws<ArgumentNullException>("store",
                () => new UserManager<TestUser>((IUserStore<TestUser>) null));
            Assert.Throws<ArgumentNullException>("serviceProvider",
                () => new UserManager<TestUser>((IServiceProvider)null));
            var manager = new UserManager<TestUser>(new NotImplementedStore());
            Assert.Throws<ArgumentNullException>(() => manager.ClaimsIdentityFactory = null);
            Assert.Throws<ArgumentNullException>(() => manager.PasswordHasher = null);
            await
                Assert.ThrowsAsync<ArgumentNullException>("user",
                    async () => await manager.CreateIdentityAsync(null, "whatever"));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await manager.CreateAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await manager.CreateAsync(null, null));
            await
                Assert.ThrowsAsync<ArgumentNullException>("password",
                    async () => await manager.CreateAsync(new TestUser(), null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await manager.UpdateAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await manager.DeleteAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("claim", async () => await manager.AddClaimAsync(null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("userName", async () => await manager.FindByNameAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("userName", async () => await manager.FindByUserNamePasswordAsync(null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("login", async () => await manager.AddLoginAsync(null, null));
            await
                Assert.ThrowsAsync<ArgumentNullException>("login", async () => await manager.RemoveLoginAsync(null, null));
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
            var manager = new UserManager<TestUser>(new EmptyStore())
            {
                UserTokenProvider = new NoOpTokenProvider()
            };
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.GetUserNameAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.SetUserNameAsync(null, "bogus"));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.AddClaimAsync(null, new Claim("a", "b")));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await manager.AddLoginAsync(null, new UserLoginInfo("", "")));
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
                async () => await manager.RemoveLoginAsync(null, new UserLoginInfo("", "")));
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
                async () => await manager.ResetPassword(null, null, null));
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
            var manager = new UserManager<TestUser>(new NoopUserStore());
            manager.Dispose();
            Assert.Throws<ObjectDisposedException>(() => manager.ClaimsIdentityFactory);
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.AddClaimAsync(null, null));
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
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.RemoveLoginAsync(null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.RemovePasswordAsync(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.RemoveFromRoleAsync(null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.RemoveFromRolesAsync(null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.RemoveClaimAsync(null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.FindByUserNamePasswordAsync(null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.FindByLoginAsync(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.FindByIdAsync(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.FindByNameAsync(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.CreateAsync(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.CreateAsync(null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.CreateIdentityAsync(null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.UpdateAsync(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.DeleteAsync(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.UpdateSecurityStampAsync(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.GetSecurityStampAsync(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.GeneratePasswordResetTokenAsync(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.ResetPassword(null, null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.GenerateEmailConfirmationTokenAsync(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.IsEmailConfirmedAsync(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.ConfirmEmailAsync(null, null));
        }

        private class BadPasswordValidtor : IPasswordValidator
        {
            public const string ErrorMessage = "I'm Bad.";

            public Task<IdentityResult> ValidateAsync(string password, CancellationToken cancellationToken = default(CancellationToken))
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

            public Task AddClaimAsync(TestUser user, Claim claim, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task RemoveClaimAsync(TestUser user, Claim claim, CancellationToken cancellationToken = default(CancellationToken))
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

            public Task RemoveLoginAsync(TestUser user, UserLoginInfo login, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task<IList<UserLoginInfo>> GetLoginsAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult<IList<UserLoginInfo>>(new List<UserLoginInfo>());
            }

            public Task<TestUser> FindByLoginAsync(UserLoginInfo login, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult<TestUser>(null);
            }

            public void Dispose()
            {
            }

            public Task SetUserNameAsync(TestUser user, string userName, CancellationToken cancellationToken = new CancellationToken())
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

            public Task AddClaimAsync(TestUser user, Claim claim, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task RemoveClaimAsync(TestUser user, Claim claim, CancellationToken cancellationToken = default(CancellationToken))
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

            public Task RemoveLoginAsync(TestUser user, UserLoginInfo login, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<IList<UserLoginInfo>> GetLoginsAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<TestUser> FindByLoginAsync(UserLoginInfo login, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public void Dispose()
            {
                throw new NotImplementedException();
            }

            public Task<string> GetUserIdAsync(TestUser user, CancellationToken cancellationToken = new CancellationToken())
            {
                throw new NotImplementedException();
            }

            public Task<string> GetUserNameAsync(TestUser user, CancellationToken cancellationToken = new CancellationToken())
            {
                throw new NotImplementedException();
            }

            public Task SetUserNameAsync(TestUser user, string userName, CancellationToken cancellationToken = new CancellationToken())
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

            public Task AddToRoleAsync(TestUser user, string roleName, CancellationToken cancellationToken = new CancellationToken())
            {
                throw new NotImplementedException();
            }

            public Task RemoveFromRoleAsync(TestUser user, string roleName, CancellationToken cancellationToken = new CancellationToken())
            {
                throw new NotImplementedException();
            }

            public Task<IList<string>> GetRolesAsync(TestUser user, CancellationToken cancellationToken = new CancellationToken())
            {
                throw new NotImplementedException();
            }

            public Task<bool> IsInRoleAsync(TestUser user, string roleName, CancellationToken cancellationToken = new CancellationToken())
            {
                throw new NotImplementedException();
            }
        }
    }
}