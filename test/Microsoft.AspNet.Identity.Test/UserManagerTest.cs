using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNet.Testing;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Identity.Test
{
    public class UserManagerTest
    {
        [Fact]
        public void ServiceProviderWireupTest()
        {
            var manager = new UserManager<TestUser, string>(TestServices.DefaultServiceProvider<TestUser, string>());
            Assert.NotNull(manager.PasswordHasher);
            Assert.NotNull(manager.PasswordValidator);
            Assert.NotNull(manager.UserValidator);
        }

         //TODO: Mock fails in K (this works fine in net45)
        //[Fact]
        //public async Task CreateTest()
        //{
        //    // Setup
        //    var store = new Mock<IUserStore<TestUser, string>>();
        //    var user = new TestUser();
        //    store.Setup(s => s.Create(user)).Verifiable();
        //    var userManager = new UserManager<TestUser, string>(store.Object);

        //    // Act
        //    var result = await userManager.Create(user);

        //    // Assert
        //    Assert.True(result.Succeeded);
        //    store.VerifyAll();
        //}

        [Fact]
        public void UsersQueryableFailWhenStoreNotImplementedTest()
        {
            var manager = new UserManager<TestUser, string>(new NoopUserStore());
            Assert.False(manager.SupportsQueryableUsers);
            Assert.Throws<NotSupportedException>(() => manager.Users.Count());
        }

        [Fact]
        public async Task UsersEmailMethodsFailWhenStoreNotImplementedTest()
        {
            var manager = new UserManager<TestUser, string>(new NoopUserStore());
            Assert.False(manager.SupportsUserEmail);
            await Assert.ThrowsAsync<NotSupportedException>(() => manager.FindByEmail(null));
            await Assert.ThrowsAsync<NotSupportedException>(() => manager.SetEmail(null, null));
            await Assert.ThrowsAsync<NotSupportedException>(() => manager.GetEmail(null));
            await Assert.ThrowsAsync<NotSupportedException>(() => manager.IsEmailConfirmed(null));
            await Assert.ThrowsAsync<NotSupportedException>(() => manager.ConfirmEmail(null, null));
        }

        [Fact]
        public async Task UsersPhoneNumberMethodsFailWhenStoreNotImplementedTest()
        {
            var manager = new UserManager<TestUser, string>(new NoopUserStore());
            Assert.False(manager.SupportsUserPhoneNumber);
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.SetPhoneNumber(null, null));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.SetPhoneNumber(null, null));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.GetPhoneNumber(null));
        }

        [Fact]
        public async Task TokenMethodsThrowWithNoTokenProviderTest()
        {
            var manager = new UserManager<TestUser, string>(new NoopUserStore());
            await Assert.ThrowsAsync<NotSupportedException>(
                async () => await manager.GenerateUserToken(null, null));
            await Assert.ThrowsAsync<NotSupportedException>(
                async () => await manager.VerifyUserToken(null, null, null));
        }

        [Fact]
        public async Task PasswordMethodsFailWhenStoreNotImplementedTest()
        {
            var manager = new UserManager<TestUser, string>(new NoopUserStore());
            Assert.False(manager.SupportsUserPassword);
            await Assert.ThrowsAsync<NotSupportedException>(() => manager.Create(null, null));
            await Assert.ThrowsAsync<NotSupportedException>(() => manager.ChangePassword(null, null, null));
            await Assert.ThrowsAsync<NotSupportedException>(() => manager.AddPassword(null, null));
            await Assert.ThrowsAsync<NotSupportedException>(() => manager.RemovePassword(null));
            await Assert.ThrowsAsync<NotSupportedException>(() => manager.CheckPassword(null, null));
            await Assert.ThrowsAsync<NotSupportedException>(() => manager.HasPassword(null));
        }

        [Fact]
        public async Task SecurityStampMethodsFailWhenStoreNotImplementedTest()
        {
            var manager = new UserManager<TestUser, string>(new NoopUserStore());
            Assert.False(manager.SupportsUserSecurityStamp);
            await Assert.ThrowsAsync<NotSupportedException>(() => manager.UpdateSecurityStamp("bogus"));
            await Assert.ThrowsAsync<NotSupportedException>(() => manager.GetSecurityStamp("bogus"));
            await Assert.ThrowsAsync<NotSupportedException>(() => manager.VerifyChangePhoneNumberToken("bogus", "1", "111-111-1111"));
            await Assert.ThrowsAsync<NotSupportedException>(() => manager.GenerateChangePhoneNumberToken("bogus", "111-111-1111"));
        }

        [Fact]
        public async Task LoginMethodsFailWhenStoreNotImplementedTest()
        {
            var manager = new UserManager<TestUser, string>(new NoopUserStore());
            Assert.False(manager.SupportsUserLogin);
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.AddLogin("bogus", null));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.RemoveLogin("bogus", null));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.GetLogins("bogus"));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.Find(null));
        }

        [Fact]
        public async Task ClaimMethodsFailWhenStoreNotImplementedTest()
        {
            var manager = new UserManager<TestUser, string>(new NoopUserStore());
            Assert.False(manager.SupportsUserClaim);
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.AddClaim("bogus", null));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.RemoveClaim("bogus", null));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.GetClaims("bogus"));
        }

        [Fact]
        public async Task TwoFactorStoreMethodsFailWhenStoreNotImplementedTest()
        {
            var manager = new UserManager<TestUser, string>(new NoopUserStore());
            Assert.False(manager.SupportsUserTwoFactor);
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.GetTwoFactorEnabled("bogus"));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.SetTwoFactorEnabled("bogus", true));
        }

        [Fact]
        public async Task RoleMethodsFailWhenStoreNotImplementedTest()
        {
            var manager = new UserManager<TestUser, string>(new NoopUserStore());
            Assert.False(manager.SupportsUserRole);
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.AddToRole("bogus", null));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.GetRoles("bogus"));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.RemoveFromRole("bogus", null));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await manager.IsInRole("bogus", "bogus"));
        }

        [Fact]
        public void DisposeAfterDisposeDoesNotThrow()
        {
            var manager = new UserManager<TestUser, string>(new NoopUserStore());
            manager.Dispose();
            manager.Dispose();
        }

        private class BadPasswordValidtor : IPasswordValidator
        {
            public const string ErrorMessage = "I'm Bad.";

            public Task<IdentityResult> Validate(string password)
            {
                return Task.FromResult(IdentityResult.Failed(ErrorMessage));
            }
        }

        [Fact]
        public async Task PasswordValidatorBlocksCreate()
        {
            // TODO: Can switch to Mock eventually
            var manager = new UserManager<TestUser, string>(new EmptyStore())
            {
                PasswordValidator = new BadPasswordValidtor()
            };
            IdentityResultAssert.IsFailure(await manager.Create(new TestUser(), "password"),
                BadPasswordValidtor.ErrorMessage);
        }

        [Fact]
        public async Task ManagerPublicNullChecks()
        {
            Assert.Throws<ArgumentNullException>("store", () => new UserManager<TestUser, string>((IUserStore<TestUser, string>)null));
            var manager = new UserManager<TestUser, string>(new NotImplementedStore());
            Assert.Throws<ArgumentNullException>(() => manager.ClaimsIdentityFactory = null);
            Assert.Throws<ArgumentNullException>(() => manager.PasswordHasher = null);
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await manager.CreateIdentity(null, "whatever"));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await manager.Create(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await manager.Create(null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("password", async () => await manager.Create(new TestUser(), null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await manager.Update(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await manager.Delete(null));
            await Assert.ThrowsAsync<ArgumentNullException>("claim", async () => await manager.AddClaim("bogus", null));
            await Assert.ThrowsAsync<ArgumentNullException>("userName", async () => await manager.FindByName(null));
            await Assert.ThrowsAsync<ArgumentNullException>("userName", async () => await manager.Find(null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("login", async () => await manager.AddLogin("bogus", null));
            await Assert.ThrowsAsync<ArgumentNullException>("login", async () => await manager.RemoveLogin("bogus", null));
            await Assert.ThrowsAsync<ArgumentNullException>("email", async () => await manager.FindByEmail(null));
            Assert.Throws<ArgumentNullException>("twoFactorProvider", () => manager.RegisterTwoFactorProvider(null, null));
            Assert.Throws<ArgumentNullException>("provider", () => manager.RegisterTwoFactorProvider("bogus", null));
        }

        [Fact]
        public async Task MethodsFailWithUnknownUserTest()
        {
            var manager = new UserManager<TestUser, string>(new EmptyStore())
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
            var manager = new UserManager<TestUser, string>(new NoopUserStore());
            manager.Dispose();
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

        private class NoOpTokenProvider : IUserTokenProvider<TestUser, string>
        {

            public Task<string> Generate(string purpose, UserManager<TestUser, string> manager, TestUser user)
            {
                return Task.FromResult("Test");
            }

            public Task<bool> Validate(string purpose, string token, UserManager<TestUser, string> manager, TestUser user)
            {
                return Task.FromResult(true);
            }

            public Task Notify(string token, UserManager<TestUser, string> manager, TestUser user)
            {
                return Task.FromResult(0);
            }

            public Task<bool> IsValidProviderForUser(UserManager<TestUser, string> manager, TestUser user)
            {
                return Task.FromResult(true);
            }
        }

        private class EmptyStore :
            IUserPasswordStore<TestUser, string>,
            IUserClaimStore<TestUser, string>,
            IUserLoginStore<TestUser, string>,
            IUserEmailStore<TestUser, string>,
            IUserPhoneNumberStore<TestUser, string>,
            IUserLockoutStore<TestUser, string>,
            IUserTwoFactorStore<TestUser, string>,
            IUserRoleStore<TestUser, string>,
            IUserSecurityStampStore<TestUser, string>
        {
            public void Dispose()
            {
            }

            public Task Create(TestUser user)
            {
                return Task.FromResult(0);
            }

            public Task Update(TestUser user)
            {
                return Task.FromResult(0);
            }

            public Task Delete(TestUser user)
            {
                return Task.FromResult(0);
            }

            public Task<TestUser> FindById(string userId)
            {
                return Task.FromResult<TestUser>(null);
            }

            public Task<TestUser> FindByName(string userName)
            {
                return Task.FromResult<TestUser>(null);
            }

            public Task SetPasswordHash(TestUser user, string passwordHash)
            {
                return Task.FromResult(0);
            }

            public Task<string> GetPasswordHash(TestUser user)
            {
                return Task.FromResult<string>(null);
            }

            public Task<bool> HasPassword(TestUser user)
            {
                return Task.FromResult(false);
            }

            public Task<IList<Claim>> GetClaims(TestUser user)
            {
                return Task.FromResult<IList<Claim>>(new List<Claim>());
            }

            public Task AddClaim(TestUser user, Claim claim)
            {
                return Task.FromResult(0);
            }

            public Task RemoveClaim(TestUser user, Claim claim)
            {
                return Task.FromResult(0);
            }

            public Task AddLogin(TestUser user, UserLoginInfo login)
            {
                return Task.FromResult(0);
            }

            public Task RemoveLogin(TestUser user, UserLoginInfo login)
            {
                return Task.FromResult(0);
            }

            public Task<IList<UserLoginInfo>> GetLogins(TestUser user)
            {
                return Task.FromResult<IList<UserLoginInfo>>(new List<UserLoginInfo>());
            }

            public Task<TestUser> Find(UserLoginInfo login)
            {
                return Task.FromResult<TestUser>(null);
            }

            public Task SetEmail(TestUser user, string email)
            {
                return Task.FromResult(0);
            }

            public Task<string> GetEmail(TestUser user)
            {
                return Task.FromResult("");
            }

            public Task<bool> GetEmailConfirmed(TestUser user)
            {
                return Task.FromResult(false);
            }

            public Task SetEmailConfirmed(TestUser user, bool confirmed)
            {
                return Task.FromResult(0);
            }

            public Task<TestUser> FindByEmail(string email)
            {
                return Task.FromResult<TestUser>(null);
            }

            public Task SetPhoneNumber(TestUser user, string phoneNumber)
            {
                return Task.FromResult(0);
            }

            public Task<string> GetPhoneNumber(TestUser user)
            {
                return Task.FromResult("");
            }

            public Task<bool> GetPhoneNumberConfirmed(TestUser user)
            {
                return Task.FromResult(false);
            }

            public Task SetPhoneNumberConfirmed(TestUser user, bool confirmed)
            {
                return Task.FromResult(0);
            }

            public Task<DateTimeOffset> GetLockoutEndDate(TestUser user)
            {
                return Task.FromResult(DateTimeOffset.MinValue);
            }

            public Task SetLockoutEndDate(TestUser user, DateTimeOffset lockoutEnd)
            {
                return Task.FromResult(0);
            }

            public Task<int> IncrementAccessFailedCount(TestUser user)
            {
                return Task.FromResult(0);
            }

            public Task ResetAccessFailedCount(TestUser user)
            {
                return Task.FromResult(0);
            }

            public Task<int> GetAccessFailedCount(TestUser user)
            {
                return Task.FromResult(0);
            }

            public Task<bool> GetLockoutEnabled(TestUser user)
            {
                return Task.FromResult(false);
            }

            public Task SetLockoutEnabled(TestUser user, bool enabled)
            {
                return Task.FromResult(0);
            }

            public Task SetTwoFactorEnabled(TestUser user, bool enabled)
            {
                return Task.FromResult(0);
            }

            public Task<bool> GetTwoFactorEnabled(TestUser user)
            {
                return Task.FromResult(false);
            }

            public Task AddToRole(TestUser user, string roleName)
            {
                return Task.FromResult(0);
            }

            public Task RemoveFromRole(TestUser user, string roleName)
            {
                return Task.FromResult(0);
            }

            public Task<IList<string>> GetRoles(TestUser user)
            {
                return Task.FromResult<IList<string>>(new List<string>());
            }

            public Task<bool> IsInRole(TestUser user, string roleName)
            {
                return Task.FromResult(false);
            }

            public Task SetSecurityStamp(TestUser user, string stamp)
            {
                return Task.FromResult(0);
            }

            public Task<string> GetSecurityStamp(TestUser user)
            {
                return Task.FromResult("");
            }
        }

        private class NotImplementedStore : 
            IUserPasswordStore<TestUser, string>, 
            IUserClaimStore<TestUser, string>,
            IUserLoginStore<TestUser, string>,
            IUserEmailStore<TestUser, string>,
            IUserPhoneNumberStore<TestUser, string>,
            IUserLockoutStore<TestUser, string>,
            IUserTwoFactorStore<TestUser, string>
        {
            public void Dispose()
            {
                throw new NotImplementedException();
            }

            public Task Create(TestUser user)
            {
                throw new NotImplementedException();
            }

            public Task Update(TestUser user)
            {
                throw new NotImplementedException();
            }

            public Task Delete(TestUser user)
            {
                throw new NotImplementedException();
            }

            public Task<TestUser> FindById(string userId)
            {
                throw new NotImplementedException();
            }

            public Task<TestUser> FindByName(string userName)
            {
                throw new NotImplementedException();
            }

            public Task SetPasswordHash(TestUser user, string passwordHash)
            {
                throw new NotImplementedException();
            }

            public Task<string> GetPasswordHash(TestUser user)
            {
                throw new NotImplementedException();
            }

            public Task<bool> HasPassword(TestUser user)
            {
                throw new NotImplementedException();
            }

            public Task<IList<Claim>> GetClaims(TestUser user)
            {
                throw new NotImplementedException();
            }

            public Task AddClaim(TestUser user, Claim claim)
            {
                throw new NotImplementedException();
            }

            public Task RemoveClaim(TestUser user, Claim claim)
            {
                throw new NotImplementedException();
            }

            public Task AddLogin(TestUser user, UserLoginInfo login)
            {
                throw new NotImplementedException();
            }

            public Task RemoveLogin(TestUser user, UserLoginInfo login)
            {
                throw new NotImplementedException();
            }

            public Task<IList<UserLoginInfo>> GetLogins(TestUser user)
            {
                throw new NotImplementedException();
            }

            public Task<TestUser> Find(UserLoginInfo login)
            {
                throw new NotImplementedException();
            }

            public Task SetEmail(TestUser user, string email)
            {
                throw new NotImplementedException();
            }

            public Task<string> GetEmail(TestUser user)
            {
                throw new NotImplementedException();
            }

            public Task<bool> GetEmailConfirmed(TestUser user)
            {
                throw new NotImplementedException();
            }

            public Task SetEmailConfirmed(TestUser user, bool confirmed)
            {
                throw new NotImplementedException();
            }

            public Task<TestUser> FindByEmail(string email)
            {
                throw new NotImplementedException();
            }

            public Task SetPhoneNumber(TestUser user, string phoneNumber)
            {
                throw new NotImplementedException();
            }

            public Task<string> GetPhoneNumber(TestUser user)
            {
                throw new NotImplementedException();
            }

            public Task<bool> GetPhoneNumberConfirmed(TestUser user)
            {
                throw new NotImplementedException();
            }

            public Task SetPhoneNumberConfirmed(TestUser user, bool confirmed)
            {
                throw new NotImplementedException();
            }

            public Task<DateTimeOffset> GetLockoutEndDate(TestUser user)
            {
                throw new NotImplementedException();
            }

            public Task SetLockoutEndDate(TestUser user, DateTimeOffset lockoutEnd)
            {
                throw new NotImplementedException();
            }

            public Task<int> IncrementAccessFailedCount(TestUser user)
            {
                throw new NotImplementedException();
            }

            public Task ResetAccessFailedCount(TestUser user)
            {
                throw new NotImplementedException();
            }

            public Task<int> GetAccessFailedCount(TestUser user)
            {
                throw new NotImplementedException();
            }

            public Task<bool> GetLockoutEnabled(TestUser user)
            {
                throw new NotImplementedException();
            }

            public Task SetLockoutEnabled(TestUser user, bool enabled)
            {
                throw new NotImplementedException();
            }

            public Task SetTwoFactorEnabled(TestUser user, bool enabled)
            {
                throw new NotImplementedException();
            }

            public Task<bool> GetTwoFactorEnabled(TestUser user)
            {
                throw new NotImplementedException();
            }
        }
    }
}