// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Core;
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

        public void ParameterWithNoBindAttribute(MySimpleModelWithTypeBasedBind parameter)
        {
        }

        public void ParameterHasFieldPrefix([Bind(Prefix = "simpleModelPrefix")] string parameter)
        {
        }

        public void ParameterHasEmptyFieldPrefix([Bind(Prefix = "")] MySimpleModel parameter,
                                                 [Bind(Prefix = "")] MySimpleModelWithTypeBasedBind parameter1)
        {
        }

        public void ParameterHasPrefixAndComplexType(
            [Bind(Prefix = "simpleModelPrefix")] MySimpleModel parameter,
            [Bind(Prefix = "simpleModelPrefix")] MySimpleModelWithTypeBasedBind parameter1)
        {
        }

        public void ParameterHasEmptyBindAttribute([Bind] MySimpleModel parameter,
                                                   [Bind] MySimpleModelWithTypeBasedBind parameter1)
        {
        }

        [Fact]
        public void GetModelBindingContext_ReturnsOnlyIncludedProperties_UsingBindAttributeInclude()
        {
            // Arrange
            var metadataProvider = new DataAnnotationsModelMetadataProvider();
            var modelMetadata = metadataProvider.GetMetadataForType(
                typeof(TypeWithIncludedPropertiesUsingBindAttribute));

            // Act
            var context = DefaultControllerActionArgumentBinder.GetModelBindingContext(
                modelMetadata,
                new ModelStateDictionary(),
                Mock.Of<OperationBindingContext>());

            // Assert
            Assert.True(context.PropertyFilter(context, "IncludedExplicitly1"));
            Assert.True(context.PropertyFilter(context, "IncludedExplicitly2"));
        }

        [Fact]
        public void GetModelBindingContext_UsesBindAttributeOnType_IfNoBindAttributeOnParameter_ForPrefix()
        {
            // Arrange
            var type = typeof(ControllerActionArgumentBinderTests);
            var methodInfo = type.GetMethod("ParameterWithNoBindAttribute");

            var metadataProvider = new DataAnnotationsModelMetadataProvider();
            var modelMetadata = metadataProvider.GetMetadataForParameter(
                methodInfo: methodInfo,
                parameterName: "parameter");

            // Act
            var context = DefaultControllerActionArgumentBinder.GetModelBindingContext(
                modelMetadata,
                new ModelStateDictionary(),
                Mock.Of<OperationBindingContext>());

            // Assert
            Assert.Equal("TypePrefix", context.ModelName);
        }

        [Theory]
        [InlineData("ParameterHasFieldPrefix", false, "simpleModelPrefix")]
        [InlineData("ParameterHasEmptyFieldPrefix", false, "")]
        [InlineData("ParameterHasPrefixAndComplexType", false, "simpleModelPrefix")]
        [InlineData("ParameterHasEmptyBindAttribute", true, "parameter")]
        public void GetModelBindingContext_ModelBindingContextIsSetWithModelName_ForParameters(
            string actionMethodName,
            bool expectedFallToEmptyPrefix,
            string expectedModelName)
        {
            // Arrange
            var type = typeof(ControllerActionArgumentBinderTests);
            var methodInfo = type.GetMethod(actionMethodName);

            var metadataProvider = new DataAnnotationsModelMetadataProvider();
            var modelMetadata = metadataProvider.GetMetadataForParameter(
                methodInfo: methodInfo,
                parameterName: "parameter");

            // Act
            var context = DefaultControllerActionArgumentBinder.GetModelBindingContext(
                modelMetadata, 
                new ModelStateDictionary(),
                Mock.Of<OperationBindingContext>());

            // Assert
            Assert.Equal(expectedFallToEmptyPrefix, context.FallbackToEmptyPrefix);
            Assert.Equal(expectedModelName, context.ModelName);
        }

        [Theory]
        [InlineData("ParameterHasEmptyFieldPrefix", false, "")]
        [InlineData("ParameterHasPrefixAndComplexType", false, "simpleModelPrefix")]
        [InlineData("ParameterHasEmptyBindAttribute", true, "parameter1")]
        public void GetModelBindingContext_ModelBindingContextIsNotSet_ForTypes(
            string actionMethodName,
            bool expectedFallToEmptyPrefix,
            string expectedModelName)
        {
            // Arrange
            var type = typeof(ControllerActionArgumentBinderTests);
            var methodInfo = type.GetMethod(actionMethodName);

            var metadataProvider = new DataAnnotationsModelMetadataProvider();
            var modelMetadata = metadataProvider.GetMetadataForParameter(
                methodInfo: methodInfo,
                parameterName: "parameter1");

            // Act
            var context = DefaultControllerActionArgumentBinder.GetModelBindingContext(
                modelMetadata,
                new ModelStateDictionary(),
                Mock.Of<OperationBindingContext>());

            // Assert
            Assert.Equal(expectedFallToEmptyPrefix, context.FallbackToEmptyPrefix);
            Assert.Equal(expectedModelName, context.ModelName);
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
            binder
                .Setup(b => b.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns(Task.FromResult<ModelBindingResult>(result: null));

            var actionContext = new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                actionDescriptor);

            var actionBindingContext = new ActionBindingContext()
            {
                ModelBinder = binder.Object,
            };

            var modelMetadataProvider = new DataAnnotationsModelMetadataProvider();
            var inputFormattersProvider = new Mock<IInputFormattersProvider>();
            inputFormattersProvider
                .SetupGet(o => o.InputFormatters)
                .Returns(new List<IInputFormatter>());
            var invoker = new DefaultControllerActionArgumentBinder(
                modelMetadataProvider,
                new DefaultObjectValidator(Mock.Of<IValidationExcludeFiltersProvider>(), modelMetadataProvider),
                new MockMvcOptionsAccessor());

            // Act
            var result = await invoker.GetActionArgumentsAsync(actionContext, actionBindingContext);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetActionArgumentsAsync_DoesNotAddActionArguments_IfBinderDoesNotSetModel()
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
            binder
                .Setup(b => b.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns(Task.FromResult(new ModelBindingResult(null, "", false)));

            var actionContext = new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                actionDescriptor);

            var actionBindingContext = new ActionBindingContext()
            {
                ModelBinder = binder.Object,
            };

            var inputFormattersProvider = new Mock<IInputFormattersProvider>();
            inputFormattersProvider
                .SetupGet(o => o.InputFormatters)
                .Returns(new List<IInputFormatter>());

            var modelMetadataProvider = new DataAnnotationsModelMetadataProvider();
            var invoker = new DefaultControllerActionArgumentBinder(
                modelMetadataProvider,
                new DefaultObjectValidator(Mock.Of<IValidationExcludeFiltersProvider>(), modelMetadataProvider),
                new MockMvcOptionsAccessor());

            // Act
            var result = await invoker.GetActionArgumentsAsync(actionContext, actionBindingContext);

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
                },
            };

            var value = "Hello world";
            var metadataProvider = new EmptyModelMetadataProvider();

            var binder = new Mock<IModelBinder>();
            binder
                .Setup(b => b.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Callback((ModelBindingContext context) =>
                {
                    context.ModelMetadata = metadataProvider.GetMetadataForType(typeof(string));
                })
                .Returns(Task.FromResult(result: new ModelBindingResult(value, "", true)));

            var actionContext = new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                actionDescriptor);

            var actionBindingContext = new ActionBindingContext()
            {
                ModelBinder = binder.Object,
            };

            var mockValidatorProvider = new Mock<IObjectModelValidator>(MockBehavior.Strict);
            mockValidatorProvider.Setup(o => o.Validate(It.IsAny<ModelValidationContext>()));

            var invoker = new DefaultControllerActionArgumentBinder(
                metadataProvider,
                mockValidatorProvider.Object,
                new MockMvcOptionsAccessor());

            // Act
            var result = await invoker.GetActionArgumentsAsync(actionContext, actionBindingContext);

            // Assert
            Assert.Equal(1, result.Count);
            Assert.Equal(value, result["foo"]);
        }

        [Fact]
        public async Task GetActionArgumentsAsync_CallsValidator_IfModelBinderSucceeds()
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

            var actionContext = new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                actionDescriptor);

            var binder = new Mock<IModelBinder>();
            binder
                .Setup(b => b.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns(Task.FromResult(result: new ModelBindingResult(
                    model: null,
                    key: string.Empty,
                    isModelSet: true)));

            var actionBindingContext = new ActionBindingContext()
            {
                ModelBinder = binder.Object,
            };

            var mockValidatorProvider = new Mock<IObjectModelValidator>(MockBehavior.Strict);
            mockValidatorProvider
                .Setup(o => o.Validate(It.IsAny<ModelValidationContext>()))
                .Verifiable();
            var invoker = new DefaultControllerActionArgumentBinder(
                new DataAnnotationsModelMetadataProvider(),
                mockValidatorProvider.Object,
                new MockMvcOptionsAccessor());

            // Act
            var result = await invoker.GetActionArgumentsAsync(actionContext, actionBindingContext);

            // Assert
            mockValidatorProvider.Verify(
                o => o.Validate(It.IsAny<ModelValidationContext>()), Times.Once());
        }

        [Fact]
        public async Task GetActionArgumentsAsync_DoesNotCallValidator_IfModelBinderFails()
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

            var actionContext = new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                actionDescriptor);

            var binder = new Mock<IModelBinder>();
            binder
                .Setup(b => b.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns(Task.FromResult<ModelBindingResult>(null));

            var actionBindingContext = new ActionBindingContext()
            {
                ModelBinder = binder.Object,
            };

            var mockValidatorProvider = new Mock<IObjectModelValidator>(MockBehavior.Strict);
            mockValidatorProvider.Setup(o => o.Validate(It.IsAny<ModelValidationContext>()))
                                 .Verifiable();
            var invoker = new DefaultControllerActionArgumentBinder(
                new DataAnnotationsModelMetadataProvider(),
                mockValidatorProvider.Object,
                new MockMvcOptionsAccessor());

            // Act
            var result = await invoker.GetActionArgumentsAsync(actionContext, actionBindingContext);

            // Assert
            mockValidatorProvider.Verify(o => o.Validate(It.IsAny<ModelValidationContext>()), Times.Never());
        }

        [Fact]
        public async Task GetActionArgumentsAsync_SetsMaxModelErrors()
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
            binder
                .Setup(b => b.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns(Task.FromResult(
                    result: new ModelBindingResult(model: "Hello", key: string.Empty, isModelSet: true)));

            var actionContext = new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                actionDescriptor);

            var actionBindingContext = new ActionBindingContext()
            {
                ModelBinder = binder.Object,
            };

            var inputFormattersProvider = new Mock<IInputFormattersProvider>();
            inputFormattersProvider
                .SetupGet(o => o.InputFormatters)
                .Returns(new List<IInputFormatter>());

            var options = new MockMvcOptionsAccessor();
            options.Options.MaxModelValidationErrors = 5;
            var mockValidatorProvider = new Mock<IObjectModelValidator>(MockBehavior.Strict);
            mockValidatorProvider.Setup(o => o.Validate(It.IsAny<ModelValidationContext>()));
            var invoker = new DefaultControllerActionArgumentBinder(
                new DataAnnotationsModelMetadataProvider(),
                mockValidatorProvider.Object,
                options);

            // Act
            var result = await invoker.GetActionArgumentsAsync(actionContext, actionBindingContext);

            // Assert
            Assert.Equal(5, actionContext.ModelState.MaxAllowedErrors);
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

        private class Person
        {
            public string Name { get; set; }
        }

        private class NonValueProviderBinderMetadataAttribute : Attribute, IBindingSourceMetadata
        {
            public BindingSource BindingSource { get { return BindingSource.Body; } }
        }

        private class ValueProviderMetadataAttribute : Attribute, IBindingSourceMetadata
        {
            public BindingSource BindingSource { get { return BindingSource.Query; } }
        }

        [Bind(new string[] { nameof(IncludedExplicitly1), nameof(IncludedExplicitly2) })]
        private class TypeWithIncludedPropertiesUsingBindAttribute
        {
            public int ExcludedByDefault1 { get; set; }

            public int ExcludedByDefault2 { get; set; }

            public int IncludedExplicitly1 { get; set; }

            public int IncludedExplicitly2 { get; set; }
        }
    }
}