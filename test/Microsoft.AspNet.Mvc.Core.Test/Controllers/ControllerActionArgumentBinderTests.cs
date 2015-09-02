// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Microsoft.AspNet.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Controllers
{ 
    public class ControllerActionArgumentBinderTests
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

            var actionContext = GetActionContext(actionDescriptor);

            var binder = new Mock<IModelBinder>();
            binder
                .Setup(b => b.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns(ModelBindingResult.NoResultAsync);
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
                .Returns(ModelBindingResult.FailedAsync(string.Empty));

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
                .Setup(b => b.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Callback((ModelBindingContext context) =>
                {
                    context.ModelMetadata = metadataProvider.GetMetadataForType(typeof(string));
                })
                .Returns(ModelBindingResult.SuccessAsync(string.Empty, value));

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

            var mockValidator = new Mock<IObjectModelValidator>(MockBehavior.Strict);
            mockValidator
                .Setup(o => o.Validate(
                    It.IsAny<IModelValidatorProvider>(),
                    It.IsAny<ModelStateDictionary>(),
                    It.IsAny<ValidationStateDictionary>(),
                    It.IsAny<string>(),
                    It.IsAny<object>()));

            var argumentBinder = GetArgumentBinder(mockValidator.Object);

            // Act
            var result = await argumentBinder
                .BindActionArgumentsAsync(actionContext, actionBindingContext, new TestController());

            // Assert
            mockValidator
                .Verify(o => o.Validate(
                    It.IsAny<IModelValidatorProvider>(),
                    It.IsAny<ModelStateDictionary>(),
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

            var actionContext = new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                actionDescriptor);

            var binder = new Mock<IModelBinder>();
            binder
                .Setup(b => b.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns(ModelBindingResult.NoResultAsync);

            var actionBindingContext = new ActionBindingContext()
            {
                ModelBinder = binder.Object,
            };

            var mockValidator = new Mock<IObjectModelValidator>(MockBehavior.Strict);
            mockValidator
                .Setup(o => o.Validate(
                    It.IsAny<IModelValidatorProvider>(),
                    It.IsAny<ModelStateDictionary>(),
                    It.IsAny<ValidationStateDictionary>(),
                    It.IsAny<string>(),
                    It.IsAny<object>()));

            var argumentBinder = GetArgumentBinder(mockValidator.Object);

            // Act
            var result = await argumentBinder
                .BindActionArgumentsAsync(actionContext, actionBindingContext, new TestController());

            // Assert
            mockValidator
                .Verify(o => o.Validate(
                    It.IsAny<IModelValidatorProvider>(),
                    It.IsAny<ModelStateDictionary>(),
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

            var actionContext = GetActionContext(actionDescriptor);
            var actionBindingContext = GetActionBindingContext();

            var mockValidator = new Mock<IObjectModelValidator>(MockBehavior.Strict);
            mockValidator
                .Setup(o => o.Validate(
                    It.IsAny<IModelValidatorProvider>(),
                    It.IsAny<ModelStateDictionary>(),
                    It.IsAny<ValidationStateDictionary>(),
                    It.IsAny<string>(),
                    It.IsAny<object>()));

            var argumentBinder = GetArgumentBinder(mockValidator.Object);

            // Act
            var result = await argumentBinder
                .BindActionArgumentsAsync(actionContext, actionBindingContext, new TestController());

            // Assert
            mockValidator
                .Verify(o => o.Validate(
                    It.IsAny<IModelValidatorProvider>(),
                    It.IsAny<ModelStateDictionary>(),
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

            var actionContext = new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                actionDescriptor);

            var binder = new Mock<IModelBinder>();
            binder
                .Setup(b => b.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns(ModelBindingResult.NoResultAsync);

            var actionBindingContext = new ActionBindingContext()
            {
                ModelBinder = binder.Object,
            };

            var mockValidator = new Mock<IObjectModelValidator>(MockBehavior.Strict);
            mockValidator
                .Setup(o => o.Validate(
                    It.IsAny<IModelValidatorProvider>(),
                    It.IsAny<ModelStateDictionary>(),
                    It.IsAny<ValidationStateDictionary>(),
                    It.IsAny<string>(),
                    It.IsAny<object>()));

            var argumentBinder = GetArgumentBinder(mockValidator.Object);

            // Act
            var result = await argumentBinder
                .BindActionArgumentsAsync(actionContext, actionBindingContext, new TestController());

            // Assert
            mockValidator
                .Verify(o => o.Validate(
                    It.IsAny<IModelValidatorProvider>(),
                    It.IsAny<ModelStateDictionary>(),
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

            var actionContext = GetActionContext(actionDescriptor);
            var actionBindingContext = GetActionBindingContext();
            var argumentBinder = GetArgumentBinder();
            var controller = new TestController();

            // Act
            var result = await argumentBinder.BindActionArgumentsAsync(actionContext, actionBindingContext, controller);

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

            var expected = new List<string> { "Hello", "World", "!!" };
            var actionContext = GetActionContext(actionDescriptor);
            var actionBindingContext = GetActionBindingContext(model: expected);
            var argumentBinder = GetArgumentBinder();
            var controller = new TestController();

            // Act
            var result = await argumentBinder.BindActionArgumentsAsync(actionContext, actionBindingContext, controller);

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

            var actionContext = GetActionContext(actionDescriptor);

            var binder = new Mock<IModelBinder>();
            binder
                .Setup(b => b.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns(ModelBindingResult.SuccessAsync(string.Empty, model: null));

            var actionBindingContext = new ActionBindingContext()
            {
                ModelBinder = binder.Object,
            };

            var argumentBinder = GetArgumentBinder();
            var controller = new TestController();

            // Some non default value.
            controller.NonNullableProperty = -1;

            // Act
            var result = await argumentBinder.BindActionArgumentsAsync(actionContext, actionBindingContext, controller);

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

            var actionContext = GetActionContext(actionDescriptor);

            var binder = new Mock<IModelBinder>();
            binder
                .Setup(b => b.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns(ModelBindingResult.SuccessAsync(key: string.Empty, model: null));

            var actionBindingContext = new ActionBindingContext()
            {
                ModelBinder = binder.Object,
            };

            var argumentBinder = GetArgumentBinder();
            var controller = new TestController();

            // Some non default value.
            controller.NullableProperty = -1;

            // Act
            var result = await argumentBinder.BindActionArgumentsAsync(actionContext, actionBindingContext, controller);

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

            var actionContext = GetActionContext(actionDescriptor);
            var actionBindingContext = GetActionBindingContext(model: inputValue);
            var argumentBinder = GetArgumentBinder();
            var controller = new TestController();

            // Act
            var result = await argumentBinder.BindActionArgumentsAsync(actionContext, actionBindingContext, controller);

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

            var actionContext = GetActionContext(actionDescriptor);
            var argumentBinder = GetArgumentBinder();
            var controller = new TestController();
            var binder = new Mock<IModelBinder>();
            binder
                .Setup(b => b.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns<ModelBindingContext>(bindingContext =>
                {
                    object model;
                    if (inputPropertyValues.TryGetValue(bindingContext.ModelName, out model))
                    {
                        return ModelBindingResult.SuccessAsync(bindingContext.ModelName, model);
                    }
                    else
                    {
                        return ModelBindingResult.FailedAsync(bindingContext.ModelName);
                    }
                });
            var actionBindingContext = new ActionBindingContext
            {
                ModelBinder = binder.Object,
            };

            // Act
            var result = await argumentBinder.BindActionArgumentsAsync(actionContext, actionBindingContext, controller);

            // Assert
            Assert.Equal(new string[] { "goodbye" }, controller.ArrayProperty);                 // Skipped
            Assert.Equal(new List<string> { "hello", "world" }, controller.CollectionProperty);
            Assert.Equal(new Person { Name = "Ginger" }, controller.NonCollectionProperty);     // Skipped
            Assert.Null(controller.NullCollectionProperty);                                     // Skipped
            Assert.Null(controller.UntouchedProperty);                                          // Not bound
            Assert.Equal("Hello", controller.StringProperty);
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
            return GetActionBindingContext("Hello");
        }

        private static ActionBindingContext GetActionBindingContext(object model)
        {
            var binder = new Mock<IModelBinder>();
            binder.Setup(b => b.BindModelAsync(It.IsAny<ModelBindingContext>()))
                  .Returns<ModelBindingContext>(mbc =>
                  {
                      return ModelBindingResult.SuccessAsync(string.Empty, model);
                  });

            return new ActionBindingContext()
            {
                ModelBinder = binder.Object,
            };
        }

        private static DefaultControllerActionArgumentBinder GetArgumentBinder(IObjectModelValidator validator = null)
        {
            if (validator == null)
            {
                validator = CreateMockValidator();
            }

            return new DefaultControllerActionArgumentBinder(
                TestModelMetadataProvider.CreateDefaultProvider(),
                validator);
        }

        private static IObjectModelValidator CreateMockValidator()
        {
            var mockValidator = new Mock<IObjectModelValidator>(MockBehavior.Strict);
            mockValidator
                .Setup(o => o.Validate(
                    It.IsAny<IModelValidatorProvider>(), 
                    It.IsAny<ModelStateDictionary>(),
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
