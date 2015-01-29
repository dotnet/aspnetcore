// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Security;
using Microsoft.AspNet.Identity.Test;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Runtime.Infrastructure;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Identity.InMemory.Test
{
    public class ApplicationUser : InMemoryUser { }

    public class HttpSignInTest
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task VerifyAccountControllerSignIn(bool isPersistent)
        {
            var app = new ApplicationBuilder(CallContextServiceLocator.Locator.ServiceProvider);
            app.UseCookieAuthentication();

            var context = new Mock<HttpContext>();
            var response = new Mock<HttpResponse>();
            context.Setup(c => c.Response).Returns(response.Object).Verifiable();
            response.Setup(r => r.SignIn(It.Is<AuthenticationProperties>(v => v.IsPersistent == isPersistent), It.IsAny<ClaimsIdentity>())).Verifiable();
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(a => a.Value).Returns(context.Object);
            app.UseServices(services =>
            {
                services.AddInstance(contextAccessor.Object);
                services.AddIdentity<ApplicationUser, IdentityRole>();
                services.AddSingleton<IUserStore<ApplicationUser>, InMemoryUserStore<ApplicationUser>>();
                services.AddSingleton<IRoleStore<IdentityRole>, InMemoryRoleStore<IdentityRole>>();
            });

            // Act
            var user = new ApplicationUser
            {
                UserName = "Yolo"
            };
            const string password = "Yol0Sw@g!";
            var userManager = app.ApplicationServices.GetRequiredService<UserManager<ApplicationUser>>();
            var signInManager = app.ApplicationServices.GetRequiredService<SignInManager<ApplicationUser>>();

            IdentityResultAssert.IsSuccess(await userManager.CreateAsync(user, password));
            var result = await signInManager.PasswordSignInAsync(user, password, isPersistent, false);

            // Assert
            Assert.True(result.Succeeded);
            context.VerifyAll();
            response.VerifyAll();
            contextAccessor.VerifyAll();
        }
    }
}
