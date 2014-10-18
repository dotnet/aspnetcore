// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core.Test
{
    public class ControllerActionArgumentBinderTests
    {
        public class MySimpleModel
        {
        }

        [Bind(Prefix = "TypePrefix")]
        public class MySimpleModelWithTypeBasedBind
        {
        }

        public void ParameterHasFieldPrefix([Bind(Prefix = "bar")] string foo)
        {
        }

        public void ParameterHasEmptyFieldPrefix([Bind(Prefix = "")] MySimpleModel foo, 
                                                 [Bind(Prefix = "")] MySimpleModelWithTypeBasedBind foo1)
        {
        }

        public void ParameterHasPrefixAndComplexType([Bind(Prefix = "bar")] MySimpleModel foo,
                                                     [Bind(Prefix = "bar")] MySimpleModelWithTypeBasedBind foo1)
        {
        }

        public void ParameterHasEmptyBindAttribute([Bind] MySimpleModel foo,
                                                   [Bind] MySimpleModelWithTypeBasedBind foo1)
        {
        }

        [Theory]
        [InlineData("ParameterHasFieldPrefix", false, "bar")]
        [InlineData("ParameterHasEmptyFieldPrefix", false, "")]
        [InlineData("ParameterHasPrefixAndComplexType", false, "bar")]
        [InlineData("ParameterHasEmptyBindAttribute", true, "foo")]
        public void GetModelBindingContext_ModelBindingContextIsSetWithModelName_ForParameters(
            string actionMethodName, bool expectedFallToEmptyPrefix, string expectedModelName)
        {
            // Arrange
            var type = typeof(ControllerActionArgumentBinderTests);
            var methodInfo = type.GetMethod(actionMethodName);
            var actionContext = new ActionContext(new RouteContext(Mock.Of<HttpContext>()),
                                                  Mock.Of<ActionDescriptor>());

            var metadataProvider = new DataAnnotationsModelMetadataProvider();
            var modelMetadata = metadataProvider.GetMetadataForParameter(modelAccessor: null,
                                                                         methodInfo: methodInfo,
                                                                         parameterName: "foo",
                                                                         binderMetadata: null);


            var actionBindingContext = new ActionBindingContext(actionContext,
                                                          Mock.Of<IModelMetadataProvider>(),
                                                          Mock.Of<IModelBinder>(),
                                                          Mock.Of<IValueProvider>(),
                                                          Mock.Of<IInputFormatterSelector>(),
                                                          Mock.Of<IModelValidatorProvider>());
            // Act
            var context = DefaultControllerActionArgumentBinder
                            .GetModelBindingContext(modelMetadata, actionBindingContext);

            // Assert
            Assert.Equal(expectedFallToEmptyPrefix, context.FallbackToEmptyPrefix);
            Assert.Equal(expectedModelName, context.ModelName);
        }

        [Theory]
        [InlineData("ParameterHasEmptyFieldPrefix", false, "")]
        [InlineData("ParameterHasPrefixAndComplexType", false, "bar")]
        [InlineData("ParameterHasEmptyBindAttribute", true, "foo1")]
        public void GetModelBindingContext_ModelBindingContextIsNotSet_ForTypes(
            string actionMethodName, bool expectedFallToEmptyPrefix, string expectedModelName)
        {
            // Arrange
            var type = typeof(ControllerActionArgumentBinderTests);
            var methodInfo = type.GetMethod(actionMethodName);
            var actionContext = new ActionContext(new RouteContext(Mock.Of<HttpContext>()),
                                                  Mock.Of<ActionDescriptor>());

            var metadataProvider = new DataAnnotationsModelMetadataProvider();
            var modelMetadata = metadataProvider.GetMetadataForParameter(modelAccessor: null,
                                                                         methodInfo: methodInfo,
                                                                         parameterName: "foo1",
                                                                         binderMetadata: null);


            var actionBindingContext = new ActionBindingContext(actionContext,
                                                          Mock.Of<IModelMetadataProvider>(),
                                                          Mock.Of<IModelBinder>(),
                                                          Mock.Of<IValueProvider>(),
                                                          Mock.Of<IInputFormatterSelector>(),
                                                          Mock.Of<IModelValidatorProvider>());
            // Act
            var context = DefaultControllerActionArgumentBinder
                            .GetModelBindingContext(modelMetadata, actionBindingContext);

            // Assert
            Assert.Equal(expectedFallToEmptyPrefix, context.FallbackToEmptyPrefix);
            Assert.Equal(expectedModelName, context.ModelName);
        }

        [Fact]
        public async Task Parameters_WithMultipleFromBody_Throw()
        {
            // Arrange
            var actionDescriptor = new ControllerActionDescriptor
            {
                MethodInfo = typeof(TestController).GetMethod("ActionWithTwoBodyParam"),
                Parameters = new List<ParameterDescriptor>
                            {
                                new ParameterDescriptor
                                {
                                    Name = "bodyParam",
                                    ParameterType = typeof(Person),
                                },
                                new ParameterDescriptor
                                {
                                    Name = "bodyParam1",
                                    ParameterType = typeof(Person),
                                }
                            }
            };

            var binder = new Mock<IModelBinder>();
            var metadataProvider = new DataAnnotationsModelMetadataProvider();
            var actionContext = new ActionContext(new RouteContext(Mock.Of<HttpContext>()),
                                                  actionDescriptor);
            actionContext.Controller = Mock.Of<object>();
            var bindingContext = new ActionBindingContext(actionContext,
                                                          metadataProvider,
                                                          Mock.Of<IModelBinder>(),
                                                          Mock.Of<IValueProvider>(),
                                                          Mock.Of<IInputFormatterSelector>(),
                                                          Mock.Of<IModelValidatorProvider>());

            var actionBindingContextProvider = new Mock<IActionBindingContextProvider>();
            actionBindingContextProvider.Setup(p => p.GetActionBindingContextAsync(It.IsAny<ActionContext>()))
                                        .Returns(Task.FromResult(bindingContext));
                                                    
            var invoker = new DefaultControllerActionArgumentBinder(
                actionBindingContextProvider.Object);

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => invoker.GetActionArgumentsAsync(actionContext));

            // Assert
            Assert.Equal("More than one parameter is bound to the HTTP request's content.",
                         ex.Message);
        }

        [Fact]
        public async Task GetActionArgumentsAsync_DoesNotAddActionArguments_IfBinderReturnsFalse()
        {
            // Arrange
            Func<object, int> method = foo => 1;
            var actionDescriptor = new ControllerActionDescriptor
            {
                MethodInfo = method.Method,
                Parameters = new List<ParameterDescriptor>
                {
                    new ParameterDescriptor
                    {
                        Name = "foo",
                        ParameterType = typeof(object),
                    }
                }
            };
            var binder = new Mock<IModelBinder>();
            binder.Setup(b => b.BindModelAsync(It.IsAny<ModelBindingContext>()))
                  .Returns(Task.FromResult(result: false));
            var actionContext = new ActionContext(new RouteContext(Mock.Of<HttpContext>()),
                                                  actionDescriptor);
            actionContext.Controller = Mock.Of<object>();
            var bindingContext = new ActionBindingContext(actionContext,
                                                          Mock.Of<IModelMetadataProvider>(),
                                                          binder.Object,
                                                          Mock.Of<IValueProvider>(),
                                                          Mock.Of<IInputFormatterSelector>(),
                                                          Mock.Of<IModelValidatorProvider>());
            var inputFormattersProvider = new Mock<IInputFormattersProvider>();
            inputFormattersProvider.SetupGet(o => o.InputFormatters)
                                            .Returns(new List<IInputFormatter>());
            var actionBindingContextProvider = new Mock<IActionBindingContextProvider>();
            actionBindingContextProvider.Setup(p => p.GetActionBindingContextAsync(It.IsAny<ActionContext>()))
                                        .Returns(Task.FromResult(bindingContext));

            var invoker = new DefaultControllerActionArgumentBinder(
                actionBindingContextProvider.Object);

            // Act
            var result = await invoker.GetActionArgumentsAsync(actionContext);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetActionArgumentsAsync_AddsActionArguments_IfBinderReturnsTrue()
        {
            // Arrange
            Func<object, int> method = foo => 1;
            var actionDescriptor = new ControllerActionDescriptor
            {
                MethodInfo = method.Method,
                Parameters = new List<ParameterDescriptor>
                            {
                                new ParameterDescriptor
                                {
                                    Name = "foo",
                                    ParameterType = typeof(string),
                                }
                            }
            };
            var value = "Hello world";
            var binder = new Mock<IModelBinder>();
            var metadataProvider = new EmptyModelMetadataProvider();
            binder.Setup(b => b.BindModelAsync(It.IsAny<ModelBindingContext>()))
                  .Callback((ModelBindingContext context) =>
                  {
                      context.ModelMetadata = metadataProvider.GetMetadataForType(modelAccessor: null,
                                                                                  modelType: typeof(string));
                      context.Model = value;
                  })
                  .Returns(Task.FromResult(result: true));
            var actionContext = new ActionContext(new RouteContext(Mock.Of<HttpContext>()),
                                                  actionDescriptor);
            actionContext.Controller = Mock.Of<object>();
            var bindingContext = new ActionBindingContext(actionContext,
                                                          metadataProvider,
                                                          binder.Object,
                                                          Mock.Of<IValueProvider>(),
                                                          Mock.Of<IInputFormatterSelector>(),
                                                          Mock.Of<IModelValidatorProvider>());

            var actionBindingContextProvider = new Mock<IActionBindingContextProvider>();
            actionBindingContextProvider.Setup(p => p.GetActionBindingContextAsync(It.IsAny<ActionContext>()))
                                        .Returns(Task.FromResult(bindingContext));

            var invoker = new DefaultControllerActionArgumentBinder(
                actionBindingContextProvider.Object);

            // Act
            var result = await invoker.GetActionArgumentsAsync(actionContext);

            // Assert
            Assert.Equal(1, result.Count);
            Assert.Equal(value, result["foo"]);
        }

        private class TestController
        {
            public string UnmarkedProperty { get; set; }

            [NonValueProviderBinderMetadata]
            public string NonValueBinderMarkedProperty { get; set; }

            [ValueProviderMetadata]
            public string ValueBinderMarkedProperty { get; set; }

            public Person ActionWithBodyParam([FromBody] Person bodyParam)
            {
                return bodyParam;
            }

            public Person ActionWithTwoBodyParam([FromBody] Person bodyParam, [FromBody] Person bodyParam1)
            {
                return bodyParam;
            }
        }

   
        private class NonValueProviderBinderMetadataAttribute : Attribute, IBinderMetadata
        {
        }

        private class ValueProviderMetadataAttribute : Attribute, IValueProviderMetadata
        {
        }
    }
}