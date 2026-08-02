// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Cors;

public class CorsApplicationModelProviderTest
{
    private readonly IOptions<MvcOptions> OptionsWithoutEndpointRouting = Options.Create(new MvcOptions { EnableEndpointRouting = false });

    [Fact]
    public void OnProvidersExecuting_WithoutGlobalAuthorizationFilter_EnableCorsAttributeAddsCorsAuthorizationFilterFactory()
    {
        // Arrange
        var corsProvider = new CorsApplicationModelProvider(OptionsWithoutEndpointRouting);
        var context = GetProviderContext(typeof(CorsController));

        // Act
        corsProvider.OnProvidersExecuting(context);

        // Assert
        var model = Assert.Single(context.Result.Controllers);
        Assert.Single(model.Filters, f => f is CorsAuthorizationFilterFactory);
        var action = Assert.Single(model.Actions);
        var selector = Assert.Single(action.Selectors);
        var constraint = Assert.Single(selector.ActionConstraints, c => c is HttpMethodActionConstraint);
        Assert.IsType<CorsHttpMethodActionConstraint>(constraint);
    }

    [Fact]
    public void OnProvidersExecuting_WithoutGlobalAuthorizationFilter_DisableCorsAttributeAddsDisableCorsAuthorizationFilter()
    {
        // Arrange
        var corsProvider = new CorsApplicationModelProvider(OptionsWithoutEndpointRouting);
        var context = GetProviderContext(typeof(DisableCorsController));

        // Act
        corsProvider.OnProvidersExecuting(context);

        // Assert
        var model = Assert.Single(context.Result.Controllers);
        Assert.Single(model.Filters, f => f is DisableCorsAuthorizationFilter);
        var action = Assert.Single(model.Actions);
        var selector = Assert.Single(action.Selectors);
        var constraint = Assert.Single(selector.ActionConstraints, c => c is HttpMethodActionConstraint);
        Assert.IsType<CorsHttpMethodActionConstraint>(constraint);
    }

    [Fact]
    public void OnProvidersExecuting_WithoutGlobalAuthorizationFilter_CustomCorsFilter_EnablesCorsPreflight()
    {
        // Arrange
        var corsProvider = new CorsApplicationModelProvider(OptionsWithoutEndpointRouting);
        var context = GetProviderContext(typeof(CustomCorsFilterController));

        // Act
        corsProvider.OnProvidersExecuting(context);

        // Assert
        var controller = Assert.Single(context.Result.Controllers);
        var action = Assert.Single(controller.Actions);
        var selector = Assert.Single(action.Selectors);
        var constraint = Assert.Single(selector.ActionConstraints, c => c is HttpMethodActionConstraint);
        Assert.IsType<CorsHttpMethodActionConstraint>(constraint);
    }

    [Fact]
    public void BuildActionModel_EnableCorsAttributeAddsCorsAuthorizationFilterFactory()
    {
        // Arrange
        var corsProvider = new CorsApplicationModelProvider(OptionsWithoutEndpointRouting);
        var context = GetProviderContext(typeof(EnableCorsController));

        // Act
        corsProvider.OnProvidersExecuting(context);

        // Assert
        var controller = Assert.Single(context.Result.Controllers);
        var action = Assert.Single(controller.Actions);
        Assert.Single(action.Filters, f => f is CorsAuthorizationFilterFactory);
        var selector = Assert.Single(action.Selectors);
        var constraint = Assert.Single(selector.ActionConstraints, c => c is HttpMethodActionConstraint);
        Assert.IsType<CorsHttpMethodActionConstraint>(constraint);
    }

    [Fact]
    public void BuildActionModel_WithoutGlobalAuthorizationFilter_DisableCorsAttributeAddsDisableCorsAuthorizationFilter()
    {
        // Arrange
        var corsProvider = new CorsApplicationModelProvider(OptionsWithoutEndpointRouting);
        var context = GetProviderContext(typeof(DisableCorsActionController));

        // Act
        corsProvider.OnProvidersExecuting(context);

        // Assert
        var controller = Assert.Single(context.Result.Controllers);
        var action = Assert.Single(controller.Actions);
        Assert.Contains(action.Filters, f => f is DisableCorsAuthorizationFilter);
        var selector = Assert.Single(action.Selectors);
        var constraint = Assert.Single(selector.ActionConstraints, c => c is HttpMethodActionConstraint);
        Assert.IsType<CorsHttpMethodActionConstraint>(constraint);
    }

    [Fact]
    public void BuildActionModel_WithoutGlobalAuthorizationFilter_CustomCorsAuthorizationFilterOnAction_EnablesCorsPreflight()
    {
        // Arrange
        var corsProvider = new CorsApplicationModelProvider(OptionsWithoutEndpointRouting);
        var context = GetProviderContext(typeof(CustomCorsFilterOnActionController));

        // Act
        corsProvider.OnProvidersExecuting(context);

        // Assert
        var controller = Assert.Single(context.Result.Controllers);
        var action = Assert.Single(controller.Actions);
        var selector = Assert.Single(action.Selectors);
        var constraint = Assert.Single(selector.ActionConstraints, c => c is HttpMethodActionConstraint);
        Assert.IsType<CorsHttpMethodActionConstraint>(constraint);
    }

