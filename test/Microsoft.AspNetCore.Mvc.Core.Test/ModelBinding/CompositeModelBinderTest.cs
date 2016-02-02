// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Test
{
    public class CompositeModelBinderTest
    {
        [Fact]
        public async Task BindModel_SuccessfulBind_ReturnsModel()
        {
            // Arrange
            var bindingContext = new DefaultModelBindingContext
            {
                FallbackToEmptyPrefix = true,
                ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(typeof(int)),
                ModelName = "someName",
                ModelState = new ModelStateDictionary(),
                OperationBindingContext = new OperationBindingContext(),
                ValueProvider = new SimpleValueProvider
                {
                    { "someName", "dummyValue" }
                },
                ValidationState = new ValidationStateDictionary(),
                FieldName = "someName",
            };

            var mockIntBinder = new StubModelBinder(context =>
            {
                Assert.Same(bindingContext.ModelMetadata, context.ModelMetadata);
                Assert.Equal("someName", context.ModelName);
                Assert.Same(bindingContext.ValueProvider, context.ValueProvider);

                context.Result = ModelBindingResult.Success("someName", 42);
            });
            var shimBinder = CreateCompositeBinder(mockIntBinder);

            // Act
            var result = await shimBinder.BindModelResultAsync(bindingContext);

            // Assert
            Assert.NotEqual(default(ModelBindingResult), result);
            Assert.True(result.IsModelSet);
            Assert.Equal(42, result.Model);
        }

        [Fact]
        public async Task BindModel_SuccessfulBind_SetsValidationStateAtTopLevel()
        {
            // Arrange
            var bindingContext = new DefaultModelBindingContext
            {
                FallbackToEmptyPrefix = true,
                IsTopLevelObject = true,
                ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(typeof(int)),
                ModelName = "someName",
                ModelState = new ModelStateDictionary(),
                OperationBindingContext = new OperationBindingContext(),
                ValueProvider = new SimpleValueProvider
                {
                    { "someName", "dummyValue" }
                },
                ValidationState = new ValidationStateDictionary(),
                FieldName = "someName",
            };

            var mockIntBinder = new StubModelBinder(context =>
            {
                Assert.Same(bindingContext.ModelMetadata, context.ModelMetadata);
                Assert.Equal("someName", context.ModelName);
                Assert.Same(bindingContext.ValueProvider, context.ValueProvider);

                context.Result = ModelBindingResult.Success("someName", 42);
            });
            var shimBinder = CreateCompositeBinder(mockIntBinder);

            // Act
            var result = await shimBinder.BindModelResultAsync(bindingContext);

            // Assert
            Assert.NotEqual(default(ModelBindingResult), result);
            Assert.True(result.IsModelSet);
            Assert.Equal(42, result.Model);

            Assert.Contains(result.Model, bindingContext.ValidationState.Keys);
            var entry = bindingContext.ValidationState[result.Model];
            Assert.Equal("someName", entry.Key);
            Assert.Same(bindingContext.ModelMetadata, entry.Metadata);
        }

        [Fact]
        public async Task BindModel_SuccessfulBind_DoesNotSetValidationState_WhenNotTopLevel()
        {
            // Arrange
            var bindingContext = new DefaultModelBindingContext
            {
                FallbackToEmptyPrefix = true,
                ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(typeof(int)),
                ModelName = "someName",
                ModelState = new ModelStateDictionary(),
                OperationBindingContext = new OperationBindingContext(),
                ValueProvider = new SimpleValueProvider
                {
                    { "someName", "dummyValue" }
                },
                ValidationState = new ValidationStateDictionary(),
                FieldName = "someName",
            };

            var mockIntBinder = new StubModelBinder(context =>
            {
                Assert.Same(bindingContext.ModelMetadata, context.ModelMetadata);
                Assert.Equal("someName", context.ModelName);
                Assert.Same(bindingContext.ValueProvider, context.ValueProvider);

                context.Result = ModelBindingResult.Success("someName", 42);
            });
            var shimBinder = CreateCompositeBinder(mockIntBinder);

            // Act
            var result = await shimBinder.BindModelResultAsync(bindingContext);

            // Assert
            Assert.NotEqual(default(ModelBindingResult), result);
            Assert.True(result.IsModelSet);
            Assert.Equal(42, result.Model);

            Assert.Empty(bindingContext.ValidationState);
        }

        [Fact]
        public async Task BindModel_SuccessfulBind_ComplexTypeFallback_ReturnsModel()
        {
            // Arrange
            var expectedModel = new List<int> { 1, 2, 3, 4, 5 };

            var bindingContext = new DefaultModelBindingContext
            {
                FallbackToEmptyPrefix = true,
                IsTopLevelObject = true,
                ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(typeof(List<int>)),
                ModelName = "someName",
                ModelState = new ModelStateDictionary(),
                OperationBindingContext = new OperationBindingContext(),
                ValueProvider = new SimpleValueProvider
                {
                    { "someOtherName", "dummyValue" }
                },
                ValidationState = new ValidationStateDictionary(),
                FieldName = "someName",
            };

            var mockIntBinder = new StubModelBinder(mbc =>
            {
                if (!string.IsNullOrEmpty(mbc.ModelName))
                {
                    return;
                }

                Assert.Same(bindingContext.ModelMetadata, mbc.ModelMetadata);
                Assert.Equal("", mbc.ModelName);
                Assert.Same(bindingContext.ValueProvider, mbc.ValueProvider);

                mbc.Result = ModelBindingResult.Success(string.Empty, expectedModel);
            });

            var shimBinder = CreateCompositeBinder(mockIntBinder);

            // Act
            var result = await shimBinder.BindModelResultAsync(bindingContext);

            // Assert
            Assert.NotEqual(default(ModelBindingResult), result);
            Assert.True(result.IsModelSet);
            Assert.Equal(string.Empty, result.Key);
            Assert.Equal(expectedModel, result.Model);
        }

        [Fact]
        public async Task ModelBinder_ReturnsNothing_IfBinderMatchesButDoesNotSetModel()
        {
            // Arrange
            var bindingContext = new DefaultModelBindingContext
            {
                FallbackToEmptyPrefix = true,
                ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(typeof(List<int>)),
                ModelName = "someName",
                ModelState = new ModelStateDictionary(),
                OperationBindingContext = new OperationBindingContext(),
                ValueProvider = new SimpleValueProvider
                {
                    { "someOtherName", "dummyValue" }
                },
                FieldName = "someName",
            };

            var mockIntBinder = new StubModelBinder(context =>
            {
                context.Result = ModelBindingResult.Failed("someName");
            });

            var composite = CreateCompositeBinder(mockIntBinder);

            // Act
            var result = await composite.BindModelResultAsync(bindingContext);

            // Assert
            Assert.Equal(default(ModelBindingResult), result);
        }

        [Fact]
        public async Task ModelBinder_DoesNotFallBackToEmpty_IfFallbackToEmptyPrefixFalse()
        {
            // Arrange
            var bindingContext = new DefaultModelBindingContext
            {
                FallbackToEmptyPrefix = false,
                ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(typeof(List<int>)),
                ModelName = "someName",
                ModelState = new ModelStateDictionary(),
                OperationBindingContext = new OperationBindingContext(),
                ValueProvider = new SimpleValueProvider
                {
                    { "someOtherName", "dummyValue" }
                },
                FieldName = "someName",
            };

            var mockIntBinder = new StubModelBinder(context =>
            {
                Assert.Equal("someName", context.ModelName);
                context.Result = ModelBindingResult.Failed("someName");
            });

            var composite = CreateCompositeBinder(mockIntBinder);

            // Act & Assert
            var result = await composite.BindModelResultAsync(bindingContext);
            Assert.Equal(1, mockIntBinder.BindModelCount);
        }

        [Fact]
        public async Task ModelBinder_DoesNotFallBackToEmpty_IfErrorsAreAdded()
        {
            // Arrange
            var bindingContext = new DefaultModelBindingContext
            {
                FallbackToEmptyPrefix = false,
                ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(typeof(List<int>)),
                ModelName = "someName",
                ModelState = new ModelStateDictionary(),
                OperationBindingContext = new OperationBindingContext(),
                ValueProvider = new SimpleValueProvider
                {
                    { "someOtherName", "dummyValue" }
                },
                FieldName = "someName",
            };

            var mockIntBinder = new StubModelBinder(context =>
            {
                Assert.Equal("someName", context.ModelName);
                context.ModelState.AddModelError(context.ModelName, "this is an error message");
                context.Result = ModelBindingResult.Failed("someName");
            });

            var composite = CreateCompositeBinder(mockIntBinder);

            // Act & Assert
            var result = await composite.BindModelResultAsync(bindingContext);
            Assert.Equal(1, mockIntBinder.BindModelCount);
        }

        [Fact]
        public async Task ModelBinder_ReturnsNonEmptyResult_SetsNullValue_SetsModelStateKey()
        {
            // Arrange
            var bindingContext = new DefaultModelBindingContext
            {
                FallbackToEmptyPrefix = true,
                ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(typeof(List<int>)),
                ModelName = "someName",
                ModelState = new ModelStateDictionary(),
                OperationBindingContext = new OperationBindingContext(),
                ValueProvider = new SimpleValueProvider
                {
                    { "someOtherName", "dummyValue" }
                },
                FieldName = "someName",
            };

            var mockIntBinder = new StubModelBinder(context =>
            {
                context.Result = ModelBindingResult.Success("someName", model: null);
            });

            var composite = CreateCompositeBinder(mockIntBinder);

            // Act
            var result = await composite.BindModelResultAsync(bindingContext);

            // Assert
            Assert.NotEqual(default(ModelBindingResult), result);
            Assert.True(result.IsModelSet);
            Assert.Equal("someName", result.Key);
            Assert.Null(result.Model);
        }

        [Fact]
        public async Task BindModel_UnsuccessfulBind_SimpleTypeNoFallback_ReturnsNothing()
        {
            // Arrange
            var shimBinder = CreateCompositeBinder(new StubModelBinder());

            var bindingContext = new DefaultModelBindingContext
            {
                FallbackToEmptyPrefix = true,
                ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(typeof(int)),
                ModelState = new ModelStateDictionary(),
                OperationBindingContext = new OperationBindingContext(),
                ValueProvider = new SimpleValueProvider(),
                FieldName = "test-field",
            };

            // Act
            var result = await shimBinder.BindModelResultAsync(bindingContext);

            // Assert
            Assert.Equal(default(ModelBindingResult), result);
        }

        [Fact]
        public async Task BindModel_WithDefaultBinders_BindsSimpleType()
        {
            // Arrange
            var binder = CreateBinderWithDefaults();

            var valueProvider = new SimpleValueProvider
            {
                { "firstName", "firstName-value"},
                { "lastName", "lastName-value"}
            };
            var bindingContext = CreateBindingContext(binder, valueProvider, typeof(SimplePropertiesModel));

            // Act
            var result = await binder.BindModelResultAsync(bindingContext);

            // Assert
            Assert.NotEqual(default(ModelBindingResult), result);
            var model = Assert.IsType<SimplePropertiesModel>(result.Model);
            Assert.Equal("firstName-value", model.FirstName);
            Assert.Equal("lastName-value", model.LastName);
        }

        [Fact]
        public async Task BindModel_WithDefaultBinders_BindsComplexType()
        {
            // Arrange
            var binder = CreateBinderWithDefaults();

            var valueProvider = new SimpleValueProvider
            {
                { "firstName", "firstName-value"},
                { "lastName", "lastName-value"},
                { "friends[0].firstName", "first-friend"},
                { "friends[0].age", "40"},
                { "friends[0].friends[0].firstname", "nested friend"},
                { "friends[1].firstName", "some other"},
                { "friends[1].lastName", "name"},
                { "resume", "4+mFeTp3tPF=" }
            };

            var bindingContext = CreateBindingContext(binder, valueProvider, typeof(Person));

            // Act
            var result = await binder.BindModelResultAsync(bindingContext);

            // Assert
            Assert.NotEqual(default(ModelBindingResult), result);
            var model = Assert.IsType<Person>(result.Model);
            Assert.Equal("firstName-value", model.FirstName);
            Assert.Equal("lastName-value", model.LastName);
            Assert.Equal(2, model.Friends.Count);
            Assert.Equal("first-friend", model.Friends[0].FirstName);
            Assert.Equal(40, model.Friends[0].Age);
            var nestedFriend = Assert.Single(model.Friends[0].Friends);
            Assert.Equal("nested friend", nestedFriend.FirstName);
            Assert.Equal("some other", model.Friends[1].FirstName);
            Assert.Equal("name", model.Friends[1].LastName);
            Assert.Equal(new byte[] { 227, 233, 133, 121, 58, 119, 180, 241 }, model.Resume);
        }

        [Fact]
        public async Task BindModel_DoesNotAddAValidationNode_IfModelIsNotSet()
        {
            // Arrange
            var valueProvider = new SimpleValueProvider();
            var mockBinder = new StubModelBinder(ModelBindingResult.Failed("someName"));

            var binder = CreateCompositeBinder(mockBinder);
            var bindingContext = CreateBindingContext(binder, valueProvider, typeof(SimplePropertiesModel));

            // Act
            var result = await binder.BindModelResultAsync(bindingContext);

            // Assert
            Assert.Equal(default(ModelBindingResult), result);
        }

        [Fact]
        public async Task BindModel_DoesNotAddAValidationNode_IfModelBindingResultIsNothing()
        {
            // Arrange
            var mockBinder = new StubModelBinder();
            var binder = CreateCompositeBinder(mockBinder);
            var valueProvider = new SimpleValueProvider();
            var bindingContext = CreateBindingContext(binder, valueProvider, typeof(SimplePropertiesModel));

            // Act
            var result = await binder.BindModelResultAsync(bindingContext);

            // Assert
            Assert.Equal(default(ModelBindingResult), result);
        }

        [Fact]
        public async Task BindModel_UsesTheValidationNodeOnModelBindingResult_IfPresent()
        {
            // Arrange
            var valueProvider = new SimpleValueProvider();

            var mockBinder = new StubModelBinder(ModelBindingResult.Success("someName", 42));

            var binder = CreateCompositeBinder(mockBinder);
            var bindingContext = CreateBindingContext(binder, valueProvider, typeof(SimplePropertiesModel));

            // Act
            var result = await binder.BindModelResultAsync(bindingContext);

            // Assert
            Assert.NotEqual(default(ModelBindingResult), result);
            Assert.True(result.IsModelSet);
        }

        private static DefaultModelBindingContext CreateBindingContext(
            IModelBinder binder,
            IValueProvider valueProvider,
            Type type)
        {
            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var bindingContext = new DefaultModelBindingContext
            {
                FallbackToEmptyPrefix = true,
                IsTopLevelObject = true,
                ModelMetadata = metadataProvider.GetMetadataForType(type),
                ModelName = "parameter",
                FieldName = "parameter",
                ModelState = new ModelStateDictionary(),
                ValueProvider = valueProvider,
                OperationBindingContext = new OperationBindingContext
                {
                    MetadataProvider = metadataProvider,
                    ModelBinder = binder,
                },
                ValidationState = new ValidationStateDictionary(),
            };
            return bindingContext;
        }

        private static CompositeModelBinder CreateBinderWithDefaults()
        {
            var binders = new IModelBinder[]
            {
                new ByteArrayModelBinder(),
                new GenericModelBinder(),
                new SimpleTypeModelBinder(),
                new MutableObjectModelBinder()
            };

            var binder = new CompositeModelBinder(binders);
            return binder;
        }

        private static CompositeModelBinder CreateCompositeBinder(IModelBinder mockIntBinder)
        {
            var shimBinder = new CompositeModelBinder(new[] { mockIntBinder });
            return shimBinder;
        }

        private class SimplePropertiesModel
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }

        private sealed class Person
        {
            public string FirstName { get; set; }

            public string LastName { get; set; }

            public int Age { get; set; }

            public List<Person> Friends { get; set; }

            public byte[] Resume { get; set; }
        }

        private class User : IValidatableObject
        {
            public string Password { get; set; }

            [Compare("Password")]
            public string ConfirmPassword { get; set; }

            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                if (Password == "password")
                {
                    yield return new ValidationResult("Password does not meet complexity requirements.");
                }
            }
        }
    }
}
