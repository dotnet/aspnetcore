// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if ASPNET50
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
#endif
using System.Threading.Tasks;
#if ASPNET50
using Moq;
#endif
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class CollectionModelBinderTest
    {
#if ASPNET50
        [Fact]
        public async Task BindComplexCollectionFromIndexes_FiniteIndexes()
        {
            // Arrange
            var valueProvider = new SimpleHttpValueProvider
            {
                { "someName[foo]", "42" },
                { "someName[baz]", "200" }
            };
            var bindingContext = GetModelBindingContext(valueProvider);
            var binder = new CollectionModelBinder<int>();

            // Act
            var boundCollection = await binder.BindComplexCollectionFromIndexes(bindingContext, new[] { "foo", "bar", "baz" });

            // Assert
            Assert.Equal(new[] { 42, 0, 200 }, boundCollection.ToArray());
        }

        [Fact]
        public async Task BindComplexCollectionFromIndexes_InfiniteIndexes()
        {
            // Arrange
            var valueProvider = new SimpleHttpValueProvider
            {
                { "someName[0]", "42" },
                { "someName[1]", "100" },
                { "someName[3]", "400" }
            };
            var bindingContext = GetModelBindingContext(valueProvider);
            var binder = new CollectionModelBinder<int>();

            // Act
            var boundCollection = await binder.BindComplexCollectionFromIndexes(bindingContext, indexNames: null);

            // Assert
            Assert.Equal(new[] { 42, 100 }, boundCollection.ToArray());
        }

        [Fact]
        public async Task BindModel_ComplexCollection()
        {
            // Arrange
            var valueProvider = new SimpleHttpValueProvider
            {
                { "someName.index", new[] { "foo", "bar", "baz" } },
                { "someName[foo]", "42" },
                { "someName[bar]", "100" },
                { "someName[baz]", "200" }
            };
            var bindingContext = GetModelBindingContext(valueProvider);
            var binder = new CollectionModelBinder<int>();

            // Act
            var retVal = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.Equal(new[] { 42, 100, 200 }, ((List<int>)retVal.Model).ToArray());
        }

        [Fact]
        public async Task BindModel_SimpleCollection()
        {
            // Arrange
            var valueProvider = new SimpleHttpValueProvider
            {
                { "someName", new[] { "42", "100", "200" } }
            };
            var bindingContext = GetModelBindingContext(valueProvider);
            var binder = new CollectionModelBinder<int>();

            // Act
            var retVal = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotNull(retVal);
            Assert.Equal(new[] { 42, 100, 200 }, ((List<int>)retVal.Model).ToArray());
        }
#endif

        [Fact]
        public async Task BindSimpleCollection_RawValueIsEmptyCollection_ReturnsEmptyList()
        {
            // Arrange
            var binder = new CollectionModelBinder<int>();

            var context = new ModelBindingContext()
            {
                OperationBindingContext = new OperationBindingContext()
                {
                    MetadataProvider = new DataAnnotationsModelMetadataProvider(),
                },
            };

            // Act
            var boundCollection = await binder.BindSimpleCollection(context, rawValue: new object[0], culture: null);

            // Assert
            Assert.NotNull(boundCollection);
            Assert.Empty(boundCollection);
        }

        [Fact]
        public async Task BindSimpleCollection_RawValueIsNull_ReturnsNull()
        {
            // Arrange
            var binder = new CollectionModelBinder<int>();

            // Act
            var boundCollection = await binder.BindSimpleCollection(bindingContext: null, rawValue: null, culture: null);

            // Assert
            Assert.Null(boundCollection);
        }

#if ASPNET50
        [Fact]
        public async Task BindSimpleCollection_SubBindingSucceeds()
        {
            // Arrange
            var culture = new CultureInfo("fr-FR");
            var bindingContext = GetModelBindingContext(new SimpleHttpValueProvider());

            Mock.Get<IModelBinder>(bindingContext.OperationBindingContext.ModelBinder)
                .Setup(o => o.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns((ModelBindingContext mbc) =>
                {
                    Assert.Equal("someName", mbc.ModelName);
                    return Task.FromResult(new ModelBindingResult(42, mbc.ModelName, true));
                });
            var modelBinder = new CollectionModelBinder<int>();

            // Act
            var boundCollection = await modelBinder.BindSimpleCollection(bindingContext, new int[1], culture);

            // Assert
            Assert.Equal(new[] { 42 }, boundCollection.ToArray());
        }

        private static ModelBindingContext GetModelBindingContext(IValueProvider valueProvider)
        {
            var metadataProvider = new EmptyModelMetadataProvider();
            var bindingContext = new ModelBindingContext
            {
                ModelMetadata = metadataProvider.GetMetadataForType(typeof(int)),
                ModelName = "someName",
                ValueProvider = valueProvider,
                OperationBindingContext = new OperationBindingContext
                {
                    ModelBinder = CreateIntBinder(),
                    MetadataProvider = metadataProvider
                }
            };
            return bindingContext;
        }

        private static IModelBinder CreateIntBinder()
        {
            Mock<IModelBinder> mockIntBinder = new Mock<IModelBinder>();
            mockIntBinder
                .Setup(o => o.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns(async (ModelBindingContext mbc) =>
                {
                    var value = await mbc.ValueProvider.GetValueAsync(mbc.ModelName);
                    if (value != null)
                    {
                        var model = value.ConvertTo(mbc.ModelType);
                        return new ModelBindingResult(model, mbc.ModelName, true);
                    }

                    return null;
                });
            return mockIntBinder.Object;
        }
#endif
    }
}
