// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.ModelBinding.Metadata;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
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
        public async Task BindActionArgumentsAsync_DoesNotAddActionArguments_IfBinderReturnsFalse()
        {
            // Arrange
            var actionDescriptor = GetActionDescriptor();
            actionDescriptor.Parameters.Add(
                new ParameterDescriptor
                {
                    Name = "foo",
                    ParameterType = typeof(object),
                        BindingInfo = new BindingInfo(),
                });

            var actionContext = GetActionContext(actionDescriptor);

            var binder = new Mock<IModelBinder>();
            binder
                .Setup(b => b.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns(Task.FromResult<ModelBindingResult>(result: null));
            var actionBindingContext = new ActionBindingContext()
            {
                ModelBinder = binder.Object,
            };

            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            
            var argumentBinder = GetArgumentBinder();

            // Act
            var result = await argumentBinder
                .BindActionArgumentsAsync(actionContext, actionBindingContext, new TestController());

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task BindActionArgumentsAsync_DoesNotAddActionArguments_IfBinderDoesNotSetModel()
        {
            // Arrange
            var actionDescriptor = GetActionDescriptor();
            actionDescriptor.Parameters.Add(
                new ParameterDescriptor
                {
                    Name = "foo",
                    ParameterType = typeof(object),
                        BindingInfo = new BindingInfo(),
                });

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

            var argumentBinder = GetArgumentBinder();
            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();

            // Act
            var result = await argumentBinder
                .BindActionArgumentsAsync(actionContext, actionBindingContext, new TestController());

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task BindActionArgumentsAsync_AddsActionArguments_IfBinderReturnsTrue()
        {
            // Arrange
            Func<object, int> method = foo => 1;
            var actionDescriptor = GetActionDescriptor();
            actionDescriptor.Parameters.Add(
                new ParameterDescriptor
                {
                    Name = "foo",
                    ParameterType = typeof(string),
                        BindingInfo = new BindingInfo(),
                });

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

            var argumentBinder = GetArgumentBinder();

            // Act
            var result = await argumentBinder
                .BindActionArgumentsAsync(actionContext, actionBindingContext, new TestController());

            // Assert
            Assert.Equal(1, result.Count);
            Assert.Equal(value, result["foo"]);
        }

        [Fact]
        public async Task BindActionArgumentsAsync_CallsValidator_IfModelBinderSucceeds()
        {
            // Arrange
            var actionDescriptor = GetActionDescriptor();
            actionDescriptor.Parameters.Add(
                new ParameterDescriptor
                {
                    Name = "foo",
                    ParameterType = typeof(object),
                });

            var actionContext = GetActionContext(actionDescriptor);
            var actionBindingContext = GetActionBindingContext();

            var mockValidatorProvider = new Mock<IObjectModelValidator>(MockBehavior.Strict);
            mockValidatorProvider
                .Setup(o => o.Validate(It.IsAny<ModelValidationContext>()))
                .Verifiable();
            var argumentBinder = GetArgumentBinder(mockValidatorProvider.Object);

            // Act
            var result = await argumentBinder
                .BindActionArgumentsAsync(actionContext, actionBindingContext, new TestController());

            // Assert
            mockValidatorProvider.Verify(
                o => o.Validate(It.IsAny<ModelValidationContext>()), Times.Once());
        }

        [Fact]
        public async Task BindActionArgumentsAsync_DoesNotCallValidator_IfModelBinderFails()
        {
            // Arrange
            Func<object, int> method = foo => 1;
            var actionDescriptor = GetActionDescriptor();
            actionDescriptor.Parameters.Add(
                new ParameterDescriptor
                {
                    Name = "foo",
                    ParameterType = typeof(object),
                        BindingInfo = new BindingInfo(),
                });

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
            var argumentBinder = GetArgumentBinder(mockValidatorProvider.Object);

            // Act
            var result = await argumentBinder
                .BindActionArgumentsAsync(actionContext, actionBindingContext, new TestController());

            // Assert
            mockValidatorProvider.Verify(o => o.Validate(It.IsAny<ModelValidationContext>()), Times.Never());
        }

        [Fact]
        public async Task BindActionArgumentsAsync_CallsValidator_ForControllerProperties_IfModelBinderSucceeds()
        {
            // Arrange
            var actionDescriptor = GetActionDescriptor();
            actionDescriptor.BoundProperties.Add(
                new ParameterDescriptor
                {
                    Name = "ValueBinderMarkedProperty",
                    ParameterType = typeof(string),
                });

            var actionContext = GetActionContext(actionDescriptor);
            var actionBindingContext = GetActionBindingContext();

            var mockValidatorProvider = new Mock<IObjectModelValidator>(MockBehavior.Strict);
            mockValidatorProvider
                .Setup(o => o.Validate(It.IsAny<ModelValidationContext>()))
                .Verifiable();
            var argumentBinder = GetArgumentBinder(mockValidatorProvider.Object);

            // Act
            var result = await argumentBinder
                .BindActionArgumentsAsync(actionContext, actionBindingContext, new TestController());

            // Assert
            mockValidatorProvider.Verify(
                o => o.Validate(It.IsAny<ModelValidationContext>()), Times.Once());
        }

        [Fact]
        public async Task BindActionArgumentsAsync_DoesNotCallValidator_ForControllerProperties_IfModelBinderFails()
        {
            // Arrange
            Func<object, int> method = foo => 1;
            var actionDescriptor = GetActionDescriptor();
            actionDescriptor.BoundProperties.Add(
                new ParameterDescriptor
                {
                    Name = "ValueBinderMarkedProperty",
                    ParameterType = typeof(string),
                });

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
            var argumentBinder = GetArgumentBinder(mockValidatorProvider.Object);

            // Act
            var result = await argumentBinder
                .BindActionArgumentsAsync(actionContext, actionBindingContext, new TestController());

            // Assert
            mockValidatorProvider.Verify(o => o.Validate(It.IsAny<ModelValidationContext>()), Times.Never());
        }


        [Fact]
        public async Task BindActionArgumentsAsync_SetsControllerProperties()
        {
            // Arrange
            var actionDescriptor = GetActionDescriptor();
            actionDescriptor.BoundProperties.Add(
                new ParameterDescriptor
                {
                    Name = "ValueBinderMarkedProperty",
                        BindingInfo = new BindingInfo(),
                    ParameterType = typeof(string)
                });

            var actionContext = GetActionContext(actionDescriptor);
            var actionBindingContext = GetActionBindingContext();
            var argumentBinder = GetArgumentBinder();
            var controller = new TestController();

            // Act
            var result = await argumentBinder.BindActionArgumentsAsync(actionContext, actionBindingContext, controller);

            // Assert
            Assert.Equal("Hello", controller.ValueBinderMarkedProperty);
            Assert.Null(controller.UnmarkedProperty);
        }

        [Fact]
        public async Task BindActionArgumentsAsync_DoesNotSetNullValues_ForNonNullableProperties()
        {
            // Arrange
            var actionDescriptor = GetActionDescriptor();
            actionDescriptor.BoundProperties.Add(
                new ParameterDescriptor
                {
                    Name = "ValueBinderMarkedProperty",
                    BindingInfo = new BindingInfo(),
                    ParameterType = typeof(int)
                });

            var actionContext = GetActionContext(actionDescriptor);

            var binder = new Mock<IModelBinder>();
            binder
                .Setup(b => b.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns(Task.FromResult(
                    result: new ModelBindingResult(model: null, key: string.Empty, isModelSet: true)));
            var actionBindingContext = new ActionBindingContext()
            {
                ModelBinder = binder.Object,
            };

            var argumentBinder = GetArgumentBinder();
            var controller = new TestController();

            // Some non default value.
            controller.NotNullableProperty = -1;

            // Act
            var result = await argumentBinder.BindActionArgumentsAsync(actionContext, actionBindingContext, controller);

            // Assert
            Assert.Equal(-1, controller.NotNullableProperty);
        }

        private static ActionContext GetActionContext(ActionDescriptor descriptor = null)
        {
           return new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                descriptor ?? GetActionDescriptor());
        }

        private static ActionDescriptor GetActionDescriptor()
        {
            Func<object, int> method = foo => 1;
            return new ControllerActionDescriptor
            {
                MethodInfo = method.Method,
                ControllerTypeInfo = typeof(TestController).GetTypeInfo(),
                BoundProperties = new List<ParameterDescriptor>(),
                Parameters = new List<ParameterDescriptor>()
            };
        }

        private static ActionBindingContext GetActionBindingContext()
        {
            var binder = new Mock<IModelBinder>();
            binder
                .Setup(b => b.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns(Task.FromResult(
                    result: new ModelBindingResult(model: "Hello", key: string.Empty, isModelSet: true)));
            return new ActionBindingContext()
            {
                ModelBinder = binder.Object,
            };
        }

        private static DefaultControllerActionArgumentBinder GetArgumentBinder(IObjectModelValidator validator = null)
        {
            if (validator == null)
            {
                var mockValidator = new Mock<IObjectModelValidator>(MockBehavior.Strict);
                mockValidator.Setup(o => o.Validate(It.IsAny<ModelValidationContext>()));
                validator = mockValidator.Object;
            }

            return new DefaultControllerActionArgumentBinder(
                TestModelMetadataProvider.CreateDefaultProvider(),
                validator);
        }

        private class TestController
        {
            public string UnmarkedProperty { get; set; }

            [NonValueProviderBinderMetadata]
            public string NonValueBinderMarkedProperty { get; set; }

            [ValueProviderMetadata]
            public string ValueBinderMarkedProperty { get; set; }

            [CustomBindingSource]
            public int NotNullableProperty { get; set; }

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

        private class CustomBindingSourceAttribute : Attribute, IBindingSourceMetadata
        {
            public BindingSource BindingSource { get { return BindingSource.Custom; } }
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
