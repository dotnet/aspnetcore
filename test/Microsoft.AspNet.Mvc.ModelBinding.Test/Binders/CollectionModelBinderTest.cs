using System.Collections.Generic;
using System.Globalization;
using System.Linq;
#if NET45
using Moq;
#endif
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class CollectionModelBinderTest
    {
#if NET45
        [Fact]
        public void BindComplexCollectionFromIndexes_FiniteIndexes()
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
            var boundCollection = binder.BindComplexCollectionFromIndexes(bindingContext, new[] { "foo", "bar", "baz" });

            // Assert
            Assert.Equal(new[] { 42, 0, 200 }, boundCollection.ToArray());
            Assert.Equal(new[] { "someName[foo]", "someName[baz]" }, bindingContext.ValidationNode.ChildNodes.Select(o => o.ModelStateKey).ToArray());
        }

        [Fact]
        public void BindComplexCollectionFromIndexes_InfiniteIndexes()
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
            var boundCollection = binder.BindComplexCollectionFromIndexes(bindingContext, indexNames: null);

            // Assert
            Assert.Equal(new[] { 42, 100 }, boundCollection.ToArray());
            Assert.Equal(new[] { "someName[0]", "someName[1]" }, bindingContext.ValidationNode.ChildNodes.Select(o => o.ModelStateKey).ToArray());
        }

        [Fact]
        public void BindModel_ComplexCollection()
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
            bool retVal = binder.BindModel(bindingContext);

            // Assert
            Assert.Equal(new[] { 42, 100, 200 }, ((List<int>)bindingContext.Model).ToArray());
        }

        [Fact]
        public void BindModel_SimpleCollection()
        {
            // Arrange
            var valueProvider = new SimpleHttpValueProvider
            {
                { "someName", new[] { "42", "100", "200" } }
            };
            var bindingContext = GetModelBindingContext(valueProvider);
            var binder = new CollectionModelBinder<int>();

            // Act
            bool retVal = binder.BindModel(bindingContext);

            // Assert
            Assert.True(retVal);
            Assert.Equal(new[] { 42, 100, 200 }, ((List<int>)bindingContext.Model).ToArray());
        }
#endif

        [Fact]
        public void BindSimpleCollection_RawValueIsEmptyCollection_ReturnsEmptyList()
        {
            // Arrange
            var binder = new CollectionModelBinder<int>();

            // Act
            var boundCollection = binder.BindSimpleCollection(bindingContext: null, rawValue: new object[0], culture: null);

            // Assert
            Assert.NotNull(boundCollection);
            Assert.Empty(boundCollection);
        }

        [Fact]
        public void BindSimpleCollection_RawValueIsNull_ReturnsNull()
        {
            // Arrange
            var binder = new CollectionModelBinder<int>();

            // Act
            var boundCollection = binder.BindSimpleCollection(bindingContext: null, rawValue: null, culture: null);

            // Assert
            Assert.Null(boundCollection);
        }

#if NET45
        [Fact]
        public void BindSimpleCollection_SubBindingSucceeds()
        {
            // Arrange
            var culture = new CultureInfo("fr-FR");
            var bindingContext = GetModelBindingContext(new SimpleHttpValueProvider());

            ModelValidationNode childValidationNode = null;
            Mock.Get<IModelBinder>(bindingContext.ModelBinder)
                .Setup(o => o.BindModel(It.IsAny<ModelBindingContext>()))
                .Returns((ModelBindingContext mbc) =>
                {
                    Assert.Equal("someName", mbc.ModelName);
                    childValidationNode = mbc.ValidationNode;
                    mbc.Model = 42;
                    return true;
                });
            var modelBinder = new CollectionModelBinder<int>();

            // Act
            var boundCollection = modelBinder.BindSimpleCollection(bindingContext, new int[1], culture);

            // Assert
            Assert.Equal(new[] { 42 }, boundCollection.ToArray());
            Assert.Equal(new[] { childValidationNode }, bindingContext.ValidationNode.ChildNodes.ToArray());
        }

        private static ModelBindingContext GetModelBindingContext(IValueProvider valueProvider)
        {
            var metadataProvider = new EmptyModelMetadataProvider();
            var bindingContext = new ModelBindingContext
            {
                ModelMetadata = metadataProvider.GetMetadataForType(null, typeof(int)),
                ModelName = "someName",
                ValueProvider = valueProvider,
                ModelBinder = CreateIntBinder(),
                MetadataProvider = metadataProvider
            };
            return bindingContext;
        }

        private static IModelBinder CreateIntBinder()
        {
            Mock<IModelBinder> mockIntBinder = new Mock<IModelBinder>();
            mockIntBinder
                .Setup(o => o.BindModel(It.IsAny<ModelBindingContext>()))
                .Returns((ModelBindingContext mbc) =>
                {
                    var value = mbc.ValueProvider.GetValue(mbc.ModelName);
                    if (value != null)
                    {
                        mbc.Model = value.ConvertTo(mbc.ModelType);
                        return true;
                    }
                    return false;
                });
            return mockIntBinder.Object;
        }
#endif
    }
}
