// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
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
            var actionName = $"{typeof(ActionsWithoutAttributeRouting).FullName}.{nameof(ActionsWithoutAttributeRouting.Index)} ({typeof(ActionsWithoutAttributeRouting).Assembly.GetName().Name})";
            var expected = $"Action '{actionName}' does not have an attribute route. Action methods on controllers annotated with ApiControllerAttribute must be attribute routed.";
            var context = GetContext(typeof(ActionsWithoutAttributeRouting));
            var provider = GetProvider();

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => provider.OnProvidersExecuting(context));
            Assert.Equal(expected, ex.Message);
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
        public void InferBindingSourceForParameter_ReturnsPath_IfParameterAppearsInAnyRoutes_MulitpleRoutes()
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
        public void InferBindingSourceForParameter_ReturnsPath_IfParameterAppearsInAnyRoute()
        {
            // Arrange
            var actionName = nameof(ParameterBindingController.ParameterNotInAllRoutes);
            var parameter = GetParameterModel(typeof(ParameterBindingController), actionName);
            var provider = GetProvider();

            // Act
            var result = provider.InferBindingSourceForParameter(parameter);

            // Assert
            Assert.Same(BindingSource.Path, result);
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
        public void InferBindingSourceForParameter_ReturnsPath_IfParameterPresentInNonOverriddenControllerRoute()
        {
            // Arrange
            var actionName = nameof(ParameterInController.MultipleRouteWithOverride);
            var parameter = GetParameterModel(typeof(ParameterInController), actionName);
            var provider = GetProvider();

            // Act
            var result = provider.InferBindingSourceForParameter(parameter);

            // Assert
            Assert.Same(BindingSource.Path, result);
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
        public void OnProvidersExecuting_DoesNotInferBindingSourceForParametersWithBindingInfo()
        {
            // Arrange
            var actionName = nameof(ParameterWithBindingInfo.Action);
            var provider = GetProvider();
            var context = GetContext(typeof(ParameterWithBindingInfo));

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            var controllerModel = Assert.Single(context.Result.Controllers);
            var actionModel = Assert.Single(controllerModel.Actions, a => a.ActionName == actionName);
            var parameterModel = Assert.Single(actionModel.Parameters);
            Assert.NotNull(parameterModel.BindingInfo);
            Assert.Same(BindingSource.Custom, parameterModel.BindingInfo.BindingSource);
        }

        [Fact]
        public void OnProvidersExecuting_Throws_IfMultipleParametersAreInferredAsBodyBound()
        {
            // Arrange
            var expected =
$@"Action '{typeof(ControllerWithMultipleInferredFromBodyParameters).FullName}.{nameof(ControllerWithMultipleInferredFromBodyParameters.Action)} ({typeof(ControllerWithMultipleInferredFromBodyParameters).Assembly.GetName().Name})' " +
"has more than one parameter that was specified or inferred as bound from request body. Only one parameter per action may be bound from body. Inspect the following parameters, and use 'FromQueryAttribute' to specify bound from query, 'FromRouteAttribute' to specify bound from route, and 'FromBodyAttribute' for parameters to be bound from body:" +
Environment.NewLine + "TestModel a" +
Environment.NewLine + "Car b";

            var context = GetContext(typeof(ControllerWithMultipleInferredFromBodyParameters));
            var provider = GetProvider();

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => provider.OnProvidersExecuting(context));
            Assert.Equal(expected, ex.Message);
        }

        [Fact]
        public void OnProvidersExecuting_Throws_IfMultipleParametersAreInferredOrSpecifiedAsBodyBound()
        {
            // Arrange
            var expected =
$@"Action '{typeof(ControllerWithMultipleInferredOrSpecifiedFromBodyParameters).FullName}.{nameof(ControllerWithMultipleInferredOrSpecifiedFromBodyParameters.Action)} ({typeof(ControllerWithMultipleInferredOrSpecifiedFromBodyParameters).Assembly.GetName().Name})' " +
"has more than one parameter that was specified or inferred as bound from request body. Only one parameter per action may be bound from body. Inspect the following parameters, and use 'FromQueryAttribute' to specify bound from query, 'FromRouteAttribute' to specify bound from route, and 'FromBodyAttribute' for parameters to be bound from body:" +
Environment.NewLine + "TestModel a" +
Environment.NewLine + "int b";

            var context = GetContext(typeof(ControllerWithMultipleInferredOrSpecifiedFromBodyParameters));
            var provider = GetProvider();

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => provider.OnProvidersExecuting(context));
            Assert.Equal(expected, ex.Message);
        }

        [Fact]
        public void OnProvidersExecuting_Throws_IfMultipleParametersAreFromBody()
        {
            // Arrange
            var expected =
$@"Action '{typeof(ControllerWithMultipleFromBodyParameters).FullName}.{nameof(ControllerWithMultipleFromBodyParameters.Action)} ({typeof(ControllerWithMultipleFromBodyParameters).Assembly.GetName().Name})' " +
"has more than one parameter that was specified or inferred as bound from request body. Only one parameter per action may be bound from body. Inspect the following parameters, and use 'FromQueryAttribute' to specify bound from query, 'FromRouteAttribute' to specify bound from route, and 'FromBodyAttribute' for parameters to be bound from body:" +
Environment.NewLine + "decimal a" +
Environment.NewLine + "int b";

            var context = GetContext(typeof(ControllerWithMultipleFromBodyParameters));
            var provider = GetProvider();

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => provider.OnProvidersExecuting(context));
            Assert.Equal(expected, ex.Message);
        }

        [Fact]
        public void OnProvidersExecuting_PreservesBindingInfo_WhenInferringFor_ParameterWithModelBinder_AndExplicitName()
        {
            // Arrange
            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var actionName = nameof(ModelBinderOnParameterController.ModelBinderAttributeWithExplicitModelName);
            var context = GetContext(typeof(ModelBinderOnParameterController), modelMetadataProvider);
            var provider = GetProvider();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            var controller = Assert.Single(context.Result.Controllers);
            var action = Assert.Single(controller.Actions, a => a.ActionName == actionName);
            var parameter = Assert.Single(action.Parameters);

            var bindingInfo = parameter.BindingInfo;
            Assert.NotNull(bindingInfo);
            Assert.Same(BindingSource.Query, bindingInfo.BindingSource);
            Assert.Equal("top", bindingInfo.BinderModelName);
        }

        [Fact]
        public void OnProvidersExecuting_PreservesBindingInfo_WhenInferringFor_ParameterWithModelBinderType()
        {
            // Arrange
            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var actionName = nameof(ModelBinderOnParameterController.ModelBinderType);
            var context = GetContext(typeof(ModelBinderOnParameterController), modelMetadataProvider);
            var provider = GetProvider();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            var controller = Assert.Single(context.Result.Controllers);
            var action = Assert.Single(controller.Actions, a => a.ActionName == actionName);
            var parameter = Assert.Single(action.Parameters);

            var bindingInfo = parameter.BindingInfo;
            Assert.NotNull(bindingInfo);
            Assert.Same(BindingSource.Custom, bindingInfo.BindingSource);
            Assert.Null(bindingInfo.BinderModelName);
        }

        [Fact]
        public void OnProvidersExecuting_PreservesBindingInfo_WhenInferringFor_ParameterWithModelBinderType_AndExplicitModelName()
        {
            // Arrange
            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var actionName = nameof(ModelBinderOnParameterController.ModelBinderTypeWithExplicitModelName);
            var context = GetContext(typeof(ModelBinderOnParameterController), modelMetadataProvider);
            var provider = GetProvider();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            var controller = Assert.Single(context.Result.Controllers);
            var action = Assert.Single(controller.Actions, a => a.ActionName == actionName);
            var parameter = Assert.Single(action.Parameters);

            var bindingInfo = parameter.BindingInfo;
            Assert.NotNull(bindingInfo);
            Assert.Same(BindingSource.Custom, bindingInfo.BindingSource);
            Assert.Equal("foo", bindingInfo.BinderModelName);
        }

        [Fact]
        public void PreservesBindingSourceInference_ForFromQueryParameter_WithDefaultName()
        {
            // Arrange
            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var actionName = nameof(ParameterBindingController.FromQuery);
            var context = GetContext(typeof(ParameterBindingController), modelMetadataProvider);
            var provider = GetProvider();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            var controller = Assert.Single(context.Result.Controllers);
            var action = Assert.Single(controller.Actions, a => a.ActionName == actionName);
            var parameter = Assert.Single(action.Parameters);

            var bindingInfo = parameter.BindingInfo;
            Assert.NotNull(bindingInfo);
            Assert.Same(BindingSource.Query, bindingInfo.BindingSource);
            Assert.Null(bindingInfo.BinderModelName);
        }

        [Fact]
        public void PreservesBindingSourceInference_ForFromQueryParameter_WithCustomName()
        {
            // Arrange
            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var actionName = nameof(ParameterBindingController.FromQueryWithCustomName);
            var context = GetContext(typeof(ParameterBindingController), modelMetadataProvider);
            var provider = GetProvider();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            var controller = Assert.Single(context.Result.Controllers);
            var action = Assert.Single(controller.Actions, a => a.ActionName == actionName);
            var parameter = Assert.Single(action.Parameters);

            var bindingInfo = parameter.BindingInfo;
            Assert.NotNull(bindingInfo);
            Assert.Same(BindingSource.Query, bindingInfo.BindingSource);
            Assert.Equal("top", bindingInfo.BinderModelName);
        }

        [Fact]
        public void PreservesBindingSourceInference_ForFromQueryParameterOnComplexType_WithDefaultName()
        {
            // Arrange
            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var actionName = nameof(ParameterBindingController.FromQueryOnComplexType);
            var context = GetContext(typeof(ParameterBindingController), modelMetadataProvider);
            var provider = GetProvider();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            var controller = Assert.Single(context.Result.Controllers);
            var action = Assert.Single(controller.Actions, a => a.ActionName == actionName);
            var parameter = Assert.Single(action.Parameters);

            var bindingInfo = parameter.BindingInfo;
            Assert.NotNull(bindingInfo);
            Assert.Same(BindingSource.Query, bindingInfo.BindingSource);
            Assert.Equal(string.Empty, bindingInfo.BinderModelName);
        }

        [Fact]
        public void PreservesBindingSourceInference_ForFromQueryParameterOnComplexType_WithCustomName()
        {
            // Arrange
            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var actionName = nameof(ParameterBindingController.FromQueryOnComplexTypeWithCustomName);
            var context = GetContext(typeof(ParameterBindingController), modelMetadataProvider);
            var provider = GetProvider();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            var controller = Assert.Single(context.Result.Controllers);
            var action = Assert.Single(controller.Actions, a => a.ActionName == actionName);
            var parameter = Assert.Single(action.Parameters);

            var bindingInfo = parameter.BindingInfo;
            Assert.NotNull(bindingInfo);
            Assert.Same(BindingSource.Query, bindingInfo.BindingSource);
            Assert.Equal("gps", bindingInfo.BinderModelName);
        }

        [Fact]
        public void PreservesBindingSourceInference_ForFromQueryParameterOnCollectionType()
        {
            // Arrange
            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var actionName = nameof(ParameterBindingController.FromQueryOnCollectionType);
            var context = GetContext(typeof(ParameterBindingController), modelMetadataProvider);
            var provider = GetProvider();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            var controller = Assert.Single(context.Result.Controllers);
            var action = Assert.Single(controller.Actions, a => a.ActionName == actionName);
            var parameter = Assert.Single(action.Parameters);

            var bindingInfo = parameter.BindingInfo;
            Assert.NotNull(bindingInfo);
            Assert.Same(BindingSource.Query, bindingInfo.BindingSource);
            Assert.Null(bindingInfo.BinderModelName);
        }

        [Fact]
        public void PreservesBindingSourceInference_ForFromQueryOnArrayType()
        {
            // Arrange
            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var actionName = nameof(ParameterBindingController.FromQueryOnArrayType);
            var context = GetContext(typeof(ParameterBindingController), modelMetadataProvider);
            var provider = GetProvider();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            var controller = Assert.Single(context.Result.Controllers);
            var action = Assert.Single(controller.Actions, a => a.ActionName == actionName);
            var parameter = Assert.Single(action.Parameters);

            var bindingInfo = parameter.BindingInfo;
            Assert.NotNull(bindingInfo);
            Assert.Same(BindingSource.Query, bindingInfo.BindingSource);
            Assert.Null(bindingInfo.BinderModelName);
        }

        [Fact]
        public void PreservesBindingSourceInference_FromQueryOnArrayTypeWithCustomName()
        {
            // Arrange
            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var actionName = nameof(ParameterBindingController.FromQueryOnArrayTypeWithCustomName);
            var context = GetContext(typeof(ParameterBindingController), modelMetadataProvider);
            var provider = GetProvider();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            var controller = Assert.Single(context.Result.Controllers);
            var action = Assert.Single(controller.Actions, a => a.ActionName == actionName);
            var parameter = Assert.Single(action.Parameters);

            var bindingInfo = parameter.BindingInfo;
            Assert.NotNull(bindingInfo);
            Assert.Same(BindingSource.Query, bindingInfo.BindingSource);
            Assert.Equal("ids", bindingInfo.BinderModelName);
        }

        [Fact]
        public void PreservesBindingSourceInference_ForFromRouteParameter_WithDefaultName()
        {
            // Arrange
            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var actionName = nameof(ParameterBindingController.FromRoute);
            var context = GetContext(typeof(ParameterBindingController), modelMetadataProvider);
            var provider = GetProvider();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            var controller = Assert.Single(context.Result.Controllers);
            var action = Assert.Single(controller.Actions, a => a.ActionName == actionName);
            var parameter = Assert.Single(action.Parameters);

            var bindingInfo = parameter.BindingInfo;
            Assert.NotNull(bindingInfo);
            Assert.Same(BindingSource.Path, bindingInfo.BindingSource);
            Assert.Null(bindingInfo.BinderModelName);
        }

        [Fact]
        public void PreservesBindingSourceInference_ForFromRouteParameter_WithCustomName()
        {
            // Arrange
            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var actionName = nameof(ParameterBindingController.FromRouteWithCustomName);
            var context = GetContext(typeof(ParameterBindingController), modelMetadataProvider);
            var provider = GetProvider();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            var controller = Assert.Single(context.Result.Controllers);
            var action = Assert.Single(controller.Actions, a => a.ActionName == actionName);
            var parameter = Assert.Single(action.Parameters);

            var bindingInfo = parameter.BindingInfo;
            Assert.NotNull(bindingInfo);
            Assert.Same(BindingSource.Path, bindingInfo.BindingSource);
            Assert.Equal("top", bindingInfo.BinderModelName);
        }

        [Fact]
        public void PreservesBindingSourceInference_ForFromRouteParameterOnComplexType_WithDefaultName()
        {
            // Arrange
            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var actionName = nameof(ParameterBindingController.FromRouteOnComplexType);
            var context = GetContext(typeof(ParameterBindingController), modelMetadataProvider);
            var provider = GetProvider();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            var controller = Assert.Single(context.Result.Controllers);
            var action = Assert.Single(controller.Actions, a => a.ActionName == actionName);
            var parameter = Assert.Single(action.Parameters);

            var bindingInfo = parameter.BindingInfo;
            Assert.NotNull(bindingInfo);
            Assert.Same(BindingSource.Path, bindingInfo.BindingSource);
            Assert.Equal(string.Empty, bindingInfo.BinderModelName);
        }

        [Fact]
        public void PreservesBindingSourceInference_ForFromRouteParameterOnComplexType_WithCustomName()
        {
            // Arrange
            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var actionName = nameof(ParameterBindingController.FromRouteOnComplexTypeWithCustomName);
            var context = GetContext(typeof(ParameterBindingController), modelMetadataProvider);
            var provider = GetProvider();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            var controller = Assert.Single(context.Result.Controllers);
            var action = Assert.Single(controller.Actions, a => a.ActionName == actionName);
            var parameter = Assert.Single(action.Parameters);

            var bindingInfo = parameter.BindingInfo;
            Assert.NotNull(bindingInfo);
            Assert.Same(BindingSource.Path, bindingInfo.BindingSource);
            Assert.Equal("gps", bindingInfo.BinderModelName);
        }

        [Fact]
        public void PreservesBindingSourceInference_ForParameterWithRequestPredicateAndPropertyFilterProvider()
        {
            // Arrange
            var expectedPredicate = CustomRequestPredicateAndPropertyFilterProviderAttribute.RequestPredicateStatic;
            var expectedPropertyFilter = CustomRequestPredicateAndPropertyFilterProviderAttribute.PropertyFilterStatic;
            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var actionName = nameof(ParameterBindingController.ParameterWithRequestPredicateProvider);
            var context = GetContext(typeof(ParameterBindingController), modelMetadataProvider);
            var provider = GetProvider();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            var controller = Assert.Single(context.Result.Controllers);
            var action = Assert.Single(controller.Actions, a => a.ActionName == actionName);
            var parameter = Assert.Single(action.Parameters);

            var bindingInfo = parameter.BindingInfo;
            Assert.NotNull(bindingInfo);
            Assert.Same(BindingSource.Query, bindingInfo.BindingSource);
            Assert.Same(expectedPredicate, bindingInfo.RequestPredicate);
            Assert.Same(expectedPropertyFilter, bindingInfo.PropertyFilterProvider.PropertyFilter);
            Assert.Null(bindingInfo.BinderModelName);
        }

        [Fact]
        public void InferParameterBindingSources_SetsCorrectBindingSourceForComplexTypesWithCancellationToken()
        {
            // Arrange
            var actionName = nameof(ParameterBindingController.ComplexTypeModelWithCancellationToken);

            // Use the default set of ModelMetadataProviders so we get metadata details for CancellationToken. 
            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var context = GetContext(typeof(ParameterBindingController), modelMetadataProvider);
            var controllerModel = Assert.Single(context.Result.Controllers);
            var actionModel = Assert.Single(controllerModel.Actions, m => m.ActionName == actionName);

            var provider = GetProvider();

            // Act
            provider.InferParameterBindingSources(actionModel);

            // Assert
            var model = GetParameterModel<TestModel>(actionModel);
            Assert.Same(BindingSource.Body, model.BindingInfo.BindingSource);

            var cancellationToken = GetParameterModel<CancellationToken>(actionModel);
            Assert.Same(BindingSource.Special, cancellationToken.BindingInfo.BindingSource);
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
        public void InferBoundPropertyModelPrefixes_SetsModelPrefix_ForComplexTypeFromValueProvider()
        {
            // Arrange
            var controller = GetControllerModel(typeof(ControllerWithBoundProperty));

            var provider = GetProvider();

            // Act
            provider.InferBoundPropertyModelPrefixes(controller);

            // Assert
            var property = Assert.Single(controller.ControllerProperties);
            Assert.Equal(string.Empty, property.BindingInfo.BinderModelName);
        }

        [Fact]
        public void InferBoundPropertyModelPrefixes_SetsModelPrefix_ForCollectionTypeFromValueProvider()
        {
            // Arrange
            var controller = GetControllerModel(typeof(ControllerWithBoundCollectionProperty));

            var provider = GetProvider();

            // Act
            provider.InferBoundPropertyModelPrefixes(controller);

            // Assert
            var property = Assert.Single(controller.ControllerProperties);
            Assert.Null(property.BindingInfo.BinderModelName);
        }

        [Fact]
        public void InferParameterModelPrefixes_SetsModelPrefix_ForComplexTypeFromValueProvider()
        {
            // Arrange
            var action = GetActionModel(typeof(ControllerWithBoundProperty), nameof(ControllerWithBoundProperty.SomeAction));

            var provider = GetProvider();

            // Act
            provider.InferParameterModelPrefixes(action);

            // Assert
            var parameter = Assert.Single(action.Parameters);
            Assert.Equal(string.Empty, parameter.BindingInfo.BinderModelName);
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

        [Fact]
        public void DiscoverApiConvention_DoesNotAddConventionItem_IfActionHasProducesResponseTypeAttribute()
        {
            // Arrange
            var actionModel = new ActionModel(
                typeof(TestApiConventionController).GetMethod(nameof(TestApiConventionController.Delete)),
                Array.Empty<object>());
            actionModel.Filters.Add(new ProducesResponseTypeAttribute(200));
            var attributes = new[] { new ApiConventionTypeAttribute(typeof(DefaultApiConventions)) };

            // Act
            ApiBehaviorApplicationModelProvider.DiscoverApiConvention(actionModel, attributes);

            // Assert
            Assert.Empty(actionModel.Properties);
        }

        [Fact]
        public void DiscoverApiConvention_DoesNotAddConventionItem_IfActionHasProducesAttribute()
        {
            // Arrange
            var actionModel = new ActionModel(
                typeof(TestApiConventionController).GetMethod(nameof(TestApiConventionController.Delete)),
                Array.Empty<object>());
            actionModel.Filters.Add(new ProducesAttribute(typeof(object)));
            var attributes = new[] { new ApiConventionTypeAttribute(typeof(DefaultApiConventions)) };

            // Act
            ApiBehaviorApplicationModelProvider.DiscoverApiConvention(actionModel, attributes);

            // Assert
            Assert.Empty(actionModel.Properties);
        }

        [Fact]
        public void DiscoverApiConvention_DoesNotAddConventionItem_IfNoConventionMatches()
        {
            // Arrange
            var actionModel = new ActionModel(
                typeof(TestApiConventionController).GetMethod(nameof(TestApiConventionController.NoMatch)),
                Array.Empty<object>());
            var attributes = new[] { new ApiConventionTypeAttribute(typeof(DefaultApiConventions)) };

            // Act
            ApiBehaviorApplicationModelProvider.DiscoverApiConvention(actionModel, attributes);

            // Assert
            Assert.Empty(actionModel.Properties);
        }

        [Fact]
        public void DiscoverApiConvention_AddsConventionItem_IfConventionMatches()
        {
            // Arrange
            var actionModel = new ActionModel(
                typeof(TestApiConventionController).GetMethod(nameof(TestApiConventionController.Delete)),
                Array.Empty<object>());
            var attributes = new[] { new ApiConventionTypeAttribute(typeof(DefaultApiConventions)) };

            // Act
            ApiBehaviorApplicationModelProvider.DiscoverApiConvention(actionModel, attributes);

            // Assert
            Assert.Collection(
                actionModel.Properties,
                kvp =>
                {
                    Assert.Equal(typeof(ApiConventionResult), kvp.Key);
                    Assert.NotNull(kvp.Value);
                });
        }

        [Fact]
        public void DiscoverApiConvention_AddsConventionItem_IfActionHasNonConventionBasedFilters()
        {
            // Arrange
            var actionModel = new ActionModel(
                typeof(TestApiConventionController).GetMethod(nameof(TestApiConventionController.Delete)),
                Array.Empty<object>());
            actionModel.Filters.Add(new AuthorizeFilter());
            actionModel.Filters.Add(new ServiceFilterAttribute(typeof(object)));
            actionModel.Filters.Add(new ConsumesAttribute("application/xml"));
            var attributes = new[] { new ApiConventionTypeAttribute(typeof(DefaultApiConventions)) };

            // Act
            ApiBehaviorApplicationModelProvider.DiscoverApiConvention(actionModel, attributes);

            // Assert
            Assert.Collection(
                actionModel.Properties,
                kvp =>
                {
                    Assert.Equal(typeof(ApiConventionResult), kvp.Key);
                    Assert.NotNull(kvp.Value);
                });
        }

        // A dynamically generated type in an assembly that has an ApiConventionAttribute.
        private static TypeBuilder CreateTestControllerType()
        {
            var attributeBuilder = new CustomAttributeBuilder(
                typeof(ApiConventionTypeAttribute).GetConstructor(new[] { typeof(Type) }),
                new[] { typeof(DefaultApiConventions) });

            var assemblyName = new AssemblyName("TestAssembly");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            assemblyBuilder.SetCustomAttribute(attributeBuilder);

            var module = assemblyBuilder.DefineDynamicModule(assemblyName.Name);
            var controllerType = module.DefineType("TestController");
            return controllerType;
        }

        private static ApiBehaviorApplicationModelProvider GetProvider(
            ApiBehaviorOptions options = null,
            IModelMetadataProvider modelMetadataProvider = null)
        {
            options = options ?? new ApiBehaviorOptions
            {
                InvalidModelStateResponseFactory = _ => null,
            };
            var optionsAccessor = Options.Create(options);

            var loggerFactory = NullLoggerFactory.Instance;
            modelMetadataProvider = modelMetadataProvider ?? new EmptyModelMetadataProvider();
            return new ApiBehaviorApplicationModelProvider(optionsAccessor, modelMetadataProvider, loggerFactory);
        }

        private static ApplicationModelProviderContext GetContext(
            Type type,
            IModelMetadataProvider modelMetadataProvider = null)
        {
            var context = new ApplicationModelProviderContext(new[] { type.GetTypeInfo() });
            var mvcOptions = Options.Create(new MvcOptions { AllowValidatingTopLevelNodes = true });
            modelMetadataProvider = modelMetadataProvider ?? new EmptyModelMetadataProvider();
            var provider = new DefaultApplicationModelProvider(mvcOptions, modelMetadataProvider);
            provider.OnProvidersExecuting(context);

            return context;
        }

        private static ControllerModel GetControllerModel(Type controllerType)
        {
            var context = GetContext(controllerType);
            return Assert.Single(context.Result.Controllers);
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

        private static ParameterModel GetParameterModel<T>(ActionModel action)
        {
            return Assert.Single(action.Parameters.Where(x => typeof(T).IsAssignableFrom(x.ParameterType)));
        }

        [ApiController]
        [Route("TestApi")]
        private class TestApiController : ControllerBase
        {
            [HttpGet]
            public IActionResult TestAction() => null;
        }

        private class SimpleController : ControllerBase
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

            [HttpPut("cancellation")]
            public IActionResult ComplexTypeModelWithCancellationToken(TestModel model, CancellationToken cancellationToken) => null;

            [HttpGet("parameter-with-model-binder-attribute")]
            public IActionResult ModelBinderAttribute([ModelBinder(Name = "top")] int value) => null;

            [HttpGet("parameter-with-fromquery")]
            public IActionResult FromQuery([FromQuery] int value) => null;

            [HttpGet("parameter-with-fromquery-and-customname")]
            public IActionResult FromQueryWithCustomName([FromQuery(Name = "top")] int value) => null;

            [HttpGet("parameter-with-fromquery-on-complextype")]
            public IActionResult FromQueryOnComplexType([FromQuery] GpsCoordinates gpsCoordinates) => null;

            [HttpGet("parameter-with-fromquery-on-complextype-and-customname")]
            public IActionResult FromQueryOnComplexTypeWithCustomName([FromQuery(Name = "gps")] GpsCoordinates gpsCoordinates) => null;

            [HttpGet("parameter-with-fromquery-on-collection-type")]
            public IActionResult FromQueryOnCollectionType([FromQuery] ICollection<int> value) => null;

            [HttpGet("parameter-with-fromquery-on-array-type")]
            public IActionResult FromQueryOnArrayType([FromQuery] int[] value) => null;

            [HttpGet("parameter-with-fromquery-on-array-type-customname")]
            public IActionResult FromQueryOnArrayTypeWithCustomName([FromQuery(Name = "ids")] int[] value) => null;

            [HttpGet("parameter-with-fromroute")]
            public IActionResult FromRoute([FromRoute] int value) => null;

            [HttpGet("parameter-with-fromroute-and-customname")]
            public IActionResult FromRouteWithCustomName([FromRoute(Name = "top")] int value) => null;

            [HttpGet("parameter-with-fromroute-on-complextype")]
            public IActionResult FromRouteOnComplexType([FromRoute] GpsCoordinates gpsCoordinates) => null;

            [HttpGet("parameter-with-fromroute-on-complextype-and-customname")]
            public IActionResult FromRouteOnComplexTypeWithCustomName([FromRoute(Name = "gps")] GpsCoordinates gpsCoordinates) => null;

            [HttpGet]
            public IActionResult ParameterWithRequestPredicateProvider([CustomRequestPredicateAndPropertyFilterProvider] int value) => null;
        }

        private class CustomRequestPredicateAndPropertyFilterProviderAttribute : Attribute, IRequestPredicateProvider, IPropertyFilterProvider
        {
            public static Func<ActionContext, bool> RequestPredicateStatic => (c) => true;
            public static Func<ModelMetadata, bool> PropertyFilterStatic => (c) => true;

            public Func<ActionContext, bool> RequestPredicate => RequestPredicateStatic;

            public Func<ModelMetadata, bool> PropertyFilter => PropertyFilterStatic;
        }

        [ApiController]
        [Route("[controller]/[action]")]
        private class ModelBinderOnParameterController
        {
            [HttpGet]
            public IActionResult ModelBinderAttributeWithExplicitModelName([ModelBinder(Name = "top")] int value) => null;

            [HttpGet]
            public IActionResult ModelBinderType([ModelBinder(typeof(TestModelBinder))] string name) => null;

            [HttpGet]
            public IActionResult ModelBinderTypeWithExplicitModelName([ModelBinder(typeof(TestModelBinder), Name = "foo")] string name) => null;
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

        [ApiController]
        private class ControllerWithBoundProperty
        {
            [FromQuery]
            public TestModel TestProperty { get; set; }

            public IActionResult SomeAction([FromQuery] TestModel test) => null;
        }

        [ApiController]
        private class ControllerWithBoundCollectionProperty
        {
            [FromQuery]
            public List<int> TestProperty { get; set; }

            public IActionResult SomeAction([FromQuery] List<int> test) => null;
        }

        private class Car { }

        [ApiController]
        private class ControllerWithMultipleInferredFromBodyParameters
        {
            [HttpGet("test")]
            public IActionResult Action(TestModel a, Car b) => null;
        }

        [ApiController]
        private class ControllerWithMultipleInferredOrSpecifiedFromBodyParameters
        {
            [HttpGet("test")]
            public IActionResult Action(TestModel a, [FromBody] int b) => null;
        }

        [ApiController]
        private class ControllerWithMultipleFromBodyParameters
        {
            [HttpGet("test")]
            public IActionResult Action([FromBody] decimal a, [FromBody] int b) => null;
        }

        [ApiController]
        private class ParameterWithBindingInfo
        {
            [HttpGet("test")]
            public IActionResult Action([ModelBinder(typeof(object))] Car car) => null;
        }

        private class TestApiConventionController
        {
            public IActionResult NoMatch() => null;

            public IActionResult Delete(int id) => null;
        }

        private class GpsCoordinates
        {
            public long Latitude { get; set; }
            public long Longitude { get; set; }
        }

        private class TestModelBinder : IModelBinder
        {
            public Task BindModelAsync(ModelBindingContext bindingContext)
            {
                throw new NotImplementedException();
            }
        }
    }
}
