using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MusicStore.Models;
using Xunit;

namespace MusicStore.Controllers
{
    public class ManageControllerTest
    {
        private readonly IServiceProvider _serviceProvider;

        public ManageControllerTest()
        {
            var efServiceProvider = new ServiceCollection().AddEntityFrameworkInMemoryDatabase().BuildServiceProvider();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            services.AddOptions();
            services
                .AddDbContext<MusicStoreContext>(b => b.UseInMemoryDatabase("Scratch").UseInternalServiceProvider(efServiceProvider));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                    .AddEntityFrameworkStores<MusicStoreContext>();

            services.AddMvc();
            services.AddSingleton<IAuthenticationService, NoOpAuth>();
            services.AddLogging();

            // IHttpContextAccessor is required for SignInManager, and UserManager
            var context = new DefaultHttpContext();
            services.AddSingleton<IHttpContextAccessor>(
                new HttpContextAccessor()
                    {
                        HttpContext = context,
                    });

            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public async Task Index_ReturnsViewBagMessagesExpected()
        {
            // Arrange
            var userId = "TestUserA";
            var phone = "abcdefg";
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };

            var userManager = _serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var userManagerResult = await userManager.CreateAsync(
                new ApplicationUser { Id = userId, UserName = "Test", TwoFactorEnabled = true, PhoneNumber = phone },
                "Pass@word1");
            Assert.True(userManagerResult.Succeeded);

            var signInManager = _serviceProvider.GetRequiredService<SignInManager<ApplicationUser>>();

            var httpContext = _serviceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext;
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims));
            httpContext.RequestServices = _serviceProvider;
 
            var schemeProvider = _serviceProvider.GetRequiredService<IAuthenticationSchemeProvider>();
  
            var controller = new ManageController(userManager, signInManager, schemeProvider);
            controller.ControllerContext.HttpContext = httpContext;

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName);

            Assert.Empty(controller.ViewBag.StatusMessage);

            Assert.NotNull(viewResult.ViewData);
            var model = Assert.IsType<IndexViewModel>(viewResult.ViewData.Model);
            Assert.True(model.TwoFactor);
            Assert.Equal(phone, model.PhoneNumber);
            Assert.True(model.HasPassword);
        }

        public class NoOpAuth : IAuthenticationService
        {
            public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string scheme)
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            public Task ChallengeAsync(HttpContext context, string scheme, AuthenticationProperties properties)
            {
                return Task.FromResult(0);
            }

            public Task ForbidAsync(HttpContext context, string scheme, AuthenticationProperties properties)
            {
                return Task.FromResult(0);
            }

            public Task SignInAsync(HttpContext context, string scheme, ClaimsPrincipal principal, AuthenticationProperties properties)
            {
                throw new NotImplementedException();
            }

            public Task SignOutAsync(HttpContext context, string scheme, AuthenticationProperties properties)
            {
                throw new NotImplementedException();
            }
        }

    }
}