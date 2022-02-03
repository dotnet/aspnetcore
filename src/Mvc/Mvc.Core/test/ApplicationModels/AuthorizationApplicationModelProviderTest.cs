// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

public class AuthorizationApplicationModelProviderTest
{
    private readonly IOptions<MvcOptions> OptionsWithoutEndpointRouting = Options.Create(new MvcOptions { EnableEndpointRouting = false });

    [Fact]
    public void OnProvidersExecuting_AuthorizeAttribute_DoesNothing_WhenEnableRoutingIsEnabled()
    {
        // Arrange
        var provider = new AuthorizationApplicationModelProvider(
            new DefaultAuthorizationPolicyProvider(Options.Create(new AuthorizationOptions())),
            Options.Create(new MvcOptions()));
        var controllerType = typeof(AccountController);
        var context = CreateProviderContext(controllerType);

        // Act
        provider.OnProvidersExecuting(context);

        // Assert
        var controller = Assert.Single(context.Result.Controllers);
        Assert.Empty(controller.Filters);
    }

    [Fact]
    public void OnProvidersExecuting_AllowAnonymousAttribute_DoesNothing_WhenEnableRoutingIsEnabled()
    {
        // Arrange
        var provider = new AuthorizationApplicationModelProvider(
            new DefaultAuthorizationPolicyProvider(Options.Create(new AuthorizationOptions())),
            Options.Create(new MvcOptions()));
        var controllerType = typeof(AnonymousController);
        var context = CreateProviderContext(controllerType);

        // Act
        provider.OnProvidersExecuting(context);

        // Assert
        var controller = Assert.Single(context.Result.Controllers);
        Assert.Empty(controller.Filters);
    }

    [Fact]
    public void CreateControllerModel_AuthorizeAttributeAddsAuthorizeFilter()
    {
        // Arrange
        var provider = new AuthorizationApplicationModelProvider(
            new DefaultAuthorizationPolicyProvider(Options.Create(new AuthorizationOptions())),
            OptionsWithoutEndpointRouting);
        var controllerType = typeof(AccountController);
        var context = CreateProviderContext(controllerType);

        // Act
        provider.OnProvidersExecuting(context);

        // Assert
        var controller = Assert.Single(context.Result.Controllers);
        Assert.Single(controller.Filters, f => f is AuthorizeFilter);
    }

    [Fact]
    public void BuildActionModels_BaseAuthorizeFiltersAreStillValidWhenOverriden()
    {
        // Arrange
        var options = Options.Create(new AuthorizationOptions());
        options.Value.AddPolicy("Base", policy => policy.RequireClaim("Basic").RequireClaim("Basic2"));
        options.Value.AddPolicy("Derived", policy => policy.RequireClaim("Derived"));

        var provider = new AuthorizationApplicationModelProvider(
            new DefaultAuthorizationPolicyProvider(options),
            OptionsWithoutEndpointRouting);
        var context = CreateProviderContext(typeof(DerivedController));

        // Act
        provider.OnProvidersExecuting(context);

        // Assert
        var controller = Assert.Single(context.Result.Controllers);
        var action = Assert.Single(controller.Actions);
        Assert.Equal("Authorize", action.ActionName);

        var attributeRoutes = action.Selectors.Where(sm => sm.AttributeRouteModel != null);
        Assert.Empty(attributeRoutes);
        var authorizeFilters = action.Filters.OfType<AuthorizeFilter>();
        Assert.Single(authorizeFilters);

        Assert.NotNull(authorizeFilters.First().Policy);
        Assert.Equal(3, authorizeFilters.First().Policy.Requirements.Count()); // Basic + Basic2 + Derived authorize
    }

    [Fact]
    public void CreateControllerModelAndActionModel_AllowAnonymousAttributeAddsAllowAnonymousFilter()
    {
        // Arrange
        var provider = new AuthorizationApplicationModelProvider(
            new DefaultAuthorizationPolicyProvider(Options.Create(new AuthorizationOptions())),
            OptionsWithoutEndpointRouting);
        var context = CreateProviderContext(typeof(AnonymousController));

        // Act
        provider.OnProvidersExecuting(context);

        // Assert
        var controller = Assert.Single(context.Result.Controllers);
        Assert.Single(controller.Filters, f => f is AllowAnonymousFilter);
        var action = Assert.Single(controller.Actions);
        Assert.Single(action.Filters, f => f is AllowAnonymousFilter);
    }

