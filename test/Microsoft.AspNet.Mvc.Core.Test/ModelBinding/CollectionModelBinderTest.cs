// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if DNX451
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
#endif
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Internal;
#if DNX451
using Moq;
#endif
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class CollectionModelBinderTest
    {
#if DNX451
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
            Assert.Equal(new[] { 42, 0, 200 }, boundCollection.Model.ToArray());
            Assert.Equal(
                new[] { "someName[foo]", "someName[baz]" },
                boundCollection.ValidationNode.ChildNodes.Select(o => o.Key).ToArray());
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
            Assert.Equal(new[] { 42, 100 }, boundCollection.Model.ToArray());
            Assert.Equal(
                new[] { "someName[0]", "someName[1]" },
                boundCollection.ValidationNode.ChildNodes.Select(o => o.Key).ToArray());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task BindModel_ComplexCollection_Succeeds(bool isReadOnly)
        {
            // Arrange
            var valueProvider = new SimpleHttpValueProvider
            {
                { "someName.index", new[] { "foo", "bar", "baz" } },
                { "someName[foo]", "42" },
                { "someName[bar]", "100" },
                { "someName[baz]", "200" }
            };
            var bindingContext = GetModelBindingContext(valueProvider, isReadOnly);
            var modelState = bindingContext.ModelState;
            var binder = new CollectionModelBinder<int>();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsModelSet);

            var list = Assert.IsAssignableFrom<IList<int>>(result.Model);
            Assert.Equal(new[] { 42, 100, 200 }, list.ToArray());

            Assert.True(modelState.IsValid);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task BindModel_ComplexCollection_BindingContextModelNonNull_Succeeds(bool isReadOnly)
        {
            // Arrange
            var valueProvider = new SimpleHttpValueProvider
            {
                { "someName.index", new[] { "foo", "bar", "baz" } },
                { "someName[foo]", "42" },
                { "someName[bar]", "100" },
                { "someName[baz]", "200" }
            };
            var bindingContext = GetModelBindingContext(valueProvider, isReadOnly);
            var modelState = bindingContext.ModelState;
            var list = new List<int>();
            bindingContext.Model = list;
            var binder = new CollectionModelBinder<int>();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsModelSet);

            Assert.Same(list, result.Model);
            Assert.Equal(new[] { 42, 100, 200 }, list.ToArray());

            Assert.True(modelState.IsValid);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task BindModel_SimpleCollection_Succeeds(bool isReadOnly)
        {
            // Arrange
            var valueProvider = new SimpleHttpValueProvider
            {
                { "someName", new[] { "42", "100", "200" } }
            };
            var bindingContext = GetModelBindingContext(valueProvider, isReadOnly);
            var modelState = bindingContext.ModelState;
            var binder = new CollectionModelBinder<int>();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsModelSet);

            var list = Assert.IsAssignableFrom<IList<int>>(result.Model);
            Assert.Equal(new[] { 42, 100, 200 }, list.ToArray());

            Assert.True(modelState.IsValid);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task BindModel_SimpleCollection_BindingContextModelNonNull_Succeeds(bool isReadOnly)
        {
            // Arrange
            var valueProvider = new SimpleHttpValueProvider
            {
                { "someName", new[] { "42", "100", "200" } }
            };
            var bindingContext = GetModelBindingContext(valueProvider, isReadOnly);
            var modelState = bindingContext.ModelState;
            var list = new List<int>();
            bindingContext.Model = list;
            var binder = new CollectionModelBinder<int>();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsModelSet);

            Assert.Same(list, result.Model);
            Assert.Equal(new[] { 42, 100, 200 }, list.ToArray());

            Assert.True(modelState.IsValid);
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
                    MetadataProvider = TestModelMetadataProvider.CreateDefaultProvider(),
                },
            };

            // Act
            var boundCollection = await binder.BindSimpleCollection(context, rawValue: new object[0], culture: null);

            // Assert
            Assert.NotNull(boundCollection.Model);
            Assert.Empty(boundCollection.Model);
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

        [Fact]
        public async Task CollectionModelBinder_DoesNotCreateCollection_ForTopLevelModel_OnFirstPass()
        {
            // Arrange
            var binder = new CollectionModelBinder<string>();

            var context = CreateContext();
            context.ModelName = "param";

            var metadataProvider = context.OperationBindingContext.MetadataProvider;
            context.ModelMetadata = metadataProvider.GetMetadataForType(typeof(List<string>));

            context.ValueProvider = new TestValueProvider(new Dictionary<string, object>());

            // Act
            var result = await binder.BindModelAsync(context);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CollectionModelBinder_CreatesEmptyCollection_ForTopLevelModel_OnFallback()
        {
            // Arrange
            var binder = new CollectionModelBinder<string>();

            var context = CreateContext();
            context.ModelName = string.Empty;

            var metadataProvider = context.OperationBindingContext.MetadataProvider;
            context.ModelMetadata = metadataProvider.GetMetadataForType(typeof(List<string>));

            context.ValueProvider = new TestValueProvider(new Dictionary<string, object>());

            // Act
            var result = await binder.BindModelAsync(context);

            // Assert
            Assert.NotNull(result);

            Assert.Empty(Assert.IsType<List<string>>(result.Model));
            Assert.Equal(string.Empty, result.Key);
            Assert.True(result.IsModelSet);

            Assert.Same(result.ValidationNode.Model, result.Model);
            Assert.Same(result.ValidationNode.Key, result.Key);
            Assert.Same(result.ValidationNode.ModelMetadata, context.ModelMetadata);
        }

        [Fact]
        public async Task CollectionModelBinder_CreatesEmptyCollection_ForTopLevelModel_WithExplicitPrefix()
        {
            // Arrange
            var binder = new CollectionModelBinder<string>();

            var context = CreateContext();
            context.ModelName = "prefix";
            context.BinderModelName = "prefix";

            var metadataProvider = context.OperationBindingContext.MetadataProvider;
            context.ModelMetadata = metadataProvider.GetMetadataForType(typeof(List<string>));

            context.ValueProvider = new TestValueProvider(new Dictionary<string, object>());

            // Act
            var result = await binder.BindModelAsync(context);

            // Assert
            Assert.NotNull(result);

            Assert.Empty(Assert.IsType<List<string>>(result.Model));
            Assert.Equal("prefix", result.Key);
            Assert.True(result.IsModelSet);

            Assert.Same(result.ValidationNode.Model, result.Model);
            Assert.Same(result.ValidationNode.Key, result.Key);
            Assert.Same(result.ValidationNode.ModelMetadata, context.ModelMetadata);
        }

        [Theory]
        [InlineData("")]
        [InlineData("param")]
        public async Task CollectionModelBinder_DoesNotCreateCollection_ForNonTopLevelModel(string prefix)
        {
            // Arrange
            var binder = new CollectionModelBinder<string>();

            var context = CreateContext();
            context.ModelName = ModelNames.CreatePropertyModelName(prefix, "ListProperty");

            var metadataProvider = context.OperationBindingContext.MetadataProvider;
            context.ModelMetadata = metadataProvider.GetMetadataForProperty(
                typeof(ModelWithListProperty),
                nameof(ModelWithListProperty.ListProperty));

            context.ValueProvider = new TestValueProvider(new Dictionary<string, object>());

            // Act
            var result = await binder.BindModelAsync(context);

            // Assert
            Assert.Null(result);
        }

#if DNX451
        [Fact]
        public async Task BindSimpleCollection_SubBindingSucceeds()
        {
            // Arrange
            var culture = new CultureInfo("fr-FR");
            var bindingContext = GetModelBindingContext(new SimpleHttpValueProvider());
            ModelValidationNode childValidationNode = null;
            Mock.Get<IModelBinder>(bindingContext.OperationBindingContext.ModelBinder)
                .Setup(o => o.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns((ModelBindingContext mbc) =>
                {
                    Assert.Equal("someName", mbc.ModelName);
                    childValidationNode = new ModelValidationNode("someName", mbc.ModelMetadata, mbc.Model);
                    return Task.FromResult(new ModelBindingResult(42, mbc.ModelName, true, childValidationNode));
                });
            var modelBinder = new CollectionModelBinder<int>();

            // Act
            var boundCollection = await modelBinder.BindSimpleCollection(bindingContext, new int[1], culture);

            // Assert
            Assert.Equal(new[] { 42 }, boundCollection.Model.ToArray());
            Assert.Equal(new[] { childValidationNode }, boundCollection.ValidationNode.ChildNodes.ToArray());
        }

        private static ModelBindingContext GetModelBindingContext(
            IValueProvider valueProvider,
            bool isReadOnly = false)
        {
            var metadataProvider = new TestModelMetadataProvider();
            metadataProvider.ForType<IList<int>>().BindingDetails(bd => bd.IsReadOnly = isReadOnly);

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
                        var modelValidationNode = new ModelValidationNode(mbc.ModelName, mbc.ModelMetadata, model);
                        return new ModelBindingResult(model, mbc.ModelName, true, modelValidationNode);
                    }

                    return null;
                });
            return mockIntBinder.Object;
        }
#endif

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

        private class ModelWithListProperty
        {
            public List<string> ListProperty { get; set; }
        }
    }
}
