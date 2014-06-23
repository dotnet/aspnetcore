// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Security;
using Microsoft.AspNet.Identity.Test;
using Microsoft.Framework.DependencyInjection;
using Moq;
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Identity.Authentication.Test
{
    public class ApplicationUser : IdentityUser { }

    public class HttpSignInTest
    {
#if NET45
        //[Theory]
        //[InlineData(true)]
        //[InlineData(false)]
        //public async Task VerifyAccountControllerSignInFunctional(bool isPersistent)
        //{
        //    IBuilder app = new Builder(new ServiceCollection().BuildServiceProvider());
        //    app.UseCookieAuthentication(new CookieAuthenticationOptions
        //    {
        //        AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie
        //    });

        // TODO: how to functionally test context?
        //    var context = new DefaultHttpContext(new FeatureCollection());
        //    var contextAccessor = new Mock<IContextAccessor<HttpContext>>();
        //    contextAccessor.Setup(a => a.Value).Returns(context);
        //    app.UseServices(services =>
        //    {
        //        services.AddInstance(contextAccessor.Object);
        //        services.AddInstance<ILoggerFactory>(new NullLoggerFactory());
        //        services.AddIdentity<ApplicationUser, IdentityRole>(s =>
        //        {
        //            s.AddUserStore(() => new InMemoryUserStore<ApplicationUser>());
        //            s.AddUserManager<ApplicationUserManager>();
        //            s.AddRoleStore(() => new InMemoryRoleStore<IdentityRole>());
        //            s.AddRoleManager<ApplicationRoleManager>();
        //        });
        //        services.AddTransient<ApplicationHttpSignInManager>();
        //    });

        //    // Act
        //    var user = new ApplicationUser
        //    {
        //        UserName = "Yolo"
        //    };
        //    const string password = "Yol0Sw@g!";
        //    var userManager = app.ApplicationServices.GetService<ApplicationUserManager>();
        //    var HttpSignInManager = app.ApplicationServices.GetService<ApplicationHttpSignInManager>();

        //    IdentityResultAssert.IsSuccess(await userManager.CreateAsync(user, password));
        //    var result = await HttpSignInManager.PasswordSignInAsync(user.UserName, password, isPersistent, false);

        //    // Assert
        //    Assert.Equal(SignInStatus.Success, result);
        //    contextAccessor.VerifyAll();
        //}

        [Fact]
        public void ConstructorNullChecks()
        {
            Assert.Throws<ArgumentNullException>("userManager", () => new SignInManager<IdentityUser>(null, null, null));
            var userManager = MockHelpers.MockUserManager<IdentityUser>().Object;
            Assert.Throws<ArgumentNullException>("authenticationManager", () => new SignInManager<IdentityUser>(userManager, null, null));
            var authManager = new Mock<IAuthenticationManager>().Object;
            Assert.Throws<ArgumentNullException>("claimsFactory", () => new SignInManager<IdentityUser>(userManager, authManager, null));
        }

        //TODO: Mock fails in K (this works fine in net45)
        //[Fact]
        //public async Task EnsureClaimsIdentityFactoryCreateIdentityCalled()
        //{
        //    // Setup
        //    var user = new TestUser { UserName = "Foo" };
        //    var userManager = MockHelpers.TestUserManager<TestUser>();
        //    var identityFactory = new Mock<IClaimsIdentityFactory<TestUser>>();
        //    const string authType = "Test";
        //    var testIdentity = new ClaimsIdentity(authType);
        //    identityFactory.Setup(s => s.CreateAsync(userManager, user, authType, CancellationToken.None)).ReturnsAsync(testIdentity).Verifiable();
        //    userManager.ClaimsIdentityFactory = identityFactory.Object;
        //    var context = new Mock<HttpContext>();
        //    var response = new Mock<HttpResponse>();
        //    context.Setup(c => c.Response).Returns(response.Object).Verifiable();
        //    response.Setup(r => r.SignIn(testIdentity, It.IsAny<AuthenticationProperties>())).Verifiable();
        //    var contextAccessor = new Mock<IContextAccessor<HttpContext>>();
        //    contextAccessor.Setup(a => a.Value).Returns(context.Object);
        //    var helper = new HttpAuthenticationManager(contextAccessor.Object);

        //    // Act
        //    helper.SignIn(user, false);

        //    // Assert
        //    identityFactory.VerifyAll();
        //    context.VerifyAll();
        //    contextAccessor.VerifyAll();
        //    response.VerifyAll();
        //}

        [Fact]
        public async Task PasswordSignInReturnsLockedOutWhenLockedOut()
        {
            // Setup
            var user = new TestUser { UserName = "Foo" };
            var manager = MockHelpers.MockUserManager<TestUser>();
            manager.Setup(m => m.SupportsUserLockout).Returns(true).Verifiable();
            manager.Setup(m => m.IsLockedOutAsync(user, CancellationToken.None)).ReturnsAsync(true).Verifiable();
            manager.Setup(m => m.FindByNameAsync(user.UserName, CancellationToken.None)).ReturnsAsync(user).Verifiable();
            var context = new Mock<HttpContext>();
            var contextAccessor = new Mock<IContextAccessor<HttpContext>>();
            contextAccessor.Setup(a => a.Value).Returns(context.Object);
            var roleManager = MockHelpers.MockRoleManager<TestRole>();
            var claimsFactory = new Mock<ClaimsIdentityFactory<TestUser, TestRole>>(manager.Object, roleManager.Object);
            var helper = new SignInManager<TestUser>(manager.Object, new HttpAuthenticationManager(contextAccessor.Object), claimsFactory.Object);

            // Act
            var result = await helper.PasswordSignInAsync(user.UserName, "bogus", false, false);

            // Assert
            Assert.Equal(SignInStatus.LockedOut, result);
            manager.VerifyAll();
        }
            
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CanPasswordSignIn(bool isPersistent)
        {
            // Setup
            var user = new TestUser { UserName = "Foo" };
            var manager = MockHelpers.MockUserManager<TestUser>();
            manager.Setup(m => m.SupportsUserLockout).Returns(true).Verifiable();
            manager.Setup(m => m.IsLockedOutAsync(user, CancellationToken.None)).ReturnsAsync(false).Verifiable();
            manager.Setup(m => m.FindByNameAsync(user.UserName, CancellationToken.None)).ReturnsAsync(user).Verifiable();
            manager.Setup(m => m.CheckPasswordAsync(user, "password", CancellationToken.None)).ReturnsAsync(true).Verifiable();
            var context = new Mock<HttpContext>();
            var response = new Mock<HttpResponse>();
            context.Setup(c => c.Response).Returns(response.Object).Verifiable();
            response.Setup(r => r.SignIn(It.IsAny<ClaimsIdentity>(), It.Is<AuthenticationProperties>(v => v.IsPersistent == isPersistent))).Verifiable();
            var contextAccessor = new Mock<IContextAccessor<HttpContext>>();
            contextAccessor.Setup(a => a.Value).Returns(context.Object);
            var roleManager = MockHelpers.MockRoleManager<TestRole>();
            var claimsFactory = new Mock<ClaimsIdentityFactory<TestUser, TestRole>>(manager.Object, roleManager.Object);
            claimsFactory.Setup(m => m.CreateAsync(user, DefaultAuthenticationTypes.ApplicationCookie, CancellationToken.None)).ReturnsAsync(new ClaimsIdentity("Microsoft.AspNet.Identity")).Verifiable();
            var helper = new SignInManager<TestUser>(manager.Object, new HttpAuthenticationManager(contextAccessor.Object), claimsFactory.Object);

            // Act
            var result = await helper.PasswordSignInAsync(user.UserName, "password", isPersistent, false);

            // Assert
            Assert.Equal(SignInStatus.Success, result);
            manager.VerifyAll();
            context.VerifyAll();
            response.VerifyAll();
            contextAccessor.VerifyAll();
            claimsFactory.VerifyAll();
        }

        [Fact]
        public async Task PasswordSignInWorksWithNonTwoFactorStore()
        {
            // Setup
            var user = new TestUser { UserName = "Foo" };
            var manager = MockHelpers.MockUserManager<TestUser>();
            manager.Setup(m => m.SupportsUserLockout).Returns(true).Verifiable();
            manager.Setup(m => m.IsLockedOutAsync(user, CancellationToken.None)).ReturnsAsync(false).Verifiable();
            manager.Setup(m => m.FindByNameAsync(user.UserName, CancellationToken.None)).ReturnsAsync(user).Verifiable();
            manager.Setup(m => m.CheckPasswordAsync(user, "password", CancellationToken.None)).ReturnsAsync(true).Verifiable();
            var context = new Mock<HttpContext>();
            var response = new Mock<HttpResponse>();
            response.Setup(r => r.SignIn(It.IsAny<ClaimsIdentity>(), It.IsAny<AuthenticationProperties>())).Verifiable();
            context.Setup(c => c.Response).Returns(response.Object).Verifiable();
            var contextAccessor = new Mock<IContextAccessor<HttpContext>>();
            contextAccessor.Setup(a => a.Value).Returns(context.Object);
            var roleManager = MockHelpers.MockRoleManager<TestRole>();
            var claimsFactory = new Mock<ClaimsIdentityFactory<TestUser, TestRole>>(manager.Object, roleManager.Object);
            var helper = new SignInManager<TestUser>(manager.Object, new HttpAuthenticationManager(contextAccessor.Object), claimsFactory.Object);

            // Act
            var result = await helper.PasswordSignInAsync(user.UserName, "password", false, false);

            // Assert
            Assert.Equal(SignInStatus.Success, result);
            manager.VerifyAll();
            context.VerifyAll();
            response.VerifyAll();
            contextAccessor.VerifyAll();
        }

        [Fact]
        public async Task PasswordSignInRequiresVerification()
        {
            // Setup
            var user = new TestUser { UserName = "Foo" };
            var manager = MockHelpers.MockUserManager<TestUser>();
            manager.Setup(m => m.SupportsUserLockout).Returns(true).Verifiable();
            manager.Setup(m => m.SupportsUserTwoFactor).Returns(true).Verifiable();
            manager.Setup(m => m.GetTwoFactorEnabledAsync(user, CancellationToken.None)).ReturnsAsync(true).Verifiable();
            manager.Setup(m => m.IsLockedOutAsync(user, CancellationToken.None)).ReturnsAsync(false).Verifiable();
            manager.Setup(m => m.FindByNameAsync(user.UserName, CancellationToken.None)).ReturnsAsync(user).Verifiable();
            manager.Setup(m => m.GetUserIdAsync(user, CancellationToken.None)).ReturnsAsync(user.Id).Verifiable();
            manager.Setup(m => m.CheckPasswordAsync(user, "password", CancellationToken.None)).ReturnsAsync(true).Verifiable();
            var context = new Mock<HttpContext>();
            var response = new Mock<HttpResponse>();
            response.Setup(r => r.SignIn(It.Is<ClaimsIdentity>(id => id.Name == user.Id))).Verifiable();
            context.Setup(c => c.Response).Returns(response.Object).Verifiable();
            var contextAccessor = new Mock<IContextAccessor<HttpContext>>();
            contextAccessor.Setup(a => a.Value).Returns(context.Object);
            var roleManager = MockHelpers.MockRoleManager<TestRole>();
            var helper = new SignInManager<TestUser>(manager.Object, new HttpAuthenticationManager(contextAccessor.Object), new ClaimsIdentityFactory<TestUser, TestRole>(manager.Object, roleManager.Object));

            // Act
            var result = await helper.PasswordSignInAsync(user.UserName, "password", false, false);

            // Assert
            Assert.Equal(SignInStatus.RequiresVerification, result);
            manager.VerifyAll();
            context.VerifyAll();
            response.VerifyAll();
            contextAccessor.VerifyAll();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CanTwoFactorSignIn(bool isPersistent)
        {
            // Setup
            var user = new TestUser { UserName = "Foo" };
            var manager = MockHelpers.MockUserManager<TestUser>();
            var provider = "twofactorprovider";
            var code = "123456";
            manager.Setup(m => m.IsLockedOutAsync(user, CancellationToken.None)).ReturnsAsync(false).Verifiable();
            manager.Setup(m => m.FindByIdAsync(user.Id, CancellationToken.None)).ReturnsAsync(user).Verifiable();
            manager.Setup(m => m.VerifyTwoFactorTokenAsync(user, provider, code, CancellationToken.None)).ReturnsAsync(true).Verifiable();
            manager.Setup(m => m.GetUserIdAsync(user, CancellationToken.None)).ReturnsAsync(user.Id).Verifiable();
            manager.Setup(m => m.GetUserNameAsync(user, CancellationToken.None)).ReturnsAsync(user.UserName).Verifiable();
            var context = new Mock<HttpContext>();
            var response = new Mock<HttpResponse>();
            response.Setup(r => r.SignIn(It.IsAny<ClaimsIdentity>(), It.Is<AuthenticationProperties>(v => v.IsPersistent == isPersistent))).Verifiable();
            context.Setup(c => c.Response).Returns(response.Object).Verifiable();
            var id = new ClaimsIdentity(HttpAuthenticationManager.TwoFactorUserIdAuthenticationType);
            id.AddClaim(new Claim(ClaimTypes.Name, user.Id));
            var authResult = new AuthenticationResult(id, new AuthenticationProperties(), new AuthenticationDescription());
            context.Setup(c => c.AuthenticateAsync(HttpAuthenticationManager.TwoFactorUserIdAuthenticationType)).ReturnsAsync(authResult).Verifiable();
            var contextAccessor = new Mock<IContextAccessor<HttpContext>>();
            contextAccessor.Setup(a => a.Value).Returns(context.Object);
            var roleManager = MockHelpers.MockRoleManager<TestRole>();
            var claimsFactory = new ClaimsIdentityFactory<TestUser, TestRole>(manager.Object, roleManager.Object);
            var helper = new SignInManager<TestUser>(manager.Object, new HttpAuthenticationManager(contextAccessor.Object), claimsFactory);

            // Act
            var result = await helper.TwoFactorSignInAsync(provider, code, isPersistent);

            // Assert
            Assert.Equal(SignInStatus.Success, result);
            manager.VerifyAll();
            context.VerifyAll();
            response.VerifyAll();
            contextAccessor.VerifyAll();
        }

        [Fact]
        public void RememberClientStoresUserId()
        {
            // Setup
            var user = new TestUser { UserName = "Foo" };
            var context = new Mock<HttpContext>();
            var response = new Mock<HttpResponse>();
            context.Setup(c => c.Response).Returns(response.Object).Verifiable();
            response.Setup(r => r.SignIn(It.Is<ClaimsIdentity>(i => i.AuthenticationType == HttpAuthenticationManager.TwoFactorRememberedAuthenticationType))).Verifiable();
            var id = new ClaimsIdentity(HttpAuthenticationManager.TwoFactorRememberedAuthenticationType);
            id.AddClaim(new Claim(ClaimTypes.Name, user.Id));
            var authResult = new AuthenticationResult(id, new AuthenticationProperties(), new AuthenticationDescription());
            var contextAccessor = new Mock<IContextAccessor<HttpContext>>();
            contextAccessor.Setup(a => a.Value).Returns(context.Object);
            var signInService = new HttpAuthenticationManager(contextAccessor.Object);

            // Act
            signInService.RememberClient(user.Id);

            // Assert
            context.VerifyAll();
            response.VerifyAll();
            contextAccessor.VerifyAll();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task RememberBrowserSkipsTwoFactorVerificationSignIn(bool isPersistent)
        {
            // Setup
            var user = new TestUser { UserName = "Foo" };
            var manager = MockHelpers.MockUserManager<TestUser>();
            manager.Setup(m => m.GetTwoFactorEnabledAsync(user, CancellationToken.None)).ReturnsAsync(true).Verifiable();
            manager.Setup(m => m.SupportsUserLockout).Returns(true).Verifiable();
            manager.Setup(m => m.SupportsUserTwoFactor).Returns(true).Verifiable();
            manager.Setup(m => m.IsLockedOutAsync(user, CancellationToken.None)).ReturnsAsync(false).Verifiable();
            manager.Setup(m => m.FindByNameAsync(user.UserName, CancellationToken.None)).ReturnsAsync(user).Verifiable();
            manager.Setup(m => m.GetUserIdAsync(user, CancellationToken.None)).ReturnsAsync(user.Id).Verifiable();
            manager.Setup(m => m.CheckPasswordAsync(user, "password", CancellationToken.None)).ReturnsAsync(true).Verifiable();
            var context = new Mock<HttpContext>();
            var response = new Mock<HttpResponse>();
            context.Setup(c => c.Response).Returns(response.Object).Verifiable();
            response.Setup(r => r.SignIn(It.Is<ClaimsIdentity>(i => i.AuthenticationType == DefaultAuthenticationTypes.ApplicationCookie), It.Is<AuthenticationProperties>(v => v.IsPersistent == isPersistent))).Verifiable();
            var id = new ClaimsIdentity(HttpAuthenticationManager.TwoFactorRememberedAuthenticationType);
            id.AddClaim(new Claim(ClaimTypes.Name, user.Id));
            var authResult = new AuthenticationResult(id, new AuthenticationProperties(), new AuthenticationDescription());
            context.Setup(c => c.AuthenticateAsync(HttpAuthenticationManager.TwoFactorRememberedAuthenticationType)).ReturnsAsync(authResult).Verifiable();
            var contextAccessor = new Mock<IContextAccessor<HttpContext>>();
            contextAccessor.Setup(a => a.Value).Returns(context.Object);
            var signInService = new HttpAuthenticationManager(contextAccessor.Object);
            var roleManager = MockHelpers.MockRoleManager<TestRole>();
            var claimsFactory = new Mock<ClaimsIdentityFactory<TestUser, TestRole>>(manager.Object, roleManager.Object);
            claimsFactory.Setup(m => m.CreateAsync(user, DefaultAuthenticationTypes.ApplicationCookie, CancellationToken.None)).ReturnsAsync(new ClaimsIdentity(DefaultAuthenticationTypes.ApplicationCookie)).Verifiable();
            var helper = new SignInManager<TestUser>(manager.Object, signInService, claimsFactory.Object);

            // Act
            var result = await helper.PasswordSignInAsync(user.UserName, "password", isPersistent, false);

            // Assert
            Assert.Equal(SignInStatus.Success, result);
            manager.VerifyAll();
            context.VerifyAll();
            response.VerifyAll();
            contextAccessor.VerifyAll();
            claimsFactory.VerifyAll();
        }

        [Theory]
        [InlineData("Microsoft.AspNet.Identity.Authentication.Application")]
        [InlineData("Foo")]
        public void SignOutCallsContextResponseSignOut(string authenticationType)
        {
            // Setup
            var manager = MockHelpers.MockUserManager<TestUser>();
            var context = new Mock<HttpContext>();
            var response = new Mock<HttpResponse>();
            context.Setup(c => c.Response).Returns(response.Object).Verifiable();
            response.Setup(r => r.SignOut(authenticationType)).Verifiable();
            var contextAccessor = new Mock<IContextAccessor<HttpContext>>();
            contextAccessor.Setup(a => a.Value).Returns(context.Object);
            var roleManager = MockHelpers.MockRoleManager<TestRole>();
            var claimsFactory = new Mock<ClaimsIdentityFactory<TestUser, TestRole>>(manager.Object, roleManager.Object);
            var helper = new SignInManager<TestUser>(manager.Object, new HttpAuthenticationManager(contextAccessor.Object), claimsFactory.Object)
            {
                AuthenticationType = authenticationType
            };

            // Act
            helper.SignOut();

            // Assert
            context.VerifyAll();
            response.VerifyAll();
        }

        [Fact]
        public async Task PasswordSignInFailsWithWrongPassword()
        {
            // Setup
            var user = new TestUser { UserName = "Foo" };
            var manager = MockHelpers.MockUserManager<TestUser>();
            manager.Setup(m => m.SupportsUserLockout).Returns(true).Verifiable();
            manager.Setup(m => m.IsLockedOutAsync(user, CancellationToken.None)).ReturnsAsync(false).Verifiable();
            manager.Setup(m => m.FindByNameAsync(user.UserName, CancellationToken.None)).ReturnsAsync(user).Verifiable();
            manager.Setup(m => m.CheckPasswordAsync(user, "bogus", CancellationToken.None)).ReturnsAsync(false).Verifiable();
            var context = new Mock<HttpContext>();
            var contextAccessor = new Mock<IContextAccessor<HttpContext>>();
            contextAccessor.Setup(a => a.Value).Returns(context.Object);
            var roleManager = MockHelpers.MockRoleManager<TestRole>();
            var claimsFactory = new Mock<ClaimsIdentityFactory<TestUser, TestRole>>(manager.Object, roleManager.Object);
            var helper = new SignInManager<TestUser>(manager.Object, new HttpAuthenticationManager(contextAccessor.Object), claimsFactory.Object);
            // Act
            var result = await helper.PasswordSignInAsync(user.UserName, "bogus", false, false);

            // Assert
            Assert.Equal(SignInStatus.Failure, result);
            manager.VerifyAll();
        }

        [Fact]
        public async Task PasswordSignInFailsWithUnknownUser()
        {
            // Setup
            var manager = MockHelpers.MockUserManager<TestUser>();
            manager.Setup(m => m.FindByNameAsync("bogus", CancellationToken.None)).ReturnsAsync(null).Verifiable();
            var context = new Mock<HttpContext>();
            var contextAccessor = new Mock<IContextAccessor<HttpContext>>();
            contextAccessor.Setup(a => a.Value).Returns(context.Object);
            var roleManager = MockHelpers.MockRoleManager<TestRole>();
            var claimsFactory = new Mock<ClaimsIdentityFactory<TestUser, TestRole>>(manager.Object, roleManager.Object);
            var helper = new SignInManager<TestUser>(manager.Object, new HttpAuthenticationManager(contextAccessor.Object), claimsFactory.Object);

            // Act
            var result = await helper.PasswordSignInAsync("bogus", "bogus", false, false);

            // Assert
            Assert.Equal(SignInStatus.Failure, result);
            manager.VerifyAll();
        }

        [Fact]
        public async Task PasswordSignInFailsWithWrongPasswordCanAccessFailedAndLockout()
        {
            // Setup
            var user = new TestUser { UserName = "Foo" };
            var manager = MockHelpers.MockUserManager<TestUser>();
            var lockedout = false;
            manager.Setup(m => m.AccessFailedAsync(user, CancellationToken.None)).Returns(() =>
            {
                lockedout = true;
                return Task.FromResult(IdentityResult.Success);
            }).Verifiable();
            manager.Setup(m => m.SupportsUserLockout).Returns(true).Verifiable();
            manager.Setup(m => m.IsLockedOutAsync(user, CancellationToken.None)).Returns(() => Task.FromResult(lockedout));
            manager.Setup(m => m.FindByNameAsync(user.UserName, CancellationToken.None)).ReturnsAsync(user).Verifiable();
            manager.Setup(m => m.CheckPasswordAsync(user, "bogus", CancellationToken.None)).ReturnsAsync(false).Verifiable();
            var context = new Mock<HttpContext>();
            var contextAccessor = new Mock<IContextAccessor<HttpContext>>();
            contextAccessor.Setup(a => a.Value).Returns(context.Object);
            var roleManager = MockHelpers.MockRoleManager<TestRole>();
            var claimsFactory = new Mock<ClaimsIdentityFactory<TestUser, TestRole>>(manager.Object, roleManager.Object);
            var helper = new SignInManager<TestUser>(manager.Object, new HttpAuthenticationManager(contextAccessor.Object), claimsFactory.Object);

            // Act
            var result = await helper.PasswordSignInAsync(user.UserName, "bogus", false, true);

            // Assert
            Assert.Equal(SignInStatus.LockedOut, result);
            manager.VerifyAll();
        }
#endif
    }
}