    [Fact]
    public void OnProvidersExecuting_WithoutGlobalAuthorizationFilter_EnableCorsGloballyEnablesCorsPreflight()
    {
        // Arrange
        var corsProvider = new CorsApplicationModelProvider(OptionsWithoutEndpointRouting);
        var context = GetProviderContext(typeof(RegularController));

        context.Result.Filters.Add(
            new CorsAuthorizationFilter(Mock.Of<ICorsService>(), Mock.Of<ICorsPolicyProvider>(), Mock.Of<ILoggerFactory>()));

        // Act
        corsProvider.OnProvidersExecuting(context);

        // Assert
        var model = Assert.Single(context.Result.Controllers);
        var action = Assert.Single(model.Actions);
        var selector = Assert.Single(action.Selectors);
        var constraint = Assert.Single(selector.ActionConstraints, c => c is HttpMethodActionConstraint);
        Assert.IsType<CorsHttpMethodActionConstraint>(constraint);
    }

    [Fact]
    public void OnProvidersExecuting_WithoutGlobalAuthorizationFilter_DisableCorsGloballyEnablesCorsPreflight()
    {
        // Arrange
        var corsProvider = new CorsApplicationModelProvider(OptionsWithoutEndpointRouting);
        var context = GetProviderContext(typeof(RegularController));
        context.Result.Filters.Add(new DisableCorsAuthorizationFilter());

        // Act
        corsProvider.OnProvidersExecuting(context);

        // Assert
        var model = Assert.Single(context.Result.Controllers);
        var action = Assert.Single(model.Actions);
        var selector = Assert.Single(action.Selectors);
        var constraint = Assert.Single(selector.ActionConstraints, c => c is HttpMethodActionConstraint);
        Assert.IsType<CorsHttpMethodActionConstraint>(constraint);
    }

    [Fact]
    public void OnProvidersExecuting_WithoutGlobalAuthorizationFilter_CustomCorsFilterGloballyEnablesCorsPreflight()
    {
        // Arrange
        var corsProvider = new CorsApplicationModelProvider(OptionsWithoutEndpointRouting);
        var context = GetProviderContext(typeof(RegularController));
        context.Result.Filters.Add(new CustomCorsFilterAttribute());

        // Act
        corsProvider.OnProvidersExecuting(context);

        // Assert
        var model = Assert.Single(context.Result.Controllers);
        var action = Assert.Single(model.Actions);
        var selector = Assert.Single(action.Selectors);
        var constraint = Assert.Single(selector.ActionConstraints, c => c is HttpMethodActionConstraint);
        Assert.IsType<CorsHttpMethodActionConstraint>(constraint);
    }

    [Fact]
    public void OnProvidersExecuting_WithoutGlobalAuthorizationFilter_CorsNotInUseDoesNotOverrideHttpConstraints()
    {
        // Arrange
        var corsProvider = new CorsApplicationModelProvider(OptionsWithoutEndpointRouting);
        var context = GetProviderContext(typeof(RegularController));

        // Act
        corsProvider.OnProvidersExecuting(context);

        // Assert
        var model = Assert.Single(context.Result.Controllers);
        var action = Assert.Single(model.Actions);
        var selector = Assert.Single(action.Selectors);
        var constraint = Assert.Single(selector.ActionConstraints, c => c is HttpMethodActionConstraint);
        Assert.IsNotType<CorsHttpMethodActionConstraint>(constraint);
    }

    private static ApplicationModelProviderContext GetProviderContext(Type controllerType)
    {
        var context = new ApplicationModelProviderContext(new[] { controllerType.GetTypeInfo() });
        var provider = new DefaultApplicationModelProvider(
            Options.Create(new MvcOptions()),
            new EmptyModelMetadataProvider());
        provider.OnProvidersExecuting(context);

        return context;
    }

    private class EnableCorsController
    {
        [EnableCors("policy")]
        [HttpGet]
        public IActionResult Action()
        {
            return null;
        }
    }

    private class DisableCorsActionController
    {
        [DisableCors]
        [HttpGet]
        public void Action()
        {
        }
    }

    [EnableCors("policy")]
    public class CorsController
    {
        [HttpGet]
        public IActionResult Action()
        {
            return null;
        }
    }

    [DisableCors]
    public class DisableCorsController
    {
        [HttpOptions]
        public IActionResult Action()
        {
            return null;
        }
    }

    public class RegularController
    {
        [HttpPost]
        public IActionResult Action()
        {
            return null;
        }
    }

    [CustomCorsFilter]
    public class CustomCorsFilterController
    {
        [HttpPost]
        public IActionResult Action()
        {
            return null;
        }
    }

    public class CustomCorsFilterOnActionController
    {
        [HttpPost]
        [CustomCorsFilter]
        public IActionResult Action()
        {
            return null;
        }
    }

    public class CustomCorsFilterAttribute : Attribute, ICorsAuthorizationFilter
    {
        public int Order { get; } = 1000;

        public Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            return Task.FromResult(0);
        }
    }
}
