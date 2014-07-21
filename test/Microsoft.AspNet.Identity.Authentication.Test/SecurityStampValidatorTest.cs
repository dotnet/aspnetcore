// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Security;
using Microsoft.AspNet.Identity.Test;
using Microsoft.AspNet.Security;
using Microsoft.AspNet.Security.Cookies;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Identity.Authentication.Test
{
    public class SecurityStampTest
    {
        [Fact]
        public async Task OnValidateIdentityThrowsWithEmptyServiceCollection()
        {
            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(c => c.ApplicationServices).Returns(new ServiceCollection().BuildServiceProvider());
            var id = new ClaimsIdentity(DefaultAuthenticationTypes.ApplicationCookie);
            var ticket = new AuthenticationTicket(id, new AuthenticationProperties { IssuedUtc = DateTimeOffset.UtcNow });
            var context = new CookieValidateIdentityContext(httpContext.Object, ticket, new CookieAuthenticationOptions());
            await Assert.ThrowsAsync<Exception>(() => SecurityStampValidator.OnValidateIdentity<IdentityUser>(TimeSpan.Zero).Invoke(context));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task OnValidateIdentityTestSuccess(bool isPersistent)
        {
            var user = new IdentityUser("test");
            var httpContext = new Mock<HttpContext>();
            var userManager = MockHelpers.MockUserManager<IdentityUser>();
            var authManager = new Mock<IAuthenticationManager>();
            var claimsManager = new Mock<IClaimsIdentityFactory<IdentityUser>>();
            var signInManager = new Mock<SignInManager<IdentityUser>>(userManager.Object,
                authManager.Object, claimsManager.Object);
            signInManager.Setup(s => s.ValidateSecurityStamp(It.IsAny<ClaimsIdentity>(), user.Id)).ReturnsAsync(user).Verifiable();
            signInManager.Setup(s => s.SignInAsync(user, isPersistent)).Returns(Task.FromResult(0)).Verifiable();
            var services = new ServiceCollection();
            services.AddInstance(signInManager.Object);
            httpContext.Setup(c => c.ApplicationServices).Returns(services.BuildServiceProvider());
            var id = new ClaimsIdentity(DefaultAuthenticationTypes.ApplicationCookie);
            id.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));

            var ticket = new AuthenticationTicket(id, new AuthenticationProperties { IssuedUtc = DateTimeOffset.UtcNow, IsPersistent = isPersistent });
            var context = new CookieValidateIdentityContext(httpContext.Object, ticket, new CookieAuthenticationOptions());
            Assert.NotNull(context.Properties);
            Assert.NotNull(context.Options);
            Assert.NotNull(context.Identity);
            await
                SecurityStampValidator.OnValidateIdentity<IdentityUser>(TimeSpan.Zero).Invoke(context);
            Assert.NotNull(context.Identity);
            signInManager.VerifyAll();
        }

        [Fact]
        public async Task OnValidateIdentityRejectsWhenValidateSecurityStampFails()
        {
            var user = new IdentityUser("test");
            var httpContext = new Mock<HttpContext>();
            var userManager = MockHelpers.MockUserManager<IdentityUser>();
            var authManager = new Mock<IAuthenticationManager>();
            var claimsManager = new Mock<IClaimsIdentityFactory<IdentityUser>>();
            var signInManager = new Mock<SignInManager<IdentityUser>>(userManager.Object,
                authManager.Object, claimsManager.Object);
            signInManager.Setup(s => s.ValidateSecurityStamp(It.IsAny<ClaimsIdentity>(), user.Id)).ReturnsAsync(null).Verifiable();
            var services = new ServiceCollection();
            services.AddInstance(signInManager.Object);
            httpContext.Setup(c => c.ApplicationServices).Returns(services.BuildServiceProvider());
            var id = new ClaimsIdentity(DefaultAuthenticationTypes.ApplicationCookie);
            id.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));

            var ticket = new AuthenticationTicket(id, new AuthenticationProperties { IssuedUtc = DateTimeOffset.UtcNow });
            var context = new CookieValidateIdentityContext(httpContext.Object, ticket, new CookieAuthenticationOptions());
            Assert.NotNull(context.Properties);
            Assert.NotNull(context.Options);
            Assert.NotNull(context.Identity);
            await
                SecurityStampValidator.OnValidateIdentity<IdentityUser>(TimeSpan.Zero).Invoke(context);
            Assert.Null(context.Identity);
            signInManager.VerifyAll();
        }

        [Fact]
        public async Task OnValidateIdentityRejectsWhenNoIssuedUtc()
        {
            var user = new IdentityUser("test");
            var httpContext = new Mock<HttpContext>();
            var userManager = MockHelpers.MockUserManager<IdentityUser>();
            var authManager = new Mock<IAuthenticationManager>();
            var claimsManager = new Mock<IClaimsIdentityFactory<IdentityUser>>();
            var signInManager = new Mock<SignInManager<IdentityUser>>(userManager.Object,
                authManager.Object, claimsManager.Object);
            signInManager.Setup(s => s.ValidateSecurityStamp(It.IsAny<ClaimsIdentity>(), user.Id)).ReturnsAsync(null).Verifiable();
            var services = new ServiceCollection();
            services.AddInstance(signInManager.Object);
            httpContext.Setup(c => c.ApplicationServices).Returns(services.BuildServiceProvider());
            var id = new ClaimsIdentity(DefaultAuthenticationTypes.ApplicationCookie);
            id.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));

            var ticket = new AuthenticationTicket(id, new AuthenticationProperties());
            var context = new CookieValidateIdentityContext(httpContext.Object, ticket, new CookieAuthenticationOptions());
            Assert.NotNull(context.Properties);
            Assert.NotNull(context.Options);
            Assert.NotNull(context.Identity);
            await
                SecurityStampValidator.OnValidateIdentity<IdentityUser>(TimeSpan.Zero).Invoke(context);
            Assert.Null(context.Identity);
            signInManager.VerifyAll();
        }

        [Fact]
        public async Task OnValidateIdentityDoesNotRejectsWhenNotExpired()
        {
            var user = new IdentityUser("test");
            var httpContext = new Mock<HttpContext>();
            var userManager = MockHelpers.MockUserManager<IdentityUser>();
            var authManager = new Mock<IAuthenticationManager>();
            var claimsManager = new Mock<IClaimsIdentityFactory<IdentityUser>>();
            var signInManager = new Mock<SignInManager<IdentityUser>>(userManager.Object,
                authManager.Object, claimsManager.Object);
            signInManager.Setup(s => s.ValidateSecurityStamp(It.IsAny<ClaimsIdentity>(), user.Id)).Throws(new Exception("Shouldn't be called"));
            signInManager.Setup(s => s.SignInAsync(user, false)).Throws(new Exception("Shouldn't be called"));
            var services = new ServiceCollection();
            services.AddInstance(signInManager.Object);
            httpContext.Setup(c => c.ApplicationServices).Returns(services.BuildServiceProvider());
            var id = new ClaimsIdentity(DefaultAuthenticationTypes.ApplicationCookie);
            id.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));

            var ticket = new AuthenticationTicket(id, new AuthenticationProperties { IssuedUtc = DateTimeOffset.UtcNow });
            var context = new CookieValidateIdentityContext(httpContext.Object, ticket, new CookieAuthenticationOptions());
            Assert.NotNull(context.Properties);
            Assert.NotNull(context.Options);
            Assert.NotNull(context.Identity);
            await
                SecurityStampValidator.OnValidateIdentity<IdentityUser>(TimeSpan.FromDays(1)).Invoke(context);
            Assert.NotNull(context.Identity);
        }
    }
}