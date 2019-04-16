// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Identity.Test
{
    public class SecurityStampTest
    {
        private class NoopHandler : IAuthenticationHandler
        {
            public Task<AuthenticateResult> AuthenticateAsync()
            {
                throw new NotImplementedException();
            }

            public Task ChallengeAsync(AuthenticationProperties properties)
            {
                throw new NotImplementedException();
            }

            public Task ForbidAsync(AuthenticationProperties properties)
            {
                throw new NotImplementedException();
            }

            public Task<bool> HandleRequestAsync()
            {
                throw new NotImplementedException();
            }

            public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
            {
                throw new NotImplementedException();
            }

            public Task SignInAsync(ClaimsPrincipal user, AuthenticationProperties properties)
            {
                throw new NotImplementedException();
            }

            public Task SignOutAsync(AuthenticationProperties properties)
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public async Task OnValidatePrincipalThrowsWithEmptyServiceCollection()
        {
            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(c => c.RequestServices).Returns(new ServiceCollection().BuildServiceProvider());
            var id = new ClaimsPrincipal(new ClaimsIdentity(IdentityConstants.ApplicationScheme));
            var ticket = new AuthenticationTicket(id, new AuthenticationProperties { IssuedUtc = DateTimeOffset.UtcNow }, IdentityConstants.ApplicationScheme);
            var context = new CookieValidatePrincipalContext(httpContext.Object, new AuthenticationSchemeBuilder(IdentityConstants.ApplicationScheme) { HandlerType = typeof(NoopHandler) }.Build(), new CookieAuthenticationOptions(), ticket);
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => SecurityStampValidator.ValidatePrincipalAsync(context));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task OnValidatePrincipalTestSuccess(bool isPersistent)
        {
            var user = new PocoUser("test");
            var httpContext = new Mock<HttpContext>();

            await RunApplicationCookieTest(user, httpContext, /*shouldStampValidate*/true, async () =>
            {
                var id = new ClaimsIdentity(IdentityConstants.ApplicationScheme);
                id.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));
                var principal = new ClaimsPrincipal(id);
                var properties = new AuthenticationProperties { IssuedUtc = DateTimeOffset.UtcNow.AddSeconds(-1), IsPersistent = isPersistent };
                var ticket = new AuthenticationTicket(principal,
                    properties,
                    IdentityConstants.ApplicationScheme);

                var context = new CookieValidatePrincipalContext(httpContext.Object, new AuthenticationSchemeBuilder(IdentityConstants.ApplicationScheme) { HandlerType = typeof(NoopHandler) }.Build(), new CookieAuthenticationOptions(), ticket);
                Assert.NotNull(context.Properties);
                Assert.NotNull(context.Options);
                Assert.NotNull(context.Principal);
                await SecurityStampValidator.ValidatePrincipalAsync(context);
                Assert.NotNull(context.Principal);
            });
        }

        private async Task RunApplicationCookieTest(PocoUser user, Mock<HttpContext> httpContext, bool shouldStampValidate, Func<Task> testCode)
        {
            var userManager = MockHelpers.MockUserManager<PocoUser>();
            var claimsManager = new Mock<IUserClaimsPrincipalFactory<PocoUser>>();
            var identityOptions = new Mock<IOptions<IdentityOptions>>();
            identityOptions.Setup(a => a.Value).Returns(new IdentityOptions());
            var options = new Mock<IOptions<SecurityStampValidatorOptions>>();
            options.Setup(a => a.Value).Returns(new SecurityStampValidatorOptions { ValidationInterval = TimeSpan.Zero });
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(a => a.HttpContext).Returns(httpContext.Object);
            var signInManager = new Mock<SignInManager<PocoUser>>(userManager.Object,
                contextAccessor.Object, claimsManager.Object, identityOptions.Object, null, new Mock<IAuthenticationSchemeProvider>().Object);
            signInManager.Setup(s => s.ValidateSecurityStampAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(shouldStampValidate ? user : default).Verifiable();

            if (shouldStampValidate)
            {
                var id = new ClaimsIdentity(IdentityConstants.ApplicationScheme);
                id.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));
                var principal = new ClaimsPrincipal(id);
                signInManager.Setup(s => s.CreateUserPrincipalAsync(user)).ReturnsAsync(principal).Verifiable();
            }

            var services = new ServiceCollection();
            services.AddSingleton(options.Object);
            services.AddSingleton(signInManager.Object);
            services.AddSingleton<ISecurityStampValidator>(new SecurityStampValidator<PocoUser>(options.Object, signInManager.Object, new SystemClock()));
            httpContext.Setup(c => c.RequestServices).Returns(services.BuildServiceProvider());

            await testCode.Invoke();
            signInManager.VerifyAll();
        }

        [Fact]
        public async Task OnValidateIdentityRejectsWhenValidateSecurityStampFails()
        {
            var user = new PocoUser("test");
            var httpContext = new Mock<HttpContext>();

            await RunApplicationCookieTest(user, httpContext, /*shouldStampValidate*/false, async () =>
            {
                var id = new ClaimsIdentity(IdentityConstants.ApplicationScheme);
                id.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));
                var ticket = new AuthenticationTicket(new ClaimsPrincipal(id),
                    new AuthenticationProperties { IssuedUtc = DateTimeOffset.UtcNow.AddSeconds(-1) },
                    IdentityConstants.ApplicationScheme);

                var context = new CookieValidatePrincipalContext(httpContext.Object, new AuthenticationSchemeBuilder(IdentityConstants.ApplicationScheme) { HandlerType = typeof(NoopHandler) }.Build(), new CookieAuthenticationOptions(), ticket);
                Assert.NotNull(context.Properties);
                Assert.NotNull(context.Options);
                Assert.NotNull(context.Principal);
                await SecurityStampValidator.ValidatePrincipalAsync(context);
                Assert.Null(context.Principal);
            });
        }

        [Fact]
        public async Task OnValidateIdentityAcceptsWhenStoreDoesNotSupportSecurityStamp()
        {
            var user = new PocoUser("test");
            var httpContext = new Mock<HttpContext>();


            var userManager = MockHelpers.MockUserManager<PocoUser>();

            var claimsManager = new Mock<IUserClaimsPrincipalFactory<PocoUser>>();
            var identityOptions = new Mock<IOptions<IdentityOptions>>();
            identityOptions.Setup(a => a.Value).Returns(new IdentityOptions());
            var options = new Mock<IOptions<SecurityStampValidatorOptions>>();
            options.Setup(a => a.Value).Returns(new SecurityStampValidatorOptions { ValidationInterval = TimeSpan.Zero });
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(a => a.HttpContext).Returns(httpContext.Object);
            var signInManager = new SignInManager<PocoUser>(userManager.Object,
                contextAccessor.Object, claimsManager.Object, identityOptions.Object, null, new Mock<IAuthenticationSchemeProvider>().Object);
            userManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user).Verifiable();
            claimsManager.Setup(c => c.CreateAsync(user)).ReturnsAsync(new ClaimsPrincipal()).Verifiable();

            var services = new ServiceCollection();
            services.AddSingleton(options.Object);
            services.AddSingleton(signInManager);
            services.AddSingleton<ISecurityStampValidator>(new SecurityStampValidator<PocoUser>(options.Object, signInManager, new SystemClock()));
            httpContext.Setup(c => c.RequestServices).Returns(services.BuildServiceProvider());

            var tid = new ClaimsIdentity(IdentityConstants.ApplicationScheme);
            tid.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(tid),
                new AuthenticationProperties { IssuedUtc = DateTimeOffset.UtcNow.AddSeconds(-1) },
                IdentityConstants.ApplicationScheme);

            var context = new CookieValidatePrincipalContext(httpContext.Object, new AuthenticationSchemeBuilder(IdentityConstants.ApplicationScheme) { HandlerType = typeof(NoopHandler) }.Build(), new CookieAuthenticationOptions(), ticket);
            Assert.NotNull(context.Properties);
            Assert.NotNull(context.Options);
            Assert.NotNull(context.Principal);
            await SecurityStampValidator.ValidatePrincipalAsync(context);
            Assert.NotNull(context.Principal);

            userManager.VerifyAll();
            claimsManager.VerifyAll();
        }

        [Fact]
        public async Task OnValidateIdentityRejectsWhenNoIssuedUtc()
        {
            var user = new PocoUser("test");
            var httpContext = new Mock<HttpContext>();
            var userManager = MockHelpers.MockUserManager<PocoUser>();
            var identityOptions = new Mock<IOptions<IdentityOptions>>();
            identityOptions.Setup(a => a.Value).Returns(new IdentityOptions());
            var claimsManager = new Mock<IUserClaimsPrincipalFactory<PocoUser>>();
            var options = new Mock<IOptions<SecurityStampValidatorOptions>>();
            options.Setup(a => a.Value).Returns(new SecurityStampValidatorOptions { ValidationInterval = TimeSpan.Zero });
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(a => a.HttpContext).Returns(httpContext.Object);
            var signInManager = new Mock<SignInManager<PocoUser>>(userManager.Object,
                contextAccessor.Object, claimsManager.Object, identityOptions.Object, null, new Mock<IAuthenticationSchemeProvider>().Object);
            signInManager.Setup(s => s.ValidateSecurityStampAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(default(PocoUser)).Verifiable();
            var services = new ServiceCollection();
            services.AddSingleton(options.Object);
            services.AddSingleton(signInManager.Object);
            services.AddSingleton<ISecurityStampValidator>(new SecurityStampValidator<PocoUser>(options.Object, signInManager.Object, new SystemClock()));
            httpContext.Setup(c => c.RequestServices).Returns(services.BuildServiceProvider());
            var id = new ClaimsIdentity(IdentityConstants.ApplicationScheme);
            id.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));

            var ticket = new AuthenticationTicket(new ClaimsPrincipal(id),
                new AuthenticationProperties(),
                IdentityConstants.ApplicationScheme);
            var context = new CookieValidatePrincipalContext(httpContext.Object, new AuthenticationSchemeBuilder(IdentityConstants.ApplicationScheme) { HandlerType = typeof(NoopHandler) }.Build(), new CookieAuthenticationOptions(), ticket);
            Assert.NotNull(context.Properties);
            Assert.NotNull(context.Options);
            Assert.NotNull(context.Principal);
            await SecurityStampValidator.ValidatePrincipalAsync(context);
            Assert.Null(context.Principal);
            signInManager.VerifyAll();
        }

        [Fact]
        public async Task OnValidateIdentityDoesNotRejectsWhenNotExpired()
        {
            var user = new PocoUser("test");
            var httpContext = new Mock<HttpContext>();
            var userManager = MockHelpers.MockUserManager<PocoUser>();
            var identityOptions = new Mock<IOptions<IdentityOptions>>();
            identityOptions.Setup(a => a.Value).Returns(new IdentityOptions());
            var claimsManager = new Mock<IUserClaimsPrincipalFactory<PocoUser>>();
            var options = new Mock<IOptions<SecurityStampValidatorOptions>>();
            options.Setup(a => a.Value).Returns(new SecurityStampValidatorOptions { ValidationInterval = TimeSpan.FromDays(1) });
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(a => a.HttpContext).Returns(httpContext.Object);
            var signInManager = new Mock<SignInManager<PocoUser>>(userManager.Object,
                contextAccessor.Object, claimsManager.Object, identityOptions.Object, null, new Mock<IAuthenticationSchemeProvider>().Object);
            signInManager.Setup(s => s.ValidateSecurityStampAsync(It.IsAny<ClaimsPrincipal>())).Throws(new Exception("Shouldn't be called"));
            signInManager.Setup(s => s.SignInAsync(user, false, null)).Throws(new Exception("Shouldn't be called"));
            var services = new ServiceCollection();
            services.AddSingleton(options.Object);
            services.AddSingleton(signInManager.Object);
            services.AddSingleton<ISecurityStampValidator>(new SecurityStampValidator<PocoUser>(options.Object, signInManager.Object, new SystemClock()));
            httpContext.Setup(c => c.RequestServices).Returns(services.BuildServiceProvider());
            var id = new ClaimsIdentity(IdentityConstants.ApplicationScheme);
            id.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));

            var ticket = new AuthenticationTicket(new ClaimsPrincipal(id),
                new AuthenticationProperties { IssuedUtc = DateTimeOffset.UtcNow },
                IdentityConstants.ApplicationScheme);
            var context = new CookieValidatePrincipalContext(httpContext.Object, new AuthenticationSchemeBuilder(IdentityConstants.ApplicationScheme) { HandlerType = typeof(NoopHandler) }.Build(), new CookieAuthenticationOptions(), ticket);
            Assert.NotNull(context.Properties);
            Assert.NotNull(context.Options);
            Assert.NotNull(context.Principal);
            await SecurityStampValidator.ValidatePrincipalAsync(context);
            Assert.NotNull(context.Principal);
        }

        private async Task RunRememberClientCookieTest(bool shouldStampValidate, bool validationSuccess)
        {
            var user = new PocoUser("test");
            var httpContext = new Mock<HttpContext>();
            var userManager = MockHelpers.MockUserManager<PocoUser>();
            userManager.Setup(u => u.GetUserIdAsync(user)).ReturnsAsync(user.Id).Verifiable();
            var claimsManager = new Mock<IUserClaimsPrincipalFactory<PocoUser>>();
            var identityOptions = new Mock<IOptions<IdentityOptions>>();
            identityOptions.Setup(a => a.Value).Returns(new IdentityOptions());
            var options = new Mock<IOptions<SecurityStampValidatorOptions>>();
            options.Setup(a => a.Value).Returns(new SecurityStampValidatorOptions { ValidationInterval = TimeSpan.Zero });
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(a => a.HttpContext).Returns(httpContext.Object);
            var signInManager = new Mock<SignInManager<PocoUser>>(userManager.Object,
                contextAccessor.Object, claimsManager.Object, identityOptions.Object, null, new Mock<IAuthenticationSchemeProvider>().Object);
            signInManager.Setup(s => s.ValidateTwoFactorSecurityStampAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(shouldStampValidate ? user : default).Verifiable();

            var services = new ServiceCollection();
            services.AddSingleton(options.Object);
            services.AddSingleton(signInManager.Object);
            services.AddSingleton<ITwoFactorSecurityStampValidator>(new TwoFactorSecurityStampValidator<PocoUser>(options.Object, signInManager.Object, new SystemClock()));
            httpContext.Setup(c => c.RequestServices).Returns(services.BuildServiceProvider());

            var principal = await signInManager.Object.StoreRememberClient(user);
            var ticket = new AuthenticationTicket(principal,
                new AuthenticationProperties { IsPersistent = true },
                IdentityConstants.TwoFactorRememberMeScheme);
            var context = new CookieValidatePrincipalContext(httpContext.Object, new AuthenticationSchemeBuilder(IdentityConstants.ApplicationScheme) { HandlerType = typeof(NoopHandler) }.Build(), new CookieAuthenticationOptions(), ticket);
            Assert.NotNull(context.Properties);
            Assert.NotNull(context.Options);
            Assert.NotNull(context.Principal);
            await SecurityStampValidator.ValidateAsync<ITwoFactorSecurityStampValidator>(context);
            Assert.Equal(validationSuccess, context.Principal != null);

            signInManager.VerifyAll();
            userManager.VerifyAll();
        }

        [Fact]
        public Task TwoFactorRememberClientOnValidatePrincipalTestSuccess()
            => RunRememberClientCookieTest(shouldStampValidate: true, validationSuccess: true);

        [Fact]
        public Task TwoFactorRememberClientOnValidatePrincipalRejectsWhenValidateSecurityStampFails()
            => RunRememberClientCookieTest(shouldStampValidate: false, validationSuccess: false);
    }
}