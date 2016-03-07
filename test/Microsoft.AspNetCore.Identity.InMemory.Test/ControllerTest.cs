// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Builder.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity.Test;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Identity.InMemory.Test
{
    public class ControllerTest
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task VerifyAccountControllerSignIn(bool isPersistent)
        {
            var context = new Mock<HttpContext>();
            var auth = new Mock<AuthenticationManager>();
            context.Setup(c => c.Authentication).Returns(auth.Object).Verifiable();
            auth.Setup(a => a.SignInAsync(new IdentityCookieOptions().ApplicationCookieAuthenticationScheme,
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>())).Returns(Task.FromResult(0)).Verifiable();
            // REVIEW: is persistant mocking broken
            //It.Is<AuthenticationProperties>(v => v.IsPersistent == isPersistent))).Returns(Task.FromResult(0)).Verifiable();
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(a => a.HttpContext).Returns(context.Object);
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(contextAccessor.Object);
            services.AddIdentity<TestUser, TestRole>();
            services.AddSingleton<IUserStore<TestUser>, InMemoryStore<TestUser, TestRole>>();
            services.AddSingleton<IRoleStore<TestRole>, InMemoryStore<TestUser, TestRole>>();
            
            var app = new ApplicationBuilder(services.BuildServiceProvider());
            app.UseCookieAuthentication();

            // Act
            var user = new TestUser
            {
                UserName = "Yolo"
            };
            const string password = "Yol0Sw@g!";
            var userManager = app.ApplicationServices.GetRequiredService<UserManager<TestUser>>();
            var signInManager = app.ApplicationServices.GetRequiredService<SignInManager<TestUser>>();

            IdentityResultAssert.IsSuccess(await userManager.CreateAsync(user, password));

            var result = await signInManager.PasswordSignInAsync(user, password, isPersistent, false);

            // Assert
            Assert.True(result.Succeeded);
            context.VerifyAll();
            auth.VerifyAll();
            contextAccessor.VerifyAll();
        }

        [Fact]
        public async Task VerifyAccountControllerExternalLoginWithTokensFlow()
        {
            // Setup the external cookie like it would look from a real OAuth2
            var externalId = "<externalId>";
            var authScheme = "<authScheme>";
            var externalIdentity = new ClaimsIdentity();
            externalIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, externalId));
            var externalPrincipal = new ClaimsPrincipal(externalIdentity);
            var externalLogin = new ExternalLoginInfo(externalPrincipal, authScheme, externalId, "displayname")
            {
                AuthenticationTokens = new[] {
                    new AuthenticationToken { Name = "refresh_token", Value = "refresh" },
                    new AuthenticationToken { Name = "access_token", Value = "access" }
                }
            };

            var auth = new Mock<AuthenticationManager>();
            auth.Setup(a => a.AuthenticateAsync(It.IsAny<AuthenticateContext>())).Returns(Task.FromResult(0));
            var context = new Mock<HttpContext>();
            context.Setup(c => c.Authentication).Returns(auth.Object).Verifiable();
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(a => a.HttpContext).Returns(context.Object);
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(contextAccessor.Object);
            services.AddIdentity<TestUser, TestRole>();
            services.AddSingleton<IUserStore<TestUser>, InMemoryStore<TestUser, TestRole>>();
            services.AddSingleton<IRoleStore<TestRole>, InMemoryStore<TestUser, TestRole>>();

            var app = new ApplicationBuilder(services.BuildServiceProvider());
            app.UseCookieAuthentication();

            // Act
            var user = new TestUser
            {
                UserName = "Yolo"
            };
            var userManager = app.ApplicationServices.GetRequiredService<UserManager<TestUser>>();
            var signInManager = app.ApplicationServices.GetRequiredService<SignInManager<TestUser>>();

            IdentityResultAssert.IsSuccess(await userManager.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await userManager.AddLoginAsync(user, new UserLoginInfo(authScheme, externalId, "whatever")));
            IdentityResultAssert.IsSuccess(await signInManager.UpdateExternalAuthenticationTokensAsync(externalLogin));
            Assert.Equal("refresh", await userManager.GetAuthenticationTokenAsync(user, authScheme, "refresh_token"));
            Assert.Equal("access", await userManager.GetAuthenticationTokenAsync(user, authScheme, "access_token"));
        }
    }
}
