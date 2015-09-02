// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
#if DNX451
using System.Globalization;
using System.Linq;
#endif
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
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
            var valueProvider = new SimpleValueProvider
            {
                { "someName[foo]", "42" },
                { "someName[baz]", "200" }
            };
            var bindingContext = GetModelBindingContext(valueProvider);
            var binder = new CollectionModelBinder<int>();

            // Act
            var collectionResult = await binder.BindComplexCollectionFromIndexes(
                bindingContext,
                new[] { "foo", "bar", "baz" });

            // Assert
            Assert.Equal(new[] { 42, 0, 200 }, collectionResult.Model.ToArray());

            // This requires a non-default IValidationStrategy
            var strategy = Assert.IsType<ExplicitIndexCollectionValidationStrategy>(collectionResult.ValidationStrategy);
            Assert.Equal(new[] { "foo", "bar", "baz" }, strategy.ElementKeys);
        }

        [Fact]
        public async Task BindComplexCollectionFromIndexes_InfiniteIndexes()
        {
            // Arrange
            var valueProvider = new SimpleValueProvider
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

            // This uses the default IValidationStrategy
            Assert.DoesNotContain(boundCollection, bindingContext.ValidationState.Keys);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task BindModel_ComplexCollection_Succeeds(bool isReadOnly)
        {
            // Arrange
            var valueProvider = new SimpleValueProvider
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
            Assert.NotEqual(ModelBindingResult.NoResult, result);
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
            var valueProvider = new SimpleValueProvider
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
            Assert.NotEqual(ModelBindingResult.NoResult, result);
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
            var valueProvider = new SimpleValueProvider
            {
                { "someName", new[] { "42", "100", "200" } }
            };
            var bindingContext = GetModelBindingContext(valueProvider, isReadOnly);
            var modelState = bindingContext.ModelState;
            var binder = new CollectionModelBinder<int>();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotEqual(ModelBindingResult.NoResult, result);
            Assert.True(result.IsModelSet);

            var list = Assert.IsAssignableFrom<IList<int>>(result.Model);
            Assert.Equal(new[] { 42, 100, 200 }, list.ToArray());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task BindModel_SimpleCollection_BindingContextModelNonNull_Succeeds(bool isReadOnly)
        {
            // Arrange
            var valueProvider = new SimpleValueProvider
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
            Assert.NotEqual(ModelBindingResult.NoResult, result);
            Assert.True(result.IsModelSet);

            Assert.Same(list, result.Model);
            Assert.Equal(new[] { 42, 100, 200 }, list.ToArray());
        }

        [Fact]
        public async Task BindModelAsync_SimpleCollectionWithNullValue_Succeeds()
        {
            // Arrange
            var binder = new CollectionModelBinder<int>();
            var valueProvider = new SimpleValueProvider
            {
                { "someName", null },
            };
            var bindingContext = GetModelBindingContext(valueProvider, isReadOnly: false);
            var modelState = bindingContext.ModelState;

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotEqual(ModelBindingResult.NoResult, result);
            Assert.True(result.IsModelSet);
            Assert.NotNull(result.Model);

            var model = Assert.IsType<List<int>>(result.Model);
            Assert.Empty(model);
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
            var boundCollection = await binder.BindSimpleCollection(context, new ValueProviderResult(new string[0]));

            // Assert
            Assert.NotNull(boundCollection.Model);
            Assert.Empty(boundCollection.Model);
        }

        [Fact]
        public async Task CollectionModelBinder_CreatesEmptyCollection_IfIsTopLevelObject()
        {
            // Arrange
            var binder = new CollectionModelBinder<string>();

            var context = CreateContext();
            context.IsTopLevelObject = true;

            // Lack of prefix and non-empty model name both ignored.
            context.ModelName = "modelName";

            var metadataProvider = context.OperationBindingContext.MetadataProvider;
            context.ModelMetadata = metadataProvider.GetMetadataForType(typeof(List<string>));

            context.ValueProvider = new TestValueProvider(new Dictionary<string, object>());

            // Act
            var result = await binder.BindModelAsync(context);

            // Assert
            Assert.NotEqual(ModelBindingResult.NoResult, result);

            Assert.Empty(Assert.IsType<List<string>>(result.Model));
            Assert.Equal("modelName", result.Key);
            Assert.True(result.IsModelSet);
        }

        // Setup like CollectionModelBinder_CreatesEmptyCollection_IfIsTopLevelObject  except
        // Model already has a value.
        [Fact]
        public async Task CollectionModelBinder_DoesNotCreateEmptyCollection_IfModelNonNull()
        {
            // Arrange
            var binder = new CollectionModelBinder<string>();

            var context = CreateContext();
            context.IsTopLevelObject = true;

            var list = new List<string>();
            context.Model = list;

            // Lack of prefix and non-empty model name both ignored.
            context.ModelName = "modelName";

            var metadataProvider = context.OperationBindingContext.MetadataProvider;
            context.ModelMetadata = metadataProvider.GetMetadataForType(typeof(List<string>));

            context.ValueProvider = new TestValueProvider(new Dictionary<string, object>());

            // Act
            var result = await binder.BindModelAsync(context);

            // Assert
            Assert.NotEqual(ModelBindingResult.NoResult, result);

            Assert.Same(list, result.Model);
            Assert.Empty(list);
            Assert.Equal("modelName", result.Key);
            Assert.True(result.IsModelSet);
        }

        [Theory]
        [InlineData("")]
        [InlineData("param")]
        public async Task CollectionModelBinder_DoesNotCreateCollection_IfNotIsTopLevelObject(string prefix)
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
            Assert.Equal(ModelBindingResult.NoResult, result);
        }

        // Model type -> can create instance.
        public static TheoryData<Type, bool> CanCreateInstanceData
        {
            get
            {
                return new TheoryData<Type, bool>
                {
                    { typeof(IEnumerable<int>), true },
                    { typeof(ICollection<int>), true },
                    { typeof(IList<int>), true },
                    { typeof(List<int>), true },
                    { typeof(LinkedList<int>), true },
                    { typeof(ISet<int>), false },
                    { typeof(ListWithInternalConstructor<int>), false },
                    { typeof(ListWithThrowingConstructor<int>), false },
                };
            }
        }

        [Theory]
        [MemberData(nameof(CanCreateInstanceData))]
        public void CanCreateInstance_ReturnsExpectedValue(Type modelType, bool expectedResult)
        {
            // Arrange
            var binder = new CollectionModelBinder<int>();

            // Act
            var result = binder.CanCreateInstance(modelType);

            // Assert
            Assert.Equal(expectedResult, result);
        }

#if DNX451
        [Fact]
        public async Task BindSimpleCollection_SubBindingSucceeds()
        {
            // Arrange
            var culture = new CultureInfo("fr-FR");
            var bindingContext = GetModelBindingContext(new SimpleValueProvider());

            Mock.Get<IModelBinder>(bindingContext.OperationBindingContext.ModelBinder)
                .Setup(o => o.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns((ModelBindingContext mbc) =>
                {
                    Assert.Equal("someName", mbc.ModelName);
                    return ModelBindingResult.SuccessAsync(mbc.ModelName, 42);
                });
            var modelBinder = new CollectionModelBinder<int>();

            // Act
            var boundCollection = await modelBinder.BindSimpleCollection(
                bindingContext,
                new ValueProviderResult(new string[] { "0" }));

            // Assert
            Assert.Equal(new[] { 42 }, boundCollection.Model.ToArray());
        }

        private static ModelBindingContext GetModelBindingContext(
            IValueProvider valueProvider,
            bool isReadOnly = false)
        {
            var metadataProvider = new TestModelMetadataProvider();
            metadataProvider.ForType<IList<int>>().BindingDetails(bd => bd.IsReadOnly = isReadOnly);

            var bindingContext = new ModelBindingContext
            {
                ModelMetadata = metadataProvider.GetMetadataForType(typeof(IList<int>)),
                ModelName = "someName",
                ModelState = new ModelStateDictionary(),
                ValueProvider = valueProvider,
                OperationBindingContext = new OperationBindingContext
                {
                    ModelBinder = CreateIntBinder(),
                    MetadataProvider = metadataProvider
                },
                ValidationState = new ValidationStateDictionary(),
            };

            return bindingContext;
        }

        private static IModelBinder CreateIntBinder()
        {
            Mock<IModelBinder> mockIntBinder = new Mock<IModelBinder>();
            mockIntBinder
                .Setup(o => o.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns((ModelBindingContext mbc) =>
                {
                    var value = mbc.ValueProvider.GetValue(mbc.ModelName);
                    if (value == ValueProviderResult.None)
                    {
                        return ModelBindingResult.NoResultAsync;
                    }

                    var model = value.ConvertTo(mbc.ModelType);
                    if (model == null)
                    {
                        return ModelBindingResult.FailedAsync(mbc.ModelName);
                    }
                    else
                    {
                        return ModelBindingResult.SuccessAsync(mbc.ModelName, model);
                    }
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

        private class ModelWithSimpleProperties
        {
            public int Id { get; set; }

            public string Name { get; set; }
        }

        private class ListWithInternalConstructor<T> : List<T>
        {
            internal ListWithInternalConstructor()
                : base()
            {
            }
        }

        private class ListWithThrowingConstructor<T> : List<T>
        {
            public ListWithThrowingConstructor()
                : base()
            {
                throw new ApplicationException("No, don't do this.");
            }
        }
    }
}
