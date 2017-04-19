// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Authorization
{
    public class AuthorizeFilterTest
    {
        [Fact]
        public void InvalidUser()
        {
            var authorizationContext = GetAuthorizationContext(services => services.AddAuthorization());
            Assert.True(authorizationContext.HttpContext.User.Identities.Any(i => i.IsAuthenticated));
        }

        [Fact]
        public async Task AuthorizeFilter_CreatedWithAuthorizeData_ThrowsWhenOnAuthorizationAsyncIsCalled()
        {
            // Arrange
            var authorizeFilter = new AuthorizeFilter(new[] { new AuthorizeAttribute() });
            var authorizationContext = GetAuthorizationContext(services => services.AddAuthorization());
            var expected = "An AuthorizationPolicy cannot be created without a valid instance of " +
                "IAuthorizationPolicyProvider.";

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => authorizeFilter.OnAuthorizationAsync(authorizationContext));
            Assert.Equal(expected, ex.Message);
        }

        [Fact]
        public async Task AuthorizeFilter_CreatedWithPolicy_ThrowsWhenOnAuthorizationAsyncIsCalled()
        {
            // Arrange
            var authorizeFilter = new AuthorizeFilter(new[] { new AuthorizeAttribute() });
            var authorizationContext = GetAuthorizationContext(services => services.AddAuthorization());
            var expected = "An AuthorizationPolicy cannot be created without a valid instance of " +
                "IAuthorizationPolicyProvider.";

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => authorizeFilter.OnAuthorizationAsync(authorizationContext));
            Assert.Equal(expected, ex.Message);
        }

        [Fact]
        public async Task AuthorizeFilterCanAuthorizeNonAuthenticatedUser()
        {
            // Arrange
            var authorizeFilter = new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build());
            var authorizationContext = GetAuthorizationContext(services => services.AddAuthorization(), anonymous: true);
            authorizationContext.HttpContext.User = new ClaimsPrincipal();

            // Act
            await authorizeFilter.OnAuthorizationAsync(authorizationContext);

            // Assert
            Assert.Null(authorizationContext.Result);
        }

        [Fact]
        public async Task AuthorizeFilterWillCallPolicyProviderOnAuthorization()
        {
            // Arrange
            var policyProvider = new Mock<IAuthorizationPolicyProvider>();
            var getPolicyCount = 0;
            policyProvider.Setup(p => p.GetPolicyAsync(It.IsAny<string>())).ReturnsAsync(new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build())
                .Callback(() => getPolicyCount++);
            var authorizeFilter = new AuthorizeFilter(policyProvider.Object, new AuthorizeAttribute[] { new AuthorizeAttribute("whatever") });
            var authorizationContext = GetAuthorizationContext(services => services.AddAuthorization());

            // Act & Assert
            await authorizeFilter.OnAuthorizationAsync(authorizationContext);
            Assert.Equal(1, getPolicyCount);
            Assert.Null(authorizationContext.Result);

            await authorizeFilter.OnAuthorizationAsync(authorizationContext);
            Assert.Equal(2, getPolicyCount);
            Assert.Null(authorizationContext.Result);

            await authorizeFilter.OnAuthorizationAsync(authorizationContext);
            Assert.Equal(3, getPolicyCount);
            Assert.Null(authorizationContext.Result);

            // Make sure we don't cache the policy
            Assert.Null(authorizeFilter.Policy);
        }

        [Fact]
        public async Task AuthorizeFilterCanAuthorizeNullUser()
        {
            // Arrange
            var authorizeFilter = new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build());
            var authorizationContext = GetAuthorizationContext(services => services.AddAuthorization(), anonymous: true);

            // Act
            await authorizeFilter.OnAuthorizationAsync(authorizationContext);

            // Assert
            Assert.Null(authorizationContext.Result);
        }

        [Fact]
        public async Task Invoke_ValidClaimShouldNotFail()
        {
            // Arrange
            var authorizeFilter = new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireClaim("Permission", "CanViewPage").Build());
            var authorizationContext = GetAuthorizationContext(services => services.AddAuthorization());

            // Act
            await authorizeFilter.OnAuthorizationAsync(authorizationContext);

            // Assert
            Assert.Null(authorizationContext.Result);
        }

        [Fact]
        public async Task Invoke_EmptyClaimsShouldRejectAnonymousUser()
        {
            // Arrange
            var authorizeFilter = new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
            var authorizationContext = GetAuthorizationContext(services =>
                services.AddAuthorization(),
                anonymous: true);

            // Act
            await authorizeFilter.OnAuthorizationAsync(authorizationContext);

            // Assert
            Assert.NotNull(authorizationContext.Result);
        }

        [Fact]
        public async Task Invoke_EmptyClaimsWithAllowAnonymousAttributeShouldNotRejectAnonymousUser()
        {
            // Arrange
            var authorizeFilter = new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
            var authorizationContext = GetAuthorizationContext(services => services.AddAuthorization(),
                anonymous: true);

            authorizationContext.Filters.Add(new AllowAnonymousFilter());

            // Act
            await authorizeFilter.OnAuthorizationAsync(authorizationContext);

            // Assert
            Assert.Null(authorizationContext.Result);
        }

        [Fact]
        public async Task Invoke_EmptyClaimsShouldAuthorizeAuthenticatedUser()
        {
            // Arrange
            var authorizeFilter = new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
            var authorizationContext = GetAuthorizationContext(services => services.AddAuthorization());

            // Act
            await authorizeFilter.OnAuthorizationAsync(authorizationContext);

            // Assert
            Assert.Null(authorizationContext.Result);
        }

        [Fact]
        public async Task Invoke_AuthSchemesFailShouldSetEmptyPrincipalOnContext()
        {
            // Arrange
            var authorizeFilter = new AuthorizeFilter(new AuthorizationPolicyBuilder("Fails")
                .RequireAuthenticatedUser()
                .Build());
            var authorizationContext = GetAuthorizationContext(services => services.AddAuthorization());

            // Act
            await authorizeFilter.OnAuthorizationAsync(authorizationContext);

            // Assert
            Assert.NotNull(authorizationContext.HttpContext.User?.Identity);
        }

        [Fact]
        public async Task Invoke_SingleValidClaimShouldSucceed()
        {
            // Arrange
            var authorizeFilter = new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireClaim("Permission", "CanViewComment", "CanViewPage").Build());
            var authorizationContext = GetAuthorizationContext(services => services.AddAuthorization());

            // Act
            await authorizeFilter.OnAuthorizationAsync(authorizationContext);

            // Assert
            Assert.Null(authorizationContext.Result);
        }

        [Fact]
        public async Task Invoke_RequireAdminRoleShouldFailWithNoHandlers()
        {
            // Arrange
            var authorizeFilter = new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireRole("Administrator").Build());
            var authorizationContext = GetAuthorizationContext(services =>
            {
                services.AddOptions();
                services.AddAuthorization();

                services.Remove(services.Where(sd => sd.ServiceType == typeof(IAuthorizationHandler)).Single());
            });

            // Act
            await authorizeFilter.OnAuthorizationAsync(authorizationContext);

            // Assert
            Assert.NotNull(authorizationContext.Result);
        }

        [Fact]
        public async Task Invoke_RequireAdminAndUserRoleWithNoPolicyShouldSucceed()
        {
            // Arrange
            var authorizeFilter = new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireRole("Administrator").Build());
            var authorizationContext = GetAuthorizationContext(services => services.AddAuthorization());

            // Act
            await authorizeFilter.OnAuthorizationAsync(authorizationContext);

            // Assert
            Assert.Null(authorizationContext.Result);
        }

        [Fact]
        public async Task Invoke_RequireUnknownRoleShouldFail()
        {
            // Arrange
            var authorizeFilter = new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireRole("Wut").Build());
            var authorizationContext = GetAuthorizationContext(services => services.AddAuthorization());

            // Act
            await authorizeFilter.OnAuthorizationAsync(authorizationContext);

            // Assert
            Assert.NotNull(authorizationContext.Result);
        }

        [Fact]
        public async Task Invoke_RequireAdminRoleButFailPolicyShouldFail()
        {
            // Arrange
            var authorizeFilter = new AuthorizeFilter(new AuthorizationPolicyBuilder()
                .RequireRole("Administrator")
                .RequireClaim("Permission", "CanViewComment")
                .Build());
            var authorizationContext = GetAuthorizationContext(services => services.AddAuthorization());

            // Act
            await authorizeFilter.OnAuthorizationAsync(authorizationContext);

            // Assert
            Assert.NotNull(authorizationContext.Result);
        }

        [Fact]
        public async Task Invoke_InvalidClaimShouldFail()
        {
            // Arrange
            var authorizeFilter = new AuthorizeFilter(new AuthorizationPolicyBuilder()
                .RequireClaim("Permission", "CanViewComment")
                .Build());
            var authorizationContext = GetAuthorizationContext(services => services.AddAuthorization());

            // Act
            await authorizeFilter.OnAuthorizationAsync(authorizationContext);

            // Assert
            Assert.NotNull(authorizationContext.Result);
        }

        [Fact]
        public async Task Invoke_FailedContextShouldNotCheckPermission()
        {
            // Arrange
            bool authorizationServiceIsCalled = false;
            var authorizationService = new Mock<IAuthorizationService>();
            authorizationService
                .Setup(x => x.AuthorizeAsync(null, null, "CanViewComment"))
                .Returns(() =>
                {
                    authorizationServiceIsCalled = true;
                    return Task.FromResult(true);
                });

            var authorizeFilter = new AuthorizeFilter(new AuthorizationPolicyBuilder()
                .RequireClaim("Permission", "CanViewComment")
                .Build());
            var authorizationContext = GetAuthorizationContext(services =>
                services.AddSingleton(authorizationService.Object));

            authorizationContext.Result = new UnauthorizedResult();

            // Act
            await authorizeFilter.OnAuthorizationAsync(authorizationContext);

            // Assert
            Assert.False(authorizationServiceIsCalled);
        }

        [Fact]
        public async Task Invoke_FailWhenLookingForClaimInOtherIdentity()
        {
            // Arrange
            var authorizeFilter = new AuthorizeFilter(new AuthorizationPolicyBuilder()
                .RequireClaim("Permission", "CanViewComment")
                .Build());
            var authorizationContext = GetAuthorizationContext(services => services.AddAuthorization());

            // Act
            await authorizeFilter.OnAuthorizationAsync(authorizationContext);

            // Assert
            Assert.NotNull(authorizationContext.Result);
        }

        [Fact]
        public async Task Invoke_CanLookingForClaimsInMultipleIdentities()
        {
            // Arrange
            var authorizeFilter = new AuthorizeFilter(new AuthorizationPolicyBuilder("Basic", "Bearer")
                .RequireClaim("Permission", "CanViewComment")
                .RequireClaim("Permission", "CupBearer")
                .Build());
            var authorizationContext = GetAuthorizationContext(services => services.AddAuthorization());

            // Act
            await authorizeFilter.OnAuthorizationAsync(authorizationContext);

            // Assert
            Assert.NotNull(authorizationContext.Result);
        }

        [Fact]
        public async Task Invoke_CanFilterToOnlyBearerScheme()
        {
            // Arrange
            var authorizeFilter = new AuthorizeFilter(new AuthorizationPolicyBuilder("Bearer")
                .RequireClaim("Permission", "CanViewPage")
                .Build());
            var authorizationContext = GetAuthorizationContext(services => services.AddAuthorization());

            // Act
            await authorizeFilter.OnAuthorizationAsync(authorizationContext);

            // Assert
            Assert.NotNull(authorizationContext.Result);
        }

        [Fact]
        public void CreateInstance_ReturnsSelfIfPolicyIsSet()
        {
            // Arrange
            var authorizeFilter = new AuthorizeFilter(new AuthorizationPolicyBuilder()
                .RequireAssertion(_ => true)
                .Build());
            var factory = (IFilterFactory)authorizeFilter;

            // Act
            var result = factory.CreateInstance(new ServiceCollection().BuildServiceProvider());

            // Assert
            Assert.Same(authorizeFilter, result);
        }

        [Fact]
        public void CreateInstance_ReturnsSelfIfPolicyProviderIsSet()
        {
            // Arrange
            var authorizeFilter = new AuthorizeFilter(new AuthorizationPolicyBuilder()
                .RequireAssertion(_ => true)
                .Build());
            var factory = (IFilterFactory)authorizeFilter;

            // Act
            var result = factory.CreateInstance(new ServiceCollection().BuildServiceProvider());

            // Assert
            Assert.Same(authorizeFilter, result);
        }

        public static TheoryData AuthorizeFiltersCreatedWithoutPolicyOrPolicyProvider
        {
            get
            {
                return new TheoryData<AuthorizeFilter>
                {
                    new AuthorizeFilter(new[] { new AuthorizeAttribute()}),
                    new AuthorizeFilter("some-policy"),
                };
            }
        }

        [Theory]
        [MemberData(nameof(AuthorizeFiltersCreatedWithoutPolicyOrPolicyProvider))]
        public void CreateInstance_ReturnsNewFilterIfPolicyAndPolicyProviderAreNotSet(AuthorizeFilter authorizeFilter)
        {
            // Arrange
            var factory = (IFilterFactory)authorizeFilter;
            var serviceProvider = new ServiceCollection()
                .AddOptions()
                .AddAuthorization(options =>
                {
                    var policy = new AuthorizationPolicyBuilder()
                        .RequireAssertion(_ => true)
                        .Build();
                    options.AddPolicy("some-policy", policy);
                })
                .BuildServiceProvider();

            // Act
            var result = factory.CreateInstance(serviceProvider);

            // Assert
            Assert.NotSame(authorizeFilter, result);
            var actual = Assert.IsType<AuthorizeFilter>(result);
            Assert.NotNull(actual.Policy);
        }

        [Theory]
        [MemberData(nameof(AuthorizeFiltersCreatedWithoutPolicyOrPolicyProvider))]
        public void CreateInstance_ReturnsNewFilterIfPolicyAndPolicyProviderAreNotSetAndCustomProviderIsUsed(
            AuthorizeFilter authorizeFilter)
        {
            // Arrange
            var factory = (IFilterFactory)authorizeFilter;
            var policyProvider = Mock.Of<IAuthorizationPolicyProvider>();
            var serviceProvider = new ServiceCollection()
                .AddSingleton(policyProvider)
                .BuildServiceProvider();

            // Act
            var result = factory.CreateInstance(serviceProvider);

            // Assert
            Assert.NotSame(authorizeFilter, result);
            var actual = Assert.IsType<AuthorizeFilter>(result);
            Assert.Same(policyProvider, actual.PolicyProvider);
        }

        private AuthorizationFilterContext GetAuthorizationContext(
            Action<ServiceCollection> registerServices,
            bool anonymous = false)
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

            var bearerPrincipal = new ClaimsPrincipal(bearerIdentity);

            validUser.AddIdentity(bearerIdentity);

            // ServiceProvider
            var serviceCollection = new ServiceCollection();
            var auth = new Mock<IAuthenticationService>();
            if (registerServices != null)
            {
                serviceCollection.AddOptions();
                serviceCollection.AddLogging();
                serviceCollection.AddSingleton(auth.Object);
                registerServices(serviceCollection);
            }

            var serviceProvider = serviceCollection.BuildServiceProvider();

            // HttpContext
            var httpContext = new Mock<HttpContext>();
            httpContext.SetupProperty(c => c.User);
            if (!anonymous)
            {
                httpContext.Object.User = validUser;
            }
            httpContext.SetupGet(c => c.RequestServices).Returns(serviceProvider);
            auth.Setup(c => c.AuthenticateAsync(httpContext.Object, "Bearer")).ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(bearerPrincipal, "Bearer")));
            auth.Setup(c => c.AuthenticateAsync(httpContext.Object, "Basic")).ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(basicPrincipal, "Basic")));
            auth.Setup(c => c.AuthenticateAsync(httpContext.Object, "Fails")).ReturnsAsync(AuthenticateResult.Fail("Fails"));

            // AuthorizationFilterContext
            var actionContext = new ActionContext(
                httpContext: httpContext.Object,
                routeData: new RouteData(),
                actionDescriptor: new ActionDescriptor());

            var authorizationContext = new Filters.AuthorizationFilterContext(
                actionContext,
                Enumerable.Empty<IFilterMetadata>().ToList()
            );

            return authorizationContext;
        }
    }
}
