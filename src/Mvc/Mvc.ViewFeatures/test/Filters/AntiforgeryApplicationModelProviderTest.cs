// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Reflection;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc.Core.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

public class AntiforgeryApplicationModelProviderTest
{
    [Fact]
    public void WorksWithAttributesOnAction()
    {
        var provider = new AntiforgeryApplicationModelProvider(
            Options.Create(new MvcOptions()),
            NullLogger<AntiforgeryMiddlewareAuthorizationFilter>.Instance);
        var context = CreateProviderContext(typeof(TestController));

        // Act
        provider.OnProvidersExecuting(context);

        // Assert
        var controller = Assert.Single(context.Result.Controllers);
        Assert.Collection(controller.Actions,
            model =>
            {
                Assert.Equal(nameof(TestController.WithAntiforgeryMetadata), model.ActionName);
                Assert.IsType<AntiforgeryMiddlewareAuthorizationFilter>(Assert.Single(model.Filters));
            },
            model =>
            {
                Assert.Equal(nameof(TestController.WithMvcAttribute), model.ActionName);
                Assert.IsType<ValidateAntiForgeryTokenAttribute>(Assert.Single(model.Filters));
            },
            model =>
            {
                Assert.Equal(nameof(TestController.NoAttributes), model.ActionName);
                Assert.Empty(model.Filters);
            });
    }

    [Theory]
    [InlineData(typeof(AntiforgeryMetadataController), typeof(AntiforgeryMiddlewareAuthorizationFilter))]
    [InlineData(typeof(MvcAttributeController), typeof(ValidateAntiForgeryTokenAttribute))]
    [InlineData(typeof(EmptyController), null)]
    public void WorksWithAttributesOnController(Type controllerType, Type filterType)
    {
        var provider = new AntiforgeryApplicationModelProvider(
            Options.Create(new MvcOptions()),
            NullLogger<AntiforgeryMiddlewareAuthorizationFilter>.Instance);
        var context = CreateProviderContext(controllerType);

        // Act
        provider.OnProvidersExecuting(context);

        // Assert
        var controller = Assert.Single(context.Result.Controllers);
        if (filterType is not null)
        {
            var filter = Assert.Single(controller.Filters);
            Assert.IsType(filterType, filter);
        }
        else
        {
            Assert.Empty(controller.Filters);
        }
    }

    [Theory]
    [InlineData(typeof(DerivedAntiforgeryMetadataController), typeof(AntiforgeryMiddlewareAuthorizationFilter))]
    [InlineData(typeof(DerivedMvcAttributeController), typeof(ValidateAntiForgeryTokenAttribute))]
    [InlineData(typeof(DerivedEmptyController), null)]
    public void WorksWithAttributesOnDerivedController(Type controllerType, Type filterType)
    {
        var provider = new AntiforgeryApplicationModelProvider(
            Options.Create(new MvcOptions()),
            NullLogger<AntiforgeryMiddlewareAuthorizationFilter>.Instance);
        var context = CreateProviderContext(controllerType);

        // Act
        provider.OnProvidersExecuting(context);

        // Assert
        var controller = Assert.Single(context.Result.Controllers);
        if (filterType is not null)
        {
            var filter = Assert.Single(controller.Filters);
            Assert.IsType(filterType, filter);
        }
        else
        {
            Assert.Empty(controller.Filters);
        }
    }

    [Fact]
    public void WorksWithMismatchedRequiresValidationOnControllersAndActions()
    {
        var provider = new AntiforgeryApplicationModelProvider(
            Options.Create(new MvcOptions()),
            NullLogger<AntiforgeryMiddlewareAuthorizationFilter>.Instance);
        var context = CreateProviderContext(typeof(AntiforgeryMetadataWithActionsController));

        // Act
        provider.OnProvidersExecuting(context);

        // Assert
        var controller = Assert.Single(context.Result.Controllers);
        Assert.Collection(controller.Actions,
            model =>
            {
                Assert.Equal(nameof(AntiforgeryMetadataWithActionsController.Post), model.ActionName);
                Assert.Empty(model.Filters);
            },
            model =>
            {
                Assert.Equal(nameof(AntiforgeryMetadataWithActionsController.Post2), model.ActionName);
                Assert.Empty(model.Filters);
            });
    }

