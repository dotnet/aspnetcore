using System.Threading;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Abstractions.Security;
using Moq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Identity.Security.Test
{
    public class SignInManagerTest
    {

#if NET45
        //TODO: Mock fails in K (this works fine in net45)
        [Fact]
        public async Task EnsureClaimsIdentityFactoryCreateIdentityCalled()
        {
            // Setup
            var store = new Mock<IUserStore<TestUser>>();
            var user = new TestUser { UserName = "Foo" };
            var userManager = new UserManager<TestUser>(store.Object);
            var identityFactory = new Mock<IClaimsIdentityFactory<TestUser>>();
            const string authType = "Test";
            var testIdentity = new ClaimsIdentity(authType);
            identityFactory.Setup(s => s.Create(userManager, user, authType, CancellationToken.None)).ReturnsAsync(testIdentity).Verifiable();
            userManager.ClaimsIdentityFactory = identityFactory.Object;
            var context = new Mock<HttpContext>();
            var response = new Mock<HttpResponse>();
            context.Setup(c => c.Response).Returns(response.Object).Verifiable();
            response.Setup(r => r.SignIn(testIdentity, It.IsAny<AuthenticationProperties>())).Verifiable();
            var helper = new SignInManager<TestUser> { UserManager = userManager, AuthenticationType = authType, Context = context.Object };

            // Act
            await helper.SignIn(user, false, false);

            // Assert
            identityFactory.VerifyAll();
        }

        [Fact]
        public async Task PasswordSignInReturnsLockedOutWhenLockedOut()
        {
            // Setup
            var user = new TestUser { UserName = "Foo" };
            var manager = new Mock<UserManager<TestUser>>();
            manager.Setup(m => m.IsLockedOut(user.Id, CancellationToken.None)).ReturnsAsync(true).Verifiable();
            manager.Setup(m => m.FindByName(user.UserName, CancellationToken.None)).ReturnsAsync(user).Verifiable();
            var helper = new SignInManager<TestUser> { UserManager = manager.Object };

            // Act
            var result = await helper.PasswordSignIn(user.UserName, "bogus", false, false);

            // Assert
            Assert.Equal(SignInStatus.LockedOut, result);
            manager.VerifyAll();
        }

        [Fact]
        public async Task CanPasswordSignIn()
        {
            // Setup
            var user = new TestUser { UserName = "Foo" };
            var manager = new Mock<UserManager<TestUser>>();
            manager.Setup(m => m.IsLockedOut(user.Id, CancellationToken.None)).ReturnsAsync(false).Verifiable();
            manager.Setup(m => m.FindByName(user.UserName, CancellationToken.None)).ReturnsAsync(user).Verifiable();
            manager.Setup(m => m.CheckPassword(user, "password", CancellationToken.None)).ReturnsAsync(true).Verifiable();
            manager.Setup(m => m.CreateIdentity(user, "Microsoft.AspNet.Identity", CancellationToken.None)).ReturnsAsync(new ClaimsIdentity("Microsoft.AspNet.Identity")).Verifiable();
            var context = new Mock<HttpContext>();
            var response = new Mock<HttpResponse>();
            context.Setup(c => c.Response).Returns(response.Object).Verifiable();
            response.Setup(r => r.SignIn(It.IsAny<ClaimsIdentity>(), It.IsAny<AuthenticationProperties>())).Verifiable();
            var helper = new SignInManager<TestUser> { UserManager = manager.Object, Context = context.Object };

            // Act
            var result = await helper.PasswordSignIn(user.UserName, "password", false, false);

            // Assert
            Assert.Equal(SignInStatus.Success, result);
            manager.VerifyAll();
        }

        [Fact]
        public async Task PasswordSignInFailsWithWrongPassword()
        {
            // Setup
            var user = new TestUser { UserName = "Foo" };
            var manager = new Mock<UserManager<TestUser>>();
            manager.Setup(m => m.IsLockedOut(user.Id, CancellationToken.None)).ReturnsAsync(false).Verifiable();
            manager.Setup(m => m.FindByName(user.UserName, CancellationToken.None)).ReturnsAsync(user).Verifiable();
            manager.Setup(m => m.CheckPassword(user, "bogus", CancellationToken.None)).ReturnsAsync(false).Verifiable();
            var helper = new SignInManager<TestUser> { UserManager = manager.Object };

            // Act
            var result = await helper.PasswordSignIn(user.UserName, "bogus", false, false);

            // Assert
            Assert.Equal(SignInStatus.Failure, result);
            manager.VerifyAll();
        }


        [Fact]
        public async Task PasswordSignInFailsWithUnknownUser()
        {
            // Setup
            var manager = new Mock<UserManager<TestUser>>();
            manager.Setup(m => m.FindByName("bogus", CancellationToken.None)).ReturnsAsync(null).Verifiable();
            var helper = new SignInManager<TestUser> { UserManager = manager.Object };

            // Act
            var result = await helper.PasswordSignIn("bogus", "bogus", false, false);

            // Assert
            Assert.Equal(SignInStatus.Failure, result);
            manager.VerifyAll();
        }

        [Fact]
        public async Task PasswordSignInFailsWithNoUserManager()
        {
            // Setup
            var helper = new SignInManager<TestUser>();

            // Act
            var result = await helper.PasswordSignIn("bogus", "bogus", false, false);

            // Assert
            Assert.Equal(SignInStatus.Failure, result);
        }

        [Fact]
        public async Task CreateUserIdentityReturnsNullNoUserManager()
        {
            // Setup
            var user = new TestUser();
            var helper = new SignInManager<TestUser>();

            // Act
            var result = await helper.CreateUserIdentity(user);

            // Assert
            Assert.Null(result);
        }


        [Fact]
        public async Task PasswordSignInFailsWithWrongPasswordCanAccessFailedAndLockout()
        {
            // Setup
            var user = new TestUser { UserName = "Foo" };
            var manager = new Mock<UserManager<TestUser>>();
            var lockedout = false;
            manager.Setup(m => m.AccessFailed(user.Id, CancellationToken.None)).Returns(() =>
            {
                lockedout = true;
                return Task.FromResult(IdentityResult.Success);
            }).Verifiable();
            manager.Setup(m => m.IsLockedOut(user.Id, CancellationToken.None)).Returns(() => Task.FromResult(lockedout));
            manager.Setup(m => m.FindByName(user.UserName, CancellationToken.None)).ReturnsAsync(user).Verifiable();
            manager.Setup(m => m.CheckPassword(user, "bogus", CancellationToken.None)).ReturnsAsync(false).Verifiable();
            var helper = new SignInManager<TestUser> { UserManager = manager.Object };

            // Act
            var result = await helper.PasswordSignIn(user.UserName, "bogus", false, true);

            // Assert
            Assert.Equal(SignInStatus.LockedOut, result);
            manager.VerifyAll();
        }
#endif
    }
}