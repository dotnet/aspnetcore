// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
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
            var user = new TestUser("test");
            var userManager = MockHelpers.MockUserManager<TestUser>();
            var claimsManager = new Mock<IUserClaimsPrincipalFactory<TestUser>>();
            var identityOptions = new Mock<IOptions<IdentityOptions>>();
            identityOptions.Setup(a => a.Value).Returns(new IdentityOptions());
            var options = new Mock<IOptions<SecurityStampValidatorOptions>>();
            options.Setup(a => a.Value).Returns(new SecurityStampValidatorOptions { ValidationInterval = TimeSpan.Zero });
            var httpContext = new Mock<HttpContext>();
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(a => a.HttpContext).Returns(httpContext.Object);
            var id = new ClaimsIdentity(IdentityConstants.ApplicationScheme);
            id.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));
            var principal = new ClaimsPrincipal(id);

            var properties = new AuthenticationProperties { IssuedUtc = DateTimeOffset.UtcNow.AddSeconds(-1), IsPersistent = isPersistent };
            var signInManager = new Mock<SignInManager<TestUser>>(userManager.Object,
                contextAccessor.Object, claimsManager.Object, identityOptions.Object, null, new Mock<IAuthenticationSchemeProvider>().Object);
            signInManager.Setup(s => s.ValidateSecurityStampAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user).Verifiable();
            signInManager.Setup(s => s.CreateUserPrincipalAsync(user)).ReturnsAsync(principal).Verifiable();
            var services = new ServiceCollection();
            services.AddSingleton(options.Object);
            services.AddSingleton(signInManager.Object);
            services.AddSingleton<ISecurityStampValidator>(new SecurityStampValidator<TestUser>(options.Object, signInManager.Object, new SystemClock()));
            httpContext.Setup(c => c.RequestServices).Returns(services.BuildServiceProvider());

            var ticket = new AuthenticationTicket(principal, 
                properties, 
                IdentityConstants.ApplicationScheme);
            var context = new CookieValidatePrincipalContext(httpContext.Object, new AuthenticationSchemeBuilder(IdentityConstants.ApplicationScheme) { HandlerType = typeof(NoopHandler) }.Build(), new CookieAuthenticationOptions(), ticket);
            Assert.NotNull(context.Properties);
            Assert.NotNull(context.Options);
            Assert.NotNull(context.Principal);
            await
                SecurityStampValidator.ValidatePrincipalAsync(context);
            Assert.NotNull(context.Principal);
            signInManager.VerifyAll();
        }

        [Fact]
        public async Task OnValidateIdentityRejectsWhenValidateSecurityStampFails()
        {
            var user = new TestUser("test");
            var userManager = MockHelpers.MockUserManager<TestUser>();
            var claimsManager = new Mock<IUserClaimsPrincipalFactory<TestUser>>();
            var identityOptions = new Mock<IOptions<IdentityOptions>>();
            identityOptions.Setup(a => a.Value).Returns(new IdentityOptions());
            var options = new Mock<IOptions<SecurityStampValidatorOptions>>();
            options.Setup(a => a.Value).Returns(new SecurityStampValidatorOptions { ValidationInterval = TimeSpan.Zero });
            var httpContext = new Mock<HttpContext>();
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(a => a.HttpContext).Returns(httpContext.Object);
            var signInManager = new Mock<SignInManager<TestUser>>(userManager.Object,
                contextAccessor.Object, claimsManager.Object, identityOptions.Object, null, new Mock<IAuthenticationSchemeProvider>().Object);
            signInManager.Setup(s => s.ValidateSecurityStampAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(default(TestUser)).Verifiable();
            var services = new ServiceCollection();
            services.AddSingleton(options.Object);
            services.AddSingleton(signInManager.Object);
            services.AddSingleton<ISecurityStampValidator>(new SecurityStampValidator<TestUser>(options.Object, signInManager.Object, new SystemClock()));
            httpContext.Setup(c => c.RequestServices).Returns(services.BuildServiceProvider());
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
            signInManager.VerifyAll();
        }

        [Fact]
        public async Task OnValidateIdentityRejectsWhenNoIssuedUtc()
        {
            var user = new TestUser("test");
            var httpContext = new Mock<HttpContext>();
            var userManager = MockHelpers.MockUserManager<TestUser>();
            var identityOptions = new Mock<IOptions<IdentityOptions>>();
            identityOptions.Setup(a => a.Value).Returns(new IdentityOptions());
            var claimsManager = new Mock<IUserClaimsPrincipalFactory<TestUser>>();
            var options = new Mock<IOptions<SecurityStampValidatorOptions>>();
            options.Setup(a => a.Value).Returns(new SecurityStampValidatorOptions { ValidationInterval = TimeSpan.Zero });
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(a => a.HttpContext).Returns(httpContext.Object);
            var signInManager = new Mock<SignInManager<TestUser>>(userManager.Object,
                contextAccessor.Object, claimsManager.Object, identityOptions.Object, null, new Mock<IAuthenticationSchemeProvider>().Object);
            signInManager.Setup(s => s.ValidateSecurityStampAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(default(TestUser)).Verifiable();
            var services = new ServiceCollection();
            services.AddSingleton(options.Object);
            services.AddSingleton(signInManager.Object);
            services.AddSingleton<ISecurityStampValidator>(new SecurityStampValidator<TestUser>(options.Object, signInManager.Object, new SystemClock()));
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
            var user = new TestUser("test");
            var httpContext = new Mock<HttpContext>();
            var userManager = MockHelpers.MockUserManager<TestUser>();
            var identityOptions = new Mock<IOptions<IdentityOptions>>();
            identityOptions.Setup(a => a.Value).Returns(new IdentityOptions());
            var claimsManager = new Mock<IUserClaimsPrincipalFactory<TestUser>>();
            var options = new Mock<IOptions<SecurityStampValidatorOptions>>();
            options.Setup(a => a.Value).Returns(new SecurityStampValidatorOptions { ValidationInterval = TimeSpan.FromDays(1) });
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(a => a.HttpContext).Returns(httpContext.Object);
            var signInManager = new Mock<SignInManager<TestUser>>(userManager.Object,
                contextAccessor.Object, claimsManager.Object, identityOptions.Object, null, new Mock<IAuthenticationSchemeProvider>().Object);
            signInManager.Setup(s => s.ValidateSecurityStampAsync(It.IsAny<ClaimsPrincipal>())).Throws(new Exception("Shouldn't be called"));
            signInManager.Setup(s => s.SignInAsync(user, false, null)).Throws(new Exception("Shouldn't be called"));
            var services = new ServiceCollection();
            services.AddSingleton(options.Object);
            services.AddSingleton(signInManager.Object);
            services.AddSingleton<ISecurityStampValidator>(new SecurityStampValidator<TestUser>(options.Object, signInManager.Object, new SystemClock()));
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
    }
}