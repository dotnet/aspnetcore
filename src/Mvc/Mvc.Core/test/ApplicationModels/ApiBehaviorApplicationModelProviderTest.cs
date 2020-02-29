// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    public class ApiBehaviorApplicationModelProviderTest
    {
        [Fact]
        public void OnProvidersExecuting_ThrowsIfControllerWithAttribute_HasActionsWithoutAttributeRouting()
        {
            // Arrange
            var actionName = $"{typeof(TestApiController).FullName}.{nameof(TestApiController.TestAction)} ({typeof(TestApiController).Assembly.GetName().Name})";
            var expected = $"Action '{actionName}' does not have an attribute route. Action methods on controllers annotated with ApiControllerAttribute must be attribute routed.";

            var controllerModel = new ControllerModel(typeof(TestApiController).GetTypeInfo(), new[] { new ApiControllerAttribute() });
            var method = typeof(TestApiController).GetMethod(nameof(TestApiController.TestAction));
            var actionModel = new ActionModel(method, Array.Empty<object>())
            {
                Controller = controllerModel,
            };
            controllerModel.Actions.Add(actionModel);

            var context = new ApplicationModelProviderContext(new[] { controllerModel.ControllerType });
            context.Result.Controllers.Add(controllerModel);

            var provider = GetProvider();

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => provider.OnProvidersExecuting(context));
            Assert.Equal(expected, ex.Message);
        }

        [Fact]
        public void OnProvidersExecuting_AppliesConventions()
        {
            // Arrange
            var controllerModel = new ControllerModel(typeof(TestApiController).GetTypeInfo(), new[] { new ApiControllerAttribute() })
            {
                Selectors = { new SelectorModel { AttributeRouteModel = new AttributeRouteModel() } },
            };

            var method = typeof(TestApiController).GetMethod(nameof(TestApiController.TestAction));

            var actionModel = new ActionModel(method, Array.Empty<object>())
            {
                Controller = controllerModel,
            };
            controllerModel.Actions.Add(actionModel);

            var parameter = method.GetParameters()[0];
            var parameterModel = new ParameterModel(parameter, Array.Empty<object>())
            {
                Action = actionModel,
            };
            actionModel.Parameters.Add(parameterModel);

            var context = new ApplicationModelProviderContext(new[] { controllerModel.ControllerType });
            context.Result.Controllers.Add(controllerModel);

            var provider = GetProvider();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            // Verify some of the side-effects of executing API behavior conventions.
            Assert.True(actionModel.ApiExplorer.IsVisible);
            Assert.NotEmpty(actionModel.Filters.OfType<ModelStateInvalidFilterFactory>());
            Assert.NotEmpty(actionModel.Filters.OfType<ClientErrorResultFilterFactory>());
            Assert.Equal(BindingSource.Body, parameterModel.BindingInfo.BindingSource);
        }

        [Fact]
        public void Constructor_SetsUpConventions()
        {
            // Arrange
            var provider = GetProvider();

            // Act & Assert
            Assert.Collection(
                provider.ActionModelConventions,
                c => Assert.IsType<ApiVisibilityConvention>(c),
                c => Assert.IsType<ClientErrorResultFilterConvention>(c),
                c => Assert.IsType<InvalidModelStateFilterConvention>(c),
                c => Assert.IsType<ConsumesConstraintForFormFileParameterConvention>(c),
                c =>
                {
                    var convention = Assert.IsType<ApiConventionApplicationModelConvention>(c);
                    Assert.Equal(typeof(ProblemDetails), convention.DefaultErrorResponseType.Type);
                },
                c => Assert.IsType<InferParameterBindingInfoConvention>(c));
        }

        [Fact]
        public void Constructor_DoesNotAddClientErrorResultFilterConvention_IfSuppressMapClientErrorsIsSet()
        {
            // Arrange
            var provider = GetProvider(new ApiBehaviorOptions { SuppressMapClientErrors = true });

            // Act & Assert
            Assert.Empty(provider.ActionModelConventions.OfType<ClientErrorResultFilterConvention>());
        }

        [Fact]
        public void Constructor_DoesNotAddInvalidModelStateFilterConvention_IfSuppressModelStateInvalidFilterIsSet()
        {
            // Arrange
            var provider = GetProvider(new ApiBehaviorOptions { SuppressModelStateInvalidFilter = true });

            // Act & Assert
            Assert.Empty(provider.ActionModelConventions.OfType<InvalidModelStateFilterConvention>());
        }

        [Fact]
        public void Constructor_DoesNotAddConsumesConstraintForFormFileParameterConvention_IfSuppressConsumesConstraintForFormFileParametersIsSet()
        {
            // Arrange
            var provider = GetProvider(new ApiBehaviorOptions { SuppressConsumesConstraintForFormFileParameters = true });

            // Act & Assert
            Assert.Empty(provider.ActionModelConventions.OfType<ConsumesConstraintForFormFileParameterConvention>());
        }

        [Fact]
        public void Constructor_DoesNotAddInferParameterBindingInfoConvention_IfSuppressInferBindingSourcesForParametersIsSet()
        {
            // Arrange
            var provider = GetProvider(new ApiBehaviorOptions { SuppressInferBindingSourcesForParameters = true });

            // Act & Assert
            Assert.Empty(provider.ActionModelConventions.OfType<InferParameterBindingInfoConvention>());
        }

        [Fact]
        public void Constructor_DoesNotSpecifyDefaultErrorType_IfSuppressMapClientErrorsIsSet()
        {
            // Arrange
            var provider = GetProvider(new ApiBehaviorOptions { SuppressMapClientErrors = true });

            // Act & Assert
            var convention = Assert.Single(provider.ActionModelConventions.OfType<ApiConventionApplicationModelConvention>());
            Assert.Equal(typeof(void), convention.DefaultErrorResponseType.Type);
        }

        private static ApiBehaviorApplicationModelProvider GetProvider(
            ApiBehaviorOptions options = null)
        {
            options = options ?? new ApiBehaviorOptions
            {
                InvalidModelStateResponseFactory = _ => null,
            };
            var optionsAccessor = Options.Create(options);

            var loggerFactory = NullLoggerFactory.Instance;
            return new ApiBehaviorApplicationModelProvider(
                optionsAccessor,
                new EmptyModelMetadataProvider(),
                Mock.Of<IClientErrorFactory>(),
                loggerFactory);
        }

        private class TestApiController : ControllerBase
        {
            public IActionResult TestAction(object value) => null;
        }
    }
}
