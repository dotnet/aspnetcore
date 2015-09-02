// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if DNX451
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Microsoft.Framework.Primitives;
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
            var values = new Dictionary<string, KeyValuePair<int, string>>()
            {
                { "someName[0]", new KeyValuePair<int, string>(42, "forty-two") },
                { "someName[1]", new KeyValuePair<int, string>(84, "eighty-four") },
            };

            var bindingContext = GetModelBindingContext(isReadOnly, values);
            var modelState = bindingContext.ModelState;
            var binder = new DictionaryModelBinder<int, string>();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotEqual(ModelBindingResult.NoResult, result);
            Assert.True(result.IsModelSet);
            var dictionary = Assert.IsAssignableFrom<IDictionary<int, string>>(result.Model);
            Assert.True(modelState.IsValid);

            Assert.NotNull(dictionary);
            Assert.Equal(2, dictionary.Count);
            Assert.Equal("forty-two", dictionary[42]);
            Assert.Equal("eighty-four", dictionary[84]);

            // This uses the default IValidationStrategy
            Assert.DoesNotContain(result.Model, bindingContext.ValidationState.Keys);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task BindModel_WithExistingModel_Succeeds(bool isReadOnly)
        {
            // Arrange
            var values = new Dictionary<string, KeyValuePair<int, string>>()
            {
                { "someName[0]", new KeyValuePair<int, string>(42, "forty-two") },
                { "someName[1]", new KeyValuePair<int, string>(84, "eighty-four") },
            };

            var bindingContext = GetModelBindingContext(isReadOnly, values);
            var modelState = bindingContext.ModelState;
            var dictionary = new Dictionary<int, string>();
            bindingContext.Model = dictionary;
            var binder = new DictionaryModelBinder<int, string>();

            // Act
            var result = await binder.BindModelAsync(bindingContext);

            // Assert
            Assert.NotEqual(ModelBindingResult.NoResult, result);
            Assert.True(result.IsModelSet);
            Assert.Same(dictionary, result.Model);
            Assert.True(modelState.IsValid);

            Assert.NotNull(dictionary);
            Assert.Equal(2, dictionary.Count);
            Assert.Equal("forty-two", dictionary[42]);
            Assert.Equal("eighty-four", dictionary[84]);

            // This uses the default IValidationStrategy
            Assert.DoesNotContain(result.Model, bindingContext.ValidationState.Keys);
        }

        // modelName, keyFormat, dictionary
        public static TheoryData<string, string, IDictionary<string, string>> StringToStringData
        {
            get
            {
                var dictionaryWithOne = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    { "one", "one" },
                };
                var dictionaryWithThree = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    { "one", "one" },
                    { "two", "two" },
                    { "three", "three" },
                };

                return new TheoryData<string, string, IDictionary<string, string>>
                {
                    { string.Empty, "[{0}]", dictionaryWithOne },
                    { string.Empty, "[{0}]", dictionaryWithThree },
                    { "prefix", "prefix[{0}]", dictionaryWithOne },
                    { "prefix", "prefix[{0}]", dictionaryWithThree },
                    { "prefix.property", "prefix.property[{0}]", dictionaryWithOne },
                    { "prefix.property", "prefix.property[{0}]", dictionaryWithThree },
                };
            }
        }

        [Theory]
        [MemberData(nameof(StringToStringData))]
        public async Task BindModel_FallsBackToBindingValues(
            string modelName,
            string keyFormat,
            IDictionary<string, string> dictionary)
        {
            // Arrange
            var binder = new DictionaryModelBinder<string, string>();
            var context = CreateContext();
            context.ModelName = modelName;
            context.OperationBindingContext.ModelBinder = CreateCompositeBinder();
            context.OperationBindingContext.ValueProvider = CreateEnumerableValueProvider(keyFormat, dictionary);
            context.ValueProvider = context.OperationBindingContext.ValueProvider;

            var metadataProvider = context.OperationBindingContext.MetadataProvider;
            context.ModelMetadata = metadataProvider.GetMetadataForProperty(
                typeof(ModelWithDictionaryProperties),
                nameof(ModelWithDictionaryProperties.DictionaryProperty));

            // Act
            var result = await binder.BindModelAsync(context);

            // Assert
            Assert.NotEqual(ModelBindingResult.NoResult, result);
            Assert.True(result.IsModelSet);
            Assert.Equal(modelName, result.Key);

            var resultDictionary = Assert.IsAssignableFrom<IDictionary<string, string>>(result.Model);
            Assert.Equal(dictionary, resultDictionary);
        }

        // Similar to one BindModel_FallsBackToBindingValues case but without an IEnumerableValueProvider.
        [Fact]
        public async Task BindModel_DoesNotFallBack_WithoutEnumerableValueProvider()
        {
            // Arrange
            var dictionary = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "one", "one" },
                { "two", "two" },
                { "three", "three" },
            };

            var binder = new DictionaryModelBinder<string, string>();
            var context = CreateContext();
            context.ModelName = "prefix";
            context.OperationBindingContext.ModelBinder = CreateCompositeBinder();
            context.OperationBindingContext.ValueProvider = CreateTestValueProvider("prefix[{0}]", dictionary);
            context.ValueProvider = context.OperationBindingContext.ValueProvider;

            var metadataProvider = context.OperationBindingContext.MetadataProvider;
            context.ModelMetadata = metadataProvider.GetMetadataForProperty(
                typeof(ModelWithDictionaryProperties),
                nameof(ModelWithDictionaryProperties.DictionaryProperty));

            // Act
            var result = await binder.BindModelAsync(context);

            // Assert
            Assert.NotEqual(ModelBindingResult.NoResult, result);
            Assert.True(result.IsModelSet);
            Assert.Equal("prefix", result.Key);

            var resultDictionary = Assert.IsAssignableFrom<IDictionary<string, string>>(result.Model);
            Assert.Empty(resultDictionary);
        }

        public static TheoryData<IDictionary<long, int>> LongToIntData
        {
            get
            {
                var dictionaryWithOne = new Dictionary<long, int>
                {
                    { 0L, 0 },
                };
                var dictionaryWithThree = new Dictionary<long, int>
                {
                    { -1L, -1 },
                    { long.MaxValue, int.MaxValue },
                    { long.MinValue, int.MinValue },
                };

                return new TheoryData<IDictionary<long, int>> { dictionaryWithOne, dictionaryWithThree };
            }
        }

        [Theory]
        [MemberData(nameof(LongToIntData))]
        public async Task BindModel_FallsBackToBindingValues_WithValueTypes(IDictionary<long, int> dictionary)
        {
            // Arrange
            var stringDictionary = dictionary.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value.ToString());
            var binder = new DictionaryModelBinder<long, int>();
            var context = CreateContext();
            context.ModelName = "prefix";
            context.OperationBindingContext.ModelBinder = CreateCompositeBinder();
            context.OperationBindingContext.ValueProvider =
                CreateEnumerableValueProvider("prefix[{0}]", stringDictionary);
            context.ValueProvider = context.OperationBindingContext.ValueProvider;

            var metadataProvider = context.OperationBindingContext.MetadataProvider;
            context.ModelMetadata = metadataProvider.GetMetadataForProperty(
                typeof(ModelWithDictionaryProperties),
                nameof(ModelWithDictionaryProperties.DictionaryWithValueTypesProperty));

            // Act
            var result = await binder.BindModelAsync(context);

            // Assert
            Assert.NotEqual(ModelBindingResult.NoResult, result);
            Assert.True(result.IsModelSet);
            Assert.Equal("prefix", result.Key);

            var resultDictionary = Assert.IsAssignableFrom<IDictionary<long, int>>(result.Model);
            Assert.Equal(dictionary, resultDictionary);
        }

        [Fact]
        public async Task BindModel_FallsBackToBindingValues_WithComplexValues()
        {
            // Arrange
            var dictionary = new Dictionary<int, ModelWithProperties>
            {
                { 23, new ModelWithProperties { Id = 43, Name = "Wilma" } },
                { 27, new ModelWithProperties { Id = 98, Name = "Fred" } },
            };
            var stringDictionary = new Dictionary<string, string>
            {
                { "prefix[23].Id", "43" },
                { "prefix[23].Name", "Wilma" },
                { "prefix[27].Id", "98" },
                { "prefix[27].Name", "Fred" },
            };
            var binder = new DictionaryModelBinder<int, ModelWithProperties>();
            var context = CreateContext();
            context.ModelName = "prefix";
            context.OperationBindingContext.ModelBinder = CreateCompositeBinder();
            context.OperationBindingContext.ValueProvider = CreateEnumerableValueProvider("{0}", stringDictionary);
            context.ValueProvider = context.OperationBindingContext.ValueProvider;

            var metadataProvider = context.OperationBindingContext.MetadataProvider;
            context.ModelMetadata = metadataProvider.GetMetadataForProperty(
                typeof(ModelWithDictionaryProperties),
                nameof(ModelWithDictionaryProperties.DictionaryWithComplexValuesProperty));

            // Act
            var result = await binder.BindModelAsync(context);

            // Assert
            Assert.NotEqual(ModelBindingResult.NoResult, result);
            Assert.True(result.IsModelSet);
            Assert.Equal("prefix", result.Key);

            var resultDictionary = Assert.IsAssignableFrom<IDictionary<int, ModelWithProperties>>(result.Model);
            Assert.Equal(dictionary, resultDictionary);

            // This requires a non-default IValidationStrategy
            Assert.Contains(result.Model, context.ValidationState.Keys);
            var entry = context.ValidationState[result.Model];
            var strategy = Assert.IsType<ShortFormDictionaryValidationStrategy<int, ModelWithProperties>>(entry.Strategy);
            Assert.Equal(
                new KeyValuePair<string, int>[]
                {
                    new KeyValuePair<string, int>("23", 23),
                    new KeyValuePair<string, int>("27", 27),
                }.OrderBy(kvp => kvp.Key),
                strategy.KeyMappings.OrderBy(kvp => kvp.Key));
        }

        [Theory]
        [MemberData(nameof(StringToStringData))]
        public async Task BindModel_FallsBackToBindingValues_WithCustomDictionary(
            string modelName,
            string keyFormat,
            IDictionary<string, string> dictionary)
        {
            // Arrange
            var expectedDictionary = new SortedDictionary<string, string>(dictionary);
            var binder = new DictionaryModelBinder<string, string>();
            var context = CreateContext();
            context.ModelName = modelName;
            context.OperationBindingContext.ModelBinder = CreateCompositeBinder();
            context.OperationBindingContext.ValueProvider = CreateEnumerableValueProvider(keyFormat, dictionary);
            context.ValueProvider = context.OperationBindingContext.ValueProvider;

            var metadataProvider = context.OperationBindingContext.MetadataProvider;
            context.ModelMetadata = metadataProvider.GetMetadataForProperty(
                typeof(ModelWithDictionaryProperties),
                nameof(ModelWithDictionaryProperties.CustomDictionaryProperty));

            // Act
            var result = await binder.BindModelAsync(context);

            // Assert
            Assert.NotEqual(ModelBindingResult.NoResult, result);
            Assert.True(result.IsModelSet);
            Assert.Equal(modelName, result.Key);

            var resultDictionary = Assert.IsAssignableFrom<SortedDictionary<string, string>>(result.Model);
            Assert.Equal(expectedDictionary, resultDictionary);
        }

        [Fact]
        public async Task DictionaryModelBinder_CreatesEmptyCollection_IfIsTopLevelObject()
        {
            // Arrange
            var binder = new DictionaryModelBinder<string, string>();

            var context = CreateContext();
            context.IsTopLevelObject = true;

            // Lack of prefix and non-empty model name both ignored.
            context.ModelName = "modelName";

            var metadataProvider = context.OperationBindingContext.MetadataProvider;
            context.ModelMetadata = metadataProvider.GetMetadataForType(typeof(Dictionary<string, string>));

            context.ValueProvider = new TestValueProvider(new Dictionary<string, object>());

            // Act
            var result = await binder.BindModelAsync(context);

            // Assert
            Assert.NotEqual(ModelBindingResult.NoResult, result);

            Assert.Empty(Assert.IsType<Dictionary<string, string>>(result.Model));
            Assert.Equal("modelName", result.Key);
            Assert.True(result.IsModelSet);
        }

        [Theory]
        [InlineData("")]
        [InlineData("param")]
        public async Task DictionaryModelBinder_DoesNotCreateCollection_IfNotIsTopLevelObject(string prefix)
        {
            // Arrange
            var binder = new DictionaryModelBinder<string, string>();

            var context = CreateContext();
            context.ModelName = ModelNames.CreatePropertyModelName(prefix, "ListProperty");

            var metadataProvider = context.OperationBindingContext.MetadataProvider;
            context.ModelMetadata = metadataProvider.GetMetadataForProperty(
                typeof(ModelWithDictionaryProperties),
                nameof(ModelWithDictionaryProperties.DictionaryProperty));

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
                    { typeof(IEnumerable<KeyValuePair<int, int>>), true },
                    { typeof(ICollection<KeyValuePair<int, int>>), true },
                    { typeof(IDictionary<int, int>), true },
                    { typeof(Dictionary<int, int>), true },
                    { typeof(SortedDictionary<int, int>), true },
                    { typeof(IList<KeyValuePair<int, int>>), false },
                    { typeof(DictionaryWithInternalConstructor<int, int>), false },
                    { typeof(DictionaryWithThrowingConstructor<int, int>), false },
                };
            }
        }

        [Theory]
        [MemberData(nameof(CanCreateInstanceData))]
        public void CanCreateInstance_ReturnsExpectedValue(Type modelType, bool expectedResult)
        {
            // Arrange
            var binder = new DictionaryModelBinder<int, int>();

            // Act
            var result = binder.CanCreateInstance(modelType);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        private static ModelBindingContext CreateContext()
        {
            var modelBindingContext = new ModelBindingContext()
            {
                ModelState = new ModelStateDictionary(),
                OperationBindingContext = new OperationBindingContext()
                {
                    HttpContext = new DefaultHttpContext(),
                    MetadataProvider = new TestModelMetadataProvider(),
                },
                ValidationState = new ValidationStateDictionary(),
            };

            return modelBindingContext;
        }

        private static IModelBinder CreateCompositeBinder()
        {
            var binders = new IModelBinder[]
            {
                new SimpleTypeModelBinder(),
                new MutableObjectModelBinder(),
            };

            return new CompositeModelBinder(binders);
        }

        private static IValueProvider CreateEnumerableValueProvider(
            string keyFormat,
            IDictionary<string, string> dictionary)
        {
            // Convert to an IDictionary<string, StringValues> then wrap it up.
            var backingStore = dictionary.ToDictionary(
                kvp => string.Format(keyFormat, kvp.Key),
                kvp => (StringValues)kvp.Value);
            var stringCollection = new ReadableStringCollection(backingStore);

            return new ReadableStringCollectionValueProvider(
                BindingSource.Form,
                stringCollection,
                CultureInfo.InvariantCulture);
        }

        // Like CreateEnumerableValueProvider except returned instance does not implement IEnumerableValueProvider.
        private static IValueProvider CreateTestValueProvider(string keyFormat, IDictionary<string, string> dictionary)
        {
            // Convert to an IDictionary<string, object> then wrap it up.
            var backingStore = dictionary.ToDictionary(
                kvp => string.Format(keyFormat, kvp.Key),
                kvp => (object)kvp.Value);

            return new TestValueProvider(BindingSource.Form, backingStore);
        }

        private static ModelBindingContext GetModelBindingContext(
            bool isReadOnly,
            IDictionary<string, KeyValuePair<int, string>> values)
        {
            var metadataProvider = new TestModelMetadataProvider();
            metadataProvider.ForType<IDictionary<int, string>>().BindingDetails(bd => bd.IsReadOnly = isReadOnly);

            var binder = new Mock<IModelBinder>();
            binder
                .Setup(mb => mb.BindModelAsync(It.IsAny<ModelBindingContext>()))
                .Returns<ModelBindingContext>(mbc =>
                {
                    KeyValuePair<int, string> value;
                    if (values.TryGetValue(mbc.ModelName, out value))
                    {
                        return ModelBindingResult.SuccessAsync(mbc.ModelName, value);
                    }
                    else
                    {
                        return ModelBindingResult.NoResultAsync;
                    }
                });

            var valueProvider = new SimpleValueProvider();
            foreach (var kvp in values)
            {
                valueProvider.Add(kvp.Key, string.Empty);
            }

            var bindingContext = new ModelBindingContext
            {
                ModelMetadata = metadataProvider.GetMetadataForType(typeof(IDictionary<int, string>)),
                ModelName = "someName",
                ModelState = new ModelStateDictionary(),
                OperationBindingContext = new OperationBindingContext
                {
                    ModelBinder = binder.Object,
                    MetadataProvider = metadataProvider,
                    ValueProvider = valueProvider,
                },
                ValueProvider = valueProvider,
                ValidationState = new ValidationStateDictionary(),
            };

            return bindingContext;
        }

        private class ModelWithDictionaryProperties
        {
            // A Dictionary<string, string> instance cannot be assigned to this property.
            public SortedDictionary<string, string> CustomDictionaryProperty { get; set; }

            public Dictionary<string, string> DictionaryProperty { get; set; }

            public Dictionary<int, ModelWithProperties> DictionaryWithComplexValuesProperty { get; set; }

            public Dictionary<long, int> DictionaryWithValueTypesProperty { get; set; }
        }

        private class ModelWithProperties
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public override bool Equals(object obj)
            {
                var other = obj as ModelWithProperties;
                return other != null &&
                    Id == other.Id &&
                    string.Equals(Name, other.Name, StringComparison.Ordinal);
            }

            public override int GetHashCode()
            {
                int nameCode = Name == null ? 0 : Name.GetHashCode();
                return nameCode ^ Id.GetHashCode();
            }

            public override string ToString()
            {
                return $"{{{ Id }, '{ Name }'}}";
            }
        }

        private class DictionaryWithInternalConstructor<TKey, TValue> : Dictionary<TKey, TValue>
        {
            internal DictionaryWithInternalConstructor()
                : base()
            {
            }
        }

        private class DictionaryWithThrowingConstructor<TKey, TValue> : Dictionary<TKey, TValue>
        {
            public DictionaryWithThrowingConstructor()
                : base()
            {
                throw new ApplicationException("No, don't do this.");
            }
        }
    }
}
#endif
