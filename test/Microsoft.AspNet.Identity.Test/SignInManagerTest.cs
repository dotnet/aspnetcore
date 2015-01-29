// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Security;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Identity.Test
{
    public class SignManagerInTest
    {
        //[Theory]
        //[InlineData(true)]
        //[InlineData(false)]
        //public async Task VerifyAccountControllerSignInFunctional(bool isPersistent)
        //{
        //    var app = new ApplicationBuilder(new ServiceCollection().BuildServiceProvider());
        //    app.UseCookieAuthentication(new CookieAuthenticationOptions
        //    {
        //        AuthenticationType = ClaimsIdentityOptions.DefaultAuthenticationType
        //    });

        // TODO: how to functionally test context?
        //    var context = new DefaultHttpContext(new FeatureCollection());
        //    var contextAccessor = new Mock<IHttpContextAccessor>();
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
        //    var userManager = app.ApplicationServices.GetRequiredService<ApplicationUserManager>();
        //    var HttpSignInManager = app.ApplicationServices.GetRequiredService<ApplicationHttpSignInManager>();

        //    IdentityResultAssert.IsSuccess(await userManager.CreateAsync(user, password));
        //    var result = await HttpSignInManager.PasswordSignInAsync(user.UserName, password, isPersistent, false);

        //    // Assert
        //    Assert.Equal(SignInStatus.Success, result);
        //    contextAccessor.VerifyAll();
        //}

        [Fact]
        public void ConstructorNullChecks()
        {
            Assert.Throws<ArgumentNullException>("userManager", () => new SignInManager<IdentityUser>(null, null, null, null, null));
            var userManager = MockHelpers.MockUserManager<IdentityUser>().Object;
            Assert.Throws<ArgumentNullException>("contextAccessor", () => new SignInManager<IdentityUser>(userManager, null, null, null));
            var contextAccessor = new Mock<IHttpContextAccessor>();
            Assert.Throws<ArgumentNullException>("contextAccessor", () => new SignInManager<IdentityUser>(userManager, contextAccessor.Object, null, null));
            var context = new Mock<HttpContext>();
            contextAccessor.Setup(a => a.Value).Returns(context.Object);
            Assert.Throws<ArgumentNullException>("claimsFactory", () => new SignInManager<IdentityUser>(userManager, contextAccessor.Object, null, null));
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
        //    var contextAccessor = new Mock<IHttpContextAccessor>();
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
            manager.Setup(m => m.GetUserIdAsync(user, CancellationToken.None)).ReturnsAsync(user.Id.ToString()).Verifiable();

            var context = new Mock<HttpContext>();
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(a => a.Value).Returns(context.Object);
            var roleManager = MockHelpers.MockRoleManager<TestRole>();
            var identityOptions = new IdentityOptions();
            var options = new Mock<IOptions<IdentityOptions>>();
            options.Setup(a => a.Options).Returns(identityOptions);
            var claimsFactory = new Mock<ClaimsIdentityFactory<TestUser, TestRole>>(manager.Object, roleManager.Object, options.Object);
            var logStore = new StringBuilder();
            var loggerFactory = MockHelpers.MockILoggerFactory(MockHelpers.MockILogger(logStore).Object);
            var helper = new SignInManager<TestUser>(manager.Object, contextAccessor.Object, claimsFactory.Object, options.Object, loggerFactory.Object);
            string expected = string.Format("{0} for user: {1} : Result : {2}", "PasswordSignInAsync", user.Id, "Lockedout");

            // Act
            var result = await helper.PasswordSignInAsync(user.UserName, "bogus", false, false);

            // Assert
            Assert.False(result.Succeeded);
            Assert.True(result.IsLockedOut);
            Assert.NotEqual(-1, logStore.ToString().IndexOf(expected));
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
            manager.Setup(m => m.ResetAccessFailedCountAsync(user, CancellationToken.None)).ReturnsAsync(IdentityResult.Success).Verifiable();
            manager.Setup(m => m.GetUserIdAsync(user, CancellationToken.None)).ReturnsAsync(user.Id.ToString()).Verifiable();

            var context = new Mock<HttpContext>();
            var response = new Mock<HttpResponse>();
            context.Setup(c => c.Response).Returns(response.Object).Verifiable();
            response.Setup(r => r.SignIn(It.Is<AuthenticationProperties>(v => v.IsPersistent == isPersistent), It.IsAny<ClaimsIdentity>())).Verifiable();
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(a => a.Value).Returns(context.Object);
            var roleManager = MockHelpers.MockRoleManager<TestRole>();
            var identityOptions = new IdentityOptions();
            var options = new Mock<IOptions<IdentityOptions>>();
            options.Setup(a => a.Options).Returns(identityOptions);
            var claimsFactory = new Mock<ClaimsIdentityFactory<TestUser, TestRole>>(manager.Object, roleManager.Object, options.Object);
            claimsFactory.Setup(m => m.CreateAsync(user, CancellationToken.None)).ReturnsAsync(new ClaimsIdentity("Microsoft.AspNet.Identity")).Verifiable();
            var logStore = new StringBuilder();
            var loggerFactory = MockHelpers.MockILoggerFactory(MockHelpers.MockILogger(logStore).Object);
            var helper = new SignInManager<TestUser>(manager.Object, contextAccessor.Object, claimsFactory.Object, options.Object, loggerFactory.Object);
            string expected = string.Format("{0} for user: {1} : Result : {2}", "PasswordSignInAsync", user.Id, "Succeeded");

            // Act
            var result = await helper.PasswordSignInAsync(user.UserName, "password", isPersistent, false);

            // Assert
            Assert.True(result.Succeeded);
            Assert.NotEqual(-1, logStore.ToString().IndexOf(expected));
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
            manager.Setup(m => m.ResetAccessFailedCountAsync(user, CancellationToken.None)).ReturnsAsync(IdentityResult.Success).Verifiable();
            manager.Setup(m => m.GetUserIdAsync(user, CancellationToken.None)).ReturnsAsync(user.Id.ToString()).Verifiable();

            var context = new Mock<HttpContext>();
            var response = new Mock<HttpResponse>();
            response.Setup(r => r.SignIn(It.IsAny<AuthenticationProperties>(), It.IsAny<ClaimsIdentity>())).Verifiable();
            context.Setup(c => c.Response).Returns(response.Object).Verifiable();
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(a => a.Value).Returns(context.Object);
            var roleManager = MockHelpers.MockRoleManager<TestRole>();
            var identityOptions = new IdentityOptions();
            var options = new Mock<IOptions<IdentityOptions>>();
            options.Setup(a => a.Options).Returns(identityOptions);
            var claimsFactory = new Mock<ClaimsIdentityFactory<TestUser, TestRole>>(manager.Object, roleManager.Object, options.Object);
            var helper = new SignInManager<TestUser>(manager.Object, contextAccessor.Object, claimsFactory.Object, options.Object);

            // Act
            var result = await helper.PasswordSignInAsync(user.UserName, "password", false, false);

            // Assert
            Assert.True(result.Succeeded);
            manager.VerifyAll();
            context.VerifyAll();
            response.VerifyAll();
            contextAccessor.VerifyAll();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task PasswordSignInRequiresVerification(bool supportsLockout)
        {
            // Setup
            var user = new TestUser { UserName = "Foo" };
            var manager = MockHelpers.MockUserManager<TestUser>();
            manager.Setup(m => m.SupportsUserLockout).Returns(supportsLockout).Verifiable();
            if (supportsLockout)
            {
                manager.Setup(m => m.IsLockedOutAsync(user, CancellationToken.None)).ReturnsAsync(false).Verifiable();
            }
            IList<string> providers = new List<string>();
            providers.Add("PhoneNumber");
            manager.Setup(m => m.GetValidTwoFactorProvidersAsync(user, CancellationToken.None)).Returns(Task.FromResult(providers)).Verifiable();
            manager.Setup(m => m.SupportsUserTwoFactor).Returns(true).Verifiable();
            manager.Setup(m => m.GetTwoFactorEnabledAsync(user, CancellationToken.None)).ReturnsAsync(true).Verifiable();
            manager.Setup(m => m.FindByNameAsync(user.UserName, CancellationToken.None)).ReturnsAsync(user).Verifiable();
            manager.Setup(m => m.GetUserIdAsync(user, CancellationToken.None)).ReturnsAsync(user.Id).Verifiable();
            manager.Setup(m => m.CheckPasswordAsync(user, "password", CancellationToken.None)).ReturnsAsync(true).Verifiable();
            if (supportsLockout)
            {
                manager.Setup(m => m.ResetAccessFailedCountAsync(user, CancellationToken.None)).ReturnsAsync(IdentityResult.Success).Verifiable();
            }
            var context = new Mock<HttpContext>();
            var response = new Mock<HttpResponse>();
            response.Setup(r => r.SignIn(It.Is<ClaimsIdentity>(id => id.Name == user.Id))).Verifiable();
            context.Setup(c => c.Response).Returns(response.Object).Verifiable();
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(a => a.Value).Returns(context.Object);
            var roleManager = MockHelpers.MockRoleManager<TestRole>();
            var identityOptions = new IdentityOptions();
            var options = new Mock<IOptions<IdentityOptions>>();
            options.Setup(a => a.Options).Returns(identityOptions);
            var logStore = new StringBuilder();
            var loggerFactory = MockHelpers.MockILoggerFactory(MockHelpers.MockILogger(logStore).Object);
            var helper = new SignInManager<TestUser>(manager.Object, contextAccessor.Object, new ClaimsIdentityFactory<TestUser, TestRole>(manager.Object, roleManager.Object, options.Object), options.Object, loggerFactory.Object);
            string expected = string.Format("{0} for user: {1} : Result : {2}", "PasswordSignInAsync", user.Id, "RequiresTwoFactor");

            // Act
            var result = await helper.PasswordSignInAsync(user.UserName, "password", false, false);

            // Assert
            Assert.False(result.Succeeded);
            Assert.True(result.RequiresTwoFactor);
            Assert.NotEqual(-1, logStore.ToString().IndexOf(expected));
            manager.VerifyAll();
            context.VerifyAll();
            response.VerifyAll();
            contextAccessor.VerifyAll();
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public async Task CanExternalSignIn(bool isPersistent, bool supportsLockout)
        {
            // Setup
            var user = new TestUser { UserName = "Foo" };
            const string loginProvider = "login";
            const string providerKey = "fookey";
            var manager = MockHelpers.MockUserManager<TestUser>();
            manager.Setup(m => m.SupportsUserLockout).Returns(supportsLockout).Verifiable();
            if (supportsLockout)
            {
                manager.Setup(m => m.IsLockedOutAsync(user, CancellationToken.None)).ReturnsAsync(false).Verifiable();
            }
            manager.Setup(m => m.FindByLoginAsync(loginProvider, providerKey, CancellationToken.None)).ReturnsAsync(user).Verifiable();
            manager.Setup(m => m.GetUserIdAsync(user, CancellationToken.None)).ReturnsAsync(user.Id.ToString()).Verifiable();

            var context = new Mock<HttpContext>();
            var response = new Mock<HttpResponse>();
            context.Setup(c => c.Response).Returns(response.Object).Verifiable();
            response.Setup(r => r.SignIn(
                It.Is<AuthenticationProperties>(v => v.IsPersistent == isPersistent),
                It.Is<ClaimsIdentity>(i => i.FindFirstValue(ClaimTypes.AuthenticationMethod) == loginProvider))).Verifiable();
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(a => a.Value).Returns(context.Object);
            var roleManager = MockHelpers.MockRoleManager<TestRole>();
            var identityOptions = new IdentityOptions();
            response.Setup(r => r.SignOut(IdentityOptions.ExternalCookieAuthenticationType)).Verifiable();
            var options = new Mock<IOptions<IdentityOptions>>();
            options.Setup(a => a.Options).Returns(identityOptions);
            var claimsFactory = new Mock<ClaimsIdentityFactory<TestUser, TestRole>>(manager.Object, roleManager.Object, options.Object);
            claimsFactory.Setup(m => m.CreateAsync(user, CancellationToken.None)).ReturnsAsync(new ClaimsIdentity("Microsoft.AspNet.Identity")).Verifiable();
            var logStore = new StringBuilder();
            var loggerFactory = MockHelpers.MockILoggerFactory(MockHelpers.MockILogger(logStore).Object);
            var helper = new SignInManager<TestUser>(manager.Object, contextAccessor.Object, claimsFactory.Object, options.Object, loggerFactory.Object);
            string expected = string.Format("{0} for user: {1} : Result : {2}", "ExternalLoginSignInAsync", user.Id.ToString(), "Succeeded");

            // Act
            var result = await helper.ExternalLoginSignInAsync(loginProvider, providerKey, isPersistent);

            // Assert
            Assert.True(result.Succeeded);
            Assert.NotEqual(-1, logStore.ToString().IndexOf(expected));
            manager.VerifyAll();
            context.VerifyAll();
            response.VerifyAll();
            contextAccessor.VerifyAll();
            claimsFactory.VerifyAll();
        }

        [Theory]
        [InlineData(true, true, true, true)]
        [InlineData(true, true, false, true)]
        [InlineData(true, false, true, true)]
        [InlineData(true, false, false, true)]
        [InlineData(false, true, true, true)]
        [InlineData(false, true, false, true)]
        [InlineData(false, false, true, true)]
        [InlineData(false, false, false, true)]
        [InlineData(true, true, true, false)]
        [InlineData(true, true, false, false)]
        [InlineData(true, false, true, false)]
        [InlineData(true, false, false, false)]
        [InlineData(false, true, true, false)]
        [InlineData(false, true, false, false)]
        [InlineData(false, false, true, false)]
        [InlineData(false, false, false, false)]
        public async Task CanTwoFactorSignIn(bool isPersistent, bool supportsLockout, bool externalLogin, bool rememberClient)
        {
            // Setup
            var user = new TestUser { UserName = "Foo" };
            var manager = MockHelpers.MockUserManager<TestUser>();
            var provider = "twofactorprovider";
            var code = "123456";
            manager.Setup(m => m.SupportsUserLockout).Returns(supportsLockout).Verifiable();
            if (supportsLockout)
            {
                manager.Setup(m => m.IsLockedOutAsync(user, CancellationToken.None)).ReturnsAsync(false).Verifiable();
                manager.Setup(m => m.ResetAccessFailedCountAsync(user, CancellationToken.None)).ReturnsAsync(IdentityResult.Success).Verifiable();
            }
            manager.Setup(m => m.FindByIdAsync(user.Id, CancellationToken.None)).ReturnsAsync(user).Verifiable();
            manager.Setup(m => m.VerifyTwoFactorTokenAsync(user, provider, code, CancellationToken.None)).ReturnsAsync(true).Verifiable();
            manager.Setup(m => m.GetUserIdAsync(user, CancellationToken.None)).ReturnsAsync(user.Id).Verifiable();
            manager.Setup(m => m.GetUserNameAsync(user, CancellationToken.None)).ReturnsAsync(user.UserName).Verifiable();
            var context = new Mock<HttpContext>();
            var response = new Mock<HttpResponse>();
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var twoFactorInfo = new SignInManager<TestUser>.TwoFactorAuthenticationInfo { UserId = user.Id };
            var loginProvider = "loginprovider";
            var id = SignInManager<TestUser>.StoreTwoFactorInfo(user.Id, externalLogin ? loginProvider : null);
            var authResult = new AuthenticationResult(id, new AuthenticationProperties(), new AuthenticationDescription());
            var roleManager = MockHelpers.MockRoleManager<TestRole>();
            var identityOptions = new IdentityOptions();
            var options = new Mock<IOptions<IdentityOptions>>();
            options.Setup(a => a.Options).Returns(identityOptions);
            var claimsFactory = new ClaimsIdentityFactory<TestUser, TestRole>(manager.Object, roleManager.Object, options.Object);
            if (externalLogin)
            {
                response.Setup(r => r.SignIn(
                    It.Is<AuthenticationProperties>(v => v.IsPersistent == isPersistent),
                    It.Is<ClaimsIdentity>(i => i.FindFirstValue(ClaimTypes.NameIdentifier) == user.Id
                        && i.FindFirstValue(ClaimTypes.AuthenticationMethod) == loginProvider))).Verifiable();
                response.Setup(r => r.SignOut(IdentityOptions.ExternalCookieAuthenticationType)).Verifiable();
            }
            else
            {
                response.Setup(r => r.SignIn(
                    It.Is<AuthenticationProperties>(v => v.IsPersistent == isPersistent),
                    It.Is<ClaimsIdentity>(i => i.FindFirstValue(ClaimTypes.NameIdentifier) == user.Id))).Verifiable();
            }
            if (rememberClient)
            {
                response.Setup(r => r.SignIn(
                    It.Is<AuthenticationProperties>(v => v.IsPersistent == true),
                    It.Is<ClaimsIdentity>(i => i.FindFirstValue(ClaimTypes.Name) == user.Id
                        && i.AuthenticationType == IdentityOptions.TwoFactorRememberMeCookieAuthenticationType))).Verifiable();
            }
            context.Setup(c => c.Response).Returns(response.Object).Verifiable();
            context.Setup(c => c.AuthenticateAsync(IdentityOptions.TwoFactorUserIdCookieAuthenticationType)).ReturnsAsync(authResult).Verifiable();
            contextAccessor.Setup(a => a.Value).Returns(context.Object);
            var logStore = new StringBuilder();
            var loggerFactory = MockHelpers.MockILoggerFactory(MockHelpers.MockILogger(logStore).Object);
            var helper = new SignInManager<TestUser>(manager.Object, contextAccessor.Object, claimsFactory, options.Object, loggerFactory.Object);
            string expected = string.Format("{0} for user: {1} : Result : {2}", "TwoFactorSignInAsync", user.Id.ToString(), "Succeeded");

            // Act
            var result = await helper.TwoFactorSignInAsync(provider, code, isPersistent, rememberClient);

            // Assert
            Assert.True(result.Succeeded);
            Assert.NotEqual(-1, logStore.ToString().IndexOf(expected));
            manager.VerifyAll();
            context.VerifyAll();
            response.VerifyAll();
            contextAccessor.VerifyAll();
        }

        [Fact]
        public async Task RememberClientStoresUserId()
        {
            // Setup
            var user = new TestUser { UserName = "Foo" };
            var manager = MockHelpers.MockUserManager<TestUser>();
            var context = new Mock<HttpContext>();
            var response = new Mock<HttpResponse>();
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var roleManager = MockHelpers.MockRoleManager<TestRole>();
            var identityOptions = new IdentityOptions();
            var options = new Mock<IOptions<IdentityOptions>>();
            options.Setup(a => a.Options).Returns(identityOptions);
            var claimsFactory = new ClaimsIdentityFactory<TestUser, TestRole>(manager.Object, roleManager.Object, options.Object);

            manager.Setup(m => m.GetUserIdAsync(user, CancellationToken.None)).ReturnsAsync(user.Id).Verifiable();
            context.Setup(c => c.Response).Returns(response.Object).Verifiable();
            response.Setup(r => r.SignIn(
                It.Is<AuthenticationProperties>(v => v.IsPersistent == true),
                It.Is<ClaimsIdentity>(i => i.FindFirstValue(ClaimTypes.Name) == user.Id
                    && i.AuthenticationType == IdentityOptions.TwoFactorRememberMeCookieAuthenticationType))).Verifiable();
            contextAccessor.Setup(a => a.Value).Returns(context.Object).Verifiable();
            options.Setup(a => a.Options).Returns(identityOptions).Verifiable();

            var helper = new SignInManager<TestUser>(manager.Object, contextAccessor.Object, claimsFactory, options.Object);

            // Act
            await helper.RememberTwoFactorClientAsync(user);

            // Assert
            manager.VerifyAll();
            context.VerifyAll();
            response.VerifyAll();
            contextAccessor.VerifyAll();
            options.VerifyAll();
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
            IList<string> providers = new List<string>();
            providers.Add("PhoneNumber");
            manager.Setup(m => m.GetValidTwoFactorProvidersAsync(user, CancellationToken.None)).Returns(Task.FromResult(providers)).Verifiable();
            manager.Setup(m => m.SupportsUserLockout).Returns(true).Verifiable();
            manager.Setup(m => m.SupportsUserTwoFactor).Returns(true).Verifiable();
            manager.Setup(m => m.IsLockedOutAsync(user, CancellationToken.None)).ReturnsAsync(false).Verifiable();
            manager.Setup(m => m.FindByNameAsync(user.UserName, CancellationToken.None)).ReturnsAsync(user).Verifiable();
            manager.Setup(m => m.GetUserIdAsync(user, CancellationToken.None)).ReturnsAsync(user.Id).Verifiable();
            manager.Setup(m => m.CheckPasswordAsync(user, "password", CancellationToken.None)).ReturnsAsync(true).Verifiable();
            var context = new Mock<HttpContext>();
            var response = new Mock<HttpResponse>();
            context.Setup(c => c.Response).Returns(response.Object).Verifiable();
            response.Setup(r => r.SignIn(It.Is<AuthenticationProperties>(v => v.IsPersistent == isPersistent), It.Is<ClaimsIdentity>(i => i.AuthenticationType == IdentityOptions.ApplicationCookieAuthenticationType))).Verifiable();
            var id = new ClaimsIdentity(IdentityOptions.TwoFactorRememberMeCookieAuthenticationType);
            id.AddClaim(new Claim(ClaimTypes.Name, user.Id));
            var authResult = new AuthenticationResult(id, new AuthenticationProperties(), new AuthenticationDescription());
            context.Setup(c => c.AuthenticateAsync(IdentityOptions.TwoFactorRememberMeCookieAuthenticationType)).ReturnsAsync(authResult).Verifiable();
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(a => a.Value).Returns(context.Object);
            var roleManager = MockHelpers.MockRoleManager<TestRole>();
            var identityOptions = new IdentityOptions();
            var options = new Mock<IOptions<IdentityOptions>>();
            options.Setup(a => a.Options).Returns(identityOptions);
            var claimsFactory = new Mock<ClaimsIdentityFactory<TestUser, TestRole>>(manager.Object, roleManager.Object, options.Object);
            claimsFactory.Setup(m => m.CreateAsync(user, CancellationToken.None)).ReturnsAsync(new ClaimsIdentity(IdentityOptions.ApplicationCookieAuthenticationType)).Verifiable();
            var helper = new SignInManager<TestUser>(manager.Object, contextAccessor.Object, claimsFactory.Object, options.Object);

            // Act
            var result = await helper.PasswordSignInAsync(user.UserName, "password", isPersistent, false);

            // Assert
            Assert.True(result.Succeeded);
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
            response.Setup(r => r.SignOut(IdentityOptions.TwoFactorUserIdCookieAuthenticationType)).Verifiable();
            response.Setup(r => r.SignOut(IdentityOptions.ExternalCookieAuthenticationType)).Verifiable();
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(a => a.Value).Returns(context.Object);
            var roleManager = MockHelpers.MockRoleManager<TestRole>();
            var identityOptions = new IdentityOptions();
            var options = new Mock<IOptions<IdentityOptions>>();
            options.Setup(a => a.Options).Returns(identityOptions);
            IdentityOptions.ApplicationCookieAuthenticationType = authenticationType;
            var claimsFactory = new Mock<ClaimsIdentityFactory<TestUser, TestRole>>(manager.Object, roleManager.Object, options.Object);
            var logStore = new StringBuilder();
            var loggerFactory = MockHelpers.MockILoggerFactory(MockHelpers.MockILogger(logStore).Object);
            var helper = new SignInManager<TestUser>(manager.Object, contextAccessor.Object, claimsFactory.Object, options.Object, loggerFactory.Object);

            // Act
            helper.SignOut();

            // Assert
            context.VerifyAll();
            response.VerifyAll();
            contextAccessor.VerifyAll();
            claimsFactory.VerifyAll();
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
            manager.Setup(m => m.GetUserIdAsync(user, CancellationToken.None)).ReturnsAsync(user.Id.ToString()).Verifiable();
            var context = new Mock<HttpContext>();
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(a => a.Value).Returns(context.Object);
            var roleManager = MockHelpers.MockRoleManager<TestRole>();
            var identityOptions = new IdentityOptions();
            var options = new Mock<IOptions<IdentityOptions>>();
            options.Setup(a => a.Options).Returns(identityOptions);
            var claimsFactory = new Mock<ClaimsIdentityFactory<TestUser, TestRole>>(manager.Object, roleManager.Object, options.Object);
            var logStore = new StringBuilder();
            var loggerFactory = MockHelpers.MockILoggerFactory(MockHelpers.MockILogger(logStore).Object);
            var helper = new SignInManager<TestUser>(manager.Object, contextAccessor.Object, claimsFactory.Object, options.Object, loggerFactory.Object);
            string expected = string.Format("{0} for user: {1} : Result : {2}", "PasswordSignInAsync", user.Id.ToString(), "Failed");
            // Act
            var result = await helper.PasswordSignInAsync(user.UserName, "bogus", false, false);

            // Assert
            Assert.False(result.Succeeded);
            Assert.NotEqual(-1, logStore.ToString().IndexOf(expected));
            manager.VerifyAll();
            context.VerifyAll();
            contextAccessor.VerifyAll();
        }

        [Fact]
        public async Task PasswordSignInFailsWithUnknownUser()
        {
            // Setup
            var manager = MockHelpers.MockUserManager<TestUser>();
            manager.Setup(m => m.FindByNameAsync("bogus", CancellationToken.None)).ReturnsAsync(null).Verifiable();
            var context = new Mock<HttpContext>();
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(a => a.Value).Returns(context.Object);
            var roleManager = MockHelpers.MockRoleManager<TestRole>();
            var identityOptions = new IdentityOptions();
            var options = new Mock<IOptions<IdentityOptions>>();
            options.Setup(a => a.Options).Returns(identityOptions);
            var claimsFactory = new Mock<ClaimsIdentityFactory<TestUser, TestRole>>(manager.Object, roleManager.Object, options.Object);
            var helper = new SignInManager<TestUser>(manager.Object, contextAccessor.Object, claimsFactory.Object, options.Object);

            // Act
            var result = await helper.PasswordSignInAsync("bogus", "bogus", false, false);

            // Assert
            Assert.False(result.Succeeded);
            manager.VerifyAll();
            context.VerifyAll();
            contextAccessor.VerifyAll();
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
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(a => a.Value).Returns(context.Object);
            var roleManager = MockHelpers.MockRoleManager<TestRole>();
            var identityOptions = new IdentityOptions();
            var options = new Mock<IOptions<IdentityOptions>>();
            options.Setup(a => a.Options).Returns(identityOptions);
            var claimsFactory = new Mock<ClaimsIdentityFactory<TestUser, TestRole>>(manager.Object, roleManager.Object, options.Object);
            var helper = new SignInManager<TestUser>(manager.Object, contextAccessor.Object, claimsFactory.Object, options.Object);

            // Act
            var result = await helper.PasswordSignInAsync(user.UserName, "bogus", false, true);

            // Assert
            Assert.False(result.Succeeded);
            Assert.True(result.IsLockedOut);
            manager.VerifyAll();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CanRequireConfirmedEmailForPasswordSignIn(bool confirmed)
        {
            // Setup
            var user = new TestUser { UserName = "Foo" };
            var manager = MockHelpers.MockUserManager<TestUser>();
            manager.Setup(m => m.IsEmailConfirmedAsync(user, CancellationToken.None)).ReturnsAsync(confirmed).Verifiable();
            if (confirmed)
            {
                manager.Setup(m => m.CheckPasswordAsync(user, "password", CancellationToken.None)).ReturnsAsync(true).Verifiable();
            }
            manager.Setup(m => m.GetUserIdAsync(user, CancellationToken.None)).ReturnsAsync(user.Id.ToString()).Verifiable();
            var context = new Mock<HttpContext>();
            var response = new Mock<HttpResponse>();
            if (confirmed)
            {
                manager.Setup(m => m.CheckPasswordAsync(user, "password", CancellationToken.None)).ReturnsAsync(true).Verifiable();
                context.Setup(c => c.Response).Returns(response.Object).Verifiable();
                response.Setup(r => r.SignIn(It.Is<AuthenticationProperties>(v => v.IsPersistent == false), It.IsAny<ClaimsIdentity>())).Verifiable();
            }
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(a => a.Value).Returns(context.Object);
            var roleManager = MockHelpers.MockRoleManager<TestRole>();
            var identityOptions = new IdentityOptions();
            identityOptions.SignIn.RequireConfirmedEmail = true;
            var options = new Mock<IOptions<IdentityOptions>>();
            options.Setup(a => a.Options).Returns(identityOptions);
            var claimsFactory = new Mock<ClaimsIdentityFactory<TestUser, TestRole>>(manager.Object, roleManager.Object, options.Object);
            var logStore = new StringBuilder();
            var loggerFactory = MockHelpers.MockILoggerFactory(MockHelpers.MockILogger(logStore).Object);
            var helper = new SignInManager<TestUser>(manager.Object, contextAccessor.Object, claimsFactory.Object, options.Object, loggerFactory.Object);
            string expected = string.Format("{0} for user: {1} : Result : {2}", "CanSignInAsync", user.Id.ToString(), confirmed.ToString());

            // Act
            var result = await helper.PasswordSignInAsync(user, "password", false, false);

            // Assert

            Assert.Equal(confirmed, result.Succeeded);
            Assert.NotEqual(confirmed, result.IsNotAllowed);
            Assert.NotEqual(-1, logStore.ToString().IndexOf(expected));
            manager.VerifyAll();
            context.VerifyAll();
            response.VerifyAll();
            contextAccessor.VerifyAll();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CanRequireConfirmedPhoneNumberForPasswordSignIn(bool confirmed)
        {
            // Setup
            var user = new TestUser { UserName = "Foo" };
            var manager = MockHelpers.MockUserManager<TestUser>();
            manager.Setup(m => m.IsPhoneNumberConfirmedAsync(user, CancellationToken.None)).ReturnsAsync(confirmed).Verifiable();
            manager.Setup(m => m.GetUserIdAsync(user, CancellationToken.None)).ReturnsAsync(user.Id.ToString()).Verifiable();
            var context = new Mock<HttpContext>();
            var response = new Mock<HttpResponse>();
            if (confirmed)
            {
                manager.Setup(m => m.CheckPasswordAsync(user, "password", CancellationToken.None)).ReturnsAsync(true).Verifiable();
                context.Setup(c => c.Response).Returns(response.Object).Verifiable();
                response.Setup(r => r.SignIn(It.Is<AuthenticationProperties>(v => v.IsPersistent == false), It.IsAny<ClaimsIdentity>())).Verifiable();
            }

            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(a => a.Value).Returns(context.Object);
            var roleManager = MockHelpers.MockRoleManager<TestRole>();
            var identityOptions = new IdentityOptions();
            identityOptions.SignIn.RequireConfirmedPhoneNumber = true;
            var options = new Mock<IOptions<IdentityOptions>>();
            options.Setup(a => a.Options).Returns(identityOptions);
            var claimsFactory = new Mock<ClaimsIdentityFactory<TestUser, TestRole>>(manager.Object, roleManager.Object, options.Object);
            var logStore = new StringBuilder();
            var loggerFactory = MockHelpers.MockILoggerFactory(MockHelpers.MockILogger(logStore).Object);
            var helper = new SignInManager<TestUser>(manager.Object, contextAccessor.Object, claimsFactory.Object, options.Object, loggerFactory.Object);
            string expected = string.Format("{0} for user: {1} : Result : {2}", "CanSignInAsync", user.Id.ToString(), confirmed.ToString());

            // Act
            var result = await helper.PasswordSignInAsync(user, "password", false, false);

            // Assert
            Assert.Equal(confirmed, result.Succeeded);
            Assert.NotEqual(confirmed, result.IsNotAllowed);
            Assert.NotEqual(-1, logStore.ToString().IndexOf(expected));
            manager.VerifyAll();
            context.VerifyAll();
            response.VerifyAll();
            contextAccessor.VerifyAll();
        }

    }
}
