// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests;

public class AuthorizeFilterIntegrationTest
{
    // This is a test for security, because we can't assume that any IAuthorizationPolicyProvider other than
    // DefaultAuthorizationPolicyProvider will return the same result for the same input. So a cache could cause
    // undesired access.
    [Fact]
    public async Task AuthorizeFilter_CalledTwiceWithNonDefaultProvider()
    {
        // Arrange
        var applicationModelProviderContext = GetProviderContext(typeof(AuthorizeController));

        var policyProvider = new TestAuthorizationPolicyProvider();

        var controller = Assert.Single(applicationModelProviderContext.Result.Controllers);
        var action = Assert.Single(controller.Actions);
        var authorizeData = action.Attributes.OfType<AuthorizeAttribute>();
        var authorizeFilter = new AuthorizeFilter(policyProvider, authorizeData);

        var actionContext = new ActionContext(GetHttpContext(), new RouteData(), new ControllerActionDescriptor());

        var authorizationFilterContext = new AuthorizationFilterContext(actionContext, new[] { authorizeFilter });

        // Act
        await authorizeFilter.OnAuthorizationAsync(authorizationFilterContext);
        await authorizeFilter.OnAuthorizationAsync(authorizationFilterContext);

        // Assert
        Assert.Equal(2, policyProvider.GetPolicyCount);
    }

    [Fact]
    public async Task AuthorizeFilter_CalledTwiceWithDefaultProvider()
    {
        // Arrange
        var applicationModelProviderContext = GetProviderContext(typeof(AuthorizeController));

        var policy = new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build();
        var policyProvider = new Mock<DefaultAuthorizationPolicyProvider>(Options.Create<AuthorizationOptions>(new AuthorizationOptions()));
        var getPolicyCalled = 0;
        policyProvider.Setup(p => p.GetPolicyAsync(It.IsAny<string>())).Callback(() => getPolicyCalled++).ReturnsAsync(policy);

        var controller = Assert.Single(applicationModelProviderContext.Result.Controllers);
        var action = Assert.Single(controller.Actions);
        var authorizeData = action.Attributes.OfType<AuthorizeAttribute>();
        var authorizeFilter = new AuthorizeFilter(policyProvider.Object, authorizeData);

        var actionContext = new ActionContext(GetHttpContext(), new RouteData(), new ControllerActionDescriptor());

        var authorizationFilterContext = new AuthorizationFilterContext(actionContext, new[] { authorizeFilter });

        // Act
        await authorizeFilter.OnAuthorizationAsync(authorizationFilterContext);
        await authorizeFilter.OnAuthorizationAsync(authorizationFilterContext);

        // Assert
        Assert.Equal(2, getPolicyCalled);
    }

    // This is a test for security, because we can't assume that any IAuthorizationPolicyProvider other than
    // DefaultAuthorizationPolicyProvider will return the same result for the same input. So a cache could cause
    // undesired access.
    [Fact]
    public async Task CombinedAuthorizeFilter_AlwaysCalledWithDefaultProvider()
    {
        // Arrange
        var applicationModelProviderContext = GetProviderContext(typeof(AuthorizeController));

        var policy = new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build();
        var policyProvider = new Mock<DefaultAuthorizationPolicyProvider>(Options.Create<AuthorizationOptions>(new AuthorizationOptions()));
        var getPolicyCalled = 0;
        policyProvider.Setup(p => p.GetPolicyAsync(It.IsAny<string>())).Callback(() => getPolicyCalled++).ReturnsAsync(policy);

        var controller = Assert.Single(applicationModelProviderContext.Result.Controllers);
        var action = Assert.Single(controller.Actions);
        var authorizeData = action.Attributes.OfType<AuthorizeAttribute>();
        var authorizeFilter = new AuthorizeFilter(policyProvider.Object, authorizeData);

        var actionContext = new ActionContext(GetHttpContext(), new RouteData(), new ControllerActionDescriptor());

        var authorizationFilterContext = new AuthorizationFilterContext(actionContext, action.Filters);

        authorizationFilterContext.Filters.Add(authorizeFilter);

        var secondFilter = new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireAssertion(a => true).Build());
        authorizationFilterContext.Filters.Add(secondFilter);

