// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if DNX451
using System.Collections.Generic;
using System.Threading.Tasks;
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
    }
}
#endif
