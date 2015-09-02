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
    public class ArrayModelBinderTest
    {
        [Fact]
        public async Task BindModelAsync_ValueProviderContainPrefix_Succeeds()
        {
            // Arrange
            var valueProvider = new SimpleValueProvider
            {
                { "someName[0]", "42" },
                { "someName[1]", "84" },
            };
            var bindingContext = GetBindingContext(valueProvider);
            var modelState = bindingContext.ModelState;
            var binder = new ArrayModelBinder<int>();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotEqual(ModelBindingResult.NoResult, result);

            var array = Assert.IsType<int[]>(result.Model);
            Assert.Equal(new[] { 42, 84 }, array);
            Assert.True(modelState.IsValid);
        }

        [Fact]
        public async Task ArrayModelBinder_CreatesEmptyCollection_IfIsTopLevelObject()
        {
            // Arrange
            var binder = new ArrayModelBinder<string>();

            var context = CreateContext();
            context.IsTopLevelObject = true;

            // Lack of prefix and non-empty model name both ignored.
            context.ModelName = "modelName";

            var metadataProvider = context.OperationBindingContext.MetadataProvider;
            context.ModelMetadata = metadataProvider.GetMetadataForType(typeof(string[]));

            context.ValueProvider = new TestValueProvider(new Dictionary<string, object>());

            // Act
            var result = await binder.BindModelAsync(context);

            // Assert
            Assert.NotEqual(ModelBindingResult.NoResult, result);

            Assert.Empty(Assert.IsType<string[]>(result.Model));
            Assert.Equal("modelName", result.Key);
            Assert.True(result.IsModelSet);
        }

        [Theory]
        [InlineData("")]
        [InlineData("param")]
        public async Task ArrayModelBinder_DoesNotCreateCollection_IfNotIsTopLevelObject(string prefix)
        {
            // Arrange
            var binder = new ArrayModelBinder<string>();

            var context = CreateContext();
            context.ModelName = ModelNames.CreatePropertyModelName(prefix, "ArrayProperty");

            var metadataProvider = context.OperationBindingContext.MetadataProvider;
            context.ModelMetadata = metadataProvider.GetMetadataForProperty(
                typeof(ModelWithArrayProperty),
                nameof(ModelWithArrayProperty.ArrayProperty));

            context.ValueProvider = new TestValueProvider(new Dictionary<string, object>());

            // Act
            var result = await binder.BindModelAsync(context);

            // Assert
            Assert.Equal(ModelBindingResult.NoResult, result);
        }

        public static TheoryData<int[]> ArrayModelData
        {
            get
            {
                return new TheoryData<int[]>
                {
                    new int[0],
                    new [] { 357 },
                    new [] { 357, 357 },
                };
            }
        }

        [Theory]
        [InlineData(null)]
        [MemberData(nameof(ArrayModelData))]
        public async Task BindModelAsync_ModelMetadataReadOnly_ReturnsNull(int[] model)
        {
            // Arrange
            var valueProvider = new SimpleValueProvider
            {
                { "someName[0]", "42" },
                { "someName[1]", "84" },
            };
            var bindingContext = GetBindingContext(valueProvider, isReadOnly: true);
            bindingContext.Model = model;
            var binder = new ArrayModelBinder<int>();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.Equal(ModelBindingResult.NoResult, result);
        }

        // Here "fails silently" means the call does not update the array but also does not throw or set an error.
        [Theory]
        [MemberData(nameof(ArrayModelData))]
        public async Task BindModelAsync_ModelMetadataNotReadOnly_ModelNonNull_FailsSilently(int[] model)
        {
            // Arrange
            var arrayLength = model.Length;
            var valueProvider = new SimpleValueProvider
            {
                { "someName[0]", "42" },
                { "someName[1]", "84" },
            };

            var bindingContext = GetBindingContext(valueProvider, isReadOnly: false);
            var modelState = bindingContext.ModelState;
            bindingContext.Model = model;
            var binder = new ArrayModelBinder<int>();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotEqual(ModelBindingResult.NoResult, result);
            Assert.True(result.IsModelSet);
            Assert.Same(model, result.Model);

            Assert.True(modelState.IsValid);
            for (var i = 0; i < arrayLength; i++)
            {
                // Array should be unchanged.
                Assert.Equal(357, model[i]);
            }
        }

        private static IModelBinder CreateIntBinder()
        {
            var mockIntBinder = new Mock<IModelBinder>();
            mockIntBinder
                .Setup(o => o.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns((ModelBindingContext mbc) =>
                {
                    var value = mbc.ValueProvider.GetValue(mbc.ModelName);
                    if (value != ValueProviderResult.None)
                    {
                        var model = value.ConvertTo(mbc.ModelType);
                        return ModelBindingResult.SuccessAsync(mbc.ModelName, model);
                    }
                    return ModelBindingResult.NoResultAsync;
                });
            return mockIntBinder.Object;
        }

        private static ModelBindingContext GetBindingContext(
            IValueProvider valueProvider,
            bool isReadOnly = false)
        {
            var metadataProvider = new TestModelMetadataProvider();
            metadataProvider.ForProperty(
                typeof(ModelWithIntArrayProperty),
                nameof(ModelWithIntArrayProperty.ArrayProperty)).BindingDetails(bd => bd.IsReadOnly = isReadOnly);

            var modelMetadata = metadataProvider.GetMetadataForProperty(
                typeof(ModelWithIntArrayProperty),
                nameof(ModelWithIntArrayProperty.ArrayProperty));
            var bindingContext = new ModelBindingContext
            {
                ModelMetadata = modelMetadata,
                ModelName = "someName",
                ModelState = new ModelStateDictionary(),
                ValueProvider = valueProvider,
                OperationBindingContext = new OperationBindingContext
                {
                    ModelBinder = CreateIntBinder(),
                    MetadataProvider = metadataProvider,
                },
            };
            return bindingContext;
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

        private class ModelWithArrayProperty
        {
            public string[] ArrayProperty { get; set; }
        }

        private class ModelWithIntArrayProperty
        {
            public int[] ArrayProperty { get; set; }
        }
    }
}
#endif
