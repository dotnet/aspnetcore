using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MusicStore.Models;
using Xunit;

namespace MusicStore.Controllers
{
    public class ManageControllerTest : IClassFixture<ManageControllerTest.Fixture>
    {
        private readonly Fixture _fixture;

        public ManageControllerTest(Fixture fixture)
        {
            _fixture = fixture;
            _fixture.CreateDatabase();
        }

        [Fact]
        public async Task Index_ReturnsViewBagMessagesExpected()
        {
            // Arrange
            var userId = "TestUserA";
            var phone = "abcdefg";
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };

            var userManager = _fixture.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var userManagerResult = await userManager.CreateAsync(
                new ApplicationUser { Id = userId, UserName = "Test", TwoFactorEnabled = true, PhoneNumber = phone },
                "Pass@word1");
            Assert.True(userManagerResult.Succeeded);

            var signInManager = _fixture.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();

            var httpContext = _fixture.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext;
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims));
            httpContext.RequestServices = _fixture.ServiceProvider;
 
            var schemeProvider = _fixture.ServiceProvider.GetRequiredService<IAuthenticationSchemeProvider>();
  
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

        public class Fixture : SqliteInMemoryFixture
        {
            public override IServiceCollection ConfigureServices(IServiceCollection services)
            {
                services = base.ConfigureServices(services);

                services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
                                services.AddOptions();


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

                return services;
            }
        }
    }
}
