// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Routing;
using Moq;
using Xunit;
using System.Linq;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class ControllerBinderDelegateProviderTest
    {
        [Fact]
        public async Task BindActionArgumentsAsync_DoesNotAddActionArgumentsOrCallBinderOrValidator_IfBindingIsNotAllowed_OnParameter()
        {
            // Arrange
            var actionDescriptor = GetActionDescriptor();
            actionDescriptor.Parameters.Add(
                new ControllerParameterDescriptor
                {
                    Name = "foo",
                    ParameterType = typeof(object),
                    BindingInfo = new BindingInfo(),
                    ParameterInfo = ParameterInfos.BindNeverParameterInfo
                });

            var controllerContext = GetControllerContext(actionDescriptor);
            var arguments = new Dictionary<string, object>(StringComparer.Ordinal);

            var binder = new Mock<IModelBinder>();
            binder
                .Setup(b => b.BindModelAsync(It.IsAny<DefaultModelBindingContext>()))
                .Verifiable();

            var mockValidator = CreateMockValidator();
            var factory = GetModelBinderFactory(binder.Object);
            var controller = new TestController();
            var parameterBinder = GetParameterBinder(factory);

            // Act
            var binderDelegate = ControllerBinderDelegateProvider.CreateBinderDelegate(
                parameterBinder,
                factory,
                TestModelMetadataProvider.CreateDefaultProvider(),
                actionDescriptor);

            await binderDelegate(controllerContext, controller, arguments);

            // Assert
            Assert.Empty(arguments);
            binder
                .Verify(o => o.BindModelAsync(
                    It.IsAny<DefaultModelBindingContext>()),
                Times.Never());
            mockValidator
                .Verify(o => o.Validate(
                    It.IsAny<ModelValidationContext>()),
                Times.Never());
        }

        [Fact]
        public async Task BindActionArgumentsAsync_DoesNotAddActionArgumentsOrCallBinderOrValidator_IfBindingIsNotAllowed_OnProperty()
        {
            // Arrange
            var actionDescriptor = GetActionDescriptor();
            actionDescriptor.BoundProperties.Add(
                new ParameterDescriptor
                {
                    Name = nameof(TestController.RequiredButBindNeverProperty),
                    ParameterType = typeof(object)
                });

            var controllerContext = GetControllerContext(actionDescriptor);
            var arguments = new Dictionary<string, object>(StringComparer.Ordinal);

            var binder = new Mock<IModelBinder>();
            binder
                .Setup(b => b.BindModelAsync(It.IsAny<DefaultModelBindingContext>()))
                .Verifiable();

            var mockValidator = CreateMockValidator();
            var factory = GetModelBinderFactory(binder.Object);
            var controller = new TestController();
            var parameterBinder = GetParameterBinder(factory);

            // Act
            var binderDelegate = ControllerBinderDelegateProvider.CreateBinderDelegate(
                parameterBinder,
                factory,
                TestModelMetadataProvider.CreateDefaultProvider(),
                actionDescriptor);

            await binderDelegate(controllerContext, controller, arguments);

            // Assert
            Assert.Empty(arguments);
            binder
                .Verify(o => o.BindModelAsync(
                    It.IsAny<DefaultModelBindingContext>()),
                Times.Never());
            mockValidator
                .Verify(o => o.Validate(
                    It.IsAny<ModelValidationContext>()),
                Times.Never());
        }

        [Fact]
        public async Task BindActionArgumentsAsync_DoesNotAddActionArguments_IfBinderReturnsNull()
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
                .Setup(b => b.BindModelAsync(It.IsAny<DefaultModelBindingContext>()))
                .Returns(Task.CompletedTask);
            var factory = GetModelBinderFactory(binder.Object);
            var parameterBinder = GetParameterBinder(factory);

            var controllerContext = GetControllerContext(actionDescriptor);
            var controller = new TestController();
            var arguments = new Dictionary<string, object>(StringComparer.Ordinal);

            // Act
            var binderDelegate = ControllerBinderDelegateProvider.CreateBinderDelegate(
                parameterBinder,
                factory,
                TestModelMetadataProvider.CreateDefaultProvider(),
                actionDescriptor);

            await binderDelegate(controllerContext, controller, arguments);

            // Assert
            Assert.Empty(arguments);
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
                .Setup(b => b.BindModelAsync(It.IsAny<DefaultModelBindingContext>()))
                .Returns(Task.CompletedTask);
            var factory = GetModelBinderFactory(binder.Object);
            var parameterBinder = GetParameterBinder(factory);

            var controllerContext = GetControllerContext(actionDescriptor);
            var controller = new TestController();
            var arguments = new Dictionary<string, object>(StringComparer.Ordinal);

            // Act
            var binderDelegate = ControllerBinderDelegateProvider.CreateBinderDelegate(
                parameterBinder,
                factory,
                TestModelMetadataProvider.CreateDefaultProvider(),
                actionDescriptor);

            await binderDelegate(controllerContext, controller, arguments);

            // Assert
            Assert.Empty(arguments);
        }

        [Fact]
        public async Task BindActionArgumentsAsync_AddsActionArguments_IfBinderReturnsNotNull()
        {
            // Arrange
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
                .Setup(b => b.BindModelAsync(It.IsAny<DefaultModelBindingContext>()))
                .Callback((ModelBindingContext context) =>
                {
                    context.ModelMetadata = metadataProvider.GetMetadataForType(typeof(string));
                    context.Result = ModelBindingResult.Success(value);
                })
                .Returns(Task.CompletedTask);
            var factory = GetModelBinderFactory(binder.Object);
            var parameterBinder = GetParameterBinder(factory);

            var controllerContext = GetControllerContext(actionDescriptor);
            var controller = new TestController();
            var arguments = new Dictionary<string, object>(StringComparer.Ordinal);

            // Act
            var binderDelegate = ControllerBinderDelegateProvider.CreateBinderDelegate(
                parameterBinder,
                factory,
                TestModelMetadataProvider.CreateDefaultProvider(),
                actionDescriptor);

            await binderDelegate(controllerContext, controller, arguments);

            // Assert
            Assert.Single(arguments);
            Assert.Equal(value, arguments["foo"]);
        }

        [Fact]
        public async Task BindActionArgumentsAsync_GetsMetadataFromParameter()
        {
            // Arrange
            var actionDescriptor = GetActionDescriptor();
            actionDescriptor.Parameters.Add(
                new ControllerParameterDescriptor
                {
                    Name = "foo",
                    ParameterType = typeof(object),
                    ParameterInfo = ParameterInfos.NoAttributesParameterInfo
                });

            var controllerContext = GetControllerContext(actionDescriptor);

            var mockBinder = new Mock<IModelBinder>();
            var factory = GetModelBinderFactory(mockBinder.Object);

            var parameterBinder = GetParameterBinder(factory, CreateMockValidator().Object);
            var controller = new TestController();
            var arguments = new Dictionary<string, object>(StringComparer.Ordinal);

            var modelMetadata = new Mock<FakeModelMetadata>();
            modelMetadata.Setup(m => m.IsBindingAllowed).Returns(true);
            var mockMetadataProvider = new Mock<DefaultModelMetadataProvider>(
                Mock.Of<ICompositeMetadataDetailsProvider>());
            mockMetadataProvider
                .Setup(p => p.GetMetadataForParameter(ParameterInfos.NoAttributesParameterInfo))
                .Returns(modelMetadata.Object);

            // Act
            var binderDelegate = ControllerBinderDelegateProvider.CreateBinderDelegate(
                parameterBinder,
                factory,
                mockMetadataProvider.Object,
                actionDescriptor);

            await binderDelegate(controllerContext, controller, arguments);

            // Assert
            mockBinder
                .Verify(o => o.BindModelAsync(
                    It.Is<ModelBindingContext>(context => context.ModelMetadata == modelMetadata.Object)),
                Times.Once());
        }

        [Fact]
        public async Task BindActionArgumentsAsync_GetsMetadataFromType_IsMetadataProviderIsNotDefaultMetadataProvider()
        {
            // Arrange
            var actionDescriptor = GetActionDescriptor();
            actionDescriptor.Parameters.Add(
                new ControllerParameterDescriptor
                {
                    Name = "foo",
                    ParameterType = typeof(Person)
                });

            var controllerContext = GetControllerContext(actionDescriptor);

            var mockBinder = new Mock<IModelBinder>();
            var factory = GetModelBinderFactory(mockBinder.Object);

            var parameterBinder = GetParameterBinder(factory, CreateMockValidator().Object);
            var controller = new TestController();
            var arguments = new Dictionary<string, object>(StringComparer.Ordinal);

            var modelMetadata = new Mock<FakeModelMetadata>();
            modelMetadata.Setup(m => m.IsBindingAllowed).Returns(true);
            var mockMetadataProvider = new Mock<IModelMetadataProvider>();
            mockMetadataProvider
                .Setup(p => p.GetMetadataForType(typeof(Person)))
                .Returns(modelMetadata.Object);

            // Act
            var binderDelegate = ControllerBinderDelegateProvider.CreateBinderDelegate(
                parameterBinder,
                factory,
                mockMetadataProvider.Object,
                actionDescriptor);

            await binderDelegate(controllerContext, controller, arguments);

            // Assert
            mockBinder
                .Verify(o => o.BindModelAsync(
                    It.Is<ModelBindingContext>(context => context.ModelMetadata == modelMetadata.Object)),
                Times.Once());
        }

        [Fact]
        public async Task BindActionArgumentsAsync_CallsValidator_IfModelBinderSucceeds()
        {
            // Arrange
            var actionDescriptor = GetActionDescriptor();
            actionDescriptor.Parameters.Add(
                new ControllerParameterDescriptor
                {
                    Name = "foo",
                    ParameterType = typeof(object),
                    ParameterInfo = ParameterInfos.CustomValidationParameterInfo
                });

            var controllerContext = GetControllerContext(actionDescriptor);

            var factory = GetModelBinderFactory("Hello");

            var mockValidator = CreateMockValidator();
            mockValidator
                .Setup(o => o.Validate(It.IsAny<ModelValidationContext>()))
                .Returns(new[] { new ModelValidationResult("memberName", "some message") });

            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var parameterBinder = GetParameterBinder(factory, mockValidator.Object, modelMetadataProvider);
            var controller = new TestController();
            var arguments = new Dictionary<string, object>(StringComparer.Ordinal);

            // Act
            var binderDelegate = ControllerBinderDelegateProvider.CreateBinderDelegate(
                parameterBinder,
                factory,
                modelMetadataProvider,
                actionDescriptor);

            await binderDelegate(controllerContext, controller, arguments);

            // Assert
            mockValidator
                .Verify(o => o.Validate(
                    It.IsAny<ModelValidationContext>()),
            Times.Once());

            Assert.False(controllerContext.ModelState.IsValid);
            Assert.Equal(
                "some message",
                controllerContext.ModelState["memberName"].Errors.Single().ErrorMessage);
        }

        [Fact]
        public async Task BindActionArgumentsAsync_DoesNotCallValidator_IfModelBinderFails()
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

            var controllerContext = GetControllerContext(actionDescriptor);
            var arguments = new Dictionary<string, object>(StringComparer.Ordinal);

            var binder = new Mock<IModelBinder>();
            binder
                .Setup(b => b.BindModelAsync(It.IsAny<DefaultModelBindingContext>()))
                .Returns(Task.CompletedTask);

            var mockValidator = CreateMockValidator();
            var factory = GetModelBinderFactory(binder.Object);
            var controller = new TestController();
            var parameterBinder = GetParameterBinder(factory, mockValidator.Object);

            // Act
            var binderDelegate = ControllerBinderDelegateProvider.CreateBinderDelegate(
                parameterBinder,
                factory,
                TestModelMetadataProvider.CreateDefaultProvider(),
                actionDescriptor);

            await binderDelegate(controllerContext, controller, arguments);

            // Assert
            mockValidator
                .Verify(o => o.Validate(
                    It.IsAny<ModelValidationContext>()),
                Times.Never());
        }

        [Fact]
        public async Task BindActionArgumentsAsync_CallsValidator_ForControllerProperties_IfModelBinderSucceeds()
        {
            // Arrange
            var actionDescriptor = GetActionDescriptor();
            actionDescriptor.BoundProperties.Add(
                new ParameterDescriptor
                {
                    Name = nameof(TestController.ValidatedProperty),
                    ParameterType = typeof(string),
                });

            var controllerContext = GetControllerContext(actionDescriptor);
            var controller = new TestController();
            var arguments = new Dictionary<string, object>(StringComparer.Ordinal);

            var mockValidator = CreateMockValidator();
            mockValidator
                .Setup(o => o.Validate(It.IsAny<ModelValidationContext>()))
                .Returns(new[] { new ModelValidationResult("memberName", "some message") });

            var factory = GetModelBinderFactory("Hello");
            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var parameterBinder = GetParameterBinder(factory, mockValidator.Object, modelMetadataProvider);

            // Act
            var binderDelegate = ControllerBinderDelegateProvider.CreateBinderDelegate(
                parameterBinder,
                factory,
                modelMetadataProvider,
                actionDescriptor);

            await binderDelegate(controllerContext, controller, arguments);

            // Assert
            mockValidator
                .Verify(o => o.Validate(
                    It.IsAny<ModelValidationContext>()),
                Times.Once());
            Assert.False(controllerContext.ModelState.IsValid);
            Assert.Equal(
                "some message",
                controllerContext.ModelState["memberName"].Errors.Single().ErrorMessage);
        }

        [Fact]
        public async Task BindActionArgumentsAsync_DoesNotCallValidator_ForControllerProperties_IfModelBinderFails()
        {
            // Arrange
            var actionDescriptor = GetActionDescriptor();
            actionDescriptor.BoundProperties.Add(
                new ParameterDescriptor
                {
                    Name = nameof(TestController.StringProperty),
                    ParameterType = typeof(string),
                });

            var controllerContext = GetControllerContext(actionDescriptor);
            var controller = new TestController();
            var arguments = new Dictionary<string, object>(StringComparer.Ordinal);

            var binder = new Mock<IModelBinder>();
            binder
                .Setup(b => b.BindModelAsync(It.IsAny<DefaultModelBindingContext>()))
                .Returns(Task.CompletedTask);

            var mockValidator = CreateMockValidator();
            var factory = GetModelBinderFactory(binder.Object);
            var parameterBinder = GetParameterBinder(factory, mockValidator.Object);

            // Act
            var binderDelegate = ControllerBinderDelegateProvider.CreateBinderDelegate(
                parameterBinder,
                factory,
                TestModelMetadataProvider.CreateDefaultProvider(),
                actionDescriptor);

            await binderDelegate(controllerContext, controller, arguments);

            // Assert
            mockValidator
                .Verify(o => o.Validate(
                    It.IsAny<ModelValidationContext>()),
                Times.Never());
        }

        [Fact]
        public async Task BindActionArgumentsAsync_SetsControllerProperties_ForReferenceTypes()
        {
            // Arrange
            var actionDescriptor = GetActionDescriptor();
            actionDescriptor.BoundProperties.Add(
                new ParameterDescriptor
                {
                    Name = nameof(TestController.StringProperty),
                    BindingInfo = new BindingInfo(),
                    ParameterType = typeof(string)
                });

            var controllerContext = GetControllerContext(actionDescriptor);
            var controller = new TestController();
            var arguments = new Dictionary<string, object>(StringComparer.Ordinal);

            var factory = GetModelBinderFactory("Hello");
            var parameterBinder = GetParameterBinder(factory);


            // Act
            var binderDelegate = ControllerBinderDelegateProvider.CreateBinderDelegate(
                parameterBinder,
                factory,
                TestModelMetadataProvider.CreateDefaultProvider(),
                actionDescriptor);

            await binderDelegate(controllerContext, controller, arguments);

            // Assert
            Assert.Equal("Hello", controller.StringProperty);
            Assert.Equal(new List<string> { "goodbye" }, controller.CollectionProperty);
            Assert.Null(controller.UntouchedProperty);
        }

        [Fact]
        public async Task BindActionArgumentsAsync_AddsToCollectionControllerProperties()
        {
            // Arrange
            var actionDescriptor = GetActionDescriptor();
            actionDescriptor.BoundProperties.Add(
                new ParameterDescriptor
                {
                    Name = nameof(TestController.CollectionProperty),
                    BindingInfo = new BindingInfo(),
                    ParameterType = typeof(ICollection<string>),
                });

            var controllerContext = GetControllerContext(actionDescriptor);
            var controller = new TestController();
            var arguments = new Dictionary<string, object>(StringComparer.Ordinal);

            var expected = new List<string> { "Hello", "World", "!!" };
            var factory = GetModelBinderFactory(expected);
            var parameterBinder = GetParameterBinder(factory);

            // Act
            var binderDelegate = ControllerBinderDelegateProvider.CreateBinderDelegate(
                parameterBinder,
                factory,
                TestModelMetadataProvider.CreateDefaultProvider(),
                actionDescriptor);

            await binderDelegate(controllerContext, controller, arguments);

            // Assert
            Assert.Equal(expected, controller.CollectionProperty);
            Assert.Null(controller.StringProperty);
            Assert.Null(controller.UntouchedProperty);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task BindActionArgumentsAsync_DoesNotSetNullValues_ForNonNullableProperties(bool isModelSet)
        {
            // Arrange
            var actionDescriptor = GetActionDescriptor();
            actionDescriptor.BoundProperties.Add(
                new ParameterDescriptor
                {
                    Name = nameof(TestController.NonNullableProperty),
                    BindingInfo = new BindingInfo() { BindingSource = BindingSource.Custom },
                    ParameterType = typeof(int)
                });

            var controllerContext = GetControllerContext(actionDescriptor);
            var controller = new TestController();
            var arguments = new Dictionary<string, object>(StringComparer.Ordinal);

            var binder = new StubModelBinder(ModelBindingResult.Success(model: null));
            var factory = GetModelBinderFactory(binder);
            var parameterBinder = GetParameterBinder(factory);


            // Some non default value.
            controller.NonNullableProperty = -1;

            // Act
            var binderDelegate = ControllerBinderDelegateProvider.CreateBinderDelegate(
                parameterBinder,
                factory,
                TestModelMetadataProvider.CreateDefaultProvider(),
                actionDescriptor);

            await binderDelegate(controllerContext, controller, arguments);

            // Assert
            Assert.Equal(-1, controller.NonNullableProperty);
        }

        [Fact]
        public async Task BindActionArgumentsAsync_SetsNullValues_ForNullableProperties()
        {
            // Arrange
            var actionDescriptor = GetActionDescriptor();
            actionDescriptor.BoundProperties.Add(
                new ParameterDescriptor
                {
                    Name = "NullableProperty",
                    BindingInfo = new BindingInfo() { BindingSource = BindingSource.Custom },
                    ParameterType = typeof(int?)
                });

            var controllerContext = GetControllerContext(actionDescriptor);
            var controller = new TestController();
            var arguments = new Dictionary<string, object>(StringComparer.Ordinal);

            var binder = new StubModelBinder(ModelBindingResult.Success(model: null));
            var factory = GetModelBinderFactory(binder);
            var parameterBinder = GetParameterBinder(factory);


            // Some non default value.
            controller.NullableProperty = -1;

            // Act
            var binderDelegate = ControllerBinderDelegateProvider.CreateBinderDelegate(
                parameterBinder,
                factory,
                TestModelMetadataProvider.CreateDefaultProvider(),
                actionDescriptor);

            await binderDelegate(controllerContext, controller, arguments);

            // Assert
            Assert.Null(controller.NullableProperty);
        }

        [Fact]
        public async Task BindActionArgumentsAsync_SupportsRequestPredicate_ForPropertiesAndParameters_NotBound()
        {
            // Arrange
            var actionDescriptor = GetActionDescriptor();

            actionDescriptor.Parameters.Add(new ParameterDescriptor
            {
                Name = "test-parameter",
                BindingInfo = new BindingInfo()
                {
                    BindingSource = BindingSource.Custom,

                    // Simulates [BindProperty] on a parameter
                    RequestPredicate = ((IRequestPredicateProvider)new BindPropertyAttribute()).RequestPredicate,
                },
                ParameterType = typeof(string)
            });

            actionDescriptor.BoundProperties.Add(new ParameterDescriptor
            {
                Name = nameof(TestController.NullableProperty),
                BindingInfo = new BindingInfo()
                {
                    BindingSource = BindingSource.Custom,

                    // Simulates [BindProperty] on a property
                    RequestPredicate = ((IRequestPredicateProvider)new BindPropertyAttribute()).RequestPredicate,
                },
                ParameterType = typeof(string)
            });

            var controllerContext = GetControllerContext(actionDescriptor);
            controllerContext.HttpContext.Request.Method = "GET";

            var controller = new TestController();
            var arguments = new Dictionary<string, object>(StringComparer.Ordinal);

            var binder = new StubModelBinder(ModelBindingResult.Success(model: null));
            var factory = GetModelBinderFactory(binder);
            var parameterBinder = GetParameterBinder(factory);

            // Some non default value.
            controller.NullableProperty = -1;

            // Act
            var binderDelegate = ControllerBinderDelegateProvider.CreateBinderDelegate(
                parameterBinder,
                factory,
                TestModelMetadataProvider.CreateDefaultProvider(),
                actionDescriptor);

            await binderDelegate(controllerContext, controller, arguments);

            // Assert
            Assert.Equal(-1, controller.NullableProperty);
            Assert.DoesNotContain("test-parameter", arguments.Keys);
        }

        [Fact]
        public async Task BindActionArgumentsAsync_SupportsRequestPredicate_ForPropertiesAndParameters_Bound()
        {
            // Arrange
            var actionDescriptor = GetActionDescriptor();

            actionDescriptor.Parameters.Add(new ParameterDescriptor
            {
                Name = "test-parameter",
                BindingInfo = new BindingInfo()
                {
                    BindingSource = BindingSource.Custom,

                    // Simulates [BindProperty] on a parameter
                    RequestPredicate = ((IRequestPredicateProvider)new BindPropertyAttribute()).RequestPredicate,
                },
                ParameterType = typeof(string)
            });

            actionDescriptor.BoundProperties.Add(new ParameterDescriptor
            {
                Name = nameof(TestController.NullableProperty),
                BindingInfo = new BindingInfo()
                {
                    BindingSource = BindingSource.Custom,

                    // Simulates [BindProperty] on a property
                    RequestPredicate = ((IRequestPredicateProvider)new BindPropertyAttribute()).RequestPredicate,
                },
                ParameterType = typeof(string)
            });

            var controllerContext = GetControllerContext(actionDescriptor);
            controllerContext.HttpContext.Request.Method = "POST";

            var controller = new TestController();
            var arguments = new Dictionary<string, object>(StringComparer.Ordinal);

            var binder = new StubModelBinder(ModelBindingResult.Success(model: null));
            var factory = GetModelBinderFactory(binder);
            var parameterBinder = GetParameterBinder(factory);

            // Some non default value.
            controller.NullableProperty = -1;

            // Act
            var binderDelegate = ControllerBinderDelegateProvider.CreateBinderDelegate(
                parameterBinder,
                factory,
                TestModelMetadataProvider.CreateDefaultProvider(),
                actionDescriptor);

            await binderDelegate(controllerContext, controller, arguments);

            // Assert
            Assert.Null(controller.NullableProperty);
            Assert.Contains("test-parameter", arguments.Keys);
            Assert.Null(arguments["test-parameter"]);
        }

        // property name, property type, property accessor, input value, expected value
        public static TheoryData<string, Type, Func<object, object>, object, object> SkippedPropertyData
        {
            get
            {
                return new TheoryData<string, Type, Func<object, object>, object, object>
                {
                    {
                        nameof(TestController.ArrayProperty),
                        typeof(string[]),
                        controller => ((TestController)controller).ArrayProperty,
                        new string[] { "hello", "world" },
                        new string[] { "goodbye" }
                    },
                    {
                        nameof(TestController.CollectionProperty),
                        typeof(ICollection<string>),
                        controller => ((TestController)controller).CollectionProperty,
                        null,
                        new List<string> { "goodbye" }
                    },
                    {
                        nameof(TestController.NonCollectionProperty),
                        typeof(Person),
                        controller => ((TestController)controller).NonCollectionProperty,
                        new Person { Name = "Fred" },
                        new Person { Name = "Ginger" }
                    },
                    {
                        nameof(TestController.NullCollectionProperty),
                        typeof(ICollection<string>),
                        controller => ((TestController)controller).NullCollectionProperty,
                        new List<string> { "hello", "world" },
                        null
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(SkippedPropertyData))]
        public async Task BindActionArgumentsAsync_SkipsReadOnlyControllerProperties(
            string propertyName,
            Type propertyType,
            Func<object, object> propertyAccessor,
            object inputValue,
            object expectedValue)
        {
            // Arrange
            var actionDescriptor = GetActionDescriptor();
            actionDescriptor.BoundProperties.Add(
                new ParameterDescriptor
                {
                    Name = propertyName,
                    BindingInfo = new BindingInfo(),
                    ParameterType = propertyType,
                });

            var controllerContext = GetControllerContext(actionDescriptor);
            var controller = new TestController();
            var arguments = new Dictionary<string, object>(StringComparer.Ordinal);

            var factory = GetModelBinderFactory(inputValue);
            var parameterBinder = GetParameterBinder(factory);

            // Act
            var binderDelegate = ControllerBinderDelegateProvider.CreateBinderDelegate(
                parameterBinder,
                factory,
                TestModelMetadataProvider.CreateDefaultProvider(),
                actionDescriptor);

            await binderDelegate(controllerContext, controller, arguments);

            // Assert
            Assert.Equal(expectedValue, propertyAccessor(controller));
            Assert.Null(controller.StringProperty);
            Assert.Null(controller.UntouchedProperty);
        }

        [Fact]
        public async Task BindActionArgumentsAsync_SetsMultipleControllerProperties()
        {
            // Arrange
            var boundPropertyTypes = new Dictionary<string, Type>
            {
                { nameof(TestController.ArrayProperty), typeof(string[]) },                // Skipped
                { nameof(TestController.CollectionProperty), typeof(List<string>) },
                { nameof(TestController.NonCollectionProperty), typeof(Person) },          // Skipped
                { nameof(TestController.NullCollectionProperty), typeof(List<string>) },   // Skipped
                { nameof(TestController.StringProperty), typeof(string) },
            };
            var inputPropertyValues = new Dictionary<string, object>
            {
                { nameof(TestController.ArrayProperty), new string[] { "hello", "world" } },
                { nameof(TestController.CollectionProperty), new List<string> { "hello", "world" } },
                { nameof(TestController.NonCollectionProperty), new Person { Name = "Fred" } },
                { nameof(TestController.NullCollectionProperty), new List<string> { "hello", "world" } },
                { nameof(TestController.StringProperty), "Hello" },
            };
            var expectedPropertyValues = new Dictionary<string, object>
            {
                { nameof(TestController.ArrayProperty), new string[] { "goodbye" } },
                { nameof(TestController.CollectionProperty), new List<string> { "hello", "world" } },
                { nameof(TestController.NonCollectionProperty), new Person { Name = "Ginger" } },
                { nameof(TestController.NullCollectionProperty), null },
                { nameof(TestController.StringProperty), "Hello" },
            };

            var actionDescriptor = GetActionDescriptor();
            foreach (var keyValuePair in boundPropertyTypes)
            {
                actionDescriptor.BoundProperties.Add(
                    new ParameterDescriptor
                    {
                        Name = keyValuePair.Key,
                        BindingInfo = new BindingInfo(),
                        ParameterType = keyValuePair.Value,
                    });
            }

            var controllerContext = GetControllerContext(actionDescriptor);
            var controller = new TestController();
            var arguments = new Dictionary<string, object>(StringComparer.Ordinal);

            var binder = new StubModelBinder(bindingContext =>
            {
                // BindingContext.ModelName will be string.Empty here. This is a 'fallback to empty prefix'
                // because the value providers have no data.
                object model;
                if (inputPropertyValues.TryGetValue(bindingContext.FieldName, out model))
                {
                    bindingContext.Result = ModelBindingResult.Success(model);
                }
                else
                {
                    bindingContext.Result = ModelBindingResult.Failed();
                }
            });

            var factory = GetModelBinderFactory(binder);
            controllerContext.ValueProviderFactories.Add(new SimpleValueProviderFactory());

            var parameterBinder = GetParameterBinder(factory);

            // Act
            var binderDelegate = ControllerBinderDelegateProvider.CreateBinderDelegate(
                parameterBinder,
                factory,
                TestModelMetadataProvider.CreateDefaultProvider(),
                actionDescriptor);

            await binderDelegate(controllerContext, controller, arguments);

            // Assert
            Assert.Equal(new string[] { "goodbye" }, controller.ArrayProperty);                 // Skipped
            Assert.Equal(new List<string> { "hello", "world" }, controller.CollectionProperty);
            Assert.Equal(new Person { Name = "Ginger" }, controller.NonCollectionProperty);     // Skipped
            Assert.Null(controller.NullCollectionProperty);                                     // Skipped
            Assert.Null(controller.UntouchedProperty);                                          // Not bound
            Assert.Equal("Hello", controller.StringProperty);
        }

        private class TransferInfo
        {
            [Range(25, 50)]
            public int AccountId { get; set; }

            public double Amount { get; set; }
        }

        public static TheoryData<List<ParameterDescriptor>> MultipleActionParametersAndValidationData
        {
            get
            {
                return new TheoryData<List<ParameterDescriptor>>
                {
                    // Irrespective of the order in which the parameters are defined on the action,
                    // the validation on the TransferInfo's AccountId should occur.
                    // Here 'accountId' parameter is bound by the prefix 'accountId' while the 'transferInfo'
                    // property is bound using the empty prefix and the 'TransferInfo' property names.
                    new List<ParameterDescriptor>()
                    {
                        new ParameterDescriptor()
                        {
                            Name = "accountId",
                            ParameterType = typeof(int)
                        },
                        new ParameterDescriptor()
                        {
                            Name = "transferInfo",
                            ParameterType = typeof(TransferInfo),
                            BindingInfo = new BindingInfo()
                            {
                                BindingSource = BindingSource.Body
                            }
                        }
                    },
                    new List<ParameterDescriptor>()
                    {
                        new ParameterDescriptor()
                        {
                            Name = "transferInfo",
                            ParameterType = typeof(TransferInfo),
                            BindingInfo = new BindingInfo()
                            {
                                BindingSource = BindingSource.Body
                            }
                        },
                        new ParameterDescriptor()
                        {
                            Name = "accountId",
                            ParameterType = typeof(int)
                        }
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(MultipleActionParametersAndValidationData))]
        public async Task MultipleActionParameter_ValidModelState(List<ParameterDescriptor> parameters)
        {
            // Since validation attribute is only present on the FromBody model's property(TransferInfo's AccountId),
            // validation should not trigger for the parameter which is bound from Uri.

            // Arrange
            var actionDescriptor = new ControllerActionDescriptor()
            {
                BoundProperties = new List<ParameterDescriptor>(),
                Parameters = parameters
            };
            var modelMetadataProvider = new EmptyModelMetadataProvider();
            var modelBinderProvider = new BodyModelBinderProvider(new[] { Mock.Of<IInputFormatter>() }, Mock.Of<IHttpRequestStreamReaderFactory>());
            var factory = TestModelBinderFactory.CreateDefault(modelBinderProvider);
            var modelValidatorProvider = new Mock<IModelValidatorProvider>(MockBehavior.Strict).Object;
            var parameterBinder = new Mock<ParameterBinder>(
                new EmptyModelMetadataProvider(),
                factory,
                modelValidatorProvider);
            parameterBinder.Setup(p => p.BindModelAsync(
                It.IsAny<ActionContext>(),
                It.IsAny<IModelBinder>(),
                It.IsAny<IValueProvider>(),
                It.IsAny<ParameterDescriptor>(),
                It.IsAny<ModelMetadata>(),
                null))
                .Returns((ActionContext context, IModelBinder modelBinder, IValueProvider valueProvider, ParameterDescriptor descriptor, ModelMetadata metadata, object v) =>
                {
                    ModelBindingResult result;
                    if (descriptor.Name == "accountId")
                    {
                        result = ModelBindingResult.Success(10);
                    }
                    else if (descriptor.Name == "transferInfo")
                    {
                        result = ModelBindingResult.Success(new TransferInfo
                        {
                            AccountId = 40,
                            Amount = 250.0
                        });
                    }
                    else
                    {
                        result = ModelBindingResult.Failed();
                    }

                    return Task.FromResult(result);
                });

            var controllerContext = new ControllerContext
            {
                ActionDescriptor = actionDescriptor,
            };

            var arguments = new Dictionary<string, object>(StringComparer.Ordinal);
            var modelState = controllerContext.ModelState;

            // Act
            var binderDelegate = ControllerBinderDelegateProvider.CreateBinderDelegate(
                parameterBinder.Object,
                factory,
                TestModelMetadataProvider.CreateDefaultProvider(),
                actionDescriptor);

            await binderDelegate(controllerContext, new TestController(), arguments);

            // Assert
            Assert.True(modelState.IsValid);
            Assert.True(arguments.TryGetValue("accountId", out var value));
            var accountId = Assert.IsType<int>(value);
            Assert.Equal(10, accountId);
            Assert.True(arguments.TryGetValue("transferInfo", out value));
            var transferInfo = Assert.IsType<TransferInfo>(value);
            Assert.NotNull(transferInfo);
            Assert.Equal(40, transferInfo.AccountId);
            Assert.Equal(250.0, transferInfo.Amount);
        }

        private static ControllerContext GetControllerContext(ControllerActionDescriptor descriptor = null)
        {
            var context = new ControllerContext()
            {
                ActionDescriptor = descriptor ?? GetActionDescriptor(),
                HttpContext = new DefaultHttpContext(),
                RouteData = new RouteData(),
            };

            context.ValueProviderFactories.Add(new SimpleValueProviderFactory());
            return context;
        }

        private static ControllerActionDescriptor GetActionDescriptor()
        {
            Func<object, int> method = foo => 1;
            return new ControllerActionDescriptor
            {
                MethodInfo = method.GetMethodInfo(),
                ControllerTypeInfo = typeof(TestController).GetTypeInfo(),
                BoundProperties = new List<ParameterDescriptor>(),
                Parameters = new List<ParameterDescriptor>()
            };
        }

        private static ModelBinderFactory GetModelBinderFactory(object model = null)
        {
            var binder = new Mock<IModelBinder>();
            binder
                .Setup(b => b.BindModelAsync(It.IsAny<DefaultModelBindingContext>()))
                .Returns<DefaultModelBindingContext>(mbc =>
                {
                    mbc.Result = ModelBindingResult.Success(model);
                    return Task.CompletedTask;
                });

            return GetModelBinderFactory(binder.Object);
        }

        private static ModelBinderFactory GetModelBinderFactory(IModelBinder binder)
        {
            var provider = new Mock<IModelBinderProvider>();
            provider
                .Setup(p => p.GetBinder(It.IsAny<ModelBinderProviderContext>()))
                .Returns(binder);

            return TestModelBinderFactory.Create(provider.Object);
        }

        private static ParameterBinder GetParameterBinder(
            IModelBinderFactory factory = null,
            IModelValidator validator = null,
            IModelMetadataProvider modelMetadataProvider = null)
        {
            if (validator == null)
            {
                validator = CreateMockValidator().Object;
            }

            if (factory == null)
            {
                factory = TestModelBinderFactory.CreateDefault();
            }

            var validatorProvider = new Mock<IModelValidatorProvider>();
            validatorProvider
                .Setup(p => p.CreateValidators(It.IsAny<ModelValidatorProviderContext>()))
                .Callback<ModelValidatorProviderContext>(context =>
                {
                    foreach (var result in context.Results)
                    {
                        result.Validator = validator;
                        result.IsReusable = true;
                    }
                });

            return new ParameterBinder(
                modelMetadataProvider ?? TestModelMetadataProvider.CreateDefaultProvider(),
                factory,
                validatorProvider.Object);
        }

        private static Mock<IModelValidator> CreateMockValidator()
        {
            var mockValidator = new Mock<IModelValidator>(MockBehavior.Strict);
            mockValidator
                .Setup(o => o.Validate(
                    It.IsAny<ModelValidationContext>()));
            return mockValidator;
        }

        // No need for bind-related attributes on properties in this controller class. Properties are added directly
        // to the BoundProperties collection, bypassing usual requirements.
        private class TestController
        {
            public string UntouchedProperty { get; set; }

            public string[] ArrayProperty { get; } = new string[] { "goodbye" };

            public ICollection<string> CollectionProperty { get; } = new List<string> { "goodbye" };

            public Person NonCollectionProperty { get; } = new Person { Name = "Ginger" };

            public ICollection<string> NullCollectionProperty { get; private set; }

            public string StringProperty { get; set; }

            public int NonNullableProperty { get; set; }

            public int? NullableProperty { get; set; }

            [CustomValidation("Test message")] public string ValidatedProperty { get; set; }

            // Despite being "required", the BindNever means this property won't be involved
            // in binding, so no validation will be performed
            [Required, BindNever] public string RequiredButBindNeverProperty { get; set; }
        }

        private class Person : IEquatable<Person>, IEquatable<object>
        {
            public string Name { get; set; }

            public bool Equals(Person other)
            {
                return other != null && string.Equals(Name, other.Name, StringComparison.Ordinal);
            }

            bool IEquatable<object>.Equals(object obj)
            {
                return Equals(obj as Person);
            }
        }

        private class CustomBindingSourceAttribute : Attribute, IBindingSourceMetadata
        {
            public BindingSource BindingSource { get { return BindingSource.Custom; } }
        }

        private class ValueProviderMetadataAttribute : Attribute, IBindingSourceMetadata
        {
            public BindingSource BindingSource { get { return BindingSource.Query; } }
        }

        private class CustomValidationAttribute : Attribute, IModelValidator
        {
            public string Message { get; }

            public CustomValidationAttribute(string message)
            {
                Message = message;
            }

            public IEnumerable<ModelValidationResult> Validate(ModelValidationContext context)
            {
                yield return new ModelValidationResult(context.ModelMetadata.BinderModelName, Message);
            }
        }

        private class ParameterInfos
        {
            public void Method(
                object param1,
                [BindNever] object param2,
                [CustomValidation("some message")] string param3)
            {
            }

            public static ParameterInfo NoAttributesParameterInfo
                = typeof(ParameterInfos)
                    .GetMethod(nameof(ParameterInfos.Method))
                    .GetParameters()[0];

            public static ParameterInfo BindNeverParameterInfo
                = typeof(ParameterInfos)
                    .GetMethod(nameof(ParameterInfos.Method))
                    .GetParameters()[1];

            public static ParameterInfo CustomValidationParameterInfo
                = typeof(ParameterInfos)
                    .GetMethod(nameof(ParameterInfos.Method))
                    .GetParameters()[2];
        }

        public abstract class FakeModelMetadata : ModelMetadata
        {
            public FakeModelMetadata()
                : base(ModelMetadataIdentity.ForType(typeof(string)))
            {
            }
        }
    }
}
