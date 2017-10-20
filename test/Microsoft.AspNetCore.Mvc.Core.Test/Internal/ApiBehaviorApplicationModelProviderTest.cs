// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class ApiBehaviorApplicationModelProviderTest
    {
        [Fact]
        public void OnProvidersExecuting_AddsModelStateInvalidFilter_IfTypeIsAnnotatedWithAttribute()
        {
            // Arrange
            var context = GetContext(typeof(TestApiController));
            var provider = GetProvider();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            var actionModel = Assert.Single(Assert.Single(context.Result.Controllers).Actions);
            Assert.IsType<ModelStateInvalidFilter>(actionModel.Filters.Last());
        }

        [Fact]
        public void OnProvidersExecuting_DoesNotAddModelStateInvalidFilterToController_IfFeatureIsDisabledViaOptions()
        {
            // Arrange
            var context = GetContext(typeof(TestApiController));
            var options = new ApiBehaviorOptions
            {
                SuppressModelStateInvalidFilter = true,
            };

            var provider = GetProvider(options);

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            var controllerModel = Assert.Single(context.Result.Controllers);
            Assert.DoesNotContain(typeof(ModelStateInvalidFilter), controllerModel.Filters.Select(f => f.GetType()));
        }

        [Fact]
        public void OnProvidersExecuting_AddsModelStateInvalidFilter_IfActionIsAnnotatedWithAttribute()
        {
            // Arrange
            var context = GetContext(typeof(SimpleController));
            var provider = GetProvider();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.Collection(
                Assert.Single(context.Result.Controllers).Actions.OrderBy(a => a.ActionName),
                action =>
                {
                    Assert.Contains(typeof(ModelStateInvalidFilter), action.Filters.Select(f => f.GetType()));
                },
                action =>
                {
                    Assert.DoesNotContain(typeof(ModelStateInvalidFilter), action.Filters.Select(f => f.GetType()));
                });
        }

        [Fact]
        public void OnProvidersExecuting_SkipsAddingFilterToActionIfFeatureIsDisabledUsingOptions()
        {
            // Arrange
            var context = GetContext(typeof(SimpleController));
            var options = new ApiBehaviorOptions
            {
                SuppressModelStateInvalidFilter = true,
            };

            var provider = GetProvider(options);

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.Collection(
                Assert.Single(context.Result.Controllers).Actions.OrderBy(a => a.ActionName),
                action =>
                {
                    Assert.DoesNotContain(typeof(ModelStateInvalidFilter), action.Filters.Select(f => f.GetType()));
                },
                action =>
                {
                    Assert.DoesNotContain(typeof(ModelStateInvalidFilter), action.Filters.Select(f => f.GetType()));
                });
        }

        [Fact]
        public void OnProvidersExecuting_MakesControllerVisibleInApiExplorer_IfItIsAnnotatedWithAttribute()
        {
            // Arrange
            var context = GetContext(typeof(TestApiController));
            var options = new ApiBehaviorOptions
            {
                SuppressModelStateInvalidFilter = true,
            };

            var provider = GetProvider(options);

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            var controller = Assert.Single(context.Result.Controllers);
            Assert.True(controller.ApiExplorer.IsVisible);
        }

        [Fact]
        public void OnProvidersExecuting_DoesNotModifyVisibilityInApiExplorer_IfValueIsAlreadySet()
        {
            // Arrange
            var context = GetContext(typeof(TestApiController));
            context.Result.Controllers[0].ApiExplorer.IsVisible = false;
            var options = new ApiBehaviorOptions
            {
                SuppressModelStateInvalidFilter = true,
            };

            var provider = GetProvider(options);

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            var controller = Assert.Single(context.Result.Controllers);
            Assert.False(controller.ApiExplorer.IsVisible);
        }

        [Fact]
        public void OnProvidersExecuting_ThrowsIfControllerWithAttribute_HasActionsWithoutAttributeRouting()
        {
            // Arrange
            var context = GetContext(typeof(ActionsWithoutAttributeRouting));
            var provider = GetProvider();

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => provider.OnProvidersExecuting(context));
            Assert.Equal(
                "Action methods on controllers annotated with ApiControllerAttribute must have an attribute route.",
                ex.Message);
        }

        [Fact]
        public void InferBindingSourceForParameter_ReturnsPath_IfParameterNameExistsInRouteAsSimpleToken()
        {
            // Arrange
            var actionName = nameof(ParameterBindingController.SimpleRouteToken);
            var parameter = GetParameterModel(typeof(ParameterBindingController), actionName);
            var provider = GetProvider();

            // Act
            var result = provider.InferBindingSourceForParameter(parameter);

            // Assert
            Assert.Same(BindingSource.Path, result);
        }

        [Fact]
        public void InferBindingSourceForParameter_ReturnsPath_IfParameterNameExistsInRouteAsOptionalToken()
        {
            // Arrange
            var actionName = nameof(ParameterBindingController.OptionalRouteToken);
            var parameter = GetParameterModel(typeof(ParameterBindingController), actionName);
            var provider = GetProvider();

            // Act
            var result = provider.InferBindingSourceForParameter(parameter);

            // Assert
            Assert.Same(BindingSource.Path, result);
        }

        [Fact]
        public void InferBindingSourceForParameter_ReturnsPath_IfParameterNameExistsInRouteAsConstrainedToken()
        {
            // Arrange
            var actionName = nameof(ParameterBindingController.ConstrainedRouteToken);
            var parameter = GetParameterModel(typeof(ParameterBindingController), actionName);
            var provider = GetProvider();

            // Act
            var result = provider.InferBindingSourceForParameter(parameter);

            // Assert
            Assert.Same(BindingSource.Path, result);
        }

        [Fact]
        public void InferBindingSourceForParameter_ReturnsPath_IfParameterNameExistsInAbsoluteRoute()
        {
            // Arrange
            var actionName = nameof(ParameterBindingController.AbsoluteRoute);
            var parameter = GetParameterModel(typeof(ParameterBindingController), actionName);
            var provider = GetProvider();

            // Act
            var result = provider.InferBindingSourceForParameter(parameter);

            // Assert
            Assert.Same(BindingSource.Path, result);
        }

        [Fact]
        public void InferBindingSourceForParameter_ReturnsPath_IfParameterAppearsInAllRoutes()
        {
            // Arrange
            var actionName = nameof(ParameterBindingController.ParameterInMultipleRoutes);
            var parameter = GetParameterModel(typeof(ParameterBindingController), actionName);
            var provider = GetProvider();

            // Act
            var result = provider.InferBindingSourceForParameter(parameter);

            // Assert
            Assert.Same(BindingSource.Path, result);
        }

        [Fact]
        public void InferBindingSourceForParameter_DoesNotReturnPath_IfParameterDoesNotAppearInAllRoutes()
        {
            // Arrange
            var actionName = nameof(ParameterBindingController.ParameterNotInAllRoutes);
            var parameter = GetParameterModel(typeof(ParameterBindingController), actionName);
            var provider = GetProvider();

            // Act
            var result = provider.InferBindingSourceForParameter(parameter);

            // Assert
            Assert.Same(BindingSource.Query, result);
        }

        [Fact]
        public void InferBindingSourceForParameter_ReturnsPath_IfParameterAppearsInControllerRoute()
        {
            // Arrange
            var actionName = nameof(ParameterInController.ActionWithoutRoute);
            var parameter = GetParameterModel(typeof(ParameterInController), actionName);
            var provider = GetProvider();

            // Act
            var result = provider.InferBindingSourceForParameter(parameter);

            // Assert
            Assert.Same(BindingSource.Path, result);
        }

        [Fact]
        public void InferBindingSourceForParameter_ReturnsPath_IfParameterAppearsInControllerRoute_AndActionHasRoute()
        {
            // Arrange
            var actionName = nameof(ParameterInController.ActionWithRoute);
            var parameter = GetParameterModel(typeof(ParameterInController), actionName);
            var provider = GetProvider();

            // Act
            var result = provider.InferBindingSourceForParameter(parameter);

            // Assert
            Assert.Same(BindingSource.Path, result);
        }

        [Fact]
        public void InferBindingSourceForParameter_ReturnsPath_IfParameterAppearsInAllActionRoutes()
        {
            // Arrange
            var actionName = nameof(ParameterInController.MultipleRoute);
            var parameter = GetParameterModel(typeof(ParameterInController), actionName);
            var provider = GetProvider();

            // Act
            var result = provider.InferBindingSourceForParameter(parameter);

            // Assert
            Assert.Same(BindingSource.Path, result);
        }

        [Fact]
        public void InferBindingSourceForParameter_DoesNotReturnPath_IfActionRouteOverridesControllerRoute()
        {
            // Arrange
            var actionName = nameof(ParameterInController.AbsoluteRoute);
            var parameter = GetParameterModel(typeof(ParameterInController), actionName);
            var provider = GetProvider();

            // Act
            var result = provider.InferBindingSourceForParameter(parameter);

            // Assert
            Assert.Same(BindingSource.Query, result);
        }

        [Fact]
        public void InferBindingSourceForParameter_DoesNotReturnPath_IfOneActionRouteOverridesControllerRoute()
        {
            // Arrange
            var actionName = nameof(ParameterInController.MultipleRouteWithOverride);
            var parameter = GetParameterModel(typeof(ParameterInController), actionName);
            var provider = GetProvider();

            // Act
            var result = provider.InferBindingSourceForParameter(parameter);

            // Assert
            Assert.Same(BindingSource.Query, result);
        }

        [Fact]
        public void InferBindingSourceForParameter_ReturnsPath_IfParameterExistsInRoute_OnControllersWithoutSelectors()
        {
            // Arrange
            var actionName = nameof(ParameterBindingNoRoutesOnController.SimpleRoute);
            var parameter = GetParameterModel(typeof(ParameterBindingNoRoutesOnController), actionName);
            var provider = GetProvider();

            // Act
            var result = provider.InferBindingSourceForParameter(parameter);

            // Assert
            Assert.Same(BindingSource.Path, result);
        }

        [Fact]
        public void InferBindingSourceForParameter_ReturnsPath_IfParameterExistsInAllRoutes_OnControllersWithoutSelectors()
        {
            // Arrange
            var actionName = nameof(ParameterBindingNoRoutesOnController.ParameterInMultipleRoutes);
            var parameter = GetParameterModel(typeof(ParameterBindingNoRoutesOnController), actionName);
            var provider = GetProvider();

            // Act
            var result = provider.InferBindingSourceForParameter(parameter);

            // Assert
            Assert.Same(BindingSource.Path, result);
        }

        [Fact]
        public void InferBindingSourceForParameter_DoesNotReturnPath_IfNeitherActionNorControllerHasTemplate()
        {
            // Arrange
            var actionName = nameof(ParameterBindingNoRoutesOnController.NoRouteTemplate);
            var parameter = GetParameterModel(typeof(ParameterBindingNoRoutesOnController), actionName);
            var provider = GetProvider();

            // Act
            var result = provider.InferBindingSourceForParameter(parameter);

            // Assert
            Assert.Same(BindingSource.Query, result);
        }

        [Fact]
        public void InferBindingSourceForParameter_ReturnsBodyForComplexTypes()
        {
            // Arrange
            var actionName = nameof(ParameterBindingController.ComplexTypeModel);
            var parameter = GetParameterModel(typeof(ParameterBindingController), actionName);
            var provider = GetProvider();

            // Act
            var result = provider.InferBindingSourceForParameter(parameter);

            // Assert
            Assert.Same(BindingSource.Body, result);
        }

        [Fact]
        public void InferBindingSourceForParameter_ReturnsBodyForSimpleTypes()
        {
            // Arrange
            var actionName = nameof(ParameterBindingController.SimpleTypeModel);
            var parameter = GetParameterModel(typeof(ParameterBindingController), actionName);
            var provider = GetProvider();

            // Act
            var result = provider.InferBindingSourceForParameter(parameter);

            // Assert
            Assert.Same(BindingSource.Query, result);
        }

        [Fact]
        public void AddMultipartFormDataConsumesAttribute_NoOpsIfBehaviorIsDisabled()
        {
            // Arrange
            var actionName = nameof(ParameterBindingController.FromFormParameter);
            var action = GetActionModel(typeof(ParameterBindingController), actionName);
            var options = new ApiBehaviorOptions
            {
                SuppressConsumesConstraintForFormFileParameters = true,
                InvalidModelStateResponseFactory = _ => null,
            };
            var provider = GetProvider(options);

            // Act
            provider.AddMultipartFormDataConsumesAttribute(action);

            // Assert
            Assert.Empty(action.Filters);
        }

        [Fact]
        public void AddMultipartFormDataConsumesAttribute_NoOpsIfConsumesConstraintIsAlreadyPresent()
        {
            // Arrange
            var actionName = nameof(ParameterBindingController.ActionWithConsumesAttribute);
            var action = GetActionModel(typeof(ParameterBindingController), actionName);
            var options = new ApiBehaviorOptions
            {
                SuppressConsumesConstraintForFormFileParameters = true,
                InvalidModelStateResponseFactory = _ => null,
            };
            var provider = GetProvider(options);

            // Act
            provider.AddMultipartFormDataConsumesAttribute(action);

            // Assert
            var attribute = Assert.Single(action.Filters);
            var consumesAttribute = Assert.IsType<ConsumesAttribute>(attribute);
            Assert.Equal("application/json", Assert.Single(consumesAttribute.ContentTypes));
        }

        [Fact]
        public void AddMultipartFormDataConsumesAttribute_AddsConsumesAttribute_WhenActionHasFromFormFileParameter()
        {
            // Arrange
            var actionName = nameof(ParameterBindingController.FormFileParameter);
            var action = GetActionModel(typeof(ParameterBindingController), actionName);
            action.Parameters[0].BindingInfo = new BindingInfo
            {
                BindingSource = BindingSource.FormFile,
            };
            var provider = GetProvider();

            // Act
            provider.AddMultipartFormDataConsumesAttribute(action);

            // Assert
            var attribute = Assert.Single(action.Filters);
            var consumesAttribute = Assert.IsType<ConsumesAttribute>(attribute);
            Assert.Equal("multipart/form-data", Assert.Single(consumesAttribute.ContentTypes));
        }

        private static ApiBehaviorApplicationModelProvider GetProvider(
            ApiBehaviorOptions options = null,
            IModelMetadataProvider modelMetadataProvider = null)
        {
            options = options ?? new ApiBehaviorOptions
            {
                InvalidModelStateResponseFactory = _ => null,
            };
            var optionsProvider = Options.Create(options);
            modelMetadataProvider = modelMetadataProvider ?? new TestModelMetadataProvider();
            var loggerFactory = NullLoggerFactory.Instance;

            return new ApiBehaviorApplicationModelProvider(optionsProvider, modelMetadataProvider, loggerFactory);
        }

        private static ApplicationModelProviderContext GetContext(Type type)
        {
            var context = new ApplicationModelProviderContext(new[] { type.GetTypeInfo() });
            new DefaultApplicationModelProvider(Options.Create(new MvcOptions())).OnProvidersExecuting(context);
            return context;
        }

        private static ActionModel GetActionModel(Type controllerType, string actionName)
        {
            var context = GetContext(controllerType);
            var controller = Assert.Single(context.Result.Controllers);
            return Assert.Single(controller.Actions, m => m.ActionName == actionName);
        }

        private static ParameterModel GetParameterModel(Type controllerType, string actionName)
        {
            var action = GetActionModel(controllerType, actionName);
            return Assert.Single(action.Parameters);
        }

        [ApiController]
        [Route("TestApi")]
        private class TestApiController : Controller
        {
            [HttpGet]
            public IActionResult TestAction() => null;
        }

        private class SimpleController : Controller
        {
            public IActionResult ActionWithoutFilter() => null;

            [TestApiBehavior]
            [HttpGet("/Simple/ActionWithFilter")]
            public IActionResult ActionWithFilter() => null;
        }

        [ApiController]
        private class ActionsWithoutAttributeRouting
        {
            public IActionResult Index() => null;
        }

        [AttributeUsage(AttributeTargets.Method)]
        private class TestApiBehavior : Attribute, IApiBehaviorMetadata
        {
        }

        [ApiController]
        [Route("[controller]/[action]")]
        private class ParameterBindingController
        {
            [HttpGet("{parameter}")]
            public IActionResult ActionWithBoundParameter([FromBody] object parameter) => null;

            [HttpGet("{id}")]
            public IActionResult SimpleRouteToken(int id) => null;

            [HttpPost("optional/{id?}")]
            public IActionResult OptionalRouteToken(int id) => null;

            [HttpDelete("delete-by-status/{status:int?}")]
            public IActionResult ConstrainedRouteToken(object status) => null;

            [HttpPut("/absolute-route/{status:int}")]
            public IActionResult AbsoluteRoute(object status) => null;

            [HttpPost("multiple/{id}")]
            [HttpPut("multiple/{id}")]
            public IActionResult ParameterInMultipleRoutes(int id) => null;

            [HttpPatch("patchroute")]
            [HttpPost("multiple/{id}")]
            [HttpPut("multiple/{id}")]
            public IActionResult ParameterNotInAllRoutes(int id) => null;

            [HttpPut("put-action/{id}")]
            public IActionResult ComplexTypeModel(TestModel model) => null;

            [HttpPut("put-action/{id}")]
            public IActionResult SimpleTypeModel(ConvertibleFromString model) => null;

            [HttpPost("form-file")]
            public IActionResult FormFileParameter(IFormFile formFile) => null;

            [HttpPost("form-file-collection")]
            public IActionResult FormFileCollectionParameter(IFormFileCollection formFiles) => null;

            [HttpPost("form-file-sequence")]
            public IActionResult FormFileSequenceParameter(IFormFile[] formFiles) => null;

            [HttpPost]
            public IActionResult FromFormParameter([FromForm] string parameter) => null;

            [HttpPost]
            [Consumes("application/json")]
            public IActionResult ActionWithConsumesAttribute([FromForm] string parameter) => null;
        }

        [ApiController]
        [Route("/route1/[controller]/[action]/{id}")]
        [Route("/route2/[controller]/[action]/{id?}")]
        private class ParameterInController
        {
            [HttpGet]
            public IActionResult ActionWithoutRoute(int id) => null;

            [HttpGet("stuff/{status}")]
            public IActionResult ActionWithRoute(int id) => null;

            [HttpGet("/absolute-route")]
            public IActionResult AbsoluteRoute(int id) => null;

            [HttpPut]
            [HttpPost("stuff/{status}")]
            public IActionResult MultipleRoute(int id) => null;

            [HttpPut]
            [HttpPost("~/stuff/{status}")]
            public IActionResult MultipleRouteWithOverride(int id) => null;
        }

        [ApiController]
        private class ParameterBindingNoRoutesOnController
        {
            [HttpGet("{parameter}")]
            public IActionResult SimpleRoute(int parameter) => null;

            [HttpGet]
            public IActionResult NoRouteTemplate(int id) => null;

            [HttpPost("multiple/{id}")]
            [HttpPut("multiple/{id}")]
            public IActionResult ParameterInMultipleRoutes(int id) => null;
        }

        private class TestModel { }

        [TypeConverter(typeof(ConvertibleFromStringConverter))]
        private class ConvertibleFromString { }

        private class ConvertibleFromStringConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
                => sourceType == typeof(string);
        }
    }
}
