// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Security;
using Microsoft.AspNet.Security;
using Microsoft.AspNet.Security.Cookies;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.OptionsModel;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Identity.Test
{
    public class SecurityStampTest
    {
        [Fact]
        public async Task OnValidateIdentityThrowsWithEmptyServiceCollection()
        {
            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(c => c.RequestServices).Returns(new ServiceCollection().BuildServiceProvider());
            var id = new ClaimsIdentity(IdentityOptions.ApplicationCookieAuthenticationType);
            var ticket = new AuthenticationTicket(id, new AuthenticationProperties { IssuedUtc = DateTimeOffset.UtcNow });
            var context = new CookieValidateIdentityContext(httpContext.Object, ticket, new CookieAuthenticationOptions());
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => SecurityStampValidator.ValidateIdentityAsync(context));
            Assert.True(ex.Message.Contains("No service for type 'Microsoft.Framework.OptionsModel.IOptions"));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task OnValidateIdentityTestSuccess(bool isPersistent)
        {
            var user = new IdentityUser("test");
            var userManager = MockHelpers.MockUserManager<IdentityUser>();
            var claimsManager = new Mock<IClaimsIdentityFactory<IdentityUser>>();
            var identityOptions = new IdentityOptions { SecurityStampValidationInterval = TimeSpan.Zero };
            var options = new Mock<IOptions<IdentityOptions>>();
            options.Setup(a => a.Options).Returns(identityOptions);
            var httpContext = new Mock<HttpContext>();
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(a => a.Value).Returns(httpContext.Object);
            var signInManager = new Mock<SignInManager<IdentityUser>>(userManager.Object,
                contextAccessor.Object, claimsManager.Object, options.Object, null);
            signInManager.Setup(s => s.ValidateSecurityStampAsync(It.IsAny<ClaimsIdentity>(), user.Id, CancellationToken.None)).ReturnsAsync(user).Verifiable();
            signInManager.Setup(s => s.SignInAsync(user, isPersistent, null, CancellationToken.None)).Returns(Task.FromResult(0)).Verifiable();
            var services = new ServiceCollection();
            services.AddInstance(options.Object);
            services.AddInstance(signInManager.Object);
            services.AddInstance<ISecurityStampValidator>(new SecurityStampValidator<IdentityUser>());
            httpContext.Setup(c => c.RequestServices).Returns(services.BuildServiceProvider());
            var id = new ClaimsIdentity(IdentityOptions.ApplicationCookieAuthenticationType);
            id.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));

            var ticket = new AuthenticationTicket(id, new AuthenticationProperties { IssuedUtc = DateTimeOffset.UtcNow, IsPersistent = isPersistent });
            var context = new CookieValidateIdentityContext(httpContext.Object, ticket, new CookieAuthenticationOptions());
            Assert.NotNull(context.Properties);
            Assert.NotNull(context.Options);
            Assert.NotNull(context.Identity);
            await
                SecurityStampValidator.ValidateIdentityAsync(context);
            Assert.NotNull(context.Identity);
            signInManager.VerifyAll();
        }

        [Fact]
        public async Task OnValidateIdentityRejectsWhenValidateSecurityStampFails()
        {
            var user = new IdentityUser("test");
            var userManager = MockHelpers.MockUserManager<IdentityUser>();
            var claimsManager = new Mock<IClaimsIdentityFactory<IdentityUser>>();
            var identityOptions = new IdentityOptions { SecurityStampValidationInterval = TimeSpan.Zero };
            var options = new Mock<IOptions<IdentityOptions>>();
            options.Setup(a => a.Options).Returns(identityOptions);
            var httpContext = new Mock<HttpContext>();
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(a => a.Value).Returns(httpContext.Object);
            var signInManager = new Mock<SignInManager<IdentityUser>>(userManager.Object,
                contextAccessor.Object, claimsManager.Object, options.Object, null);
            signInManager.Setup(s => s.ValidateSecurityStampAsync(It.IsAny<ClaimsIdentity>(), user.Id, CancellationToken.None)).ReturnsAsync(null).Verifiable();
            var services = new ServiceCollection();
            services.AddInstance(options.Object);
            services.AddInstance(signInManager.Object);
            services.AddInstance<ISecurityStampValidator>(new SecurityStampValidator<IdentityUser>());
            httpContext.Setup(c => c.RequestServices).Returns(services.BuildServiceProvider());
            var id = new ClaimsIdentity(IdentityOptions.ApplicationCookieAuthenticationType);
            id.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));

            var ticket = new AuthenticationTicket(id, new AuthenticationProperties { IssuedUtc = DateTimeOffset.UtcNow });
            var context = new CookieValidateIdentityContext(httpContext.Object, ticket, new CookieAuthenticationOptions());
            Assert.NotNull(context.Properties);
            Assert.NotNull(context.Options);
            Assert.NotNull(context.Identity);
            await
                SecurityStampValidator.ValidateIdentityAsync(context);
            Assert.Null(context.Identity);
            signInManager.VerifyAll();
        }

        [Fact]
        public async Task OnValidateIdentityRejectsWhenNoIssuedUtc()
        {
            var user = new IdentityUser("test");
            var httpContext = new Mock<HttpContext>();
            var userManager = MockHelpers.MockUserManager<IdentityUser>();
            var claimsManager = new Mock<IClaimsIdentityFactory<IdentityUser>>();
            var identityOptions = new IdentityOptions { SecurityStampValidationInterval = TimeSpan.Zero };
            var options = new Mock<IOptions<IdentityOptions>>();
            options.Setup(a => a.Options).Returns(identityOptions);
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(a => a.Value).Returns(httpContext.Object);
            var signInManager = new Mock<SignInManager<IdentityUser>>(userManager.Object,
                contextAccessor.Object, claimsManager.Object, options.Object, null);
            signInManager.Setup(s => s.ValidateSecurityStampAsync(It.IsAny<ClaimsIdentity>(), user.Id, CancellationToken.None)).ReturnsAsync(null).Verifiable();
            var services = new ServiceCollection();
            services.AddInstance(options.Object);
            services.AddInstance(signInManager.Object);
            services.AddInstance<ISecurityStampValidator>(new SecurityStampValidator<IdentityUser>());
            httpContext.Setup(c => c.RequestServices).Returns(services.BuildServiceProvider());
            var id = new ClaimsIdentity(IdentityOptions.ApplicationCookieAuthenticationType);
            id.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));

            var ticket = new AuthenticationTicket(id, new AuthenticationProperties());
            var context = new CookieValidateIdentityContext(httpContext.Object, ticket, new CookieAuthenticationOptions());
            Assert.NotNull(context.Properties);
            Assert.NotNull(context.Options);
            Assert.NotNull(context.Identity);
            await
                SecurityStampValidator.ValidateIdentityAsync(context);
            Assert.Null(context.Identity);
            signInManager.VerifyAll();
        }

        [Fact]
        public async Task OnValidateIdentityDoesNotRejectsWhenNotExpired()
        {
            var user = new IdentityUser("test");
            var httpContext = new Mock<HttpContext>();
            var userManager = MockHelpers.MockUserManager<IdentityUser>();
            var claimsManager = new Mock<IClaimsIdentityFactory<IdentityUser>>();
            var identityOptions = new IdentityOptions { SecurityStampValidationInterval = TimeSpan.FromDays(1) };
            var options = new Mock<IOptions<IdentityOptions>>();
            options.Setup(a => a.Options).Returns(identityOptions);
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(a => a.Value).Returns(httpContext.Object);
            var signInManager = new Mock<SignInManager<IdentityUser>>(userManager.Object,
                contextAccessor.Object, claimsManager.Object, options.Object, null);
            signInManager.Setup(s => s.ValidateSecurityStampAsync(It.IsAny<ClaimsIdentity>(), user.Id, CancellationToken.None)).Throws(new Exception("Shouldn't be called"));
            signInManager.Setup(s => s.SignInAsync(user, false, null, CancellationToken.None)).Throws(new Exception("Shouldn't be called"));
            var services = new ServiceCollection();
            services.AddInstance(options.Object);
            services.AddInstance(signInManager.Object);
            services.AddInstance<ISecurityStampValidator>(new SecurityStampValidator<IdentityUser>());
            httpContext.Setup(c => c.RequestServices).Returns(services.BuildServiceProvider());
            var id = new ClaimsIdentity(IdentityOptions.ApplicationCookieAuthenticationType);
            id.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));

            var ticket = new AuthenticationTicket(id, new AuthenticationProperties { IssuedUtc = DateTimeOffset.UtcNow });
            var context = new CookieValidateIdentityContext(httpContext.Object, ticket, new CookieAuthenticationOptions());
            Assert.NotNull(context.Properties);
            Assert.NotNull(context.Options);
            Assert.NotNull(context.Identity);
            await
                SecurityStampValidator.ValidateIdentityAsync(context);
            Assert.NotNull(context.Identity);
        }
    }
}