        var thirdFilter = new AuthorizeFilter(policyProvider.Object, authorizeData);
        authorizationFilterContext.Filters.Add(thirdFilter);

        // Act
        await thirdFilter.OnAuthorizationAsync(authorizationFilterContext);
        await thirdFilter.OnAuthorizationAsync(authorizationFilterContext);

        // Assert
        Assert.Equal(4, getPolicyCalled);
    }

    // This is a test for security, because we can't assume that any IAuthorizationPolicyProvider other than
    // DefaultAuthorizationPolicyProvider will return the same result for the same input. So a cache could cause
    // undesired access.
    [Fact]
    public async Task CombinedAuthorizeFilter_AlwaysCalledWithNonDefaultProvider()
    {
        // Arrange
        var applicationModelProviderContext = GetProviderContext(typeof(AuthorizeController));

        var policyProvider = new TestAuthorizationPolicyProvider();

        var controller = Assert.Single(applicationModelProviderContext.Result.Controllers);
        var action = Assert.Single(controller.Actions);
        var authorizeData = action.Attributes.OfType<AuthorizeAttribute>();
        var authorizeFilter = new AuthorizeFilter(policyProvider, authorizeData);

        var actionContext = new ActionContext(GetHttpContext(), new RouteData(), new ControllerActionDescriptor());

        var authorizationFilterContext = new AuthorizationFilterContext(actionContext, action.Filters);

        authorizationFilterContext.Filters.Add(authorizeFilter);

        var secondFilter = new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireAssertion(a => true).Build());
        authorizationFilterContext.Filters.Add(secondFilter);

        var thirdFilter = new AuthorizeFilter(policyProvider, authorizeData);
        authorizationFilterContext.Filters.Add(thirdFilter);

        // Act
        await thirdFilter.OnAuthorizationAsync(authorizationFilterContext);
        await thirdFilter.OnAuthorizationAsync(authorizationFilterContext);

        // Assert
        Assert.Equal(4, policyProvider.GetPolicyCount);
    }

    private HttpContext GetHttpContext()
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = GetServices()
        };

        return httpContext;
    }

    private static ApplicationModelProviderContext GetProviderContext(Type controllerType)
    {
        var context = new ApplicationModelProviderContext(new[] { controllerType.GetTypeInfo() });
        var provider = new DefaultApplicationModelProvider(
            Options.Create(new MvcOptions()),
            TestModelMetadataProvider.CreateDefaultProvider());
        provider.OnProvidersExecuting(context);

        return context;
    }

    private static IServiceProvider GetServices()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddAuthorization();
        serviceCollection.AddMvc();
        serviceCollection
            .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
            .AddTransient<ILogger<DefaultAuthorizationService>, Logger<DefaultAuthorizationService>>();
        return serviceCollection.BuildServiceProvider();
    }

    public class TestAuthorizationPolicyProvider : IAuthorizationPolicyProvider
    {
        public int GetPolicyCount = 0;

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        {
            throw new NotImplementedException();
        }

        public Task<AuthorizationPolicy> GetPolicyAsync(string policyName)
        {
            GetPolicyCount++;

            var requirements = new IAuthorizationRequirement[]
            {
                    new AssertionRequirement((con) => { return true; })
            };
            return Task.FromResult(new AuthorizationPolicy(requirements, new string[] { }));
        }

        public Task<AuthorizationPolicy> GetFallbackPolicyAsync()
        {
            return Task.FromResult<AuthorizationPolicy>(null);
        }
    }

    public class AuthorizeController
    {
        [Authorize(Policy = "Base")]
        public virtual void Authorize()
        { }
    }
}
