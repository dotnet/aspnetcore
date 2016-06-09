// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class DefaultControllerArgumentBinderTests
    {
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
                .Returns(TaskCache.CompletedTask);
            var factory = GetModelBinderFactory(binder.Object);
            var argumentBinder = GetArgumentBinder(factory);

            var controllerContext = GetControllerContext(actionDescriptor);
            var controller = new TestController();
            var arguments = new Dictionary<string, object>(StringComparer.Ordinal);

            // Act
            await argumentBinder.BindArgumentsAsync(controllerContext, controller, arguments);

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
                .Returns(TaskCache.CompletedTask);
            var factory = GetModelBinderFactory(binder.Object);
            var argumentBinder = GetArgumentBinder(factory);

            var controllerContext = GetControllerContext(actionDescriptor);
            var controller = new TestController();
            var arguments = new Dictionary<string, object>(StringComparer.Ordinal);

            // Act
            await argumentBinder.BindArgumentsAsync(controllerContext, controller, arguments);

            // Assert
            Assert.Empty(arguments);
        }

        [Fact]
        public async Task BindActionArgumentsAsync_AddsActionArguments_IfBinderReturnsNotNull()
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
                .Setup(b => b.BindModelAsync(It.IsAny<DefaultModelBindingContext>()))
                .Callback((ModelBindingContext context) =>
                {
                    context.ModelMetadata = metadataProvider.GetMetadataForType(typeof(string));
                    context.Result = ModelBindingResult.Success(value);
                })
                .Returns(TaskCache.CompletedTask);
            var factory = GetModelBinderFactory(binder.Object);
            var argumentBinder = GetArgumentBinder(factory);

            var controllerContext = GetControllerContext(actionDescriptor);
            var controller = new TestController();
            var arguments = new Dictionary<string, object>(StringComparer.Ordinal);

            // Act
            await argumentBinder.BindArgumentsAsync(controllerContext, controller, arguments);

            // Assert
            Assert.Equal(1, arguments.Count);
            Assert.Equal(value, arguments["foo"]);
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

            var controllerContext = GetControllerContext(actionDescriptor);

            var factory = GetModelBinderFactory("Hello");

            var mockValidator = new Mock<IObjectModelValidator>(MockBehavior.Strict);
            mockValidator
                .Setup(o => o.Validate(
                    It.IsAny<ActionContext>(),
                    It.IsAny<ValidationStateDictionary>(),
                    It.IsAny<string>(),
                    It.IsAny<object>()));

            var argumentBinder = GetArgumentBinder(factory, mockValidator.Object);
            var controller = new TestController();
            var arguments = new Dictionary<string, object>(StringComparer.Ordinal);

            // Act
            await argumentBinder.BindArgumentsAsync(controllerContext, controller, arguments);

            // Assert
            mockValidator
                .Verify(o => o.Validate(
                    It.IsAny<ActionContext>(),
                    It.IsAny<ValidationStateDictionary>(),
                    It.IsAny<string>(),
                    It.IsAny<object>()),
            Times.Once());
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

            var controllerContext = GetControllerContext(actionDescriptor);
            var arguments = new Dictionary<string, object>(StringComparer.Ordinal);

            var binder = new Mock<IModelBinder>();
            binder
                .Setup(b => b.BindModelAsync(It.IsAny<DefaultModelBindingContext>()))
                .Returns(TaskCache.CompletedTask);

            var mockValidator = new Mock<IObjectModelValidator>(MockBehavior.Strict);
            mockValidator
                .Setup(o => o.Validate(
                    It.IsAny<ActionContext>(),
                    It.IsAny<ValidationStateDictionary>(),
                    It.IsAny<string>(),
                    It.IsAny<object>()));

            var factory = GetModelBinderFactory(binder.Object);
            var controller = new TestController();
            var argumentBinder = GetArgumentBinder(factory, mockValidator.Object);

            // Act
            await argumentBinder.BindArgumentsAsync(controllerContext, controller, arguments);

            // Assert
            mockValidator
                .Verify(o => o.Validate(
                    It.IsAny<ActionContext>(),
                    It.IsAny<ValidationStateDictionary>(),
                    It.IsAny<string>(),
                    It.IsAny<object>()),
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
                    Name = nameof(TestController.StringProperty),
                    ParameterType = typeof(string),
                });

            var controllerContext = GetControllerContext(actionDescriptor);
            var controller = new TestController();
            var arguments = new Dictionary<string, object>(StringComparer.Ordinal);

            var mockValidator = new Mock<IObjectModelValidator>(MockBehavior.Strict);
            mockValidator
                .Setup(o => o.Validate(
                    It.IsAny<ActionContext>(),
                    It.IsAny<ValidationStateDictionary>(),
                    It.IsAny<string>(),
                    It.IsAny<object>()));

            var factory = GetModelBinderFactory("Hello");
            var argumentBinder = GetArgumentBinder(factory, mockValidator.Object);

            // Act
            await argumentBinder.BindArgumentsAsync(controllerContext, controller, arguments);

            // Assert
            mockValidator
                .Verify(o => o.Validate(
                    It.IsAny<ActionContext>(),
                    It.IsAny<ValidationStateDictionary>(),
                    It.IsAny<string>(),
                    It.IsAny<object>()),
                Times.Once());
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
                    Name = nameof(TestController.StringProperty),
                    ParameterType = typeof(string),
                });

            var controllerContext = GetControllerContext(actionDescriptor);
            var controller = new TestController();
            var arguments = new Dictionary<string, object>(StringComparer.Ordinal);

            var binder = new Mock<IModelBinder>();
            binder
                .Setup(b => b.BindModelAsync(It.IsAny<DefaultModelBindingContext>()))
                .Returns(TaskCache.CompletedTask);

            var mockValidator = new Mock<IObjectModelValidator>(MockBehavior.Strict);
            mockValidator
                .Setup(o => o.Validate(
                    It.IsAny<ActionContext>(),
                    It.IsAny<ValidationStateDictionary>(),
                    It.IsAny<string>(),
                    It.IsAny<object>()));

            var factory = GetModelBinderFactory(binder.Object);
            var argumentBinder = GetArgumentBinder(factory, mockValidator.Object);

            // Act
            await argumentBinder.BindArgumentsAsync(controllerContext, controller, arguments);

            // Assert
            mockValidator
                .Verify(o => o.Validate(
                    It.IsAny<ActionContext>(),
                    It.IsAny<ValidationStateDictionary>(),
                    It.IsAny<string>(),
                    It.IsAny<object>()),
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
            var argumentBinder = GetArgumentBinder(factory);


            // Act
            await argumentBinder.BindArgumentsAsync(controllerContext, controller, arguments);

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
            var argumentBinder = GetArgumentBinder(factory);

            // Act
            await argumentBinder.BindArgumentsAsync(controllerContext, controller, arguments);

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
            var argumentBinder = GetArgumentBinder(factory);


            // Some non default value.
            controller.NonNullableProperty = -1;

            // Act
            await argumentBinder.BindArgumentsAsync(controllerContext, controller, arguments);

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
            var argumentBinder = GetArgumentBinder(factory);


            // Some non default value.
            controller.NullableProperty = -1;

            // Act
            await argumentBinder.BindArgumentsAsync(controllerContext, controller, arguments);

            // Assert
            Assert.Null(controller.NullableProperty);
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
            var argumentBinder = GetArgumentBinder(factory);


            // Act
            await argumentBinder.BindArgumentsAsync(controllerContext, controller, arguments);

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
                    bindingContext.Result = ModelBindingResult.Success( model);
                }
                else
                {
                    bindingContext.Result = ModelBindingResult.Failed();
                }
            });

            var factory = GetModelBinderFactory(binder);
            controllerContext.ValueProviderFactories.Add(new SimpleValueProviderFactory());

            var argumentBinder = GetArgumentBinder(factory);

            // Act
            await argumentBinder.BindArgumentsAsync(controllerContext, controller, arguments);

            // Assert
            Assert.Equal(new string[] { "goodbye" }, controller.ArrayProperty);                 // Skipped
            Assert.Equal(new List<string> { "hello", "world" }, controller.CollectionProperty);
            Assert.Equal(new Person { Name = "Ginger" }, controller.NonCollectionProperty);     // Skipped
            Assert.Null(controller.NullCollectionProperty);                                     // Skipped
            Assert.Null(controller.UntouchedProperty);                                          // Not bound
            Assert.Equal("Hello", controller.StringProperty);
        }

        public static TheoryData BindModelAsyncData
        {
            get
            {
                var emptyBindingInfo = new BindingInfo();
                var bindingInfoWithName = new BindingInfo
                {
                    BinderModelName = "bindingInfoName",
                    BinderType = typeof(Person),
                };

                // parameterBindingInfo, metadataBinderModelName, parameterName, expectedBinderModelName
                return new TheoryData<BindingInfo, string, string, string>
                {
                    // If the parameter name is not a prefix match, it is ignored. But name is required to create a
                    // ModelBindingContext.
                    { null, null, "parameterName", string.Empty },
                    { emptyBindingInfo, null, "parameterName", string.Empty },
                    { bindingInfoWithName, null, "parameterName", "bindingInfoName" },
                    { null, "modelBinderName", "parameterName", "modelBinderName" },
                    { null, null, "parameterName", string.Empty },
                    // Parameter's BindingInfo has highest precedence
                    { bindingInfoWithName, "modelBinderName", "parameterName", "bindingInfoName" },
                };
            }
        }

        [Theory]
        [MemberData(nameof(BindModelAsyncData))]
        public async Task BindModelAsync_PassesExpectedBindingInfoAndMetadata_IfPrefixDoesNotMatch(
            BindingInfo parameterBindingInfo,
            string metadataBinderModelName,
            string parameterName,
            string expectedModelName)
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();
            metadataProvider.ForType<Person>().BindingDetails(binding =>
            {
                binding.BinderModelName = metadataBinderModelName;
            });

            var metadata = metadataProvider.GetMetadataForType(typeof(Person));
            var modelBinder = new Mock<IModelBinder>();
            modelBinder
                .Setup(b => b.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Callback((ModelBindingContext context) =>
                {
                    Assert.Equal(expectedModelName, context.ModelName, StringComparer.Ordinal);
                })
                .Returns(TaskCache.CompletedTask);

            var parameterDescriptor = new ParameterDescriptor
            {
                BindingInfo = parameterBindingInfo,
                Name = parameterName,
                ParameterType = typeof(Person),
            };

            var factory = new Mock<IModelBinderFactory>(MockBehavior.Strict);
            factory
                .Setup(f => f.CreateBinder(It.IsAny<ModelBinderFactoryContext>()))
                .Callback((ModelBinderFactoryContext context) =>
                {
                    // Confirm expected data is passed through to ModelBindingFactory.
                    Assert.Same(parameterDescriptor.BindingInfo, context.BindingInfo);
                    Assert.Same(parameterDescriptor, context.CacheToken);
                    Assert.Equal(metadata, context.Metadata);
                })
                .Returns(modelBinder.Object);

            var argumentBinder = new DefaultControllerArgumentBinder(
                metadataProvider,
                factory.Object, 
                CreateMockValidator());

            var controllerContext = GetControllerContext();
            controllerContext.ActionDescriptor.Parameters.Add(parameterDescriptor);

            // Act & Assert
            await argumentBinder.BindModelAsync(parameterDescriptor, controllerContext);
        }

        [Fact]
        public async Task BindModelAsync_PassesExpectedBindingInfoAndMetadata_IfPrefixMatches()
        {
            // Arrange
            var expectedModelName = "expectedName";

            var metadataProvider = new TestModelMetadataProvider();
            var metadata = metadataProvider.GetMetadataForType(typeof(Person));
            var modelBinder = new Mock<IModelBinder>();
            modelBinder
                .Setup(b => b.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Callback((ModelBindingContext context) =>
                {
                    Assert.Equal(expectedModelName, context.ModelName, StringComparer.Ordinal);
                })
                .Returns(TaskCache.CompletedTask);

            var parameterDescriptor = new ParameterDescriptor
            {
                Name = expectedModelName,
                ParameterType = typeof(Person),
            };

            var factory = new Mock<IModelBinderFactory>(MockBehavior.Strict);
            factory
                .Setup(f => f.CreateBinder(It.IsAny<ModelBinderFactoryContext>()))
                .Callback((ModelBinderFactoryContext context) =>
                {
                    // Confirm expected data is passed through to ModelBindingFactory.
                    Assert.Null(context.BindingInfo);
                    Assert.Same(parameterDescriptor, context.CacheToken);
                    Assert.Equal(metadata, context.Metadata);
                })
                .Returns(modelBinder.Object);

            var argumentBinder = new DefaultControllerArgumentBinder(
                metadataProvider, 
                factory.Object, 
                CreateMockValidator());

            var valueProvider = new SimpleValueProvider
            {
                { expectedModelName, new object() },
            };
            var valueProviderFactory = new SimpleValueProviderFactory(valueProvider);

            var controllerContext = GetControllerContext();
            controllerContext.ActionDescriptor.Parameters.Add(parameterDescriptor);
            controllerContext.ValueProviderFactories.Insert(0, valueProviderFactory);

            // Act & Assert
            await argumentBinder.BindModelAsync(parameterDescriptor, controllerContext);
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
                    return TaskCache.CompletedTask;
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

        private static DefaultControllerArgumentBinder GetArgumentBinder(
            IModelBinderFactory factory = null,
            IObjectModelValidator validator = null)
        {
            if (validator == null)
            {
                validator = CreateMockValidator();
            }

            if (factory == null)
            {
                factory = TestModelBinderFactory.CreateDefault();
            }

            return new DefaultControllerArgumentBinder(
                TestModelMetadataProvider.CreateDefaultProvider(),
                factory,
                validator);
        }

        private static IObjectModelValidator CreateMockValidator()
        {
            var mockValidator = new Mock<IObjectModelValidator>(MockBehavior.Strict);
            mockValidator
                .Setup(o => o.Validate(
                    It.IsAny<ActionContext>(),
                    It.IsAny<ValidationStateDictionary>(),
                    It.IsAny<string>(),
                    It.IsAny<object>()));
            return mockValidator.Object;
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
    }
}
