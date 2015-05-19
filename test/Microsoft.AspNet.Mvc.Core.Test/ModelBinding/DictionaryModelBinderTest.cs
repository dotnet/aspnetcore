// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if DNX451
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Internal;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class DictionaryModelBinderTest
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task BindModel_Succeeds(bool isReadOnly)
        {
            // Arrange
            var bindingContext = GetModelBindingContext(isReadOnly);
            var modelState = bindingContext.ModelState;
            var binder = new DictionaryModelBinder<int, string>();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsModelSet);
            var dictionary = Assert.IsAssignableFrom<IDictionary<int, string>>(result.Model);
            Assert.True(modelState.IsValid);

            Assert.NotNull(dictionary);
            Assert.Equal(2, dictionary.Count);
            Assert.Equal("forty-two", dictionary[42]);
            Assert.Equal("eighty-four", dictionary[84]);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task BindModel_BindingContextModelNonNull_Succeeds(bool isReadOnly)
        {
            // Arrange
            var bindingContext = GetModelBindingContext(isReadOnly);
            var modelState = bindingContext.ModelState;
            var dictionary = new Dictionary<int, string>();
            bindingContext.Model = dictionary;
            var binder = new DictionaryModelBinder<int, string>();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsModelSet);
            Assert.Same(dictionary, result.Model);
            Assert.True(modelState.IsValid);

            Assert.NotNull(dictionary);
            Assert.Equal(2, dictionary.Count);
            Assert.Equal("forty-two", dictionary[42]);
            Assert.Equal("eighty-four", dictionary[84]);
        }

        [Fact]
        public async Task DictionaryModelBinder_DoesNotCreateCollection_ForTopLevelModel_OnFirstPass()
        {
            // Arrange
            var binder = new DictionaryModelBinder<string, string>();

            var context = CreateContext();
            context.ModelName = "param";

            var metadataProvider = context.OperationBindingContext.MetadataProvider;
            context.ModelMetadata = metadataProvider.GetMetadataForType(typeof(Dictionary<string, string>));

            context.ValueProvider = new TestValueProvider(new Dictionary<string, object>());

            // Act
            var result = await binder.BindModelAsync(context);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DictionaryModelBinder_CreatesEmptyCollection_ForTopLevelModel_OnFallback()
        {
            // Arrange
            var binder = new DictionaryModelBinder<string, string>();

            var context = CreateContext();
            context.ModelName = string.Empty;

            var metadataProvider = context.OperationBindingContext.MetadataProvider;
            context.ModelMetadata = metadataProvider.GetMetadataForType(typeof(Dictionary<string, string>));

            context.ValueProvider = new TestValueProvider(new Dictionary<string, object>());

            // Act
            var result = await binder.BindModelAsync(context);

            // Assert
            Assert.NotNull(result);

            Assert.Empty(Assert.IsType<Dictionary<string, string>>(result.Model));
            Assert.Equal(string.Empty, result.Key);
            Assert.True(result.IsModelSet);

            Assert.Same(result.ValidationNode.Model, result.Model);
            Assert.Same(result.ValidationNode.Key, result.Key);
            Assert.Same(result.ValidationNode.ModelMetadata, context.ModelMetadata);
        }

        [Fact]
        public async Task DictionaryModelBinder_CreatesEmptyCollection_ForTopLevelModel_WithExplicitPrefix()
        {
            // Arrange
            var binder = new DictionaryModelBinder<string, string>();

            var context = CreateContext();
            context.ModelName = "prefix";
            context.BinderModelName = "prefix";

            var metadataProvider = context.OperationBindingContext.MetadataProvider;
            context.ModelMetadata = metadataProvider.GetMetadataForType(typeof(Dictionary<string, string>));

            context.ValueProvider = new TestValueProvider(new Dictionary<string, object>());

            // Act
            var result = await binder.BindModelAsync(context);

            // Assert
            Assert.NotNull(result);

            Assert.Empty(Assert.IsType<Dictionary<string, string>>(result.Model));
            Assert.Equal("prefix", result.Key);
            Assert.True(result.IsModelSet);

            Assert.Same(result.ValidationNode.Model, result.Model);
            Assert.Same(result.ValidationNode.Key, result.Key);
            Assert.Same(result.ValidationNode.ModelMetadata, context.ModelMetadata);
        }

        [Theory]
        [InlineData("")]
        [InlineData("param")]
        public async Task DictionaryModelBinder_DoesNotCreateCollection_ForNonTopLevelModel(string prefix)
        {
            // Arrange
            var binder = new DictionaryModelBinder<string, string>();

            var context = CreateContext();
            context.ModelName = ModelNames.CreatePropertyModelName(prefix, "ListProperty");

            var metadataProvider = context.OperationBindingContext.MetadataProvider;
            context.ModelMetadata = metadataProvider.GetMetadataForProperty(
                typeof(ModelWithDictionaryProperty),
                nameof(ModelWithDictionaryProperty.DictionaryProperty));

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
                }
            };

            return modelBindingContext;
        }

        private static ModelBindingContext GetModelBindingContext(bool isReadOnly)
        {
            var metadataProvider = new TestModelMetadataProvider();
            metadataProvider.ForType<List<int>>().BindingDetails(bd => bd.IsReadOnly = isReadOnly);
            var valueProvider = new SimpleHttpValueProvider
            {
                { "someName[0]", new KeyValuePair<int, string>(42, "forty-two") },
                { "someName[1]", new KeyValuePair<int, string>(84, "eighty-four") },
            };

            var bindingContext = new ModelBindingContext
            {
                ModelMetadata = metadataProvider.GetMetadataForType(typeof(IDictionary<int, string>)),
                ModelName = "someName",
                ValueProvider = valueProvider,
                OperationBindingContext = new OperationBindingContext
                {
                    ModelBinder = CreateKvpBinder(),
                    MetadataProvider = metadataProvider
                }
            };

            return bindingContext;
        }

        private static IModelBinder CreateKvpBinder()
        {
            Mock<IModelBinder> mockKvpBinder = new Mock<IModelBinder>();
            mockKvpBinder
                .Setup(o => o.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns(async (ModelBindingContext mbc) =>
                {
                    var value = await mbc.ValueProvider.GetValueAsync(mbc.ModelName);
                    if (value != null)
                    {
                        var model = value.ConvertTo(mbc.ModelType);
                        return new ModelBindingResult(model, key: null, isModelSet: true);
                    }
                    return null;
                });
            return mockKvpBinder.Object;
        }

        private class ModelWithDictionaryProperty
        {
            public Dictionary<string, string> DictionaryProperty { get; set; }
        }
    }
}
#endif
