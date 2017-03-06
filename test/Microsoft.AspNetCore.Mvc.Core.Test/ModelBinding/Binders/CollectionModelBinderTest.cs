// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    public class CollectionModelBinderTest
    {
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
            var binder = new CollectionModelBinder<int>(CreateIntBinder());

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
            var binder = new CollectionModelBinder<int>(CreateIntBinder());

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
            var binder = new CollectionModelBinder<int>(CreateIntBinder());

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(bindingContext.Result.IsModelSet);

            var list = Assert.IsAssignableFrom<IList<int>>(bindingContext.Result.Model);
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
            var binder = new CollectionModelBinder<int>(CreateIntBinder());

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(bindingContext.Result.IsModelSet);

            Assert.Same(list, bindingContext.Result.Model);
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
            var binder = new CollectionModelBinder<int>(CreateIntBinder());

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(bindingContext.Result.IsModelSet);

            var list = Assert.IsAssignableFrom<IList<int>>(bindingContext.Result.Model);
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
            var binder = new CollectionModelBinder<int>(CreateIntBinder());

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(bindingContext.Result.IsModelSet);

            Assert.Same(list, bindingContext.Result.Model);
            Assert.Equal(new[] { 42, 100, 200 }, list.ToArray());
        }

        [Fact]
        public async Task BindModelAsync_SimpleCollectionWithNullValue_Succeeds()
        {
            // Arrange
            var binder = new CollectionModelBinder<int>(CreateIntBinder());
            var valueProvider = new SimpleValueProvider
            {
                { "someName", null },
            };
            var bindingContext = GetModelBindingContext(valueProvider, isReadOnly: false);

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.True(bindingContext.Result.IsModelSet);

            var model = Assert.IsType<List<int>>(bindingContext.Result.Model);
            Assert.Empty(model);
        }

        [Fact]
        public async Task BindSimpleCollection_RawValueIsEmptyCollection_ReturnsEmptyList()
        {
            // Arrange
            var binder = new CollectionModelBinder<int>(CreateIntBinder());
            var context = GetModelBindingContext(new SimpleValueProvider());

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
            var binder = new CollectionModelBinder<string>(new StubModelBinder(result: ModelBindingResult.Failed()));

            var bindingContext = CreateContext();
            bindingContext.IsTopLevelObject = true;

            // Lack of prefix and non-empty model name both ignored.
            bindingContext.ModelName = "modelName";

            var metadataProvider = new TestModelMetadataProvider();
            bindingContext.ModelMetadata = metadataProvider.GetMetadataForType(typeof(List<string>));

            bindingContext.ValueProvider = new TestValueProvider(new Dictionary<string, object>());

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.Empty(Assert.IsType<List<string>>(bindingContext.Result.Model));
            Assert.True(bindingContext.Result.IsModelSet);
        }

        // Setup like CollectionModelBinder_CreatesEmptyCollection_IfIsTopLevelObject  except
        // Model already has a value.
        [Fact]
        public async Task CollectionModelBinder_DoesNotCreateEmptyCollection_IfModelNonNull()
        {
            // Arrange
            var binder = new CollectionModelBinder<string>(new StubModelBinder(result: ModelBindingResult.Failed()));

            var bindingContext = CreateContext();
            bindingContext.IsTopLevelObject = true;

            var list = new List<string>();
            bindingContext.Model = list;

            // Lack of prefix and non-empty model name both ignored.
            bindingContext.ModelName = "modelName";

            var metadataProvider = new TestModelMetadataProvider();
            bindingContext.ModelMetadata = metadataProvider.GetMetadataForType(typeof(List<string>));

            bindingContext.ValueProvider = new TestValueProvider(new Dictionary<string, object>());

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.Same(list, bindingContext.Result.Model);
            Assert.Empty(list);
            Assert.True(bindingContext.Result.IsModelSet);
        }

        [Theory]
        [InlineData("")]
        [InlineData("param")]
        public async Task CollectionModelBinder_DoesNotCreateCollection_IfNotIsTopLevelObject(string prefix)
        {
            // Arrange
            var binder = new CollectionModelBinder<string>(new StubModelBinder(result: ModelBindingResult.Failed()));

            var bindingContext = CreateContext();
            bindingContext.ModelName = ModelNames.CreatePropertyModelName(prefix, "ListProperty");

            var metadataProvider = new TestModelMetadataProvider();
            bindingContext.ModelMetadata = metadataProvider.GetMetadataForProperty(
                typeof(ModelWithListProperty),
                nameof(ModelWithListProperty.ListProperty));

            bindingContext.ValueProvider = new TestValueProvider(new Dictionary<string, object>());

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.False(bindingContext.Result.IsModelSet);
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
                };
            }
        }

        [Theory]
        [MemberData(nameof(CanCreateInstanceData))]
        public void CanCreateInstance_ReturnsExpectedValue(Type modelType, bool expectedResult)
        {
            // Arrange
            var binder = new CollectionModelBinder<int>(CreateIntBinder());

            // Act
            var result = binder.CanCreateInstance(modelType);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public async Task BindSimpleCollection_SubBindingSucceeds()
        {
            // Arrange
            var culture = new CultureInfo("fr-FR");
            var bindingContext = GetModelBindingContext(new SimpleValueProvider());

            var elementBinder = new StubModelBinder(mbc =>
            {
                Assert.Equal("someName", mbc.ModelName);
                mbc.Result = ModelBindingResult.Success(42);
            });

            var modelBinder = new CollectionModelBinder<int>(elementBinder);

            // Act
            var boundCollection = await modelBinder.BindSimpleCollection(
                bindingContext,
                new ValueProviderResult(new string[] { "0" }));

            // Assert
            Assert.Equal(new[] { 42 }, boundCollection.Model.ToArray());
        }

        private static DefaultModelBindingContext GetModelBindingContext(
            IValueProvider valueProvider,
            bool isReadOnly = false)
        {
            var metadataProvider = new TestModelMetadataProvider();
            metadataProvider
                .ForProperty<ModelWithIListProperty>(nameof(ModelWithIListProperty.ListProperty))
                .BindingDetails(bd => bd.IsReadOnly = isReadOnly);
            var metadata = metadataProvider.GetMetadataForProperty(
                typeof(ModelWithIListProperty),
                nameof(ModelWithIListProperty.ListProperty));

            var bindingContext = new DefaultModelBindingContext
            {
                ModelMetadata = metadata,
                ModelName = "someName",
                ModelState = new ModelStateDictionary(),
                ValueProvider = valueProvider,
                ValidationState = new ValidationStateDictionary(),
                FieldName = "testfieldname",
            };

            return bindingContext;
        }

        private static IModelBinder CreateIntBinder()
        {
            return new StubModelBinder(context =>
            {
                var value = context.ValueProvider.GetValue(context.ModelName);
                if (value == ValueProviderResult.None)
                {
                    return ModelBindingResult.Failed();
                }

                object valueToConvert = null;
                if (value.Values.Count == 1)
                {
                    valueToConvert = value.Values[0];
                }
                else if (value.Values.Count > 1)
                {
                    valueToConvert = value.Values.ToArray();
                }

                var model = ModelBindingHelper.ConvertTo(valueToConvert, context.ModelType, value.Culture);
                if (model == null)
                {
                    return ModelBindingResult.Failed();
                }
                else
                {
                    return ModelBindingResult.Success(model);
                }
            });
        }

        private static DefaultModelBindingContext CreateContext()
        {
            var modelBindingContext = new DefaultModelBindingContext()
            {
                ActionContext = new ActionContext()
                {
                    HttpContext = new DefaultHttpContext(),
                },
            };

            return modelBindingContext;
        }

        private class ModelWithListProperty
        {
            public List<string> ListProperty { get; set; }
        }

        private class ModelWithIListProperty
        {
            public IList<int> ListProperty { get; set; }
        }

        private class ModelWithSimpleProperties
        {
            public int Id { get; set; }

            public string Name { get; set; }
        }
    }
}
