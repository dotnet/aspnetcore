using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.DependencyInjection.Fallback;
using Microsoft.AspNet.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Identity.Test
{
    public class UserManagerTest
    {
        private class TestManager : UserManager<TestUser>
        {
            public IUserStore<TestUser> StorePublic { get { return base.Store; } }

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
            store.Setup(s => s.Create(user, CancellationToken.None)).Returns(Task.FromResult(0)).Verifiable();
            var validator = new Mock<UserValidator<TestUser>>();
            var userManager = new UserManager<TestUser>(store.Object);
            validator.Setup(v => v.Validate(userManager, user, CancellationToken.None)).Returns(Task.FromResult(IdentityResult.Success)).Verifiable();
            userManager.UserValidator = validator.Object;

            // Act
            var result = await userManager.Create(user);

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
            store.Setup(s => s.Delete(user, CancellationToken.None)).Returns(Task.FromResult(0)).Verifiable();
            var userManager = new UserManager<TestUser>(store.Object);

            // Act
            var result = await userManager.Delete(user);

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
            store.Setup(s => s.Update(user, CancellationToken.None)).Returns(Task.FromResult(0)).Verifiable();
            var validator = new Mock<UserValidator<TestUser>>();
            var userManager = new UserManager<TestUser>(store.Object);
            validator.Setup(v => v.Validate(userManager, user, CancellationToken.None)).Returns(Task.FromResult(IdentityResult.Success)).Verifiable();
            userManager.UserValidator = validator.Object;

            // Act
            var result = await userManager.Update(user);

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
            store.Setup(s => s.FindById(user.Id, CancellationToken.None)).Returns(Task.FromResult(user)).Verifiable();
            var userManager = new UserManager<TestUser>(store.Object);

            // Act
            var result = await userManager.FindById(user.Id);

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
            store.Setup(s => s.FindByName(user.UserName, CancellationToken.None)).Returns(Task.FromResult(user)).Verifiable();
            var userManager = new UserManager<TestUser>(store.Object);

            // Act
            var result = await userManager.FindByName(user.UserName);

            // Assert
            Assert.Equal(user, result);
            store.VerifyAll();
        }

#endif

        [Fact]
        public async Task CheckPasswordWithNullUserReturnsFalse()
        {
            var manager = new UserManager<TestUser>(new EmptyStore());
            Assert.False(await manager.CheckPassword(null, "whatevs"));
        }

        [Fact]
        public async Task FindWithUnknownUserAndPasswordReturnsNull()
        {
            var manager = new UserManager<TestUser>(new EmptyStore());
            Assert.Null(await manager.Find("bogus", "whatevs"));
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
            await Assert.ThrowsAsync<NotSupportedException>(() => manager.FindByEmail(null));
            await Assert.ThrowsAsync<NotSupportedException>(() => manager.SetEmail(null, null));
            await Assert.ThrowsAsync<NotSupportedException>(() => manager.GetEmail(null));
            await Assert.ThrowsAsync<NotSupportedException>(() => manager.IsEmailConfirmed(null));
            await Assert.ThrowsAsync<NotSupportedException>(() => manager.ConfirmEmail(null, null));
        }

        [Fact]
        public async Task UsersPhoneNumberMethodsFailWhenStoreNotImplemented()
        {
            var manager = new UserManager<TestUser>(new NoopUserStore());
            Assert.False(manager.SupportsUserPhoneNumber);
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.SetPhoneNumber(null, null));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.SetPhoneNumber(null, null));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.GetPhoneNumber(null));
        }

        [Fact]
        public async Task TokenMethodsThrowWithNoTokenProvider()
        {
            var manager = new UserManager<TestUser>(new NoopUserStore());
            await Assert.ThrowsAsync<NotSupportedException>(
                async () => await manager.GenerateUserToken(null, null));
            await Assert.ThrowsAsync<NotSupportedException>(
                async () => await manager.VerifyUserToken(null, null, null));
        }

        [Fact]
        public async Task PasswordMethodsFailWhenStoreNotImplemented()
        {
            var manager = new UserManager<TestUser>(new NoopUserStore());
            Assert.False(manager.SupportsUserPassword);
            await Assert.ThrowsAsync<NotSupportedException>(() => manager.Create(null, null));
            await Assert.ThrowsAsync<NotSupportedException>(() => manager.ChangePassword(null, null, null));
            await Assert.ThrowsAsync<NotSupportedException>(() => manager.AddPassword(null, null));
            await Assert.ThrowsAsync<NotSupportedException>(() => manager.RemovePassword(null));
            await Assert.ThrowsAsync<NotSupportedException>(() => manager.CheckPassword(null, null));
            await Assert.ThrowsAsync<NotSupportedException>(() => manager.HasPassword(null));
        }

        [Fact]
        public async Task SecurityStampMethodsFailWhenStoreNotImplemented()
        {
            var manager = new UserManager<TestUser>(new NoopUserStore());
            Assert.False(manager.SupportsUserSecurityStamp);
            await Assert.ThrowsAsync<NotSupportedException>(() => manager.UpdateSecurityStamp("bogus"));
            await Assert.ThrowsAsync<NotSupportedException>(() => manager.GetSecurityStamp("bogus"));
            await
                Assert.ThrowsAsync<NotSupportedException>(
                    () => manager.VerifyChangePhoneNumberToken("bogus", "1", "111-111-1111"));
            await
                Assert.ThrowsAsync<NotSupportedException>(
                    () => manager.GenerateChangePhoneNumberToken("bogus", "111-111-1111"));
        }

        [Fact]
        public async Task LoginMethodsFailWhenStoreNotImplemented()
        {
            var manager = new UserManager<TestUser>(new NoopUserStore());
            Assert.False(manager.SupportsUserLogin);
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.AddLogin("bogus", null));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.RemoveLogin("bogus", null));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.GetLogins("bogus"));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.Find(null));
        }

        [Fact]
        public async Task ClaimMethodsFailWhenStoreNotImplemented()
        {
            var manager = new UserManager<TestUser>(new NoopUserStore());
            Assert.False(manager.SupportsUserClaim);
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.AddClaim("bogus", null));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.RemoveClaim("bogus", null));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.GetClaims("bogus"));
        }

        [Fact]
        public async Task TwoFactorStoreMethodsFailWhenStoreNotImplemented()
        {
            var manager = new UserManager<TestUser>(new NoopUserStore());
            Assert.False(manager.SupportsUserTwoFactor);
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.GetTwoFactorEnabled("bogus"));
            await
                Assert.ThrowsAsync<NotSupportedException>(async () => await manager.SetTwoFactorEnabled("bogus", true));
        }

        [Fact]
        public async Task LockoutStoreMethodsFailWhenStoreNotImplemented()
        {
            var manager = new UserManager<TestUser>(new NoopUserStore());
            Assert.False(manager.SupportsUserLockout);
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.GetLockoutEnabled("bogus"));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.SetLockoutEnabled("bogus", true));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.AccessFailed("bogus"));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.IsLockedOut("bogus"));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.ResetAccessFailedCount("bogus"));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.GetAccessFailedCount("bogus"));
        }

        [Fact]
        public async Task RoleMethodsFailWhenStoreNotImplemented()
        {
            var manager = new UserManager<TestUser>(new NoopUserStore());
            Assert.False(manager.SupportsUserRole);
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.AddToRole("bogus", null));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.GetRoles("bogus"));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.RemoveFromRole("bogus", null));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.IsInRole("bogus", "bogus"));
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
            IdentityResultAssert.IsFailure(await manager.Create(new TestUser(), "password"),
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
                    async () => await manager.CreateIdentity(null, "whatever"));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await manager.Create(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await manager.Create(null, null));
            await
                Assert.ThrowsAsync<ArgumentNullException>("password",
                    async () => await manager.Create(new TestUser(), null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await manager.Update(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await manager.Delete(null));
            await Assert.ThrowsAsync<ArgumentNullException>("claim", async () => await manager.AddClaim("bogus", null));
            await Assert.ThrowsAsync<ArgumentNullException>("userName", async () => await manager.FindByName(null));
            await Assert.ThrowsAsync<ArgumentNullException>("userName", async () => await manager.Find(null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("login", async () => await manager.AddLogin("bogus", null));
            await
                Assert.ThrowsAsync<ArgumentNullException>("login", async () => await manager.RemoveLogin("bogus", null));
            await Assert.ThrowsAsync<ArgumentNullException>("email", async () => await manager.FindByEmail(null));
            Assert.Throws<ArgumentNullException>("twoFactorProvider",
                () => manager.RegisterTwoFactorProvider(null, null));
            Assert.Throws<ArgumentNullException>("provider", () => manager.RegisterTwoFactorProvider("bogus", null));
        }

        [Fact]
        public async Task MethodsFailWithUnknownUserTest()
        {
            var manager = new UserManager<TestUser>(new EmptyStore())
            {
                UserTokenProvider = new NoOpTokenProvider()
            };
            const string error = "UserId not found.";
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.AddClaim(null, new Claim("a", "b")), error);
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.AddLogin(null, new UserLoginInfo("", "")), error);
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.AddPassword(null, null), error);
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.AddToRole(null, null), error);
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.ChangePassword(null, null, null), error);
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.GetClaims(null), error);
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.GetLogins(null), error);
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.GetRoles(null), error);
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.IsInRole(null, null), error);
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.RemoveClaim(null, new Claim("a", "b")), error);
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.RemoveLogin(null, new UserLoginInfo("", "")), error);
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.RemovePassword(null), error);
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.RemoveFromRole(null, null), error);
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.UpdateSecurityStamp(null), error);
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.GetSecurityStamp(null), error);
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.HasPassword(null), error);
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.GeneratePasswordResetToken(null), error);
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.ResetPassword(null, null, null), error);
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.IsEmailConfirmed(null), error);
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.GenerateEmailConfirmationToken(null), error);
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.ConfirmEmail(null, null), error);
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.GetEmail(null), error);
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.SetEmail(null, null), error);
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.IsPhoneNumberConfirmed(null), error);
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.ChangePhoneNumber(null, null, null), error);
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.VerifyChangePhoneNumberToken(null, null, null), error);
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.GetPhoneNumber(null), error);
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.SetPhoneNumber(null, null), error);
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.GetTwoFactorEnabled(null), error);
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.SetTwoFactorEnabled(null, true), error);
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.GenerateTwoFactorToken(null, null), error);
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.VerifyTwoFactorToken(null, null, null), error);
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.NotifyTwoFactorToken(null, null, null), error);
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.GetValidTwoFactorProviders(null), error);
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.VerifyUserToken(null, null, null), error);
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.AccessFailed(null), error);
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.ResetAccessFailedCount(null), error);
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.GetAccessFailedCount(null), error);
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.GetLockoutEnabled(null), error);
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.SetLockoutEnabled(null, false), error);
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.SetLockoutEndDate(null, DateTimeOffset.UtcNow), error);
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.GetLockoutEndDate(null), error);
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.IsLockedOut(null), error);
        }

        [Fact]
        public async Task MethodsThrowWhenDisposedTest()
        {
            var manager = new UserManager<TestUser>(new NoopUserStore());
            manager.Dispose();
            Assert.Throws<ObjectDisposedException>(() => manager.ClaimsIdentityFactory);
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.AddClaim("bogus", null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.AddLogin("bogus", null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.AddPassword("bogus", null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.AddToRole("bogus", null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.ChangePassword("bogus", null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.GetClaims("bogus"));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.GetLogins("bogus"));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.GetRoles("bogus"));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.IsInRole("bogus", null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.RemoveClaim("bogus", null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.RemoveLogin("bogus", null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.RemovePassword("bogus"));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.RemoveFromRole("bogus", null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.RemoveClaim("bogus", null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.Find("bogus", null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.Find(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.FindById(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.FindByName(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.Create(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.Create(null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.CreateIdentity(null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.Update(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.Delete(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.UpdateSecurityStamp(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.GetSecurityStamp(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.GeneratePasswordResetToken(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.ResetPassword(null, null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.GenerateEmailConfirmationToken(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.IsEmailConfirmed(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.ConfirmEmail(null, null));
        }

        private class BadPasswordValidtor : IPasswordValidator
        {
            public const string ErrorMessage = "I'm Bad.";

            public Task<IdentityResult> Validate(string password, CancellationToken cancellationToken = default(CancellationToken))
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
            public Task<IList<Claim>> GetClaims(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult<IList<Claim>>(new List<Claim>());
            }

            public Task AddClaim(TestUser user, Claim claim, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task RemoveClaim(TestUser user, Claim claim, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task SetEmail(TestUser user, string email, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task<string> GetEmail(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult("");
            }

            public Task<bool> GetEmailConfirmed(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(false);
            }

            public Task SetEmailConfirmed(TestUser user, bool confirmed, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task<TestUser> FindByEmail(string email, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult<TestUser>(null);
            }

            public Task<DateTimeOffset> GetLockoutEndDate(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(DateTimeOffset.MinValue);
            }

            public Task SetLockoutEndDate(TestUser user, DateTimeOffset lockoutEnd, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task<int> IncrementAccessFailedCount(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task ResetAccessFailedCount(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task<int> GetAccessFailedCount(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task<bool> GetLockoutEnabled(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(false);
            }

            public Task SetLockoutEnabled(TestUser user, bool enabled, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task AddLogin(TestUser user, UserLoginInfo login, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task RemoveLogin(TestUser user, UserLoginInfo login, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task<IList<UserLoginInfo>> GetLogins(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult<IList<UserLoginInfo>>(new List<UserLoginInfo>());
            }

            public Task<TestUser> Find(UserLoginInfo login, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult<TestUser>(null);
            }

            public void Dispose()
            {
            }

            public Task Create(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task Update(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task Delete(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task<TestUser> FindById(string userId, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult<TestUser>(null);
            }

            public Task<TestUser> FindByName(string userName, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult<TestUser>(null);
            }

            public Task SetPasswordHash(TestUser user, string passwordHash, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task<string> GetPasswordHash(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult<string>(null);
            }

            public Task<bool> HasPassword(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(false);
            }

            public Task SetPhoneNumber(TestUser user, string phoneNumber, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task<string> GetPhoneNumber(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult("");
            }

            public Task<bool> GetPhoneNumberConfirmed(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(false);
            }

            public Task SetPhoneNumberConfirmed(TestUser user, bool confirmed, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task AddToRole(TestUser user, string roleName, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task RemoveFromRole(TestUser user, string roleName, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task<IList<string>> GetRoles(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult<IList<string>>(new List<string>());
            }

            public Task<bool> IsInRole(TestUser user, string roleName, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(false);
            }

            public Task SetSecurityStamp(TestUser user, string stamp, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task<string> GetSecurityStamp(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult("");
            }

            public Task SetTwoFactorEnabled(TestUser user, bool enabled, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task<bool> GetTwoFactorEnabled(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(false);
            }

            public Task<string> GetUserId(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult<string>(null);
            }

            public Task<string> GetUserName(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult<string>(null);
            }
        }

        private class NoOpTokenProvider : IUserTokenProvider<TestUser>
        {
            public Task<string> Generate(string purpose, UserManager<TestUser> manager, TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult("Test");
            }

            public Task<bool> Validate(string purpose, string token, UserManager<TestUser> manager,
                TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(true);
            }

            public Task Notify(string token, UserManager<TestUser> manager, TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(0);
            }

            public Task<bool> IsValidProviderForUser(UserManager<TestUser> manager, TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Task.FromResult(true);
            }
        }

        private class NotImplementedStore :
            IUserPasswordStore<TestUser>,
            IUserClaimStore<TestUser>,
            IUserLoginStore<TestUser>,
            IUserEmailStore<TestUser>,
            IUserPhoneNumberStore<TestUser>,
            IUserLockoutStore<TestUser>,
            IUserTwoFactorStore<TestUser>
        {
            public Task<IList<Claim>> GetClaims(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task AddClaim(TestUser user, Claim claim, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task RemoveClaim(TestUser user, Claim claim, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task SetEmail(TestUser user, string email, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<string> GetEmail(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<bool> GetEmailConfirmed(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task SetEmailConfirmed(TestUser user, bool confirmed, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<TestUser> FindByEmail(string email, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<DateTimeOffset> GetLockoutEndDate(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task SetLockoutEndDate(TestUser user, DateTimeOffset lockoutEnd, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<int> IncrementAccessFailedCount(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task ResetAccessFailedCount(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<int> GetAccessFailedCount(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<bool> GetLockoutEnabled(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task SetLockoutEnabled(TestUser user, bool enabled, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task AddLogin(TestUser user, UserLoginInfo login, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task RemoveLogin(TestUser user, UserLoginInfo login, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<IList<UserLoginInfo>> GetLogins(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<TestUser> Find(UserLoginInfo login, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public void Dispose()
            {
                throw new NotImplementedException();
            }

            public Task<string> GetUserId(TestUser user, CancellationToken cancellationToken = new CancellationToken())
            {
                throw new NotImplementedException();
            }

            public Task<string> GetUserName(TestUser user, CancellationToken cancellationToken = new CancellationToken())
            {
                throw new NotImplementedException();
            }

            public Task Create(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task Update(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task Delete(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<TestUser> FindById(string userId, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<TestUser> FindByName(string userName, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task SetPasswordHash(TestUser user, string passwordHash, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<string> GetPasswordHash(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<bool> HasPassword(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task SetPhoneNumber(TestUser user, string phoneNumber, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<string> GetPhoneNumber(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<bool> GetPhoneNumberConfirmed(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task SetPhoneNumberConfirmed(TestUser user, bool confirmed, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task SetTwoFactorEnabled(TestUser user, bool enabled, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<bool> GetTwoFactorEnabled(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }
        }
    }
}