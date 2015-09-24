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
            var valueProvider = new SimpleValueProvider();

            // Create string binder to create the value but not the key.
            var bindingContext = GetBindingContext(valueProvider, CreateStringBinder());
            var binder = new KeyValuePairModelBinder<int, string>();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotEqual(ModelBindingResult.NoResult, result);
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
            var valueProvider = new SimpleValueProvider();

            // Create int binder to create the value but not the key.
            var bindingContext = GetBindingContext(valueProvider, CreateIntBinder());
            var binder = new KeyValuePairModelBinder<int, string>();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
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
            var valueProvider = new SimpleValueProvider();

            // Create int binder to create the value but not the key.
            var bindingContext = GetBindingContext(valueProvider);
            var mockBinder = new Mock<IModelBinder>();
            mockBinder.Setup(o => o.BindModelAsync(It.IsAny<ModelBindingContext>()))
                      .Returns(ModelBindingResult.NoResultAsync);

            bindingContext.OperationBindingContext.ModelBinder = mockBinder.Object;
            var binder = new KeyValuePairModelBinder<int, string>();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.Equal(ModelBindingResult.NoResult, result);
            Assert.True(bindingContext.ModelState.IsValid);
            Assert.Equal(0, bindingContext.ModelState.ErrorCount);
        }

        [Fact]
        public async Task BindModel_SubBindingSucceeds()
        {
            // Arrange
            var innerBinder = new CompositeModelBinder(new[] { CreateStringBinder(), CreateIntBinder() });
            var valueProvider = new SimpleValueProvider();
            var bindingContext = GetBindingContext(valueProvider, innerBinder);

            var binder = new KeyValuePairModelBinder<int, string>();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotEqual(ModelBindingResult.NoResult, result);
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
        [InlineData(42, true)]
        public async Task TryBindStrongModel_InnerBinderReturnsAResult_ReturnsInnerBinderResult(
            object model,
            bool isSuccess)
        {
            // Arrange
            ModelBindingResult innerResult;
            if (isSuccess)
            {
                innerResult = ModelBindingResult.Success(string.Empty, model, validationNode: null);
            }
            else
            {
                innerResult = ModelBindingResult.Failed(string.Empty);
            }
            
            var innerBinder = new Mock<IModelBinder>();
            innerBinder
                .Setup(o => o.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns((ModelBindingContext context) =>
                {
                    Assert.Equal("someName.key", context.ModelName);
                    return Task.FromResult(innerResult);
                });
            var bindingContext = GetBindingContext(new SimpleValueProvider(), innerBinder.Object);

            var binder = new KeyValuePairModelBinder<int, string>();
            var modelValidationNodeList = new List<ModelValidationNode>();

            // Act
            var result = await binder.TryBindStrongModel<int>(bindingContext, "key", modelValidationNodeList);

            // Assert
            Assert.Equal(innerResult, result);
            Assert.Empty(bindingContext.ModelState);
        }

        [Fact]
        public async Task KeyValuePairModelBinder_CreatesEmptyCollection_IfIsTopLevelObject()
        {
            // Arrange
            var binder = new KeyValuePairModelBinder<string, string>();

            var context = CreateContext();
            context.IsTopLevelObject = true;

            // Lack of prefix and non-empty model name both ignored.
            context.ModelName = "modelName";

            var metadataProvider = context.OperationBindingContext.MetadataProvider;
            context.ModelMetadata = metadataProvider.GetMetadataForType(typeof(KeyValuePair<string, string>));

            context.ValueProvider = new TestValueProvider(new Dictionary<string, object>());

            // Act
            var result = await binder.BindModelAsync(context);

            // Assert
            Assert.NotEqual(ModelBindingResult.NoResult, result);

            Assert.Equal(default(KeyValuePair<string, string>), Assert.IsType<KeyValuePair<string, string>>(result.Model));
            Assert.Equal("modelName", result.Key);
            Assert.True(result.IsModelSet);

            Assert.Equal(result.ValidationNode.Model, result.Model);
            Assert.Same(result.ValidationNode.Key, result.Key);
            Assert.Same(result.ValidationNode.ModelMetadata, context.ModelMetadata);
        }

        [Theory]
        [InlineData("")]
        [InlineData("param")]
        public async Task KeyValuePairModelBinder_DoesNotCreateCollection_IfNotIsTopLevelObject(string prefix)
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
            Assert.Equal(ModelBindingResult.NoResult, result);
        }

        private static ModelBindingContext CreateContext()
        {
            var modelBindingContext = new ModelBindingContext()
            {
                OperationBindingContext = new OperationBindingContext()
                {
                    HttpContext = new DefaultHttpContext(),
                    MetadataProvider = new TestModelMetadataProvider(),
                    ModelBinder = new SimpleTypeModelBinder(),
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
                ModelState = new ModelStateDictionary(),
                ValueProvider = valueProvider,
                OperationBindingContext = new OperationBindingContext
                {
                    ModelBinder = innerBinder ?? CreateIntBinder(),
                    MetadataProvider = metataProvider,
                    ValidatorProvider = new DataAnnotationsModelValidatorProvider(
                        new TestOptionsManager<MvcDataAnnotationsLocalizationOptions>(),
                        stringLocalizerFactory: null)
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
                        return ModelBindingResult.SuccessAsync(mbc.ModelName, model, validationNode);
                    }
                    return ModelBindingResult.NoResultAsync;
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
                        return ModelBindingResult.SuccessAsync(mbc.ModelName, model, validationNode);
                    }
                    return ModelBindingResult.NoResultAsync;
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
