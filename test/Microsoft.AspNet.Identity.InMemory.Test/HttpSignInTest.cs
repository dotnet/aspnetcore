// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Builder.Internal;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.AspNet.Identity.Test;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Identity.InMemory.Test
{
    public class HttpSignInTest
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
            services.AddSingleton<IUserStore<TestUser>, InMemoryUserStore<TestUser>>();
            services.AddSingleton<IRoleStore<TestRole>, InMemoryRoleStore<TestRole>>();
            
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
    }
}
