// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if DNX451
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class KeyValuePairModelBinderTest
    {
        [Fact]
        public async Task BindModel_MissingKey_ReturnsResult_AndAddsModelValidationError()
        {
            // Arrange
            var valueProvider = new SimpleHttpValueProvider();

            // Create string binder to create the value but not the key.
            var bindingContext = GetBindingContext(valueProvider, CreateStringBinder());
            var binder = new KeyValuePairModelBinder<int, string>();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Model);
            Assert.False(bindingContext.ModelState.IsValid);
            Assert.Null(result.ValidationNode);
            Assert.Equal("someName", bindingContext.ModelName);
            var error = Assert.Single(bindingContext.ModelState["someName.Key"].Errors);
            Assert.Equal("A value is required.", error.ErrorMessage);
        }

        [Fact]
        public async Task BindModel_MissingValue_ReturnsResult_AndAddsModelValidationError()
        {
            // Arrange
            var valueProvider = new SimpleHttpValueProvider();

            // Create int binder to create the value but not the key.
            var bindingContext = GetBindingContext(valueProvider, CreateIntBinder());
            var binder = new KeyValuePairModelBinder<int, string>();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Model);
            Assert.False(bindingContext.ModelState.IsValid);
            Assert.Null(result.ValidationNode);
            Assert.Equal("someName", bindingContext.ModelName);
            Assert.Equal(bindingContext.ModelState["someName.Value"].Errors.First().ErrorMessage, "A value is required.");
        }

        [Fact]
        public async Task BindModel_MissingKeyAndMissingValue_DoNotAddModelStateError()
        {
            // Arrange
            var valueProvider = new SimpleHttpValueProvider();

            // Create int binder to create the value but not the key.
            var bindingContext = GetBindingContext(valueProvider);
            var mockBinder = new Mock<IModelBinder>();
            mockBinder.Setup(o => o.BindModelAsync(It.IsAny<ModelBindingContext>()))
                      .Returns(Task.FromResult<ModelBindingResult>(null));

            bindingContext.OperationBindingContext.ModelBinder = mockBinder.Object;
            var binder = new KeyValuePairModelBinder<int, string>();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.Null(result);
            Assert.True(bindingContext.ModelState.IsValid);
            Assert.Equal(0, bindingContext.ModelState.ErrorCount);
        }

        [Fact]
        public async Task BindModel_SubBindingSucceeds()
        {
            // Arrange
            var innerBinder = new CompositeModelBinder(new[] { CreateStringBinder(), CreateIntBinder() });
            var valueProvider = new SimpleHttpValueProvider();
            var bindingContext = GetBindingContext(valueProvider, innerBinder);

            var binder = new KeyValuePairModelBinder<int, string>();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(new KeyValuePair<int, string>(42, "some-value"), result.Model);
            Assert.NotNull(result.ValidationNode);
            Assert.Equal(new KeyValuePair<int, string>(42, "some-value"), result.ValidationNode.Model);
            Assert.Equal("someName", result.ValidationNode.Key);

            var validationNode = result.ValidationNode.ChildNodes[0];
            Assert.Equal("someName.Key", validationNode.Key);
            Assert.Equal(42, validationNode.Model);
            Assert.Empty(validationNode.ChildNodes);

            validationNode = result.ValidationNode.ChildNodes[1];
            Assert.Equal("someName.Value", validationNode.Key);
            Assert.Equal("some-value", validationNode.Model);
            Assert.Empty(validationNode.ChildNodes);
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData(null, true)]
        [InlineData(42, false)]
        [InlineData(42, true)]
        public async Task TryBindStrongModel_InnerBinderReturnsNotNull_ReturnsInnerBinderResult(
            object model,
            bool isModelSet)
        {
            // Arrange
            var innerResult = new ModelBindingResult(model, key: string.Empty, isModelSet: isModelSet);
            var innerBinder = new Mock<IModelBinder>();
            innerBinder
                .Setup(o => o.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns((ModelBindingContext context) =>
                {
                    Assert.Equal("someName.key", context.ModelName);
                    return Task.FromResult(innerResult);
                });
            var bindingContext = GetBindingContext(new SimpleHttpValueProvider(), innerBinder.Object);

            var binder = new KeyValuePairModelBinder<int, string>();
            var modelValidationNodeList = new List<ModelValidationNode>();

            // Act
            var result = await binder.TryBindStrongModel<int>(bindingContext, "key", modelValidationNodeList);

            // Assert
            Assert.Same(innerResult, result);
            Assert.Empty(bindingContext.ModelState);
        }

        [Fact]
        public async Task KeyValuePairModelBinder_DoesNotCreateCollection_ForTopLevelModel_OnFirstPass()
        {
            // Arrange
            var binder = new KeyValuePairModelBinder<string, string>();

            var context = CreateContext();
            context.ModelName = "param";

            var metadataProvider = context.OperationBindingContext.MetadataProvider;
            context.ModelMetadata = metadataProvider.GetMetadataForType(typeof(KeyValuePair<string, string>));

            context.ValueProvider = new TestValueProvider(new Dictionary<string, object>());

            // Act
            var result = await binder.BindModelAsync(context);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task KeyValuePairModelBinder_CreatesEmptyCollection_ForTopLevelModel_OnFallback()
        {
            // Arrange
            var binder = new KeyValuePairModelBinder<string, string>();

            var context = CreateContext();
            context.ModelName = string.Empty;

            var metadataProvider = context.OperationBindingContext.MetadataProvider;
            context.ModelMetadata = metadataProvider.GetMetadataForType(typeof(KeyValuePair<string, string>));

            context.ValueProvider = new TestValueProvider(new Dictionary<string, object>());

            // Act
            var result = await binder.BindModelAsync(context);

            // Assert
            Assert.NotNull(result);

            Assert.Equal(default(KeyValuePair<string, string>), Assert.IsType<KeyValuePair<string, string>>(result.Model));
            Assert.Equal(string.Empty, result.Key);
            Assert.True(result.IsModelSet);

            Assert.Equal(result.ValidationNode.Model, result.Model);
            Assert.Same(result.ValidationNode.Key, result.Key);
            Assert.Same(result.ValidationNode.ModelMetadata, context.ModelMetadata);
        }

        [Fact]
        public async Task KeyValuePairModelBinder_CreatesEmptyCollection_ForTopLevelModel_WithExplicitPrefix()
        {
            // Arrange
            var binder = new KeyValuePairModelBinder<string, string>();

            var context = CreateContext();
            context.ModelName = "prefix";
            context.BinderModelName = "prefix";

            var metadataProvider = context.OperationBindingContext.MetadataProvider;
            context.ModelMetadata = metadataProvider.GetMetadataForType(typeof(KeyValuePair<string, string>));

            context.ValueProvider = new TestValueProvider(new Dictionary<string, object>());

            // Act
            var result = await binder.BindModelAsync(context);

            // Assert
            Assert.NotNull(result);

            Assert.Equal(default(KeyValuePair<string, string>), Assert.IsType<KeyValuePair<string, string>>(result.Model));
            Assert.Equal("prefix", result.Key);
            Assert.True(result.IsModelSet);

            Assert.Equal(result.ValidationNode.Model, result.Model);
            Assert.Same(result.ValidationNode.Key, result.Key);
            Assert.Same(result.ValidationNode.ModelMetadata, context.ModelMetadata);
        }

        [Theory]
        [InlineData("")]
        [InlineData("param")]
        public async Task KeyValuePairModelBinder_DoesNotCreateCollection_ForNonTopLevelModel(string prefix)
        {
            // Arrange
            var binder = new KeyValuePairModelBinder<string, string>();

            var context = CreateContext();
            context.ModelName = ModelNames.CreatePropertyModelName(prefix, "KeyValuePairProperty");

            var metadataProvider = context.OperationBindingContext.MetadataProvider;
            context.ModelMetadata = metadataProvider.GetMetadataForProperty(
                typeof(ModelWithKeyValuePairProperty),
                nameof(ModelWithKeyValuePairProperty.KeyValuePairProperty));

            context.ValueProvider = new TestValueProvider(new Dictionary<string, object>());

            // Act
            var result = await binder.BindModelAsync(context);

            // Assert
            Assert.Null(result);
        }

        private static ModelBindingContext CreateContext()
        {
            var modelBindingContext = new ModelBindingContext()
            {
                OperationBindingContext = new OperationBindingContext()
                {
                    HttpContext = new DefaultHttpContext(),
                    MetadataProvider = new TestModelMetadataProvider(),
                    ModelBinder = new TypeMatchModelBinder(),
                }
            };

            return modelBindingContext;
        }

        private static ModelBindingContext GetBindingContext(
            IValueProvider valueProvider,
            IModelBinder innerBinder = null,
            Type keyValuePairType = null)
        {
            var metataProvider = new EmptyModelMetadataProvider();
            var bindingContext = new ModelBindingContext
            {
                ModelMetadata = metataProvider.GetMetadataForType(keyValuePairType ?? typeof(KeyValuePair<int, string>)),
                ModelName = "someName",
                ValueProvider = valueProvider,
                OperationBindingContext = new OperationBindingContext
                {
                    ModelBinder = innerBinder ?? CreateIntBinder(),
                    MetadataProvider = metataProvider,
                    ValidatorProvider = new DataAnnotationsModelValidatorProvider()
                }
            };
            return bindingContext;
        }

        private static IModelBinder CreateIntBinder()
        {
            var mockIntBinder = new Mock<IModelBinder>();
            mockIntBinder
                .Setup(o => o.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns((ModelBindingContext mbc) =>
                {
                    if (mbc.ModelType == typeof(int))
                    {
                        var model = 42;
                        var validationNode = new ModelValidationNode(mbc.ModelName, mbc.ModelMetadata, model);
                        return Task.FromResult(new ModelBindingResult(model, mbc.ModelName, true, validationNode));
                    }
                    return Task.FromResult<ModelBindingResult>(null);
                });
            return mockIntBinder.Object;
        }

        private static IModelBinder CreateStringBinder()
        {
            var mockStringBinder = new Mock<IModelBinder>();
            mockStringBinder
                .Setup(o => o.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns((ModelBindingContext mbc) =>
                {
                    if (mbc.ModelType == typeof(string))
                    {
                        var model = "some-value";
                        var validationNode = new ModelValidationNode(mbc.ModelName, mbc.ModelMetadata, model);
                        return Task.FromResult(new ModelBindingResult(model, mbc.ModelName, true, validationNode));
                    }
                    return Task.FromResult<ModelBindingResult>(null);
                });
            return mockStringBinder.Object;
        }

        private class ModelWithKeyValuePairProperty
        {
            public KeyValuePair<string, string> KeyValuePairProperty { get; set; }
        }
    }
}
#endif
