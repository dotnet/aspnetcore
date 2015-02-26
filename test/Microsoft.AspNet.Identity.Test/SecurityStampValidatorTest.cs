// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Authentication;
using Microsoft.AspNet.Authentication.Cookies;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Identity.Test
{
    public class SecurityStampTest
    {
        [Fact]
        public async Task OnValidatePrincipalThrowsWithEmptyServiceCollection()
        {
            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(c => c.RequestServices).Returns(new ServiceCollection().BuildServiceProvider());
            var id = new ClaimsPrincipal(new ClaimsIdentity(IdentityOptions.ApplicationCookieAuthenticationScheme));
            var ticket = new AuthenticationTicket(id, new AuthenticationProperties { IssuedUtc = DateTimeOffset.UtcNow }, IdentityOptions.ApplicationCookieAuthenticationScheme);
            var context = new CookieValidatePrincipalContext(httpContext.Object, ticket, new CookieAuthenticationOptions());
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => SecurityStampValidator.ValidatePrincipalAsync(context));
            Assert.True(ex.Message.Contains("No service for type 'Microsoft.Framework.OptionsModel.IOptions"));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task OnValidatePrincipalTestSuccess(bool isPersistent)
        {
            var user = new TestUser("test");
            var userManager = MockHelpers.MockUserManager<TestUser>();
            var claimsManager = new Mock<IUserClaimsPrincipalFactory<TestUser>>();
            var identityOptions = new IdentityOptions { SecurityStampValidationInterval = TimeSpan.Zero };
            var options = new Mock<IOptions<IdentityOptions>>();
            options.Setup(a => a.Options).Returns(identityOptions);
            var httpContext = new Mock<HttpContext>();
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(a => a.HttpContext).Returns(httpContext.Object);
            var signInManager = new Mock<SignInManager<TestUser>>(userManager.Object,
                contextAccessor.Object, claimsManager.Object, options.Object, null);
            signInManager.Setup(s => s.ValidateSecurityStampAsync(It.IsAny<ClaimsPrincipal>(), user.Id)).ReturnsAsync(user).Verifiable();
            signInManager.Setup(s => s.SignInAsync(user, isPersistent, null)).Returns(Task.FromResult(0)).Verifiable();
            var services = new ServiceCollection();
            services.AddInstance(options.Object);
            services.AddInstance(signInManager.Object);
            services.AddInstance<ISecurityStampValidator>(new SecurityStampValidator<TestUser>());
            httpContext.Setup(c => c.RequestServices).Returns(services.BuildServiceProvider());
            var id = new ClaimsIdentity(IdentityOptions.ApplicationCookieAuthenticationScheme);
            id.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));

            var ticket = new AuthenticationTicket(new ClaimsPrincipal(id), 
                new AuthenticationProperties { IssuedUtc = DateTimeOffset.UtcNow, IsPersistent = isPersistent }, 
                IdentityOptions.ApplicationCookieAuthenticationScheme);
            var context = new CookieValidatePrincipalContext(httpContext.Object, ticket, new CookieAuthenticationOptions());
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
            var identityOptions = new IdentityOptions { SecurityStampValidationInterval = TimeSpan.Zero };
            var options = new Mock<IOptions<IdentityOptions>>();
            options.Setup(a => a.Options).Returns(identityOptions);
            var httpContext = new Mock<HttpContext>();
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(a => a.HttpContext).Returns(httpContext.Object);
            var signInManager = new Mock<SignInManager<TestUser>>(userManager.Object,
                contextAccessor.Object, claimsManager.Object, options.Object, null);
            signInManager.Setup(s => s.ValidateSecurityStampAsync(It.IsAny<ClaimsPrincipal>(), user.Id)).ReturnsAsync(null).Verifiable();
            var services = new ServiceCollection();
            services.AddInstance(options.Object);
            services.AddInstance(signInManager.Object);
            services.AddInstance<ISecurityStampValidator>(new SecurityStampValidator<TestUser>());
            httpContext.Setup(c => c.RequestServices).Returns(services.BuildServiceProvider());
            var id = new ClaimsIdentity(IdentityOptions.ApplicationCookieAuthenticationScheme);
            id.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));

            var ticket = new AuthenticationTicket(new ClaimsPrincipal(id),
                new AuthenticationProperties { IssuedUtc = DateTimeOffset.UtcNow },
                IdentityOptions.ApplicationCookieAuthenticationScheme);
            var context = new CookieValidatePrincipalContext(httpContext.Object, ticket, new CookieAuthenticationOptions());
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
            var claimsManager = new Mock<IUserClaimsPrincipalFactory<TestUser>>();
            var identityOptions = new IdentityOptions { SecurityStampValidationInterval = TimeSpan.Zero };
            var options = new Mock<IOptions<IdentityOptions>>();
            options.Setup(a => a.Options).Returns(identityOptions);
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(a => a.HttpContext).Returns(httpContext.Object);
            var signInManager = new Mock<SignInManager<TestUser>>(userManager.Object,
                contextAccessor.Object, claimsManager.Object, options.Object, null);
            signInManager.Setup(s => s.ValidateSecurityStampAsync(It.IsAny<ClaimsPrincipal>(), user.Id)).ReturnsAsync(null).Verifiable();
            var services = new ServiceCollection();
            services.AddInstance(options.Object);
            services.AddInstance(signInManager.Object);
            services.AddInstance<ISecurityStampValidator>(new SecurityStampValidator<TestUser>());
            httpContext.Setup(c => c.RequestServices).Returns(services.BuildServiceProvider());
            var id = new ClaimsIdentity(IdentityOptions.ApplicationCookieAuthenticationScheme);
            id.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));

            var ticket = new AuthenticationTicket(new ClaimsPrincipal(id),
                new AuthenticationProperties(),
                IdentityOptions.ApplicationCookieAuthenticationScheme);
            var context = new CookieValidatePrincipalContext(httpContext.Object, ticket, new CookieAuthenticationOptions());
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
            var claimsManager = new Mock<IUserClaimsPrincipalFactory<TestUser>>();
            var identityOptions = new IdentityOptions { SecurityStampValidationInterval = TimeSpan.FromDays(1) };
            var options = new Mock<IOptions<IdentityOptions>>();
            options.Setup(a => a.Options).Returns(identityOptions);
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(a => a.HttpContext).Returns(httpContext.Object);
            var signInManager = new Mock<SignInManager<TestUser>>(userManager.Object,
                contextAccessor.Object, claimsManager.Object, options.Object, null);
            signInManager.Setup(s => s.ValidateSecurityStampAsync(It.IsAny<ClaimsPrincipal>(), user.Id)).Throws(new Exception("Shouldn't be called"));
            signInManager.Setup(s => s.SignInAsync(user, false, null)).Throws(new Exception("Shouldn't be called"));
            var services = new ServiceCollection();
            services.AddInstance(options.Object);
            services.AddInstance(signInManager.Object);
            services.AddInstance<ISecurityStampValidator>(new SecurityStampValidator<TestUser>());
            httpContext.Setup(c => c.RequestServices).Returns(services.BuildServiceProvider());
            var id = new ClaimsIdentity(IdentityOptions.ApplicationCookieAuthenticationScheme);
            id.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));

            var ticket = new AuthenticationTicket(new ClaimsPrincipal(id),
                new AuthenticationProperties { IssuedUtc = DateTimeOffset.UtcNow },
                IdentityOptions.ApplicationCookieAuthenticationScheme);
            var context = new CookieValidatePrincipalContext(httpContext.Object, ticket, new CookieAuthenticationOptions());
            Assert.NotNull(context.Properties);
            Assert.NotNull(context.Options);
            Assert.NotNull(context.Principal);
            await SecurityStampValidator.ValidatePrincipalAsync(context);
            Assert.NotNull(context.Principal);
        }
    }
}