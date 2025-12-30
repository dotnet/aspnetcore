// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Authorization;

public class AuthorizeFilterTest
{
    private readonly ActionContext ActionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());

    [Fact]
    public void InvalidUser()
    {
        var authorizationContext = GetAuthorizationContext();
        Assert.Contains(authorizationContext.HttpContext.User.Identities, i => i.IsAuthenticated);
    }

    [Fact]
    public async Task DefaultConstructor_DeniesAnonymousUsers()
    {
        // Arrange
        var authorizationContext = GetAuthorizationContext(anonymous: true);

        // The type 'AuthorizeFilter' is both a filter by itself and also a filter factory.
        // The default filter provider first checks if a type is a filter factory and creates an instance of
        // this filter.
        var authorizeFilterFactory = new AuthorizeFilter();
        var filterFactory = authorizeFilterFactory as IFilterFactory;
        var authorizeFilter = (AuthorizeFilter)filterFactory.CreateInstance(
            authorizationContext.HttpContext.RequestServices);
        authorizationContext.Filters.Add(authorizeFilter);

        // Act
        await authorizeFilter.OnAuthorizationAsync(authorizationContext);

        // Assert
        Assert.IsType<ChallengeResult>(authorizationContext.Result);
    }

    [Fact]
    public async Task AuthorizeFilter_CreatedWithAuthorizeData_ThrowsWhenOnAuthorizationAsyncIsCalled()
    {
        // Arrange
        var authorizeFilter = new AuthorizeFilter(new[] { new AuthorizeAttribute() });
        var authorizationContext = GetAuthorizationContext();
        authorizationContext.Filters.Add(authorizeFilter);
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
        var authorizationContext = GetAuthorizationContext();
        authorizationContext.Filters.Add(authorizeFilter);
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
        var authorizationContext = GetAuthorizationContext(anonymous: true);
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
        var authorizationContext = GetAuthorizationContext();
        authorizationContext.Filters.Add(authorizeFilter);

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
        var authorizationContext = GetAuthorizationContext(anonymous: true);

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
        var authorizationContext = GetAuthorizationContext();

        // Act
        await authorizeFilter.OnAuthorizationAsync(authorizationContext);

        // Assert
        Assert.Null(authorizationContext.Result);
    }

    [Fact]
    public async Task Invoke_EmptyClaimsShouldChallengeAnonymousUser()
    {
        // Arrange
        var authorizeFilter = new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        var authorizationContext = GetAuthorizationContext(anonymous: true);
        authorizationContext.Filters.Add(authorizeFilter);

        // Act
        await authorizeFilter.OnAuthorizationAsync(authorizationContext);

        // Assert
        Assert.IsType<ChallengeResult>(authorizationContext.Result);
    }

    [Fact]
    public async Task Invoke_EmptyClaimsWithAllowAnonymousAttributeShouldNotRejectAnonymousUser()
    {
        // Arrange
        var authorizeFilter = new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        var authorizationContext = GetAuthorizationContext(anonymous: true);

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
        var authorizationContext = GetAuthorizationContext();

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
        var authorizationContext = GetAuthorizationContext();

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
        var authorizationContext = GetAuthorizationContext();

        // Act
        await authorizeFilter.OnAuthorizationAsync(authorizationContext);

        // Assert
        Assert.Null(authorizationContext.Result);
    }

    private class TestPolicyProvider : IAuthorizationPolicyProvider
    {
        private readonly AuthorizationPolicy _true =
            new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build();
        private readonly AuthorizationPolicy _false =
            new AuthorizationPolicyBuilder().RequireAssertion(_ => false).Build();

        public int GetPolicyCalls = 0;

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
            => Task.FromResult(_true);

        public Task<AuthorizationPolicy> GetPolicyAsync(string policyName)
        {
            GetPolicyCalls++;
            return Task.FromResult(policyName == "true" ? _true : _false);
        }

        public Task<AuthorizationPolicy> GetFallbackPolicyAsync()
            => Task.FromResult<AuthorizationPolicy>(null);
    }

    [Fact]
    public async Task AuthorizationFilterCombinesMultipleFiltersWithPolicyProvider()
    {
        // Arrange
        var authorizeFilter = new AuthorizeFilter(new TestPolicyProvider(), new IAuthorizeData[]
        {
                new AuthorizeAttribute { Policy = "true"},
                new AuthorizeAttribute { Policy = "false"}
        });
        var authorizationContext = GetAuthorizationContext(anonymous: false);
        // Effective policy should fail, if both are combined
        authorizationContext.Filters.Add(authorizeFilter);
        var secondFilter = new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireAssertion(a => true).Build());
        authorizationContext.Filters.Add(secondFilter);

        // Act
        await secondFilter.OnAuthorizationAsync(authorizationContext);

        // Assert
        Assert.IsType<ForbidResult>(authorizationContext.Result);
    }

    [Fact]
    public async Task AuthorizationFilterCombinesMultipleFiltersWithDifferentPolicyProvider()
    {
        // Arrange
        var testProvider1 = new TestPolicyProvider();
        var testProvider2 = new TestPolicyProvider();
        var authorizeFilter = new AuthorizeFilter(testProvider1, new IAuthorizeData[]
        {
                new AuthorizeAttribute { Policy = "true"},
                new AuthorizeAttribute { Policy = "false"}
        });
        var authorizationContext = GetAuthorizationContext(anonymous: false);
        // Effective policy should fail, if both are combined
        authorizationContext.Filters.Add(authorizeFilter);
        var secondFilter = new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireAssertion(a => true).Build());
        authorizationContext.Filters.Add(secondFilter);
        var thirdFilter = new AuthorizeFilter(testProvider2, new IAuthorizeData[] { new AuthorizeAttribute(policy: "something") });
        authorizationContext.Filters.Add(thirdFilter);

        // Act
        await thirdFilter.OnAuthorizationAsync(authorizationContext);

        // Assert
        Assert.IsType<ForbidResult>(authorizationContext.Result);
        Assert.Equal(2, testProvider1.GetPolicyCalls);
        Assert.Equal(1, testProvider2.GetPolicyCalls);

        // Make sure the policy calls are not cached
        await thirdFilter.OnAuthorizationAsync(authorizationContext);

        // Assert
        Assert.IsType<ForbidResult>(authorizationContext.Result);
        Assert.Equal(4, testProvider1.GetPolicyCalls);
        Assert.Equal(2, testProvider2.GetPolicyCalls);
    }

    [Fact]
    public async Task AuthorizationFilterCombinesMultipleFilters()
    {
        // Arrange
        var authorizeFilter = new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireAssertion(a => false).Build());
        var authorizationContext = GetAuthorizationContext(anonymous: false);
        // Effective policy should fail, if both are combined
        authorizationContext.Filters.Add(authorizeFilter);
        var secondFilter = new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireAssertion(a => true).Build());
        authorizationContext.Filters.Add(secondFilter);

        // Act
        await secondFilter.OnAuthorizationAsync(authorizationContext);

        // Assert
        Assert.IsType<ForbidResult>(authorizationContext.Result);
    }

    [Fact]
    public async Task AuthorizationFilterIgnoresFirstFilterWhenCombining()
    {
        // Arrange
        var authorizeFilter = new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireAssertion(a => false).Build());
        var authorizationContext = GetAuthorizationContext(anonymous: false);
        // Effective policy should fail, if both are combined
        authorizationContext.Filters.Add(authorizeFilter);
        var secondFilter = new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireAssertion(a => false).Build());
        authorizationContext.Filters.Add(secondFilter);

        // Act
        await authorizeFilter.OnAuthorizationAsync(authorizationContext);

        // Assert
        Assert.Null(authorizationContext.Result);
    }

    [Fact]
    public async Task AuthorizationFilterCombinesDerivedFilters()
    {
        // Arrange
        var authorizeFilter = new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireAssertion(a => true).Build());
        var authorizationContext = GetAuthorizationContext(anonymous: false);
        // Effective policy should fail, if both are combined
        authorizationContext.Filters.Add(authorizeFilter);
        authorizationContext.Filters.Add(new DerivedAuthorizeFilter());
        authorizationContext.Filters.Add(new DerivedAuthorizeFilter());
        var lastFilter = new DerivedAuthorizeFilter();
        authorizationContext.Filters.Add(lastFilter);

        // Act
        await lastFilter.OnAuthorizationAsync(authorizationContext);

        // Assert
        Assert.IsType<ForbidResult>(authorizationContext.Result);
    }

    public class DerivedAuthorizeFilter : AuthorizeFilter
    {
        public DerivedAuthorizeFilter() : base(new AuthorizationPolicyBuilder().RequireAssertion(a => false).Build())
        { }
    }

    [Fact]
    public async Task AuthZResourceShouldBeAuthorizationFilterContext()
    {
        // Arrange
        var authorizeFilter = new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireAssertion(c => c.Resource is AuthorizationFilterContext).Build());
        var authorizationContext = GetAuthorizationContext();

        // Act
        await authorizeFilter.OnAuthorizationAsync(authorizationContext);

        // Assert
        Assert.Null(authorizationContext.Result);
    }

    [Fact]
    public async Task Invoke_RequireUnknownRoleShouldForbid()
    {
        // Arrange
        var authorizeFilter = new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireRole("Wut").Build());
        var authorizationContext = GetAuthorizationContext();
        authorizationContext.Filters.Add(authorizeFilter);

        // Act
        await authorizeFilter.OnAuthorizationAsync(authorizationContext);

        // Assert
        Assert.IsType<ForbidResult>(authorizationContext.Result);
    }

    [Fact]
    public async Task Invoke_InvalidClaimShouldForbid()
    {
        // Arrange
        var authorizeFilter = new AuthorizeFilter(new AuthorizationPolicyBuilder()
            .RequireClaim("Permission", "CanViewComment")
            .Build());
        var authorizationContext = GetAuthorizationContext();
        authorizationContext.Filters.Add(authorizeFilter);

        // Act
        await authorizeFilter.OnAuthorizationAsync(authorizationContext);

        // Assert
        Assert.IsType<ForbidResult>(authorizationContext.Result);
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

    public static TheoryData<AuthorizeFilter> AuthorizeFiltersCreatedWithoutPolicyOrPolicyProvider
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

    [Fact]
    public async Task GetEffectivePolicyAsync_CombinesPoliciesFromAuthFilters()
    {
        // Arrange
        var policy1 = new AuthorizationPolicyBuilder()
            .RequireClaim("Claim1")
            .Build();

        var policy2 = new AuthorizationPolicyBuilder()
            .RequireClaim("Claim2")
            .Build();
        var filter1 = new AuthorizeFilter(policy1);
        var filter2 = new AuthorizeFilter(policy2);

        var context = new AuthorizationFilterContext(ActionContext, new[] { filter1, filter2 });

        // Act
        var effectivePolicy = await filter1.GetEffectivePolicyAsync(context);

        // Assert
        Assert.NotSame(policy1, effectivePolicy);
        Assert.NotSame(policy2, effectivePolicy);
        Assert.Equal(new[] { "Claim1", "Claim2" }, effectivePolicy.Requirements.Cast<ClaimsAuthorizationRequirement>().Select(c => c.ClaimType));
    }

    [Fact]
    public async Task GetEffectivePolicyAsync_CombinesPoliciesFromEndpoint()
    {
        // Arrange
        var policy1 = new AuthorizationPolicyBuilder()
            .RequireClaim("Claim1")
            .Build();

        var policy2 = new AuthorizationPolicyBuilder()
            .RequireClaim("Claim2")
            .Build();

        var filter = new AuthorizeFilter(policy1);
        var options = new AuthorizationOptions();
        options.AddPolicy("policy2", policy2);
        var policyProvider = new DefaultAuthorizationPolicyProvider(Options.Create(options));

        ActionContext.HttpContext.RequestServices = new ServiceCollection()
            .AddSingleton<IAuthorizationPolicyProvider>(policyProvider)
            .BuildServiceProvider();

        ActionContext.HttpContext.SetEndpoint(new Endpoint(
            _ => null,
            new EndpointMetadataCollection(new AuthorizeAttribute("policy2")),
            "test"));
        var context = new AuthorizationFilterContext(ActionContext, new[] { filter, });

        // Act
        var effectivePolicy = await filter.GetEffectivePolicyAsync(context);

        // Assert
        Assert.NotSame(policy1, effectivePolicy);
        Assert.NotSame(policy2, effectivePolicy);
        Assert.Equal(new[] { "Claim1", "Claim2" }, effectivePolicy.Requirements.Cast<ClaimsAuthorizationRequirement>().Select(c => c.ClaimType));
    }

    private AuthorizationFilterContext GetAuthorizationContext(
        bool anonymous = false,
        Action<IServiceCollection> registerServices = null)
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

        serviceCollection.AddOptions();
        serviceCollection.AddLogging();
        serviceCollection.AddSingleton(auth.Object);
        serviceCollection.AddAuthorization();
        registerServices?.Invoke(serviceCollection);

        var serviceProvider = serviceCollection.BuildServiceProvider();

        // HttpContext
        var httpContext = new Mock<HttpContext>();
        auth.Setup(c => c.AuthenticateAsync(httpContext.Object, "Bearer")).ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(bearerPrincipal, "Bearer")));
        auth.Setup(c => c.AuthenticateAsync(httpContext.Object, "Basic")).ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(basicPrincipal, "Basic")));
        auth.Setup(c => c.AuthenticateAsync(httpContext.Object, "Fails")).ReturnsAsync(AuthenticateResult.Fail("Fails"));
        httpContext.SetupProperty(c => c.User);
        if (!anonymous)
        {
            httpContext.Object.User = validUser;
        }
        httpContext.SetupGet(c => c.RequestServices).Returns(serviceProvider);
        var contextItems = new Dictionary<object, object>();
        httpContext.SetupGet(c => c.Items).Returns(contextItems);
        httpContext.SetupGet(c => c.Features).Returns(Mock.Of<IFeatureCollection>());

        // AuthorizationFilterContext
        var actionContext = new ActionContext(
            httpContext: httpContext.Object,
            routeData: new RouteData(),
            actionDescriptor: new ActionDescriptor());

        var authorizationContext = new AuthorizationFilterContext(
            actionContext,
            Enumerable.Empty<IFilterMetadata>().ToList()
        );

        return authorizationContext;
    }
}
