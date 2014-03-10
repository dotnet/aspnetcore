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

        // TODO: Mock fails in K (this works fine in net45)
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
        public void UsersEmailMethodsFailWhenStoreNotImplementedTest()
        {
            var manager = new UserManager<TestUser, string>(new NoopUserStore());
            Assert.False(manager.SupportsUserEmail);
            //Assert.Throws<NotSupportedException>(() => manager.FindByEmail(null));
            //Assert.Throws<NotSupportedException>(() => manager.SetEmail(null, null));
            //Assert.Throws<NotSupportedException>(() => manager.GetEmail(null));
            //Assert.Throws<NotSupportedException>(() => manager.IsEmailConfirmed(null));
            //Assert.Throws<NotSupportedException>(() => manager.ConfirmEmail(null, null));
        }

        [Fact]
        public void UsersPhoneNumberMethodsFailWhenStoreNotImplementedTest()
        {
            var manager = new UserManager<TestUser, string>(new NoopUserStore());
            Assert.False(manager.SupportsUserPhoneNumber);
            //Assert.Throws<NotSupportedException>(() => manager.SetPhoneNumber(null, null)));
            //Assert.Throws<NotSupportedException>(() => manager.SetPhoneNumber(null, null));
            //Assert.Throws<NotSupportedException>(() => manager.GetPhoneNumber(null));
        }

        //[Fact]
        //public void TokenMethodsThrowWithNoTokenProviderTest()
        //{
        //    var manager = new UserManager<TestUser, string>(new NoopUserStore());
        //    Assert.Throws<NotSupportedException>(
        //        () => AsyncHelper.RunSync(() => manager.GenerateUserTokenAsync(null, null)));
        //    Assert.Throws<NotSupportedException>(
        //        () => AsyncHelper.RunSync(() => manager.VerifyUserTokenAsync(null, null, null)));
        //}

        [Fact]
        public void PasswordMethodsFailWhenStoreNotImplementedTest()
        {
            var manager = new UserManager<TestUser, string>(new NoopUserStore());
            Assert.False(manager.SupportsUserPassword);
            //Assert.Throws<NotSupportedException>(() => AsyncHelper.RunSync(() => manager.CreateAsync(null, null)));
            //Assert.Throws<NotSupportedException>(
            //    () => AsyncHelper.RunSync(() => manager.ChangePasswordAsync(null, null, null)));
            //Assert.Throws<NotSupportedException>(() => AsyncHelper.RunSync(() => manager.AddPasswordAsync(null, null)));
            //Assert.Throws<NotSupportedException>(() => AsyncHelper.RunSync(() => manager.RemovePasswordAsync(null)));
            //Assert.Throws<NotSupportedException>(() => AsyncHelper.RunSync(() => manager.CheckPasswordAsync(null, null)));
            //Assert.Throws<NotSupportedException>(() => AsyncHelper.RunSync(() => manager.HasPasswordAsync(null)));
        }

        [Fact]
        public void SecurityStampMethodsFailWhenStoreNotImplementedTest()
        {
            var manager = new UserManager<TestUser, string>(new NoopUserStore());
            Assert.False(manager.SupportsUserSecurityStamp);
            //Assert.Throws<NotSupportedException>(
            //    () => AsyncHelper.RunSync(() => manager.UpdateSecurityStampAsync("bogus")));
            //Assert.Throws<NotSupportedException>(() => AsyncHelper.RunSync(() => manager.GetSecurityStampAsync("bogus")));
            //Assert.Throws<NotSupportedException>(
            //    () => AsyncHelper.RunSync(() => manager.VerifyChangePhoneNumberTokenAsync("bogus", "1", "111-111-1111")));
            //Assert.Throws<NotSupportedException>(
            //    () => AsyncHelper.RunSync(() => manager.GenerateChangePhoneNumberTokenAsync("bogus", "111-111-1111")));
        }

        [Fact]
        public void LoginMethodsFailWhenStoreNotImplementedTest()
        {
            var manager = new UserManager<TestUser, string>(new NoopUserStore());
            Assert.False(manager.SupportsUserLogin);
            //Assert.Throws<NotSupportedException>(() => AsyncHelper.RunSync(() => manager.AddLoginAsync("bogus", null)));
            //Assert.Throws<NotSupportedException>(
            //    () => AsyncHelper.RunSync(() => manager.RemoveLoginAsync("bogus", null)));
            //Assert.Throws<NotSupportedException>(() => AsyncHelper.RunSync(() => manager.GetLoginsAsync("bogus")));
            //Assert.Throws<NotSupportedException>(() => AsyncHelper.RunSync(() => manager.FindAsync(null)));
        }

        [Fact]
        public void ClaimMethodsFailWhenStoreNotImplementedTest()
        {
            var manager = new UserManager<TestUser, string>(new NoopUserStore());
            Assert.False(manager.SupportsUserClaim);
            //Assert.Throws<NotSupportedException>(() => manager.AddClaim("bogus", null));
            //Assert.Throws<NotSupportedException>(() => manager.RemoveClaim("bogus", null));
            //Assert.Throws<NotSupportedException>(() => manager.GetClaims("bogus"));
        }

        [Fact]
        public void TwoFactorStoreMethodsFailWhenStoreNotImplementedTest()
        {
            var manager = new UserManager<TestUser, string>(new NoopUserStore());
            Assert.False(manager.SupportsUserTwoFactor);
            //Assert.Throws<NotSupportedException>(() => manager.GetTwoFactorEnabled("bogus"));
            //Assert.Throws<NotSupportedException>(() => manager.SetTwoFactorEnabled("bogus", true));
        }

        [Fact]
        public void RoleMethodsFailWhenStoreNotImplementedTest()
        {
            var manager = new UserManager<TestUser, string>(new NoopUserStore());
            Assert.False(manager.SupportsUserRole);
            //Assert.Throws<NotSupportedException>(() => manager.AddToRole("bogus", null).Wait());
            //Assert.Throws<NotSupportedException>(async () => await manager.GetRoles("bogus"));
            //Assert.Throws<NotSupportedException>(async () => await manager.RemoveFromRole("bogus", null));
            //Assert.Throws<NotSupportedException>(async () => await manager.IsInRole("bogus", "bogus"));
        }

        [Fact]
        public void DisposeAfterDisposeWorksTest()
        {
            var manager = new UserManager<TestUser, string>(new NoopUserStore());
            manager.Dispose();
            manager.Dispose();
        }

        [Fact]
        public void ManagerPublicNullCheckTest()
        {
            Assert.Throws<ArgumentNullException>(() => new UserManager<TestUser, string>((IUserStore<TestUser, string>)null));
            var manager = new UserManager<TestUser, string>(new NoopUserStore());
            Assert.Throws<ArgumentNullException>(() => manager.ClaimsIdentityFactory = null);
            Assert.Throws<ArgumentNullException>(() => manager.PasswordHasher = null);
            //Assert.Throws<ArgumentNullException>(() => manager.CreateIdentity(null, "whatever"));
            //Assert.Throws<ArgumentNullException>(() => manager.Create(null));
            //Assert.Throws<ArgumentNullException>(() => manager.Create(null, null));
            //Assert.Throws<ArgumentNullException>(() => manager.Create(new TestUser(), null));
            //Assert.Throws<ArgumentNullException>(() => manager.Update(null));
            //Assert.Throws<ArgumentNullException>(() => manager.Delete(null));
            //Assert.Throws<ArgumentNullException>(() => manager.AddClaim("bogus", null));
            //Assert.Throws<ArgumentNullException>(() => manager.FindByName(null));
            //Assert.Throws<ArgumentNullException>(() => manager.Find(null, null));
            //Assert.Throws<ArgumentNullException>(() => manager.AddLogin("bogus", null));
            //Assert.Throws<ArgumentNullException>(() => manager.RemoveLogin("bogus", null));
            //Assert.Throws<ArgumentNullException>(() => manager.FindByEmail(null));
            Assert.Throws<ArgumentNullException>(() => manager.RegisterTwoFactorProvider(null, null));
            Assert.Throws<ArgumentNullException>(() => manager.RegisterTwoFactorProvider("bogus", null));
        }

        //[Fact]
        //public void MethodsFailWithUnknownUserTest()
        //{
        //    var db = UnitTestHelper.CreateDefaultDb();
        //    var manager = new UserManager<IdentityUser>(new UserStore<IdentityUser>(db));
        //    manager.UserTokenProvider = new NoOpTokenProvider();
        //    var error = "UserId not found.";
        //    ExceptionHelper.ThrowsWithError<InvalidOperationException>(
        //        () => AsyncHelper.RunSync(() => manager.AddClaimAsync(null, new Claim("a", "b"))), error);
        //    ExceptionHelper.ThrowsWithError<InvalidOperationException>(
        //        () => AsyncHelper.RunSync(() => manager.AddLoginAsync(null, new UserLoginInfo("", ""))), error);
        //    ExceptionHelper.ThrowsWithError<InvalidOperationException>(
        //        () => AsyncHelper.RunSync(() => manager.AddPasswordAsync(null, null)), error);
        //    ExceptionHelper.ThrowsWithError<InvalidOperationException>(
        //        () => AsyncHelper.RunSync(() => manager.AddToRoleAsync(null, null)), error);
        //    ExceptionHelper.ThrowsWithError<InvalidOperationException>(
        //        () => AsyncHelper.RunSync(() => manager.ChangePasswordAsync(null, null, null)), error);
        //    ExceptionHelper.ThrowsWithError<InvalidOperationException>(
        //        () => AsyncHelper.RunSync(() => manager.GetClaimsAsync(null)), error);
        //    ExceptionHelper.ThrowsWithError<InvalidOperationException>(
        //        () => AsyncHelper.RunSync(() => manager.GetLoginsAsync(null)), error);
        //    ExceptionHelper.ThrowsWithError<InvalidOperationException>(
        //        () => AsyncHelper.RunSync(() => manager.GetRolesAsync(null)), error);
        //    ExceptionHelper.ThrowsWithError<InvalidOperationException>(
        //        () => AsyncHelper.RunSync(() => manager.IsInRoleAsync(null, null)), error);
        //    ExceptionHelper.ThrowsWithError<InvalidOperationException>(
        //        () => AsyncHelper.RunSync(() => manager.RemoveClaimAsync(null, new Claim("a", "b"))), error);
        //    ExceptionHelper.ThrowsWithError<InvalidOperationException>(
        //        () => AsyncHelper.RunSync(() => manager.RemoveLoginAsync(null, new UserLoginInfo("", ""))), error);
        //    ExceptionHelper.ThrowsWithError<InvalidOperationException>(
        //        () => AsyncHelper.RunSync(() => manager.RemovePasswordAsync(null)), error);
        //    ExceptionHelper.ThrowsWithError<InvalidOperationException>(
        //        () => AsyncHelper.RunSync(() => manager.RemoveFromRoleAsync(null, null)), error);
        //    ExceptionHelper.ThrowsWithError<InvalidOperationException>(
        //        () => AsyncHelper.RunSync(() => manager.UpdateSecurityStampAsync(null)), error);
        //    ExceptionHelper.ThrowsWithError<InvalidOperationException>(
        //        () => AsyncHelper.RunSync(() => manager.GetSecurityStampAsync(null)), error);
        //    ExceptionHelper.ThrowsWithError<InvalidOperationException>(
        //        () => AsyncHelper.RunSync(() => manager.HasPasswordAsync(null)), error);
        //    ExceptionHelper.ThrowsWithError<InvalidOperationException>(
        //        () => AsyncHelper.RunSync(() => manager.GeneratePasswordResetTokenAsync(null)), error);
        //    ExceptionHelper.ThrowsWithError<InvalidOperationException>(
        //        () => AsyncHelper.RunSync(() => manager.ResetPasswordAsync(null, null, null)), error);
        //    ExceptionHelper.ThrowsWithError<InvalidOperationException>(
        //        () => AsyncHelper.RunSync(() => manager.IsEmailConfirmedAsync(null)), error);
        //    ExceptionHelper.ThrowsWithError<InvalidOperationException>(
        //        () => AsyncHelper.RunSync(() => manager.GenerateEmailConfirmationTokenAsync(null)), error);
        //    ExceptionHelper.ThrowsWithError<InvalidOperationException>(
        //        () => AsyncHelper.RunSync(() => manager.ConfirmEmailAsync(null, null)), error);
        //    ExceptionHelper.ThrowsWithError<InvalidOperationException>(
        //        () => AsyncHelper.RunSync(() => manager.GetEmailAsync(null)), error);
        //    ExceptionHelper.ThrowsWithError<InvalidOperationException>(
        //        () => AsyncHelper.RunSync(() => manager.SetEmailAsync(null, null)), error);
        //    ExceptionHelper.ThrowsWithError<InvalidOperationException>(
        //        () => AsyncHelper.RunSync(() => manager.IsPhoneNumberConfirmedAsync(null)), error);
        //    ExceptionHelper.ThrowsWithError<InvalidOperationException>(
        //        () => AsyncHelper.RunSync(() => manager.ChangePhoneNumberAsync(null, null, null)), error);
        //    ExceptionHelper.ThrowsWithError<InvalidOperationException>(
        //        () => AsyncHelper.RunSync(() => manager.VerifyChangePhoneNumberTokenAsync(null, null, null)), error);
        //    ExceptionHelper.ThrowsWithError<InvalidOperationException>(
        //        () => AsyncHelper.RunSync(() => manager.GetPhoneNumberAsync(null)), error);
        //    ExceptionHelper.ThrowsWithError<InvalidOperationException>(
        //        () => AsyncHelper.RunSync(() => manager.SetPhoneNumberAsync(null, null)), error);
        //    ExceptionHelper.ThrowsWithError<InvalidOperationException>(
        //        () => AsyncHelper.RunSync(() => manager.GetTwoFactorEnabledAsync(null)), error);
        //    ExceptionHelper.ThrowsWithError<InvalidOperationException>(
        //        () => AsyncHelper.RunSync(() => manager.SetTwoFactorEnabledAsync(null, true)), error);
        //    ExceptionHelper.ThrowsWithError<InvalidOperationException>(
        //        () => AsyncHelper.RunSync(() => manager.GenerateTwoFactorTokenAsync(null, null)), error);
        //    ExceptionHelper.ThrowsWithError<InvalidOperationException>(
        //        () => AsyncHelper.RunSync(() => manager.VerifyTwoFactorTokenAsync(null, null, null)), error);
        //    ExceptionHelper.ThrowsWithError<InvalidOperationException>(
        //        () => AsyncHelper.RunSync(() => manager.NotifyTwoFactorTokenAsync(null, null, null)), error);
        //    ExceptionHelper.ThrowsWithError<InvalidOperationException>(
        //        () => AsyncHelper.RunSync(() => manager.GetValidTwoFactorProvidersAsync(null)), error);
        //    ExceptionHelper.ThrowsWithError<InvalidOperationException>(
        //        () => AsyncHelper.RunSync(() => manager.VerifyUserTokenAsync(null, null, null)), error);
        //    ExceptionHelper.ThrowsWithError<InvalidOperationException>(
        //        () => AsyncHelper.RunSync(() => manager.AccessFailedAsync(null)), error);
        //    ExceptionHelper.ThrowsWithError<InvalidOperationException>(
        //        () => AsyncHelper.RunSync(() => manager.SetLockoutEnabledAsync(null, false)), error);
        //    ExceptionHelper.ThrowsWithError<InvalidOperationException>(
        //        () => AsyncHelper.RunSync(() => manager.SetLockoutEndDateAsync(null, DateTimeOffset.UtcNow)), error);
        //    ExceptionHelper.ThrowsWithError<InvalidOperationException>(
        //        () => AsyncHelper.RunSync(() => manager.IsLockedOutAsync(null)), error);
        //}

        //[Fact]
        //public void MethodsThrowWhenDisposedTest()
        //{
        //    var manager = new UserManager<TestUser, string>(new NoopUserStore());
        //    manager.Dispose();
        //    Assert.Throws<ObjectDisposedException>(() => manager.AddClaim("bogus", null));
        //    Assert.Throws<ObjectDisposedException>(() => manager.AddLogin("bogus", null));
        //    Assert.Throws<ObjectDisposedException>(() => manager.AddPassword("bogus", null));
        //    Assert.Throws<ObjectDisposedException>(() => manager.AddToRole("bogus", null));
        //    Assert.Throws<ObjectDisposedException>(() => manager.ChangePassword("bogus", null, null));
        //    Assert.Throws<ObjectDisposedException>(() => manager.GetClaims("bogus"));
        //    Assert.Throws<ObjectDisposedException>(() => manager.GetLogins("bogus"));
        //    Assert.Throws<ObjectDisposedException>(() => manager.GetRoles("bogus"));
        //    Assert.Throws<ObjectDisposedException>(() => manager.IsInRole("bogus", null));
        //    Assert.Throws<ObjectDisposedException>(() => manager.RemoveClaim("bogus", null));
        //    Assert.Throws<ObjectDisposedException>(() => manager.RemoveLogin("bogus", null));
        //    Assert.Throws<ObjectDisposedException>(() => manager.RemovePassword("bogus"));
        //    Assert.Throws<ObjectDisposedException>(() => manager.RemoveFromRole("bogus", null));
        //    Assert.Throws<ObjectDisposedException>(() => manager.RemoveClaim("bogus", null));
        //    Assert.Throws<ObjectDisposedException>(() => manager.Find("bogus", null));
        //    Assert.Throws<ObjectDisposedException>(() => manager.Find(null));
        //    Assert.Throws<ObjectDisposedException>(() => manager.FindById(null));
        //    Assert.Throws<ObjectDisposedException>(() => manager.FindByName(null));
        //    Assert.Throws<ObjectDisposedException>(() => manager.Create(null));
        //    Assert.Throws<ObjectDisposedException>(() => manager.Create(null, null));
        //    Assert.Throws<ObjectDisposedException>(() => manager.CreateIdentity(null, null));
        //    Assert.Throws<ObjectDisposedException>(() => manager.Update(null));
        //    Assert.Throws<ObjectDisposedException>(() => manager.Delete(null));
        //    Assert.Throws<ObjectDisposedException>(() => manager.UpdateSecurityStamp(null));
        //    Assert.Throws<ObjectDisposedException>(() => manager.GetSecurityStamp(null));
        //    Assert.Throws<ObjectDisposedException>(() => manager.GeneratePasswordResetToken(null));
        //    Assert.Throws<ObjectDisposedException>(() => manager.ResetPassword(null, null, null));
        //    Assert.Throws<ObjectDisposedException>(() => manager.GenerateEmailConfirmationToken(null));
        //    Assert.Throws<ObjectDisposedException>(() => manager.IsEmailConfirmed(null));
        //    Assert.Throws<ObjectDisposedException>(() => manager.ConfirmEmail(null, null));
        //}

        private class TestUser : IUser<string>
        {
            public string Id { get; private set; }
            public string UserName { get; set; }
        }

        private class NoopUserStore : IUserStore<TestUser, string>
        {
            public Task Create(TestUser user)
            {
                return Task.FromResult(0);
            }

            public Task Update(TestUser user)
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

            public void Dispose()
            {
            }

            public Task Delete(TestUser user)
            {
                return Task.FromResult(0);
            }
        }
    }
}