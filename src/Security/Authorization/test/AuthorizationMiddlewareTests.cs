// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Authorization.Test.TestObjects;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Authorization.Test
{
    public class AuthorizationMiddlewareTests
    {
        [Fact]
        public async Task NoEndpoint_AnonymousUser_Allows()
        {
            // Arrange
            var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            var policyProvider = new Mock<IAuthorizationPolicyProvider>();
            policyProvider.Setup(p => p.GetDefaultPolicyAsync()).ReturnsAsync(policy);
            var next = new TestRequestDelegate();

            var middleware = CreateMiddleware(next.Invoke, policyProvider.Object);
            var context = GetHttpContext(anonymous: true);

            // Act
            await middleware.Invoke(context);

            // Assert
            Assert.True(next.Called);
        }

        [Fact]
        public async Task NoEndpointWithFallback_AnonymousUser_Challenges()
        {
            // Arrange
            var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            var policyProvider = new Mock<IAuthorizationPolicyProvider>();
            policyProvider.Setup(p => p.GetFallbackPolicyAsync()).ReturnsAsync(policy);
            var next = new TestRequestDelegate();

            var middleware = CreateMiddleware(next.Invoke, policyProvider.Object);
            var context = GetHttpContext(anonymous: true);

            // Act
            await middleware.Invoke(context);

            // Assert
            Assert.False(next.Called);
        }
        
        [Fact]
        public async Task HasEndpointWithoutAuth_AnonymousUser_Allows()
        {
            // Arrange
            var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            var policyProvider = new Mock<IAuthorizationPolicyProvider>();
            policyProvider.Setup(p => p.GetDefaultPolicyAsync()).ReturnsAsync(policy);
            var next = new TestRequestDelegate();

            var middleware = CreateMiddleware(next.Invoke, policyProvider.Object);
            var context = GetHttpContext(anonymous: true, endpoint: CreateEndpoint());

            // Act
            await middleware.Invoke(context);

            // Assert
            Assert.True(next.Called);
        }

        [Fact]
        public async Task HasEndpointWithFallbackWithoutAuth_AnonymousUser_Challenges()
        {
            // Arrange
            var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            var policyProvider = new Mock<IAuthorizationPolicyProvider>();
            policyProvider.Setup(p => p.GetDefaultPolicyAsync()).ReturnsAsync(policy);
            policyProvider.Setup(p => p.GetFallbackPolicyAsync()).ReturnsAsync(policy);
            var next = new TestRequestDelegate();

            var middleware = CreateMiddleware(next.Invoke, policyProvider.Object);
            var context = GetHttpContext(anonymous: true, endpoint: CreateEndpoint());

            // Act
            await middleware.Invoke(context);

            // Assert
            Assert.False(next.Called);
        }

        [Fact]
        public async Task HasEndpointWithOnlyFallbackAuth_AnonymousUser_Allows()
        {
            // Arrange
            var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            var policyProvider = new Mock<IAuthorizationPolicyProvider>();
            policyProvider.Setup(p => p.GetDefaultPolicyAsync()).ReturnsAsync(new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build());
            policyProvider.Setup(p => p.GetFallbackPolicyAsync()).ReturnsAsync(policy);
            var next = new TestRequestDelegate();
            var authenticationService = new TestAuthenticationService();

            var middleware = CreateMiddleware(next.Invoke, policyProvider.Object);
            var context = GetHttpContext(anonymous: true, endpoint: CreateEndpoint(new AuthorizeAttribute()), authenticationService: authenticationService);

            // Act
            await middleware.Invoke(context);

            // Assert
            Assert.True(next.Called);
        }

        [Fact]
        public async Task HasEndpointWithAuth_AnonymousUser_Challenges()
        {
            // Arrange
            var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            var policyProvider = new Mock<IAuthorizationPolicyProvider>();
            policyProvider.Setup(p => p.GetDefaultPolicyAsync()).ReturnsAsync(policy);
            var next = new TestRequestDelegate();
            var authenticationService = new TestAuthenticationService();

            var middleware = CreateMiddleware(next.Invoke, policyProvider.Object);
            var context = GetHttpContext(anonymous: true, endpoint: CreateEndpoint(new AuthorizeAttribute()), authenticationService: authenticationService);

            // Act
            await middleware.Invoke(context);

            // Assert
            Assert.False(next.Called);
            Assert.True(authenticationService.ChallengeCalled);
        }

        [Fact]
        public async Task HasEndpointWithAuth_ChallengesAuthenticationSchemes()
        {
            // Arrange
            var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            var policyProvider = new Mock<IAuthorizationPolicyProvider>();
            policyProvider.Setup(p => p.GetDefaultPolicyAsync()).ReturnsAsync(policy);
            var next = new TestRequestDelegate();
            var authenticationService = new TestAuthenticationService();

            var middleware = CreateMiddleware(next.Invoke, policyProvider.Object);
            var context = GetHttpContext(endpoint: CreateEndpoint(new AuthorizeAttribute() { AuthenticationSchemes = "whatever"}), authenticationService: authenticationService);

            // Act
            await middleware.Invoke(context);

            // Assert
            Assert.False(next.Called);
            Assert.True(authenticationService.ChallengeCalled);
        }
        
        [Fact]
        public async Task HasEndpointWithAuth_AnonymousUser_ChallengePerScheme()
        {
            // Arrange
            var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().AddAuthenticationSchemes("schema1", "schema2").Build();
            var policyProvider = new Mock<IAuthorizationPolicyProvider>();
            policyProvider.Setup(p => p.GetDefaultPolicyAsync()).ReturnsAsync(policy);
            var next = new TestRequestDelegate();
            var authenticationService = new TestAuthenticationService();

            var middleware = CreateMiddleware(next.Invoke, policyProvider.Object);
            var context = GetHttpContext(anonymous: true, endpoint: CreateEndpoint(new AuthorizeAttribute()), authenticationService: authenticationService);

            // Act
            await middleware.Invoke(context);

            // Assert
            Assert.False(next.Called);
            Assert.Equal(2, authenticationService.ChallengeCount);
        }

        [Fact]
        public async Task OnAuthorizationAsync_WillCallPolicyProvider()
        {
            // Arrange
            var policy = new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build();
            var policyProvider = new Mock<IAuthorizationPolicyProvider>();
            var getPolicyCount = 0;
            var getFallbackPolicyCount = 0;
            policyProvider.Setup(p => p.GetPolicyAsync(It.IsAny<string>())).ReturnsAsync(policy)
                .Callback(() => getPolicyCount++);
            policyProvider.Setup(p => p.GetFallbackPolicyAsync()).ReturnsAsync(policy)
                .Callback(() => getFallbackPolicyCount++);
            var next = new TestRequestDelegate();
            var middleware = CreateMiddleware(next.Invoke, policyProvider.Object);
            var context = GetHttpContext(anonymous: true, endpoint: CreateEndpoint(new AuthorizeAttribute("whatever")));

            // Act & Assert
            await middleware.Invoke(context);
            Assert.Equal(1, getPolicyCount);
            Assert.Equal(0, getFallbackPolicyCount);
            Assert.Equal(1, next.CalledCount);

            await middleware.Invoke(context);
            Assert.Equal(2, getPolicyCount);
            Assert.Equal(0, getFallbackPolicyCount);
            Assert.Equal(2, next.CalledCount);

            await middleware.Invoke(context);
            Assert.Equal(3, getPolicyCount);
            Assert.Equal(0, getFallbackPolicyCount);
            Assert.Equal(3, next.CalledCount);
        }

        [Fact]
        public async Task Invoke_ValidClaimShouldNotFail()
        {
            // Arrange
            var policy = new AuthorizationPolicyBuilder().RequireClaim("Permission", "CanViewPage").Build();
            var policyProvider = new Mock<IAuthorizationPolicyProvider>();
            policyProvider.Setup(p => p.GetDefaultPolicyAsync()).ReturnsAsync(policy);
            var next = new TestRequestDelegate();

            var middleware = CreateMiddleware(next.Invoke, policyProvider.Object);
            var context = GetHttpContext(endpoint: CreateEndpoint(new AuthorizeAttribute()));

            // Act
            await middleware.Invoke(context);

            // Assert
            Assert.True(next.Called);
        }

        [Fact]
        public async Task HasEndpointWithAuthAndAllowAnonymous_AnonymousUser_Allows()
        {
            // Arrange
            var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            var policyProvider = new Mock<IAuthorizationPolicyProvider>();
            policyProvider.Setup(p => p.GetDefaultPolicyAsync()).ReturnsAsync(policy);
            var next = new TestRequestDelegate();
            var authenticationService = new TestAuthenticationService();

            var middleware = CreateMiddleware(next.Invoke, policyProvider.Object);
            var context = GetHttpContext(anonymous: true, endpoint: CreateEndpoint(new AuthorizeAttribute(), new AllowAnonymousAttribute()), authenticationService: authenticationService);

            // Act
            await middleware.Invoke(context);

            // Assert
            Assert.True(next.Called);
            Assert.False(authenticationService.ChallengeCalled);
        }

        [Fact]
        public async Task HasEndpointWithAuth_AuthenticatedUser_Allows()
        {
            // Arrange
            var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            var policyProvider = new Mock<IAuthorizationPolicyProvider>();
            policyProvider.Setup(p => p.GetDefaultPolicyAsync()).ReturnsAsync(policy);
            var next = new TestRequestDelegate();
            var authenticationService = new TestAuthenticationService();

            var middleware = CreateMiddleware(next.Invoke, policyProvider.Object);
            var context = GetHttpContext(endpoint: CreateEndpoint(new AuthorizeAttribute()), authenticationService: authenticationService);

            // Act
            await middleware.Invoke(context);

            // Assert
            Assert.True(next.Called);
            Assert.False(authenticationService.ChallengeCalled);
        }

        [Fact]
        public async Task Invoke_AuthSchemesFailShouldSetEmptyPrincipalOnContext()
        {
            // Arrange
            var policy = new AuthorizationPolicyBuilder("Fails").RequireAuthenticatedUser().Build();
            var policyProvider = new Mock<IAuthorizationPolicyProvider>();
            policyProvider.Setup(p => p.GetDefaultPolicyAsync()).ReturnsAsync(policy);
            var next = new TestRequestDelegate();
            var authenticationService = new TestAuthenticationService();

            var middleware = CreateMiddleware(next.Invoke, policyProvider.Object);
            var context = GetHttpContext(endpoint: CreateEndpoint(new AuthorizeAttribute()), authenticationService: authenticationService);

            // Act
            await middleware.Invoke(context);

            // Assert
            Assert.False(next.Called);
            Assert.NotNull(context.User?.Identity);
            Assert.True(authenticationService.AuthenticateCalled);
            Assert.True(authenticationService.ChallengeCalled);
        }

        [Fact]
        public async Task Invoke_SingleValidClaimShouldSucceed()
        {
            // Arrange
            var policy = new AuthorizationPolicyBuilder().RequireClaim("Permission", "CanViewComment", "CanViewPage").Build();
            var policyProvider = new Mock<IAuthorizationPolicyProvider>();
            policyProvider.Setup(p => p.GetDefaultPolicyAsync()).ReturnsAsync(policy);
            var next = new TestRequestDelegate();

            var middleware = CreateMiddleware(next.Invoke, policyProvider.Object);
            var context = GetHttpContext(endpoint: CreateEndpoint(new AuthorizeAttribute()));

            // Act
            await middleware.Invoke(context);

            // Assert
            Assert.True(next.Called);
        }

        [Fact]
        public async Task AuthZResourceCanBeHttpContextAndHaveEndpoint()
        {
            // Arrange
            HttpContext resource = null;
            var policy = new AuthorizationPolicyBuilder().RequireAssertion(c =>
            {
                resource = c.Resource as HttpContext;
                return true;
            }).Build();
            var policyProvider = new Mock<IAuthorizationPolicyProvider>();
            policyProvider.Setup(p => p.GetDefaultPolicyAsync()).ReturnsAsync(policy);
            var next = new TestRequestDelegate();

            var middleware = CreateMiddleware(next.Invoke, policyProvider.Object, useContextAsResource: true);
            var endpoint = CreateEndpoint(new AuthorizeAttribute());
            var context = GetHttpContext(endpoint: endpoint);

            // Act
            await middleware.Invoke(context);

            // Assert
            Assert.NotNull(resource);
            Assert.Equal(context, resource);
            Assert.Equal(endpoint, resource.GetEndpoint());
        }

        [Fact]
        public async Task AuthZResourceShouldBeEndpointByDefault()
        {
            // Arrange
            object resource = null;
            var policy = new AuthorizationPolicyBuilder().RequireAssertion(c =>
            {
                resource = c.Resource as Endpoint;
                return true;
            }).Build();
            var policyProvider = new Mock<IAuthorizationPolicyProvider>();
            policyProvider.Setup(p => p.GetDefaultPolicyAsync()).ReturnsAsync(policy);
            var next = new TestRequestDelegate();

            var middleware = CreateMiddleware(next.Invoke, policyProvider.Object, useContextAsResource: true);
            var endpoint = CreateEndpoint(new AuthorizeAttribute());
            var context = GetHttpContext(endpoint: endpoint);

            // Act
            await middleware.Invoke(context);

            // Assert
            Assert.Equal(endpoint, resource);
        }
        
        [Fact]
        public async Task Invoke_RequireUnknownRoleShouldForbid()
        {
            // Arrange
            var policy = new AuthorizationPolicyBuilder().RequireRole("Wut").Build();
            var policyProvider = new Mock<IAuthorizationPolicyProvider>();
            policyProvider.Setup(p => p.GetDefaultPolicyAsync()).ReturnsAsync(policy);
            var next = new TestRequestDelegate();
            var authenticationService = new TestAuthenticationService();

            var middleware = CreateMiddleware(next.Invoke, policyProvider.Object);
            var context = GetHttpContext(endpoint: CreateEndpoint(new AuthorizeAttribute()), authenticationService: authenticationService);

            // Act
            await middleware.Invoke(context);

            // Assert
            Assert.False(next.Called);
            Assert.False(authenticationService.ChallengeCalled);
            Assert.True(authenticationService.ForbidCalled);
        }

        [Fact]
        public async Task Invoke_RequireUnknownRole_ForbidPerScheme()
        {
            // Arrange
            var policy = new AuthorizationPolicyBuilder().RequireRole("Wut").AddAuthenticationSchemes("Basic", "Bearer").Build();
            var policyProvider = new Mock<IAuthorizationPolicyProvider>();
            policyProvider.Setup(p => p.GetDefaultPolicyAsync()).ReturnsAsync(policy);
            var next = new TestRequestDelegate();
            var authenticationService = new TestAuthenticationService();

            var middleware = CreateMiddleware(next.Invoke, policyProvider.Object);
            var context = GetHttpContext(endpoint: CreateEndpoint(new AuthorizeAttribute()), authenticationService: authenticationService);

            // Act
            await middleware.Invoke(context);

            // Assert
            Assert.False(next.Called);
            Assert.Equal(2, authenticationService.ForbidCount);
        }

        [Fact]
        public async Task Invoke_InvalidClaimShouldForbid()
        {
            // Arrange
            var policy = new AuthorizationPolicyBuilder()
                .RequireClaim("Permission", "CanViewComment")
                .Build();
            var policyProvider = new Mock<IAuthorizationPolicyProvider>();
            policyProvider.Setup(p => p.GetDefaultPolicyAsync()).ReturnsAsync(policy);
            var next = new TestRequestDelegate();
            var authenticationService = new TestAuthenticationService();

            var middleware = CreateMiddleware(next.Invoke, policyProvider.Object);
            var context = GetHttpContext(endpoint: CreateEndpoint(new AuthorizeAttribute()), authenticationService: authenticationService);

            // Act
            await middleware.Invoke(context);

            // Assert
            Assert.False(next.Called);
            Assert.False(authenticationService.ChallengeCalled);
            Assert.True(authenticationService.ForbidCalled);
        }

        private AuthorizationMiddleware CreateMiddleware(RequestDelegate requestDelegate = null, IAuthorizationPolicyProvider policyProvider = null, bool useContextAsResource = false)
        {
            requestDelegate = requestDelegate ?? ((context) => Task.CompletedTask);

            return new AuthorizationMiddleware(requestDelegate, policyProvider, new OptionsWrapper<AuthorizationMiddlewareOptions>(new AuthorizationMiddlewareOptions { UseHttpContextAsResource = useContextAsResource }));
        }

        private Endpoint CreateEndpoint(params object[] metadata)
        {
            return new Endpoint(context => Task.CompletedTask, new EndpointMetadataCollection(metadata), "Test endpoint");
        }

        private HttpContext GetHttpContext(
            bool anonymous = false,
            Action<IServiceCollection> registerServices = null,
            Endpoint endpoint = null,
            IAuthenticationService authenticationService = null)
        {
            var basicPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new Claim[] {
                        new Claim("Permission", "CanViewPage"),
                        new Claim(ClaimTypes.Role, "Administrator"),
                        new Claim(ClaimTypes.Role, "User"),
                        new Claim(ClaimTypes.NameIdentifier, "John")},
                        "Basic"));

            var validUser = basicPrincipal;

            var bearerIdentity = new ClaimsIdentity(
                    new Claim[] {
                        new Claim("Permission", "CupBearer"),
                        new Claim(ClaimTypes.Role, "Token"),
                        new Claim(ClaimTypes.NameIdentifier, "John Bear")},
                        "Bearer");

            validUser.AddIdentity(bearerIdentity);

            // ServiceProvider
            var serviceCollection = new ServiceCollection();

            authenticationService = authenticationService ?? Mock.Of<IAuthenticationService>();

            serviceCollection.AddSingleton(authenticationService);
            serviceCollection.AddTransient<IAuthorizationMiddlewareResultHandler, AuthorizationMiddlewareResultHandler>();
            serviceCollection.AddOptions();
            serviceCollection.AddLogging();
            serviceCollection.AddAuthorization();
            registerServices?.Invoke(serviceCollection);

            var serviceProvider = serviceCollection.BuildServiceProvider();

            //// HttpContext
            var httpContext = new DefaultHttpContext();
            if (endpoint != null)
            {
                httpContext.SetEndpoint(endpoint);
            }
            httpContext.RequestServices = serviceProvider;
            if (!anonymous)
            {
                httpContext.User = validUser;
            }

            return httpContext;
        }

        private class TestRequestDelegate
        {
            private readonly int _statusCode;

            public bool Called => CalledCount > 0;
            public int CalledCount { get; private set; }

            public TestRequestDelegate(int statusCode = 200)
            {
                _statusCode = statusCode;
            }

            public Task Invoke(HttpContext context)
            {
                CalledCount++;
                context.Response.StatusCode = _statusCode;
                return Task.CompletedTask;
            }
        }
    }
}
