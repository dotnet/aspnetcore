// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if DNX451
using System.Threading.Tasks;
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
            var valueProvider = new SimpleHttpValueProvider
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
            Assert.NotNull(result);

            var array = Assert.IsType<int[]>(result.Model);
            Assert.Equal(new[] { 42, 84 }, array);
            Assert.True(modelState.IsValid);
        }

        [Fact]
        public async Task BindModelAsync_ValueProviderDoesNotContainPrefix_ReturnsNull()
        {
            // Arrange
            var bindingContext = GetBindingContext(new SimpleHttpValueProvider());
            var binder = new ArrayModelBinder<int>();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.Null(result);
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
            var valueProvider = new SimpleHttpValueProvider
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
            Assert.Null(result);
        }

        // Here "fails silently" means the call does not update the array but also does not throw or set an error.
        [Theory]
        [MemberData(nameof(ArrayModelData))]
        public async Task BindModelAsync_ModelMetadataNotReadOnly_ModelNonNull_FailsSilently(int[] model)
        {
            // Arrange
            var arrayLength = model.Length;
            var valueProvider = new SimpleHttpValueProvider
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
            Assert.NotNull(result);
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
            return mockIntBinder.Object;
        }

        private static ModelBindingContext GetBindingContext(
            IValueProvider valueProvider,
            bool isReadOnly = false)
        {
            var metadataProvider = new TestModelMetadataProvider();
            metadataProvider.ForType<int[]>().BindingDetails(bd => bd.IsReadOnly = isReadOnly);

            var bindingContext = new ModelBindingContext
            {
                ModelMetadata = metadataProvider.GetMetadataForType(typeof(int[])),
                ModelName = "someName",
                ValueProvider = valueProvider,
                OperationBindingContext = new OperationBindingContext
                {
                    ModelBinder = CreateIntBinder(),
                    MetadataProvider = metadataProvider
                },
            };
            return bindingContext;
        }
    }
}
#endif
