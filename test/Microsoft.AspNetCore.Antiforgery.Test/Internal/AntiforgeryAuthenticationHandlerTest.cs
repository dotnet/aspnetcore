// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Antiforgery.Internal
{
    public class AntiforgeryAuthenticationHandlerTest
    {
        [Fact]
        public async Task IntializeAsync_NoOp_WhenAnonymous()
        {
            // Arrange
            var antiforgery = new Mock<IAntiforgery>(MockBehavior.Strict);
            var handler = new AntiforgeryAuthenticationHandler(antiforgery.Object);

            antiforgery
                .Setup(a => a.IsRequestValidAsync(It.IsAny<HttpContext>()))
                .ReturnsAsync(false)
                .Verifiable();

            var httpContext = new DefaultHttpContext();

            // Act
            await handler.InitializeAsync(httpContext);

            // Assert
            antiforgery.Verify(a => a.IsRequestValidAsync(It.IsAny<HttpContext>()), Times.Never());
        }

        [Fact]
        public async Task IntializeAsync_ValidatesRequest_WhenLoggedIn()
        {
            // Arrange
            var antiforgery = new Mock<IAntiforgery>(MockBehavior.Strict);
            var handler = new AntiforgeryAuthenticationHandler(antiforgery.Object);

            antiforgery
                .Setup(a => a.IsRequestValidAsync(It.IsAny<HttpContext>()))
                .ReturnsAsync(true)
                .Verifiable();

            var httpContext = new DefaultHttpContext();

            var authenticationFeature = new HttpAuthenticationFeature();
            httpContext.Features.Set<IHttpAuthenticationFeature>(authenticationFeature);
            authenticationFeature.User = new ClaimsPrincipal();

            // Act
            await handler.InitializeAsync(httpContext);

            // Assert
            antiforgery.Verify(a => a.IsRequestValidAsync(It.IsAny<HttpContext>()), Times.Once());
        }

        [Fact]
        public async Task IntializeAsync_ClearsUser_WhenInvalid()
        {
            // Arrange
            var antiforgery = new Mock<IAntiforgery>(MockBehavior.Strict);
            var handler = new AntiforgeryAuthenticationHandler(antiforgery.Object);

            antiforgery
                .Setup(a => a.IsRequestValidAsync(It.IsAny<HttpContext>()))
                .ReturnsAsync(false)
                .Verifiable();

            var httpContext = new DefaultHttpContext();

            var authenticationFeature = new HttpAuthenticationFeature();
            httpContext.Features.Set<IHttpAuthenticationFeature>(authenticationFeature);
            authenticationFeature.User = new ClaimsPrincipal();

            // Act
            await handler.InitializeAsync(httpContext);

            // Assert
            Assert.Null(authenticationFeature.User);
            antiforgery.Verify(a => a.IsRequestValidAsync(It.IsAny<HttpContext>()), Times.Once());
        }

        [Fact]
        public async Task IntializeAsync_AttachesAuthorizationHandler()
        {
            // Arrange
            var antiforgery = new Mock<IAntiforgery>(MockBehavior.Strict);
            var handler = new AntiforgeryAuthenticationHandler(antiforgery.Object);

            antiforgery
                .Setup(a => a.IsRequestValidAsync(It.IsAny<HttpContext>()))
                .ReturnsAsync(false)
                .Verifiable();

            var httpContext = new DefaultHttpContext();

            var authenticationFeature = new HttpAuthenticationFeature();
            httpContext.Features.Set<IHttpAuthenticationFeature>(authenticationFeature);

            // Act
            await handler.InitializeAsync(httpContext);

            // Assert
            Assert.Same(handler, authenticationFeature.Handler);
            antiforgery.Verify(a => a.IsRequestValidAsync(It.IsAny<HttpContext>()), Times.Never());
        }

        [Fact]
        public async Task AuthenticateAsync_NoPriorHandler_NoOp()
        {
            // Arrange
            var antiforgery = new Mock<IAntiforgery>(MockBehavior.Strict);
            var handler = new AntiforgeryAuthenticationHandler(antiforgery.Object);

            antiforgery
                .Setup(a => a.IsRequestValidAsync(It.IsAny<HttpContext>()))
                .ReturnsAsync(false)
                .Verifiable();

            antiforgery
                .Setup(a => a.ValidateRequestAsync(It.IsAny<HttpContext>(), It.IsAny<ClaimsPrincipal>()))
                .Verifiable();

            var httpContext = new DefaultHttpContext();

            var authenticationFeature = new HttpAuthenticationFeature();
            httpContext.Features.Set<IHttpAuthenticationFeature>(authenticationFeature);

            await handler.InitializeAsync(httpContext);

            var authenticateContext = new AuthenticateContext("Test");

            // Act
            await handler.AuthenticateAsync(authenticateContext);

            // Assert
            Assert.False(authenticateContext.Accepted);

            antiforgery.Verify(a => a.IsRequestValidAsync(It.IsAny<HttpContext>()), Times.Never());
            antiforgery.Verify(
                a => a.ValidateRequestAsync(It.IsAny<HttpContext>(), It.IsAny<ClaimsPrincipal>()),
                Times.Never());
        }

        [Fact]
        public async Task AuthenticateAsync_PriorHandlerDoesNotAuthenticate_NoOp()
        {
            // Arrange
            var antiforgery = new Mock<IAntiforgery>(MockBehavior.Strict);
            var handler = new AntiforgeryAuthenticationHandler(antiforgery.Object);

            antiforgery
                .Setup(a => a.IsRequestValidAsync(It.IsAny<HttpContext>()))
                .ReturnsAsync(false)
                .Verifiable();

            antiforgery
                .Setup(a => a.ValidateRequestAsync(It.IsAny<HttpContext>(), It.IsAny<ClaimsPrincipal>()))
                .Verifiable();

            var httpContext = new DefaultHttpContext();

            var authenticationFeature = new HttpAuthenticationFeature();
            httpContext.Features.Set<IHttpAuthenticationFeature>(authenticationFeature);
            var priorHandler = new Mock<IAuthenticationHandler>(MockBehavior.Strict);
            authenticationFeature.Handler = priorHandler.Object;

            priorHandler
                .Setup(h => h.AuthenticateAsync(It.IsAny<AuthenticateContext>()))
                .Returns(TaskCache.CompletedTask)
                .Callback<AuthenticateContext>(c => c.NotAuthenticated());

            await handler.InitializeAsync(httpContext);

            var authenticateContext = new AuthenticateContext("Test");

            // Act
            await handler.AuthenticateAsync(authenticateContext);

            // Assert
            Assert.True(authenticateContext.Accepted);
            Assert.Null(authenticateContext.Principal);

            antiforgery.Verify(a => a.IsRequestValidAsync(It.IsAny<HttpContext>()), Times.Never());
            antiforgery.Verify(
                a => a.ValidateRequestAsync(It.IsAny<HttpContext>(), It.IsAny<ClaimsPrincipal>()),
                Times.Never());
        }

        [Fact]
        public async Task AuthenticateAsync_PriorHandlerSetsPrincipal_Valid()
        {
            // Arrange
            var antiforgery = new Mock<IAntiforgery>(MockBehavior.Strict);
            var handler = new AntiforgeryAuthenticationHandler(antiforgery.Object);

            var principal = new ClaimsPrincipal();

            antiforgery
                .Setup(a => a.IsRequestValidAsync(It.IsAny<HttpContext>()))
                .ReturnsAsync(false)
                .Verifiable();

            antiforgery
                .Setup(a => a.ValidateRequestAsync(It.IsAny<HttpContext>(), principal))
                .Returns(TaskCache.CompletedTask)
                .Verifiable();

            var httpContext = new DefaultHttpContext();

            var authenticationFeature = new HttpAuthenticationFeature();
            httpContext.Features.Set<IHttpAuthenticationFeature>(authenticationFeature);
            var priorHandler = new Mock<IAuthenticationHandler>(MockBehavior.Strict);
            authenticationFeature.Handler = priorHandler.Object;

            priorHandler
                .Setup(h => h.AuthenticateAsync(It.IsAny<AuthenticateContext>()))
                .Returns(TaskCache.CompletedTask)
                .Callback<AuthenticateContext>(c => c.Authenticated(principal, null, null));

            await handler.InitializeAsync(httpContext);

            var authenticateContext = new AuthenticateContext("Test");

            // Act
            await handler.AuthenticateAsync(authenticateContext);

            // Assert
            Assert.True(authenticateContext.Accepted);
            Assert.Same(principal, authenticateContext.Principal);

            antiforgery.Verify(a => a.IsRequestValidAsync(It.IsAny<HttpContext>()), Times.Never());
            antiforgery.Verify(
                a => a.ValidateRequestAsync(It.IsAny<HttpContext>(), principal),
                Times.Once());
        }

        [Fact]
        public async Task AuthenticateAsync_PriorHandlerSetsPrincipal_Invalid()
        {
            // Arrange
            var antiforgery = new Mock<IAntiforgery>(MockBehavior.Strict);
            var handler = new AntiforgeryAuthenticationHandler(antiforgery.Object);

            var principal = new ClaimsPrincipal();

            antiforgery
                .Setup(a => a.IsRequestValidAsync(It.IsAny<HttpContext>()))
                .ReturnsAsync(false)
                .Verifiable();

            antiforgery
                .Setup(a => a.ValidateRequestAsync(It.IsAny<HttpContext>(), principal))
                .Throws(new AntiforgeryValidationException("invalid"))
                .Verifiable();

            var httpContext = new DefaultHttpContext();

            var authenticationFeature = new HttpAuthenticationFeature();
            httpContext.Features.Set<IHttpAuthenticationFeature>(authenticationFeature);
            var priorHandler = new Mock<IAuthenticationHandler>(MockBehavior.Strict);
            authenticationFeature.Handler = priorHandler.Object;

            priorHandler
                .Setup(h => h.AuthenticateAsync(It.IsAny<AuthenticateContext>()))
                .Returns(TaskCache.CompletedTask)
                .Callback<AuthenticateContext>(c => c.Authenticated(principal, null, null));

            await handler.InitializeAsync(httpContext);

            var authenticateContext = new AuthenticateContext("Test");

            // Act
            await handler.AuthenticateAsync(authenticateContext);

            // Assert
            Assert.True(authenticateContext.Accepted);
            Assert.Null(authenticateContext.Principal);
            Assert.NotNull(authenticateContext.Error);

            antiforgery.Verify(a => a.IsRequestValidAsync(It.IsAny<HttpContext>()), Times.Never());
            antiforgery.Verify(
                a => a.ValidateRequestAsync(It.IsAny<HttpContext>(), principal),
                Times.Once());
        }
    }
}