    [Fact]
    public void OnProvidersExecuting_DefaultPolicyProvider_NoAuthorizationData_NoFilterCreated()
    {
        // Arrange
        var requirements = new IAuthorizationRequirement[]
        {
                new AssertionRequirement((con) => { return true; })
        };
        var authorizationPolicy = new AuthorizationPolicy(requirements, new string[] { "dingos" });
        var authOptions = Options.Create(new AuthorizationOptions());
        authOptions.Value.AddPolicy("Base", authorizationPolicy);
        var policyProvider = new DefaultAuthorizationPolicyProvider(authOptions);

        var provider = new AuthorizationApplicationModelProvider(policyProvider, OptionsWithoutEndpointRouting);
        var context = CreateProviderContext(typeof(BaseController));

        // Act
        var action = GetBaseControllerActionModel(provider);

        // Assert
        var authorizationFilter = Assert.IsType<AuthorizeFilter>(Assert.Single(action.Filters));
        Assert.NotNull(authorizationFilter.Policy);
        Assert.Null(authorizationFilter.AuthorizeData);
        Assert.Null(authorizationFilter.PolicyProvider);
    }

    [Fact]
    public void OnProvidersExecuting_NonDefaultPolicyProvider_HasNoPolicy_HasPolicyProviderAndAuthorizeData()
    {
        // Arrange
        var requirements = new IAuthorizationRequirement[]
        {
                new AssertionRequirement((con) => { return true; })
        };
        var authorizationPolicy = new AuthorizationPolicy(requirements, new string[] { "dingos" });
        var authorizationPolicyProviderMock = new Mock<IAuthorizationPolicyProvider>();
        authorizationPolicyProviderMock
            .Setup(s => s.GetPolicyAsync(It.IsAny<string>()))
            .Returns(Task.FromResult(authorizationPolicy))
            .Verifiable();

        var provider = new AuthorizationApplicationModelProvider(authorizationPolicyProviderMock.Object, OptionsWithoutEndpointRouting);

        // Act
        var action = GetBaseControllerActionModel(provider);

        // Assert
        var actionFilter = Assert.IsType<AuthorizeFilter>(Assert.Single(action.Filters));
        Assert.Null(actionFilter.Policy);
        Assert.NotNull(actionFilter.AuthorizeData);
        Assert.NotNull(actionFilter.PolicyProvider);
    }

    [Fact]
    public void CreateControllerModelAndActionModel_NoAuthNoFilter()
    {
        // Arrange
        var provider = new AuthorizationApplicationModelProvider(
            new DefaultAuthorizationPolicyProvider(Options.Create(new AuthorizationOptions())),
            OptionsWithoutEndpointRouting);
        var context = CreateProviderContext(typeof(NoAuthController));

        // Act
        provider.OnProvidersExecuting(context);

        // Assert
        var controller = Assert.Single(context.Result.Controllers);
        Assert.Empty(controller.Filters);
        var action = Assert.Single(controller.Actions);
        Assert.Empty(action.Filters);
    }

    private ActionModel GetBaseControllerActionModel(AuthorizationApplicationModelProvider authorizationApplicationModelProvider)
    {
        var context = CreateProviderContext(typeof(BaseController));

        authorizationApplicationModelProvider.OnProvidersExecuting(context);

        var controller = Assert.Single(context.Result.Controllers);
        Assert.Empty(controller.Filters);
        var action = Assert.Single(controller.Actions);

        return action;
    }

    private static ApplicationModelProviderContext CreateProviderContext(Type controllerType)
    {
        var defaultProvider = new DefaultApplicationModelProvider(
            Options.Create(new MvcOptions()),
            new EmptyModelMetadataProvider());

        var context = new ApplicationModelProviderContext(new[] { controllerType.GetTypeInfo() });
        defaultProvider.OnProvidersExecuting(context);
        return context;
    }

    private class BaseController
    {
        [Authorize(Policy = "Base")]
        public virtual void Authorize()
        {
        }
    }

    private class DerivedController : BaseController
    {
        [Authorize(Policy = "Derived")]
        public override void Authorize()
        {
        }
    }

    [Authorize]
    public class AccountController
    {
    }

    public class NoAuthController
    {
        public void NoAuthAction()
        { }
    }

    [AllowAnonymous]
    public class AnonymousController
    {
        [AllowAnonymous]
        public void SomeAction()
        {
        }
    }
}
