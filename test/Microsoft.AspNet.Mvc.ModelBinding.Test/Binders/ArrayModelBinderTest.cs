// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if ASPNET50
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class ArrayModelBinderTest
    {
        [Fact]
        public async Task BindModel()
        {
            // Arrange
            var valueProvider = new SimpleHttpValueProvider
            {
                { "someName[0]", "42" },
                { "someName[1]", "84" }
            };
            ModelBindingContext bindingContext = GetBindingContext(valueProvider);
            var binder = new ArrayModelBinder<int>();

            // Act
            var retVal = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(retVal);

            int[] array = bindingContext.Model as int[];
            Assert.Equal(new[] { 42, 84 }, array);
        }

        [Fact]
        public async Task GetBinder_ValueProviderDoesNotContainPrefix_ReturnsNull()
        {
            // Arrange
            ModelBindingContext bindingContext = GetBindingContext(new SimpleHttpValueProvider());
            var binder = new ArrayModelBinder<int>();

            // Act
            var bound = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.False(bound);
        }

        [Fact]
        public async Task GetBinder_ModelMetadataReturnsReadOnly_ReturnsNull()
        {
            // Arrange
            var valueProvider = new SimpleHttpValueProvider
            {
                { "foo[0]", "42" },
            };
            ModelBindingContext bindingContext = GetBindingContext(valueProvider);
            bindingContext.ModelMetadata.IsReadOnly = true;
            var binder = new ArrayModelBinder<int>();

            // Act
            var bound = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.False(bound);
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
                        mbc.Model = value.ConvertTo(mbc.ModelType);
                        return true;
                    }
                    return false;
                });
            return mockIntBinder.Object;
        }

        private static ModelBindingContext GetBindingContext(IValueProvider valueProvider)
        {
            var metadataProvider = new EmptyModelMetadataProvider();
            ModelBindingContext bindingContext = new ModelBindingContext
            {
                ModelMetadata = metadataProvider.GetMetadataForType(null, typeof(int[])),
                ModelName = "someName",
                ValueProvider = valueProvider,
                ModelBinder = CreateIntBinder(),
                MetadataProvider = metadataProvider
            };
            return bindingContext;
        }
    }
}
#endif