    [Theory]
    [InlineData(typeof(DerivedAntiforgeryMetadataMvcAttributeController))]
    [InlineData(typeof(AntiforgeryMetadataMvcAttributeController))]
    public void ThrowsIfMultipleAntiforgeryAttributesAreApplied(Type controllerType)
    {
        var provider = new AntiforgeryApplicationModelProvider(
            Options.Create(new MvcOptions()),
            NullLogger<AntiforgeryMiddlewareAuthorizationFilter>.Instance);
        var context = CreateProviderContext(controllerType);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => provider.OnProvidersExecuting(context));
        Assert.Equal(
            $"Cannot apply [{nameof(ValidateAntiForgeryTokenAttribute)}] and [{nameof(RequireAntiforgeryTokenAttribute)}] at the same time.",
            exception.Message);
    }

    [Fact]
    public void IgnoreAntiforgeryTokenOnAction_AddsValidationNotRequiredEndpointMetadata()
    {
        var provider = new AntiforgeryApplicationModelProvider(
            Options.Create(new MvcOptions()),
            NullLogger<AntiforgeryMiddlewareAuthorizationFilter>.Instance);
        var context = CreateProviderContext(typeof(IgnoreAntiforgeryTokenOnActionController));

        // Act
        provider.OnProvidersExecuting(context);

        // Assert
        var controller = Assert.Single(context.Result.Controllers);
        Assert.Collection(controller.Actions,
            ignoredAction =>
            {
                Assert.Equal(nameof(IgnoreAntiforgeryTokenOnActionController.Ignored), ignoredAction.ActionName);
                var selector = Assert.Single(ignoredAction.Selectors);
                var metadata = selector.EndpointMetadata.OfType<IAntiforgeryMetadata>().LastOrDefault();
                Assert.NotNull(metadata);
                Assert.False(metadata.RequiresValidation);
            },
            normalAction =>
            {
                Assert.Equal(nameof(IgnoreAntiforgeryTokenOnActionController.Normal), normalAction.ActionName);
                var selector = Assert.Single(normalAction.Selectors);
                Assert.Empty(selector.EndpointMetadata.OfType<IAntiforgeryMetadata>());
            });
    }

    [Fact]
    public void IgnoreAntiforgeryTokenOnController_AddsValidationNotRequiredEndpointMetadataToControllerSelectors()
    {
        var provider = new AntiforgeryApplicationModelProvider(
            Options.Create(new MvcOptions()),
            NullLogger<AntiforgeryMiddlewareAuthorizationFilter>.Instance);
        var context = CreateProviderContext(typeof(IgnoreAntiforgeryTokenOnControllerController));

        // Act
        provider.OnProvidersExecuting(context);

        // Assert
        var controller = Assert.Single(context.Result.Controllers);
        var controllerSelector = Assert.Single(controller.Selectors);
        var metadata = controllerSelector.EndpointMetadata.OfType<IAntiforgeryMetadata>().LastOrDefault();
        Assert.NotNull(metadata);
        Assert.False(metadata.RequiresValidation);

        // The action's own selectors should still be untouched; the framework's
        // ActionAttributeRouteModel.FlattenSelectors merges controller metadata into them later.
        var action = Assert.Single(controller.Actions);
        var actionSelector = Assert.Single(action.Selectors);
        Assert.Empty(actionSelector.EndpointMetadata.OfType<IAntiforgeryMetadata>());
    }

    [Fact]
    public void IgnoreAntiforgeryTokenOnAction_OverridesAutoValidateAntiforgeryTokenOnController()
    {
        var provider = new AntiforgeryApplicationModelProvider(
            Options.Create(new MvcOptions()),
            NullLogger<AntiforgeryMiddlewareAuthorizationFilter>.Instance);
        var context = CreateProviderContext(typeof(AutoValidateWithIgnoredActionController));

        // Act
        provider.OnProvidersExecuting(context);

        // Assert: action selector ends with a not-required IAntiforgeryMetadata so GetMetadata<T>()
        // (last-wins) reports the action-level preference. Controller-level metadata is merged
        // before action-level metadata by ActionAttributeRouteModel.FlattenSelectors.
        var controller = Assert.Single(context.Result.Controllers);
        var action = Assert.Single(controller.Actions);
        var actionSelector = Assert.Single(action.Selectors);
        var lastMetadata = actionSelector.EndpointMetadata.OfType<IAntiforgeryMetadata>().LastOrDefault();
        Assert.NotNull(lastMetadata);
        Assert.False(lastMetadata.RequiresValidation);
    }

    [Fact]
    public void NoAntiforgeryAttributes_DoesNotAddEndpointMetadata()
    {
        var provider = new AntiforgeryApplicationModelProvider(
            Options.Create(new MvcOptions()),
            NullLogger<AntiforgeryMiddlewareAuthorizationFilter>.Instance);
        var context = CreateProviderContext(typeof(EmptyController));

        // Act
        provider.OnProvidersExecuting(context);

        // Assert
        var controller = Assert.Single(context.Result.Controllers);
        foreach (var selector in controller.Selectors)
        {
            Assert.Empty(selector.EndpointMetadata.OfType<IAntiforgeryMetadata>());
        }
        var action = Assert.Single(controller.Actions);
        foreach (var selector in action.Selectors)
        {
            Assert.Empty(selector.EndpointMetadata.OfType<IAntiforgeryMetadata>());
        }
    }

    [Fact]
    public void EnableEndpointRoutingDisabled_DoesNotAddEndpointMetadata()
    {
        var provider = new AntiforgeryApplicationModelProvider(
            Options.Create(new MvcOptions { EnableEndpointRouting = false }),
            NullLogger<AntiforgeryMiddlewareAuthorizationFilter>.Instance);
        var context = CreateProviderContext(typeof(IgnoreAntiforgeryTokenOnControllerController));

        // Act
        provider.OnProvidersExecuting(context);

        // Assert
        var controller = Assert.Single(context.Result.Controllers);
        foreach (var selector in controller.Selectors)
        {
            Assert.Empty(selector.EndpointMetadata.OfType<IAntiforgeryMetadata>());
        }
    }

    private static ApplicationModelProviderContext CreateProviderContext(Type controllerType)
    {
        var defaultProvider = new DefaultApplicationModelProvider(
            Options.Create(new MvcOptions()),
            new EmptyModelMetadataProvider());

        var context = new ApplicationModelProviderContext(new[]
        {
            controllerType.GetTypeInfo()
        });
        defaultProvider.OnProvidersExecuting(context);
        return context;
    }

    private static ActionModel GetActionModel(
        string actionName,
        object[] actionAttributes = null,
        object[] controllerAttributes = null)
    {
        actionAttributes ??= Array.Empty<object>();
        controllerAttributes ??= Array.Empty<object>();

        var controllerModel = new ControllerModel(typeof(TestController).GetTypeInfo(), controllerAttributes);
        var actionModel = new ActionModel(typeof(TestController).GetMethod(actionName), actionAttributes)
        {
            Controller = controllerModel,
        };

        controllerModel.Actions.Add(actionModel);

        return actionModel;
    }

    private class TestController
    {
        [HttpPost("with-antiforgery-metadata")]
        [RequireAntiforgeryToken]
        public IActionResult WithAntiforgeryMetadata() => null;

        [HttpPost("with-mvc-attribute")]
        [ValidateAntiForgeryToken]
        public IActionResult WithMvcAttribute() => null;

        [HttpPost("with-mvc-attribute")]
        public IActionResult NoAttributes() => null;
    }

    [RequireAntiforgeryToken]
    private class AntiforgeryMetadataController
    {
        [HttpPost]
        public IActionResult Post() => null;
    }

    private class DerivedAntiforgeryMetadataController : AntiforgeryMetadataController
    {
    }

    [ValidateAntiForgeryToken]
    private class MvcAttributeController
    {
        [HttpPost]
        public IActionResult Post() => null;
    }

    private class DerivedMvcAttributeController : MvcAttributeController
    {
    }

    [ValidateAntiForgeryToken]
    [RequireAntiforgeryToken]
    private class AntiforgeryMetadataMvcAttributeController
    {
        [HttpPost]
        public IActionResult Post() => null;
    }

    private class DerivedAntiforgeryMetadataMvcAttributeController : AntiforgeryMetadataMvcAttributeController
    {
    }

    private class EmptyController
    {
        [HttpPost]
        public IActionResult Post() => null;
    }

    private class DerivedEmptyController : EmptyController { }

    [RequireAntiforgeryToken]
    private class AntiforgeryMetadataWithActionsController
    {
        [HttpPost]
        public IActionResult Post() => null;

        [HttpPost]
        [RequireAntiforgeryToken(false)]
        public IActionResult Post2() => null;
    }

    private class IgnoreAntiforgeryTokenOnActionController
    {
        [HttpPost("ignored")]
        [IgnoreAntiforgeryToken]
        public IActionResult Ignored() => null;

        [HttpPost("normal")]
        public IActionResult Normal() => null;
    }

    [IgnoreAntiforgeryToken]
    private class IgnoreAntiforgeryTokenOnControllerController
    {
        [HttpPost]
        public IActionResult Post() => null;
    }

    [AutoValidateAntiforgeryToken]
    private class AutoValidateWithIgnoredActionController
    {
        [HttpPost("webhook")]
        [IgnoreAntiforgeryToken]
        public IActionResult Webhook() => null;
    }
}
