// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Specialized;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4.Configuration;
using IdentityServer4.Endpoints.Results;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer
{
    public class AutoRedirectEndSessionEndpointTests
    {
        [Fact]
        public async Task AutoRedirectSessionEndpoint_AutoRedirectsValidatedPostLogoutRequests_ToApplicationsWithProfiles()
        {
            // Arrange
            var session = new Mock<IUserSession>();
            session.Setup(s => s.GetUserAsync()).ReturnsAsync(new ClaimsPrincipal());

            var endSessionValidator = new Mock<IEndSessionRequestValidator>();
            endSessionValidator.Setup(esv => esv.ValidateAsync(It.IsAny<NameValueCollection>(), It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(new EndSessionValidationResult()
                {
                    IsError = false,
                    ValidatedRequest = new ValidatedEndSessionRequest()
                    {
                        Client = ClientBuilder.IdentityServerSPA("MySPA").Build(),
                        PostLogOutUri = "https://www.example.com/logout"
                    }
                });

            var identityServerOptions = Options.Create(new IdentityServerOptions());
            identityServerOptions.Value.Authentication.CookieAuthenticationScheme = IdentityConstants.ApplicationScheme;
            identityServerOptions.Value.UserInteraction.LogoutUrl = "/Identity/Account/Logout";
            identityServerOptions.Value.UserInteraction.ErrorUrl = "/Identity/Error";

            var endpoint = new AutoRedirectEndSessionEndpoint(new TestLogger<AutoRedirectEndSessionEndpoint>(), endSessionValidator.Object, identityServerOptions, session.Object);
            var ctx = new DefaultHttpContext();
            SetupRequestServices(ctx);
            ctx.Request.Method = HttpMethods.Post;
            ctx.Request.ContentType = "application/x-www-form-urlencoded";

            // Act
            var response = await endpoint.ProcessAsync(ctx);

            // Assert
            Assert.NotNull(response);
            var redirect = Assert.IsType<AutoRedirectEndSessionEndpoint.RedirectResult>(response);
            Assert.Equal("https://www.example.com/logout", redirect.Url);
            await response.ExecuteAsync(ctx);
            Assert.Equal(StatusCodes.Status302Found, ctx.Response.StatusCode);
            Assert.Equal("https://www.example.com/logout", ctx.Response.Headers[HeaderNames.Location]);
        }

        [Fact]
        public async Task AutoRedirectSessionEndpoint_AutoRedirectsValidatedGetLogoutRequests_ToApplicationsWithProfiles()
        {
            // Arrange
            var session = new Mock<IUserSession>();
            session.Setup(s => s.GetUserAsync()).ReturnsAsync(new ClaimsPrincipal());

            var endSessionValidator = new Mock<IEndSessionRequestValidator>();
            endSessionValidator.Setup(esv => esv.ValidateAsync(It.IsAny<NameValueCollection>(), It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(new EndSessionValidationResult()
                {
                    IsError = false,
                    ValidatedRequest = new ValidatedEndSessionRequest()
                    {
                        Client = ClientBuilder.IdentityServerSPA("MySPA").Build(),
                        PostLogOutUri = "https://www.example.com/logout"
                    }
                });

            var identityServerOptions = Options.Create(new IdentityServerOptions());
            identityServerOptions.Value.Authentication.CookieAuthenticationScheme = IdentityConstants.ApplicationScheme;
            identityServerOptions.Value.UserInteraction.LogoutUrl = "/Identity/Account/Logout";
            identityServerOptions.Value.UserInteraction.ErrorUrl = "/Identity/Error";

            var endpoint = new AutoRedirectEndSessionEndpoint(new TestLogger<AutoRedirectEndSessionEndpoint>(), endSessionValidator.Object, identityServerOptions, session.Object);
            var ctx = new DefaultHttpContext();
            SetupRequestServices(ctx);
            ctx.Request.Method = HttpMethods.Get;

            // Act
            var response = await endpoint.ProcessAsync(ctx);

            // Assert
            Assert.NotNull(response);
            var redirect = Assert.IsType<AutoRedirectEndSessionEndpoint.RedirectResult>(response);
            Assert.Equal("https://www.example.com/logout", redirect.Url);
            await response.ExecuteAsync(ctx);
            Assert.Equal(StatusCodes.Status302Found, ctx.Response.StatusCode);
            Assert.Equal("https://www.example.com/logout", ctx.Response.Headers[HeaderNames.Location]);
        }

        [Fact]
        public async Task AutoRedirectSessionEndpoint_RedirectsToError_WhenValidationFails()
        {
            // Arrange
            var session = new Mock<IUserSession>();
            session.Setup(s => s.GetUserAsync()).ReturnsAsync(new ClaimsPrincipal());

            var endSessionValidator = new Mock<IEndSessionRequestValidator>();
            endSessionValidator.Setup(esv => esv.ValidateAsync(It.IsAny<NameValueCollection>(), It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(new EndSessionValidationResult()
                {
                    IsError = true,
                    Error = "SomeError"
                });

            var identityServerOptions = Options.Create(new IdentityServerOptions());
            identityServerOptions.Value.Authentication.CookieAuthenticationScheme = IdentityConstants.ApplicationScheme;
            identityServerOptions.Value.UserInteraction.LogoutUrl = "/Identity/Account/Logout";
            identityServerOptions.Value.UserInteraction.ErrorUrl = "/Identity/Error";

            var endpoint = new AutoRedirectEndSessionEndpoint(new TestLogger<AutoRedirectEndSessionEndpoint>(), endSessionValidator.Object, identityServerOptions, session.Object);
            var ctx = new DefaultHttpContext();
            SetupRequestServices(ctx);
            ctx.Request.Method = HttpMethods.Post;
            ctx.Request.ContentType = "application/x-www-form-urlencoded";

            // Act
            var response = await endpoint.ProcessAsync(ctx);

            // Assert
            Assert.NotNull(response);
            var redirect = Assert.IsType<AutoRedirectEndSessionEndpoint.RedirectResult>(response);
            Assert.Equal("/Identity/Error", redirect.Url);
            await response.ExecuteAsync(ctx);
            Assert.Equal(StatusCodes.Status302Found, ctx.Response.StatusCode);
            Assert.Equal("/Identity/Error", ctx.Response.Headers[HeaderNames.Location]);
        }

        [Fact]
        public async Task AutoRedirectSessionEndpoint_RedirectsToLogoutUri_WhenClientDoesntHaveAProfile()
        {
            // Arrange
            var session = new Mock<IUserSession>();
            session.Setup(s => s.GetUserAsync()).ReturnsAsync(new ClaimsPrincipal());

            var endSessionValidator = new Mock<IEndSessionRequestValidator>();
            endSessionValidator.Setup(esv => esv.ValidateAsync(It.IsAny<NameValueCollection>(), It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(new EndSessionValidationResult()
                {
                    IsError = false,
                    ValidatedRequest = new ValidatedEndSessionRequest()
                    {
                        Client = new Client()
                    }
                });

            var identityServerOptions = Options.Create(new IdentityServerOptions());
            identityServerOptions.Value.Authentication.CookieAuthenticationScheme = IdentityConstants.ApplicationScheme;
            identityServerOptions.Value.UserInteraction.LogoutUrl = "/Identity/Account/Logout";
            identityServerOptions.Value.UserInteraction.ErrorUrl = "/Identity/Error";

            var endpoint = new AutoRedirectEndSessionEndpoint(new TestLogger<AutoRedirectEndSessionEndpoint>(), endSessionValidator.Object, identityServerOptions, session.Object);
            var ctx = new DefaultHttpContext();
            SetupRequestServices(ctx);
            ctx.Request.Method = HttpMethods.Post;
            ctx.Request.ContentType = "application/x-www-form-urlencoded";

            // Act
            var response = await endpoint.ProcessAsync(ctx);

            // Assert
            Assert.NotNull(response);
            var redirect = Assert.IsType<AutoRedirectEndSessionEndpoint.RedirectResult>(response);
            Assert.Equal("/Identity/Account/Logout", redirect.Url);
            await response.ExecuteAsync(ctx);
            Assert.Equal(StatusCodes.Status302Found, ctx.Response.StatusCode);
            Assert.Equal("/Identity/Account/Logout", ctx.Response.Headers[HeaderNames.Location]);
        }

        [Fact]
        public async Task AutoRedirectSessionEndpoint_RedirectsToLogoutUri_WhenTheValidationRequestDoesNotContainAClient()
        {
            // Arrange
            var session = new Mock<IUserSession>();
            session.Setup(s => s.GetUserAsync()).ReturnsAsync(new ClaimsPrincipal());

            var endSessionValidator = new Mock<IEndSessionRequestValidator>();
            endSessionValidator.Setup(esv => esv.ValidateAsync(It.IsAny<NameValueCollection>(), It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(new EndSessionValidationResult()
                {
                    IsError = false,
                    ValidatedRequest = new ValidatedEndSessionRequest()
                });

            var identityServerOptions = Options.Create(new IdentityServerOptions());
            identityServerOptions.Value.Authentication.CookieAuthenticationScheme = IdentityConstants.ApplicationScheme;
            identityServerOptions.Value.UserInteraction.LogoutUrl = "/Identity/Account/Logout";
            identityServerOptions.Value.UserInteraction.ErrorUrl = "/Identity/Error";

            var endpoint = new AutoRedirectEndSessionEndpoint(new TestLogger<AutoRedirectEndSessionEndpoint>(), endSessionValidator.Object, identityServerOptions, session.Object);
            var ctx = new DefaultHttpContext();
            SetupRequestServices(ctx);
            ctx.Request.Method = HttpMethods.Post;
            ctx.Request.ContentType = "application/x-www-form-urlencoded";

            // Act
            var response = await endpoint.ProcessAsync(ctx);

            // Assert
            Assert.NotNull(response);
            var redirect = Assert.IsType<AutoRedirectEndSessionEndpoint.RedirectResult>(response);
            Assert.Equal("/Identity/Account/Logout", redirect.Url);
            await response.ExecuteAsync(ctx);
            Assert.Equal(StatusCodes.Status302Found, ctx.Response.StatusCode);
            Assert.Equal("/Identity/Account/Logout", ctx.Response.Headers[HeaderNames.Location]);
        }

        [Theory]
        [InlineData("PUT")]
        [InlineData("DELETE")]
        [InlineData("PATCH")]
        [InlineData("OPTIONS")]
        [InlineData("HEAD")]
        public async Task AutoRedirectSessionEndpoint_ReturnsBadRequest_WhenMethodIsNotPostOrGet(string method)
        {
            // Arrange
            var session = new Mock<IUserSession>();
            var endSessionValidator = new Mock<IEndSessionRequestValidator>();
            var identityServerOptions = Options.Create(new IdentityServerOptions());

            var endpoint = new AutoRedirectEndSessionEndpoint(new TestLogger<AutoRedirectEndSessionEndpoint>(), endSessionValidator.Object, identityServerOptions, session.Object);
            var ctx = new DefaultHttpContext();
            SetupRequestServices(ctx);
            ctx.Request.Method = method;

            // Act
            var response = await endpoint.ProcessAsync(ctx);

            // Assert
            Assert.NotNull(response);
            var statusCode = Assert.IsType<StatusCodeResult>(response);
            Assert.Equal(StatusCodes.Status400BadRequest, statusCode.StatusCode);
        }

        [Fact]
        public async Task AutoRedirectSessionEndpoint_ReturnsBadRequest_WhenCannotReadTheRequestBody()
        {
            // Arrange
            var session = new Mock<IUserSession>();
            var endSessionValidator = new Mock<IEndSessionRequestValidator>();
            var identityServerOptions = Options.Create(new IdentityServerOptions());

            var endpoint = new AutoRedirectEndSessionEndpoint(new TestLogger<AutoRedirectEndSessionEndpoint>(), endSessionValidator.Object, identityServerOptions, session.Object);
            var ctx = new DefaultHttpContext();
            SetupRequestServices(ctx);
            ctx.Request.Method = HttpMethods.Post;

            // Act & Assert
            var response = await endpoint.ProcessAsync(ctx);

            // Assert
            Assert.NotNull(response);
            var statusCode = Assert.IsType<StatusCodeResult>(response);
            Assert.Equal(StatusCodes.Status400BadRequest, statusCode.StatusCode);
        }

        private void SetupRequestServices(DefaultHttpContext ctx)
        {
            var collection = new ServiceCollection();
            var authService = new Mock<IAuthenticationService>();
            authService.Setup(service => service.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            collection.AddSingleton(authService.Object);
            ctx.RequestServices = collection.BuildServiceProvider();
        }
    }
}
