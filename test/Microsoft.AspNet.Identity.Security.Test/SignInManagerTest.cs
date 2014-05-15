// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Security;
using Microsoft.AspNet.Identity.InMemory;
using Microsoft.AspNet.Identity.Test;
using Microsoft.AspNet.PipelineCore;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.Security.Cookies;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.Logging;
using Moq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNet.Builder;

namespace Microsoft.AspNet.Identity.Security.Test
{
    public class SignInManagerTest
    {
#if NET45
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task VerifyAccountControllerSignIn(bool isPersistent)
        {
            IBuilder app = new Builder.Builder(new ServiceCollection().BuildServiceProvider());
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie
            });

            var context = new Mock<HttpContext>();
            var response = new Mock<HttpResponse>();
            context.Setup(c => c.Response).Returns(response.Object).Verifiable();
            response.Setup(r => r.SignIn(It.IsAny<ClaimsIdentity>(), It.Is<AuthenticationProperties>(v => v.IsPersistent == isPersistent))).Verifiable();
            var contextAccessor = new Mock<IContextAccessor<HttpContext>>();
            contextAccessor.Setup(a => a.Value).Returns(context.Object);
            app.UseServices(services =>
            {
                services.AddInstance(contextAccessor.Object);
                services.AddInstance<ILoggerFactory>(new NullLoggerFactory());
                services.AddIdentity<ApplicationUser, IdentityRole>(s =>
                {
                    s.AddInMemory();
                }).AddSecurity<ApplicationUser>();
            });

            // Act
            var user = new ApplicationUser
            {
                UserName = "Yolo"
            };
            const string password = "Yol0Sw@g!";
            var userManager = app.ApplicationServices.GetService<UserManager<ApplicationUser>>();
            var signInManager = app.ApplicationServices.GetService<SignInManager<ApplicationUser>>();

            IdentityResultAssert.IsSuccess(await userManager.CreateAsync(user, password));
            var result = await signInManager.PasswordSignInAsync(user.UserName, password, isPersistent, false);

            // Assert
            Assert.Equal(SignInStatus.Success, result);
            context.VerifyAll();
            response.VerifyAll();
            contextAccessor.VerifyAll();
        }

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
        //        services.AddTransient<ApplicationSignInManager>();
        //    });

        //    // Act
        //    var user = new ApplicationUser
        //    {
        //        UserName = "Yolo"
        //    };
        //    const string password = "Yol0Sw@g!";
        //    var userManager = app.ApplicationServices.GetService<ApplicationUserManager>();
        //    var signInManager = app.ApplicationServices.GetService<ApplicationSignInManager>();

        //    IdentityResultAssert.IsSuccess(await userManager.CreateAsync(user, password));
        //    var result = await signInManager.PasswordSignInAsync(user.UserName, password, isPersistent, false);

        //    // Assert
        //    Assert.Equal(SignInStatus.Success, result);
        //    contextAccessor.VerifyAll();
        //}

        [Fact]
        public void ConstructorNullChecks()
        {
            Assert.Throws<ArgumentNullException>("userManager", () => new SignInManager<IdentityUser>(null, null));
            var userManager = MockHelpers.MockUserManager<IdentityUser>().Object;
            Assert.Throws<ArgumentNullException>("contextAccessor", () => new SignInManager<IdentityUser>(userManager, null));
        }

        //TODO: Mock fails in K (this works fine in net45)
        [Fact]
        public async Task EnsureClaimsIdentityFactoryCreateIdentityCalled()
        {
            // Setup
            var user = new TestUser { UserName = "Foo" };
            var userManager = MockHelpers.TestUserManager<TestUser>();
            var identityFactory = new Mock<IClaimsIdentityFactory<TestUser>>();
            const string authType = "Test";
            var testIdentity = new ClaimsIdentity(authType);
            identityFactory.Setup(s => s.CreateAsync(userManager, user, authType, CancellationToken.None)).ReturnsAsync(testIdentity).Verifiable();
            userManager.ClaimsIdentityFactory = identityFactory.Object;
            var context = new Mock<HttpContext>();
            var response = new Mock<HttpResponse>();
            context.Setup(c => c.Response).Returns(response.Object).Verifiable();
            response.Setup(r => r.SignIn(testIdentity, It.IsAny<AuthenticationProperties>())).Verifiable();
            var contextAccessor = new Mock<IContextAccessor<HttpContext>>();
            contextAccessor.Setup(a => a.Value).Returns(context.Object);
            var helper = new SignInManager<TestUser>(userManager, contextAccessor.Object)
            {
                AuthenticationType = authType
            };

            // Act
            await helper.SignInAsync(user, false, false);

            // Assert
            identityFactory.VerifyAll();
            context.VerifyAll();
            contextAccessor.VerifyAll();
            response.VerifyAll();
        }

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
            var helper = new SignInManager<TestUser>(manager.Object, contextAccessor.Object);

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
            manager.Setup(m => m.CreateIdentityAsync(user, DefaultAuthenticationTypes.ApplicationCookie, CancellationToken.None)).ReturnsAsync(new ClaimsIdentity("Microsoft.AspNet.Identity")).Verifiable();
            var context = new Mock<HttpContext>();
            var response = new Mock<HttpResponse>();
            context.Setup(c => c.Response).Returns(response.Object).Verifiable();
            response.Setup(r => r.SignIn(It.IsAny<ClaimsIdentity>(), It.Is<AuthenticationProperties>(v => v.IsPersistent == isPersistent))).Verifiable();
            var contextAccessor = new Mock<IContextAccessor<HttpContext>>();
            contextAccessor.Setup(a => a.Value).Returns(context.Object);
            var helper = new SignInManager<TestUser>(manager.Object, contextAccessor.Object);

            // Act
            var result = await helper.PasswordSignInAsync(user.UserName, "password", isPersistent, false);

            // Assert
            Assert.Equal(SignInStatus.Success, result);
            manager.VerifyAll();
            context.VerifyAll();
            response.VerifyAll();
            contextAccessor.VerifyAll();
        }

        [Theory]
        [InlineData("Microsoft.AspNet.Identity.Security.Application")]
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
            var helper = new SignInManager<TestUser>(manager.Object, contextAccessor.Object)
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
            var helper = new SignInManager<TestUser>(manager.Object, contextAccessor.Object);
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
            var helper = new SignInManager<TestUser>(manager.Object, contextAccessor.Object);

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
            var helper = new SignInManager<TestUser>(manager.Object, contextAccessor.Object);

            // Act
            var result = await helper.PasswordSignInAsync(user.UserName, "bogus", false, true);

            // Assert
            Assert.Equal(SignInStatus.LockedOut, result);
            manager.VerifyAll();
        }
#endif
        public class ApplicationSignInManager : SignInManager<ApplicationUser>
        {
            public ApplicationSignInManager(ApplicationUserManager manager, IContextAccessor<HttpContext> contextAccessor)
                : base(manager, contextAccessor)
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie;
            }
        }

        public class NullLoggerFactory : ILoggerFactory
        {
            public ILogger Create(string name)
            {
                return new NullLogger();
            }
        }

        public class NullLogger : ILogger
        {
            public bool WriteCore(TraceType eventType, int eventId, object state, Exception exception, Func<object, Exception, string> formatter)
            {
                return false;
            }
        }

    }
}