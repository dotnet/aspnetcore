// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Authentication;
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
        //        AuthenticationScheme = ClaimsIdentityOptions.DefaultAuthenticationScheme
        //    });

        // TODO: how to functionally test context?
        //    var context = new DefaultHttpContext(new FeatureCollection());
        //    var contextAccessor = new Mock<IHttpContextAccessor>();
        //    contextAccessor.Setup(a => a.Value).Returns(context);
        //    app.UseServices(services =>
        //    {
        //        services.AddInstance(contextAccessor.Object);
        //        services.AddInstance<Ilogger>(new Nulllogger());
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
        //    contextAccessor.Verify();
        //}

        [Fact]
        public void ConstructorNullChecks()
        {
            Assert.Throws<ArgumentNullException>("userManager", () => new SignInManager<TestUser>(null, null, null, null, null));
            var userManager = MockHelpers.MockUserManager<TestUser>().Object;
            Assert.Throws<ArgumentNullException>("contextAccessor", () => new SignInManager<TestUser>(userManager, null, null, null));
            var contextAccessor = new Mock<IHttpContextAccessor>();
            Assert.Throws<ArgumentNullException>("contextAccessor", () => new SignInManager<TestUser>(userManager, contextAccessor.Object, null, null));
            var context = new Mock<HttpContext>();
            contextAccessor.Setup(a => a.HttpContext).Returns(context.Object);
            Assert.Throws<ArgumentNullException>("claimsFactory", () => new SignInManager<TestUser>(userManager, contextAccessor.Object, null, null));
        }

        //TODO: Mock fails in K (this works fine in net45)
        //[Fact]
        //public async Task EnsureClaimsPrincipalFactoryCreateIdentityCalled()
        //{
        //    // Setup
        //    var user = new TestUser { UserName = "Foo" };
        //    var userManager = MockHelpers.TestUserManager<TestUser>();
        //    var identityFactory = new Mock<IUserClaimsPrincipalFactory<TestUser>>();
        //    const string authType = "Test";
        //    var testIdentity = new ClaimsIdentity(authType);
        //    identityFactory.Setup(s => s.CreateAsync(userManager, user, authType)).ReturnsAsync(testIdentity).Verifiable();
        //    userManager.UserClaimsPrincipalFactory = identityFactory.Object;
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
        //    identityFactory.Verify();
        //    context.Verify();
        //    contextAccessor.Verify();
        //    response.Verify();
        //}

        [Fact]
        public async Task PasswordSignInReturnsLockedOutWhenLockedOut()
        {
            // Setup
            var user = new TestUser { UserName = "Foo" };
            var manager = SetupUserManager(user);
            manager.Setup(m => m.SupportsUserLockout).Returns(true).Verifiable();
            manager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(true).Verifiable();

            var context = new Mock<HttpContext>();
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(a => a.HttpContext).Returns(context.Object);
            var roleManager = MockHelpers.MockRoleManager<TestRole>();
            var identityOptions = new IdentityOptions();
            var options = new Mock<IOptions<IdentityOptions>>();
            options.Setup(a => a.Options).Returns(identityOptions);
            var claimsFactory = new UserClaimsPrincipalFactory<TestUser, TestRole>(manager.Object, roleManager.Object, options.Object);
            var logStore = new StringBuilder();
            var logger = MockHelpers.MockILogger<SignInManager<TestUser>>(logStore);
            var helper = new SignInManager<TestUser>(manager.Object, contextAccessor.Object, claimsFactory, options.Object, null);
            helper.Logger = logger.Object;
            string expectedScope = string.Format("{0} for {1}: {2}", "PasswordSignInAsync", "user", user.Id);
            string expectedLog = string.Format("{0} : {1}", "PasswordSignInAsync", "Lockedout");

            // Act
            var result = await helper.PasswordSignInAsync(user.UserName, "bogus", false, false);

            // Assert
            Assert.False(result.Succeeded);
            Assert.True(result.IsLockedOut);
            Assert.NotEqual(-1, logStore.ToString().IndexOf(expectedLog));
            Assert.NotEqual(-1, logStore.ToString().IndexOf(expectedScope));
            manager.Verify();
        }

        private static Mock<UserManager<TestUser>> SetupUserManager(TestUser user)
        {
            var manager = MockHelpers.MockUserManager<TestUser>();
            manager.Setup(m => m.FindByNameAsync(user.UserName)).ReturnsAsync(user);
            manager.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
            manager.Setup(m => m.GetUserIdAsync(user)).ReturnsAsync(user.Id.ToString());
            manager.Setup(m => m.GetUserNameAsync(user)).ReturnsAsync(user.UserName);
            return manager;
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CanPasswordSignIn(bool isPersistent)
        {
            // Setup
            var user = new TestUser { UserName = "Foo" };
            var manager = SetupUserManager(user);
            manager.Setup(m => m.SupportsUserLockout).Returns(true).Verifiable();
            manager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false).Verifiable();
            manager.Setup(m => m.CheckPasswordAsync(user, "password")).ReturnsAsync(true).Verifiable();

            var context = new Mock<HttpContext>();
            var response = new Mock<HttpResponse>();
            context.Setup(c => c.Response).Returns(response.Object).Verifiable();
            SetupSignIn(response, user.Id, isPersistent);
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(a => a.HttpContext).Returns(context.Object);
            var roleManager = MockHelpers.MockRoleManager<TestRole>();
            var identityOptions = new IdentityOptions();
            var options = new Mock<IOptions<IdentityOptions>>();
            options.Setup(a => a.Options).Returns(identityOptions);
            var claimsFactory = new UserClaimsPrincipalFactory<TestUser, TestRole>(manager.Object, roleManager.Object, options.Object);
            var logStore = new StringBuilder();
            var logger = MockHelpers.MockILogger<SignInManager<TestUser>>(logStore);
            var helper = new SignInManager<TestUser>(manager.Object, contextAccessor.Object, claimsFactory, options.Object, null);
            helper.Logger = logger.Object;
            string expectedScope = string.Format("{0} for {1}: {2}", "PasswordSignInAsync", "user", user.Id);
            string expectedLog = string.Format("{0} : {1}", "PasswordSignInAsync", "Succeeded");

            // Act
            var result = await helper.PasswordSignInAsync(user.UserName, "password", isPersistent, false);

            // Assert
            Assert.True(result.Succeeded);
            Assert.NotEqual(-1, logStore.ToString().IndexOf(expectedLog));
            Assert.NotEqual(-1, logStore.ToString().IndexOf(expectedScope));
            manager.Verify();
            context.Verify();
            response.Verify();
            contextAccessor.Verify();
        }

        [Fact]
        public async Task PasswordSignInWorksWithNonTwoFactorStore()
        {
            // Setup
            var user = new TestUser { UserName = "Foo" };
            var manager = SetupUserManager(user);
            manager.Setup(m => m.SupportsUserLockout).Returns(true).Verifiable();
            manager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false).Verifiable();
            manager.Setup(m => m.CheckPasswordAsync(user, "password")).ReturnsAsync(true).Verifiable();
            manager.Setup(m => m.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success).Verifiable();

            var context = new Mock<HttpContext>();
            var response = new Mock<HttpResponse>();
            SetupSignIn(response);
            context.Setup(c => c.Response).Returns(response.Object).Verifiable();
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(a => a.HttpContext).Returns(context.Object);
            var roleManager = MockHelpers.MockRoleManager<TestRole>();
            var identityOptions = new IdentityOptions();
            var options = new Mock<IOptions<IdentityOptions>>();
            options.Setup(a => a.Options).Returns(identityOptions);
            var claimsFactory = new UserClaimsPrincipalFactory<TestUser, TestRole>(manager.Object, roleManager.Object, options.Object);
            var helper = new SignInManager<TestUser>(manager.Object, contextAccessor.Object, claimsFactory, options.Object);

            // Act
            var result = await helper.PasswordSignInAsync(user.UserName, "password", false, false);

            // Assert
            Assert.True(result.Succeeded);
            manager.Verify();
            context.Verify();
            response.Verify();
            contextAccessor.Verify();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task PasswordSignInRequiresVerification(bool supportsLockout)
        {
            // Setup
            var user = new TestUser { UserName = "Foo" };
            var manager = SetupUserManager(user);
            manager.Setup(m => m.SupportsUserLockout).Returns(supportsLockout).Verifiable();
            if (supportsLockout)
            {
                manager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false).Verifiable();
            }
            IList<string> providers = new List<string>();
            providers.Add("PhoneNumber");
            manager.Setup(m => m.GetValidTwoFactorProvidersAsync(user)).Returns(Task.FromResult(providers)).Verifiable();
            manager.Setup(m => m.SupportsUserTwoFactor).Returns(true).Verifiable();
            manager.Setup(m => m.GetTwoFactorEnabledAsync(user)).ReturnsAsync(true).Verifiable();
            manager.Setup(m => m.CheckPasswordAsync(user, "password")).ReturnsAsync(true).Verifiable();
            if (supportsLockout)
            {
                manager.Setup(m => m.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success).Verifiable();
            }
            var context = new Mock<HttpContext>();
            var response = new Mock<HttpResponse>();
            response.Setup(r => r.SignIn(IdentityOptions.TwoFactorUserIdCookieAuthenticationScheme,
                It.Is<ClaimsPrincipal>(id => id.FindFirstValue(ClaimTypes.Name) == user.Id),
                It.IsAny<AuthenticationProperties>())).Verifiable();
            context.Setup(c => c.Response).Returns(response.Object).Verifiable();
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(a => a.HttpContext).Returns(context.Object);
            var roleManager = MockHelpers.MockRoleManager<TestRole>();
            var identityOptions = new IdentityOptions();
            var options = new Mock<IOptions<IdentityOptions>>();
            options.Setup(a => a.Options).Returns(identityOptions);
            var logStore = new StringBuilder();
            var logger = MockHelpers.MockILogger<SignInManager<TestUser>>(logStore);
            var helper = new SignInManager<TestUser>(manager.Object,
                contextAccessor.Object,
                new UserClaimsPrincipalFactory<TestUser, TestRole>(manager.Object, roleManager.Object, options.Object),
                options.Object,
                null);
            helper.Logger = logger.Object;
            string expectedScope = string.Format("{0} for {1}: {2}", "PasswordSignInAsync", "user", user.Id);
            string expectedLog = string.Format("{0} : {1}", "PasswordSignInAsync", "RequiresTwoFactor");

            // Act
            var result = await helper.PasswordSignInAsync(user.UserName, "password", false, false);

            // Assert
            Assert.False(result.Succeeded);
            Assert.True(result.RequiresTwoFactor);
            Assert.NotEqual(-1, logStore.ToString().IndexOf(expectedLog));
            Assert.NotEqual(-1, logStore.ToString().IndexOf(expectedScope));
            manager.Verify();
            context.Verify();
            response.Verify();
            contextAccessor.Verify();
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
            var manager = SetupUserManager(user);
            manager.Setup(m => m.SupportsUserLockout).Returns(supportsLockout).Verifiable();
            if (supportsLockout)
            {
                manager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false).Verifiable();
            }
            manager.Setup(m => m.FindByLoginAsync(loginProvider, providerKey)).ReturnsAsync(user).Verifiable();

            var context = new Mock<HttpContext>();
            var response = new Mock<HttpResponse>();
            context.Setup(c => c.Response).Returns(response.Object).Verifiable();
            SetupSignIn(response, user.Id, isPersistent, loginProvider);
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(a => a.HttpContext).Returns(context.Object);
            var roleManager = MockHelpers.MockRoleManager<TestRole>();
            var identityOptions = new IdentityOptions();
            response.Setup(r => r.SignOut(IdentityOptions.ExternalCookieAuthenticationScheme)).Verifiable();
            var options = new Mock<IOptions<IdentityOptions>>();
            options.Setup(a => a.Options).Returns(identityOptions);
            var claimsFactory = new UserClaimsPrincipalFactory<TestUser, TestRole>(manager.Object, roleManager.Object, options.Object);
            var logStore = new StringBuilder();
            var logger = MockHelpers.MockILogger<SignInManager<TestUser>>(logStore);
            var helper = new SignInManager<TestUser>(manager.Object, contextAccessor.Object, claimsFactory, options.Object, null);
            helper.Logger = logger.Object;
            string expectedScope = string.Format("{0} for {1}: {2}", "ExternalLoginSignInAsync", "user", user.Id);
            string expectedLog = string.Format("{0} : {1}", "ExternalLoginSignInAsync", "Succeeded");

            // Act
            var result = await helper.ExternalLoginSignInAsync(loginProvider, providerKey, isPersistent);

            // Assert
            Assert.True(result.Succeeded);
            Assert.NotEqual(-1, logStore.ToString().IndexOf(expectedLog));
            Assert.NotEqual(-1, logStore.ToString().IndexOf(expectedScope));
            manager.Verify();
            context.Verify();
            response.Verify();
            contextAccessor.Verify();
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
            var manager = SetupUserManager(user);
            var provider = "twofactorprovider";
            var code = "123456";
            manager.Setup(m => m.SupportsUserLockout).Returns(supportsLockout).Verifiable();
            if (supportsLockout)
            {
                manager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false).Verifiable();
                manager.Setup(m => m.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success).Verifiable();
            }
            manager.Setup(m => m.VerifyTwoFactorTokenAsync(user, provider, code)).ReturnsAsync(true).Verifiable();
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
            var claimsFactory = new UserClaimsPrincipalFactory<TestUser, TestRole>(manager.Object, roleManager.Object, options.Object);
            if (externalLogin)
            {
                response.Setup(r => r.SignIn(
                    IdentityOptions.ApplicationCookieAuthenticationScheme,
                    It.Is<ClaimsPrincipal>(i => i.FindFirstValue(ClaimTypes.AuthenticationMethod) == loginProvider
                        && i.FindFirstValue(ClaimTypes.NameIdentifier) == user.Id),
                    It.Is<AuthenticationProperties>(v => v.IsPersistent == isPersistent))).Verifiable();
                response.Setup(r => r.SignOut(IdentityOptions.ExternalCookieAuthenticationScheme)).Verifiable();
            }
            else
            {
                SetupSignIn(response, user.Id);
            }
            if (rememberClient)
            {
                response.Setup(r => r.SignIn(
                    IdentityOptions.TwoFactorRememberMeCookieAuthenticationScheme,
                    It.Is<ClaimsPrincipal>(i => i.FindFirstValue(ClaimTypes.Name) == user.Id
                        && i.Identities.First().AuthenticationType == IdentityOptions.TwoFactorRememberMeCookieAuthenticationType),
                    It.Is<AuthenticationProperties>(v => v.IsPersistent == true))).Verifiable();
            }
            context.Setup(c => c.Response).Returns(response.Object).Verifiable();
            context.Setup(c => c.AuthenticateAsync(IdentityOptions.TwoFactorUserIdCookieAuthenticationScheme)).ReturnsAsync(authResult).Verifiable();
            contextAccessor.Setup(a => a.HttpContext).Returns(context.Object);
            var logStore = new StringBuilder();
            var logger = MockHelpers.MockILogger<SignInManager<TestUser>>(logStore);
            var helper = new SignInManager<TestUser>(manager.Object, contextAccessor.Object, claimsFactory, options.Object, null);
            helper.Logger = logger.Object;
            string expectedScope = string.Format("{0} for {1}: {2}", "TwoFactorSignInAsync", "user", user.Id);
            string expectedLog = string.Format("{0} : {1}", "TwoFactorSignInAsync", "Succeeded");

            // Act
            var result = await helper.TwoFactorSignInAsync(provider, code, isPersistent, rememberClient);

            // Assert
            Assert.True(result.Succeeded);
            Assert.NotEqual(-1, logStore.ToString().IndexOf(expectedLog));
            Assert.NotEqual(-1, logStore.ToString().IndexOf(expectedScope));
            manager.Verify();
            context.Verify();
            response.Verify();
            contextAccessor.Verify();
        }

        [Fact]
        public async Task RememberClientStoresUserId()
        {
            // Setup
            var user = new TestUser { UserName = "Foo" };
            var manager = SetupUserManager(user);
            var context = new Mock<HttpContext>();
            var response = new Mock<HttpResponse>();
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var roleManager = MockHelpers.MockRoleManager<TestRole>();
            var identityOptions = new IdentityOptions();
            var options = new Mock<IOptions<IdentityOptions>>();
            options.Setup(a => a.Options).Returns(identityOptions);
            var claimsFactory = new UserClaimsPrincipalFactory<TestUser, TestRole>(manager.Object, roleManager.Object, options.Object);

            context.Setup(c => c.Response).Returns(response.Object).Verifiable();
            response.Setup(r => r.SignIn(
                IdentityOptions.TwoFactorRememberMeCookieAuthenticationScheme,
                It.Is<ClaimsPrincipal>(i => i.FindFirstValue(ClaimTypes.Name) == user.Id
                    && i.Identities.First().AuthenticationType == IdentityOptions.TwoFactorRememberMeCookieAuthenticationType),
                It.Is<AuthenticationProperties>(v => v.IsPersistent == true))).Verifiable();
            contextAccessor.Setup(a => a.HttpContext).Returns(context.Object).Verifiable();
            options.Setup(a => a.Options).Returns(identityOptions).Verifiable();

            var helper = new SignInManager<TestUser>(manager.Object, contextAccessor.Object, claimsFactory, options.Object);

            // Act
            await helper.RememberTwoFactorClientAsync(user);

            // Assert
            manager.Verify();
            context.Verify();
            response.Verify();
            contextAccessor.Verify();
            options.Verify();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task RememberBrowserSkipsTwoFactorVerificationSignIn(bool isPersistent)
        {
            // Setup
            var user = new TestUser { UserName = "Foo" };
            var manager = SetupUserManager(user);
            manager.Setup(m => m.GetTwoFactorEnabledAsync(user)).ReturnsAsync(true).Verifiable();
            IList<string> providers = new List<string>();
            providers.Add("PhoneNumber");
            manager.Setup(m => m.GetValidTwoFactorProvidersAsync(user)).Returns(Task.FromResult(providers)).Verifiable();
            manager.Setup(m => m.SupportsUserLockout).Returns(true).Verifiable();
            manager.Setup(m => m.SupportsUserTwoFactor).Returns(true).Verifiable();
            manager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false).Verifiable();
            manager.Setup(m => m.CheckPasswordAsync(user, "password")).ReturnsAsync(true).Verifiable();
            var context = new Mock<HttpContext>();
            var response = new Mock<HttpResponse>();
            context.Setup(c => c.Response).Returns(response.Object).Verifiable();
            SetupSignIn(response);
            var id = new ClaimsIdentity(IdentityOptions.TwoFactorRememberMeCookieAuthenticationType);
            id.AddClaim(new Claim(ClaimTypes.Name, user.Id));
            var authResult = new AuthenticationResult(new ClaimsPrincipal(id), new AuthenticationProperties(), new AuthenticationDescription());
            context.Setup(c => c.AuthenticateAsync(IdentityOptions.TwoFactorRememberMeCookieAuthenticationScheme)).ReturnsAsync(authResult).Verifiable();
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(a => a.HttpContext).Returns(context.Object);
            var roleManager = MockHelpers.MockRoleManager<TestRole>();
            var identityOptions = new IdentityOptions();
            var options = new Mock<IOptions<IdentityOptions>>();
            options.Setup(a => a.Options).Returns(identityOptions);
            var claimsFactory = new Mock<UserClaimsPrincipalFactory<TestUser, TestRole>>(manager.Object, roleManager.Object, options.Object);
            claimsFactory.Setup(m => m.CreateAsync(user)).ReturnsAsync(new ClaimsPrincipal(new ClaimsIdentity(IdentityOptions.ApplicationCookieAuthenticationType))).Verifiable();
            var helper = new SignInManager<TestUser>(manager.Object, contextAccessor.Object, claimsFactory.Object, options.Object);

            // Act
            var result = await helper.PasswordSignInAsync(user.UserName, "password", isPersistent, false);

            // Assert
            Assert.True(result.Succeeded);
            manager.Verify();
            context.Verify();
            response.Verify();
            contextAccessor.Verify();
            claimsFactory.Verify();
        }

        [Theory]
        [InlineData("Microsoft.AspNet.Identity.Authentication.Application")]
        [InlineData("Foo")]
        public void SignOutCallsContextResponseSignOut(string authenticationScheme)
        {
            // Setup
            var manager = MockHelpers.MockUserManager<TestUser>();
            var context = new Mock<HttpContext>();
            var response = new Mock<HttpResponse>();
            context.Setup(c => c.Response).Returns(response.Object).Verifiable();
            response.Setup(r => r.SignOut(authenticationScheme)).Verifiable();
            response.Setup(r => r.SignOut(IdentityOptions.TwoFactorUserIdCookieAuthenticationScheme)).Verifiable();
            response.Setup(r => r.SignOut(IdentityOptions.ExternalCookieAuthenticationScheme)).Verifiable();
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(a => a.HttpContext).Returns(context.Object);
            var roleManager = MockHelpers.MockRoleManager<TestRole>();
            var identityOptions = new IdentityOptions();
            var options = new Mock<IOptions<IdentityOptions>>();
            options.Setup(a => a.Options).Returns(identityOptions);
            IdentityOptions.ApplicationCookieAuthenticationScheme = authenticationScheme;
            var claimsFactory = new Mock<UserClaimsPrincipalFactory<TestUser, TestRole>>(manager.Object, roleManager.Object, options.Object);
            var logStore = new StringBuilder();
            var logger = MockHelpers.MockILogger<SignInManager<TestUser>>(logStore);
            var helper = new SignInManager<TestUser>(manager.Object, contextAccessor.Object, claimsFactory.Object, options.Object, null);
            helper.Logger = logger.Object;

            // Act
            helper.SignOut();

            // Assert
            context.Verify();
            response.Verify();
            contextAccessor.Verify();
            claimsFactory.Verify();
        }

        [Fact]
        public async Task PasswordSignInFailsWithWrongPassword()
        {
            // Setup
            var user = new TestUser { UserName = "Foo" };
            var manager = SetupUserManager(user);
            manager.Setup(m => m.SupportsUserLockout).Returns(true).Verifiable();
            manager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false).Verifiable();
            manager.Setup(m => m.CheckPasswordAsync(user, "bogus")).ReturnsAsync(false).Verifiable();
            var context = new Mock<HttpContext>();
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(a => a.HttpContext).Returns(context.Object);
            var roleManager = MockHelpers.MockRoleManager<TestRole>();
            var identityOptions = new IdentityOptions();
            var options = new Mock<IOptions<IdentityOptions>>();
            options.Setup(a => a.Options).Returns(identityOptions);
            var claimsFactory = new Mock<UserClaimsPrincipalFactory<TestUser, TestRole>>(manager.Object, roleManager.Object, options.Object);
            var logStore = new StringBuilder();
            var logger = MockHelpers.MockILogger<SignInManager<TestUser>>(logStore);
            var helper = new SignInManager<TestUser>(manager.Object, contextAccessor.Object, claimsFactory.Object, options.Object);
            helper.Logger = logger.Object;
            string expectedScope = string.Format("{0} for {1}: {2}", "PasswordSignInAsync", "user", user.Id);
            string expectedLog = string.Format("{0} : {1}", "PasswordSignInAsync", "Failed");
            // Act
            var result = await helper.PasswordSignInAsync(user.UserName, "bogus", false, false);

            // Assert
            Assert.False(result.Succeeded);
            Assert.NotEqual(-1, logStore.ToString().IndexOf(expectedLog));
            Assert.NotEqual(-1, logStore.ToString().IndexOf(expectedScope));
            manager.Verify();
            context.Verify();
            contextAccessor.Verify();
        }

        [Fact]
        public async Task PasswordSignInFailsWithUnknownUser()
        {
            // Setup
            var manager = MockHelpers.MockUserManager<TestUser>();
            manager.Setup(m => m.FindByNameAsync("bogus")).ReturnsAsync(null).Verifiable();
            var context = new Mock<HttpContext>();
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(a => a.HttpContext).Returns(context.Object);
            var roleManager = MockHelpers.MockRoleManager<TestRole>();
            var identityOptions = new IdentityOptions();
            var options = new Mock<IOptions<IdentityOptions>>();
            options.Setup(a => a.Options).Returns(identityOptions);
            var claimsFactory = new Mock<UserClaimsPrincipalFactory<TestUser, TestRole>>(manager.Object, roleManager.Object, options.Object);
            var helper = new SignInManager<TestUser>(manager.Object, contextAccessor.Object, claimsFactory.Object, options.Object);

            // Act
            var result = await helper.PasswordSignInAsync("bogus", "bogus", false, false);

            // Assert
            Assert.False(result.Succeeded);
            manager.Verify();
            context.Verify();
            contextAccessor.Verify();
        }

        [Fact]
        public async Task PasswordSignInFailsWithWrongPasswordCanAccessFailedAndLockout()
        {
            // Setup
            var user = new TestUser { UserName = "Foo" };
            var manager = SetupUserManager(user);
            var lockedout = false;
            manager.Setup(m => m.AccessFailedAsync(user)).Returns(() =>
            {
                lockedout = true;
                return Task.FromResult(IdentityResult.Success);
            }).Verifiable();
            manager.Setup(m => m.SupportsUserLockout).Returns(true).Verifiable();
            manager.Setup(m => m.IsLockedOutAsync(user)).Returns(() => Task.FromResult(lockedout));
            manager.Setup(m => m.CheckPasswordAsync(user, "bogus")).ReturnsAsync(false).Verifiable();
            var context = new Mock<HttpContext>();
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(a => a.HttpContext).Returns(context.Object);
            var roleManager = MockHelpers.MockRoleManager<TestRole>();
            var identityOptions = new IdentityOptions();
            var options = new Mock<IOptions<IdentityOptions>>();
            options.Setup(a => a.Options).Returns(identityOptions);
            var claimsFactory = new Mock<UserClaimsPrincipalFactory<TestUser, TestRole>>(manager.Object, roleManager.Object, options.Object);
            var helper = new SignInManager<TestUser>(manager.Object, contextAccessor.Object, claimsFactory.Object, options.Object);

            // Act
            var result = await helper.PasswordSignInAsync(user.UserName, "bogus", false, true);

            // Assert
            Assert.False(result.Succeeded);
            Assert.True(result.IsLockedOut);
            manager.Verify();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CanRequireConfirmedEmailForPasswordSignIn(bool confirmed)
        {
            // Setup
            var user = new TestUser { UserName = "Foo" };
            var manager = SetupUserManager(user);
            manager.Setup(m => m.IsEmailConfirmedAsync(user)).ReturnsAsync(confirmed).Verifiable();
            if (confirmed)
            {
                manager.Setup(m => m.CheckPasswordAsync(user, "password")).ReturnsAsync(true).Verifiable();
            }
            var context = new Mock<HttpContext>();
            var response = new Mock<HttpResponse>();
            if (confirmed)
            {
                manager.Setup(m => m.CheckPasswordAsync(user, "password")).ReturnsAsync(true).Verifiable();
                context.Setup(c => c.Response).Returns(response.Object).Verifiable();
                SetupSignIn(response);
            }
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(a => a.HttpContext).Returns(context.Object);
            var roleManager = MockHelpers.MockRoleManager<TestRole>();
            var identityOptions = new IdentityOptions();
            identityOptions.SignIn.RequireConfirmedEmail = true;
            var options = new Mock<IOptions<IdentityOptions>>();
            options.Setup(a => a.Options).Returns(identityOptions);
            var claimsFactory = new Mock<UserClaimsPrincipalFactory<TestUser, TestRole>>(manager.Object, roleManager.Object, options.Object);
            var logStore = new StringBuilder();
            var logger = MockHelpers.MockILogger<SignInManager<TestUser>>(logStore);
            var helper = new SignInManager<TestUser>(manager.Object, contextAccessor.Object, claimsFactory.Object, options.Object);
            helper.Logger = logger.Object;
            string expectedScope = string.Format("{0} for {1}: {2}", "PasswordSignInAsync", "user", user.Id);
            string expectedLog = string.Format("{0} : {1}", "CanSignInAsync", confirmed.ToString());

            // Act
            var result = await helper.PasswordSignInAsync(user, "password", false, false);

            // Assert

            Assert.Equal(confirmed, result.Succeeded);
            Assert.NotEqual(confirmed, result.IsNotAllowed);
            Assert.NotEqual(-1, logStore.ToString().IndexOf(expectedLog));
            Assert.NotEqual(-1, logStore.ToString().IndexOf(expectedScope));

            manager.Verify();
            context.Verify();
            response.Verify();
            contextAccessor.Verify();
        }

        private static void SetupSignIn(Mock<HttpResponse> response, string userId = null, bool? isPersistent = null, string loginProvider = null)
        {
            response.Setup(r => r.SignIn(IdentityOptions.ApplicationCookieAuthenticationScheme,
                It.Is<ClaimsPrincipal>(id =>
                    (userId == null || id.FindFirstValue(ClaimTypes.NameIdentifier) == userId) &&
                    (loginProvider == null || id.FindFirstValue(ClaimTypes.AuthenticationMethod) == loginProvider)),
                It.Is<AuthenticationProperties>(v => isPersistent == null || v.IsPersistent == isPersistent))).Verifiable();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CanRequireConfirmedPhoneNumberForPasswordSignIn(bool confirmed)
        {
            // Setup
            var user = new TestUser { UserName = "Foo" };
            var manager = SetupUserManager(user);
            manager.Setup(m => m.IsPhoneNumberConfirmedAsync(user)).ReturnsAsync(confirmed).Verifiable();
            var context = new Mock<HttpContext>();
            var response = new Mock<HttpResponse>();
            if (confirmed)
            {
                manager.Setup(m => m.CheckPasswordAsync(user, "password")).ReturnsAsync(true).Verifiable();
                context.Setup(c => c.Response).Returns(response.Object).Verifiable();
                SetupSignIn(response);
            }

            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(a => a.HttpContext).Returns(context.Object);
            var roleManager = MockHelpers.MockRoleManager<TestRole>();
            var identityOptions = new IdentityOptions();
            identityOptions.SignIn.RequireConfirmedPhoneNumber = true;
            var options = new Mock<IOptions<IdentityOptions>>();
            options.Setup(a => a.Options).Returns(identityOptions);
            var claimsFactory = new Mock<UserClaimsPrincipalFactory<TestUser, TestRole>>(manager.Object, roleManager.Object, options.Object);
            var logStore = new StringBuilder();
            var logger = MockHelpers.MockILogger<SignInManager<TestUser>>(logStore);
            var helper = new SignInManager<TestUser>(manager.Object, contextAccessor.Object, claimsFactory.Object, options.Object, null);
            helper.Logger = logger.Object;
            string expectedScope = string.Format("{0} for {1}: {2}", "PasswordSignInAsync", "user", user.Id);
            string expectedLog = string.Format("{0} : {1}", "CanSignInAsync", confirmed.ToString());

            // Act
            var result = await helper.PasswordSignInAsync(user, "password", false, false);

            // Assert
            Assert.Equal(confirmed, result.Succeeded);
            Assert.NotEqual(confirmed, result.IsNotAllowed);
            Assert.NotEqual(-1, logStore.ToString().IndexOf(expectedLog));
            Assert.NotEqual(-1, logStore.ToString().IndexOf(expectedScope));
            manager.Verify();
            context.Verify();
            response.Verify();
            contextAccessor.Verify();
        }
    }
}
