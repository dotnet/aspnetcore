// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

public class DictionaryModelBinderTest
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BindModel_Succeeds(bool isReadOnly)
    {
        // Arrange
        var values = new Dictionary<string, string>()
            {
                { "someName[0].Key", "42" },
                { "someName[0].Value", "forty-two" },
                { "someName[1].Key", "84" },
                { "someName[1].Value", "eighty-four" },
            };

        // Value Provider

        var bindingContext = GetModelBindingContext(isReadOnly, values);
        bindingContext.ValueProvider = CreateEnumerableValueProvider("{0}", values);

        var binder = new DictionaryModelBinder<int, string>(
            new SimpleTypeModelBinder(typeof(int), NullLoggerFactory.Instance),
            new SimpleTypeModelBinder(typeof(string), NullLoggerFactory.Instance),
            NullLoggerFactory.Instance);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);

        var dictionary = Assert.IsAssignableFrom<IDictionary<int, string>>(bindingContext.Result.Model);
        Assert.NotNull(dictionary);
        Assert.Equal(2, dictionary.Count);
        Assert.Equal("forty-two", dictionary[42]);
        Assert.Equal("eighty-four", dictionary[84]);

        // This uses the default IValidationStrategy
        Assert.DoesNotContain(bindingContext.Result.Model, bindingContext.ValidationState.Keys);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BindModel_WithExistingModel_Succeeds(bool isReadOnly)
    {
        // Arrange
        var values = new Dictionary<string, string>()
            {
                { "someName[0].Key", "42" },
                { "someName[0].Value", "forty-two" },
                { "someName[1].Key", "84" },
                { "someName[1].Value", "eighty-four" },
            };

        var bindingContext = GetModelBindingContext(isReadOnly, values);
        bindingContext.ValueProvider = CreateEnumerableValueProvider("{0}", values);

        var dictionary = new Dictionary<int, string>();
        bindingContext.Model = dictionary;

        var binder = new DictionaryModelBinder<int, string>(
            new SimpleTypeModelBinder(typeof(int), NullLoggerFactory.Instance),
            new SimpleTypeModelBinder(typeof(string), NullLoggerFactory.Instance),
            NullLoggerFactory.Instance);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);

        Assert.Same(dictionary, bindingContext.Result.Model);
        Assert.NotNull(dictionary);
        Assert.Equal(2, dictionary.Count);
        Assert.Equal("forty-two", dictionary[42]);
        Assert.Equal("eighty-four", dictionary[84]);

        // This uses the default IValidationStrategy
        Assert.DoesNotContain(bindingContext.Result.Model, bindingContext.ValidationState.Keys);
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
        var binder = new DictionaryModelBinder<string, string>(
            new SimpleTypeModelBinder(typeof(string), NullLoggerFactory.Instance),
            new SimpleTypeModelBinder(typeof(string), NullLoggerFactory.Instance),
            NullLoggerFactory.Instance);

        var bindingContext = CreateContext();
        bindingContext.ModelName = modelName;
        bindingContext.ValueProvider = CreateEnumerableValueProvider(keyFormat, dictionary);
        bindingContext.FieldName = modelName;

        var metadataProvider = new TestModelMetadataProvider();
        bindingContext.ModelMetadata = metadataProvider.GetMetadataForProperty(
            typeof(ModelWithDictionaryProperties),
            nameof(ModelWithDictionaryProperties.DictionaryProperty));

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);

        var resultDictionary = Assert.IsAssignableFrom<IDictionary<string, string>>(bindingContext.Result.Model);
        Assert.Equal(dictionary, resultDictionary);
    }

    [Theory]
    [MemberData(nameof(StringToStringData))]
    public async Task BindModel_FallsBackToBindingValues_WhenParameterHasDefaultValue(
        string modelName,
        string keyFormat,
        IDictionary<string, string> dictionary)
    {
        // Arrange
        var binder = new DictionaryModelBinder<string, string>(
            new SimpleTypeModelBinder(typeof(string), NullLoggerFactory.Instance),
            new SimpleTypeModelBinder(typeof(string), NullLoggerFactory.Instance),
            NullLoggerFactory.Instance);

        var bindingContext = CreateContext();
        bindingContext.ModelName = modelName;
        bindingContext.ValueProvider = CreateEnumerableValueProvider(keyFormat, dictionary);
        bindingContext.FieldName = modelName;

        var metadataProvider = new TestModelMetadataProvider();
        var parameter = typeof(DictionaryModelBinderTest)
            .GetMethod(nameof(ActionWithDefaultValueDictionaryParameter), BindingFlags.Instance | BindingFlags.NonPublic)
            .GetParameters()[0];
        bindingContext.ModelMetadata = metadataProvider.GetMetadataForParameter(parameter);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);

        var resultDictionary = Assert.IsAssignableFrom<IDictionary<string, string>>(bindingContext.Result.Model);
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

        var binder = new DictionaryModelBinder<string, string>(
            new SimpleTypeModelBinder(typeof(string), NullLoggerFactory.Instance),
            new SimpleTypeModelBinder(typeof(string), NullLoggerFactory.Instance),
            NullLoggerFactory.Instance);

        var bindingContext = CreateContext();
        bindingContext.ModelName = "prefix";
        bindingContext.ValueProvider = CreateTestValueProvider("prefix[{0}]", dictionary);
        bindingContext.FieldName = bindingContext.ModelName;

        var metadataProvider = new TestModelMetadataProvider();
        bindingContext.ModelMetadata = metadataProvider.GetMetadataForProperty(
            typeof(ModelWithDictionaryProperties),
            nameof(ModelWithDictionaryProperties.DictionaryProperty));

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);

        var resultDictionary = Assert.IsAssignableFrom<IDictionary<string, string>>(bindingContext.Result.Model);
        Assert.Empty(resultDictionary);
    }

    // Similar to one BindModel_FallsBackToBindingValues case but without an IEnumerableValueProvider.
    [Fact]
    public async Task BindModel_DoesNotFallBack_WithoutEnumerableValueProvider_WhenParameterHasDefaultValue()
    {
        // Arrange
        var dictionary = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "one", "one" },
                { "two", "two" },
                { "three", "three" },
            };

        var binder = new DictionaryModelBinder<string, string>(
            new SimpleTypeModelBinder(typeof(string), NullLoggerFactory.Instance),
            new SimpleTypeModelBinder(typeof(string), NullLoggerFactory.Instance),
            NullLoggerFactory.Instance);

        var bindingContext = CreateContext();
        bindingContext.ModelName = "prefix";
        bindingContext.ValueProvider = CreateTestValueProvider("prefix[{0}]", dictionary);
        bindingContext.FieldName = bindingContext.ModelName;

        var metadataProvider = new TestModelMetadataProvider();
        var parameter = typeof(DictionaryModelBinderTest)
            .GetMethod(nameof(ActionWithDefaultValueDictionaryParameter), BindingFlags.Instance | BindingFlags.NonPublic)
            .GetParameters()[0];
        bindingContext.ModelMetadata = metadataProvider.GetMetadataForParameter(parameter);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.False(bindingContext.Result.IsModelSet);
        Assert.Null(bindingContext.Result.Model);
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
        var stringDictionary = dictionary.ToDictionary(kvp => kvp.Key.ToString(CultureInfo.InvariantCulture), kvp => kvp.Value.ToString(CultureInfo.InvariantCulture));

        var binder = new DictionaryModelBinder<long, int>(
            new SimpleTypeModelBinder(typeof(long), NullLoggerFactory.Instance),
            new SimpleTypeModelBinder(typeof(int), NullLoggerFactory.Instance),
            NullLoggerFactory.Instance);

        var bindingContext = CreateContext();
        bindingContext.ModelName = "prefix";
        bindingContext.ValueProvider = CreateEnumerableValueProvider("prefix[{0}]", stringDictionary);
        bindingContext.FieldName = bindingContext.ModelName;

        var metadataProvider = new TestModelMetadataProvider();
        bindingContext.ModelMetadata = metadataProvider.GetMetadataForProperty(
            typeof(ModelWithDictionaryProperties),
            nameof(ModelWithDictionaryProperties.DictionaryWithValueTypesProperty));

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);

        var resultDictionary = Assert.IsAssignableFrom<IDictionary<long, int>>(bindingContext.Result.Model);
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

        var bindingContext = CreateContext();
        bindingContext.ModelName = "prefix";
        bindingContext.ValueProvider = CreateEnumerableValueProvider("{0}", stringDictionary);
        bindingContext.FieldName = bindingContext.ModelName;

        var metadataProvider = new TestModelMetadataProvider();
        bindingContext.ModelMetadata = metadataProvider.GetMetadataForProperty(
            typeof(ModelWithDictionaryProperties),
            nameof(ModelWithDictionaryProperties.DictionaryWithComplexValuesProperty));

        var valueMetadata = metadataProvider.GetMetadataForType(typeof(ModelWithProperties));

        var binder = new DictionaryModelBinder<int, ModelWithProperties>(
            new SimpleTypeModelBinder(typeof(int), NullLoggerFactory.Instance),
            new ComplexObjectModelBinder(new Dictionary<ModelMetadata, IModelBinder>()
            {
                    { valueMetadata.Properties["Id"], new SimpleTypeModelBinder(typeof(int), NullLoggerFactory.Instance) },
                    { valueMetadata.Properties["Name"], new SimpleTypeModelBinder(typeof(string), NullLoggerFactory.Instance) },
            },
            Array.Empty<IModelBinder>(),
            NullLogger<ComplexObjectModelBinder>.Instance),
            NullLoggerFactory.Instance);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);

        var resultDictionary = Assert.IsAssignableFrom<IDictionary<int, ModelWithProperties>>(bindingContext.Result.Model);
        Assert.Equal(dictionary, resultDictionary);

        // This requires a non-default IValidationStrategy
        Assert.Contains(bindingContext.Result.Model, bindingContext.ValidationState.Keys);
        var entry = bindingContext.ValidationState[bindingContext.Result.Model];
        var strategy = Assert.IsType<ShortFormDictionaryValidationStrategy<int, ModelWithProperties>>(entry.Strategy);
        Assert.Equal(
            new KeyValuePair<string, int>[]
            {
                    new KeyValuePair<string, int>("prefix[23]", 23),
                    new KeyValuePair<string, int>("prefix[27]", 27),
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
        var binder = new DictionaryModelBinder<string, string>(
            new SimpleTypeModelBinder(typeof(string), NullLoggerFactory.Instance),
            new SimpleTypeModelBinder(typeof(string), NullLoggerFactory.Instance),
            NullLoggerFactory.Instance);

        var bindingContext = CreateContext();
        bindingContext.ModelName = modelName;

        bindingContext.ValueProvider = CreateEnumerableValueProvider(keyFormat, dictionary);
        bindingContext.FieldName = bindingContext.ModelName;

        var metadataProvider = new TestModelMetadataProvider();
        bindingContext.ModelMetadata = metadataProvider.GetMetadataForProperty(
            typeof(ModelWithDictionaryProperties),
            nameof(ModelWithDictionaryProperties.CustomDictionaryProperty));

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);

        var resultDictionary = Assert.IsAssignableFrom<SortedDictionary<string, string>>(bindingContext.Result.Model);
        Assert.Equal(expectedDictionary, resultDictionary);
    }

    private IActionResult ActionWithDictionaryParameter(Dictionary<string, string> parameter) => null;

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task DictionaryModelBinder_CreatesEmptyCollection_IfIsTopLevelObject(
        bool allowValidatingTopLevelNodes,
        bool isBindingRequired)
    {
        // Arrange
        var expectedErrorCount = isBindingRequired ? 1 : 0;
        var binder = new DictionaryModelBinder<string, string>(
            new SimpleTypeModelBinder(typeof(string), NullLoggerFactory.Instance),
            new SimpleTypeModelBinder(typeof(string), NullLoggerFactory.Instance),
            NullLoggerFactory.Instance,
            allowValidatingTopLevelNodes);

        var bindingContext = CreateContext();
        bindingContext.IsTopLevelObject = true;

        // Lack of prefix and non-empty model name both ignored.
        bindingContext.ModelName = "modelName";

        var metadataProvider = new TestModelMetadataProvider();
        var parameter = typeof(DictionaryModelBinderTest)
            .GetMethod(nameof(ActionWithDictionaryParameter), BindingFlags.Instance | BindingFlags.NonPublic)
            .GetParameters()[0];
        metadataProvider
            .ForParameter(parameter)
            .BindingDetails(b => b.IsBindingRequired = isBindingRequired);
        bindingContext.ModelMetadata = metadataProvider.GetMetadataForParameter(parameter);

        bindingContext.ValueProvider = new TestValueProvider(new Dictionary<string, object>());

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.Empty(Assert.IsType<Dictionary<string, string>>(bindingContext.Result.Model));
        Assert.True(bindingContext.Result.IsModelSet);
        Assert.Equal(expectedErrorCount, bindingContext.ModelState.ErrorCount);
    }

    [Fact]
    public async Task DictionaryModelBinder_CreatesEmptyCollectionAndAddsError_IfIsTopLevelObject()
    {
        // Arrange
        var binder = new DictionaryModelBinder<string, string>(
            new SimpleTypeModelBinder(typeof(string), NullLoggerFactory.Instance),
            new SimpleTypeModelBinder(typeof(string), NullLoggerFactory.Instance),
            NullLoggerFactory.Instance,
            allowValidatingTopLevelNodes: true);

        var bindingContext = CreateContext();
        bindingContext.IsTopLevelObject = true;
        bindingContext.FieldName = "fieldName";
        bindingContext.ModelName = "modelName";

        var metadataProvider = new TestModelMetadataProvider();
        var parameter = typeof(DictionaryModelBinderTest)
            .GetMethod(nameof(ActionWithDictionaryParameter), BindingFlags.Instance | BindingFlags.NonPublic)
            .GetParameters()[0];
        metadataProvider
            .ForParameter(parameter)
            .BindingDetails(b => b.IsBindingRequired = true);
        bindingContext.ModelMetadata = metadataProvider.GetMetadataForParameter(parameter);

        bindingContext.ValueProvider = new TestValueProvider(new Dictionary<string, object>());

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.Empty(Assert.IsType<Dictionary<string, string>>(bindingContext.Result.Model));
        Assert.True(bindingContext.Result.IsModelSet);

        var keyValuePair = Assert.Single(bindingContext.ModelState);
        Assert.Equal("modelName", keyValuePair.Key);
        var error = Assert.Single(keyValuePair.Value.Errors);
        Assert.Equal("A value for the 'fieldName' parameter or property was not provided.", error.ErrorMessage);
    }

    private IActionResult ActionWithDefaultValueDictionaryParameter(Dictionary<string, string> parameter = null) => null;

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task DictionaryModelBinder_DoesNotCreateEmptyCollection_IfIsTopLevelObjectAndHasDefaultValue(
        bool allowValidatingTopLevelNodes,
        bool isBindingRequired)
    {
        // Arrange
        var expectedErrorCount = isBindingRequired ? 1 : 0;
        var binder = new DictionaryModelBinder<string, string>(
            new SimpleTypeModelBinder(typeof(string), NullLoggerFactory.Instance),
            new SimpleTypeModelBinder(typeof(string), NullLoggerFactory.Instance),
            NullLoggerFactory.Instance,
            allowValidatingTopLevelNodes);

        var bindingContext = CreateContext();
        bindingContext.IsTopLevelObject = true;

        // Lack of prefix and non-empty model name both ignored.
        bindingContext.ModelName = "modelName";

        var metadataProvider = new TestModelMetadataProvider();
        var parameter = typeof(DictionaryModelBinderTest)
            .GetMethod(nameof(ActionWithDefaultValueDictionaryParameter), BindingFlags.Instance | BindingFlags.NonPublic)
            .GetParameters()[0];
        metadataProvider
            .ForParameter(parameter)
            .BindingDetails(b => b.IsBindingRequired = isBindingRequired);
        bindingContext.ModelMetadata = metadataProvider.GetMetadataForParameter(parameter);

        bindingContext.ValueProvider = new TestValueProvider(new Dictionary<string, object>());

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.Null(bindingContext.Result.Model);
        Assert.False(bindingContext.Result.IsModelSet);
        Assert.Equal(expectedErrorCount, bindingContext.ModelState.ErrorCount);
    }

    [Theory]
    [InlineData("", false, false)]
    [InlineData("", true, false)]
    [InlineData("", false, true)]
    [InlineData("", true, true)]
    [InlineData("param", false, false)]
    [InlineData("param", true, false)]
    [InlineData("param", false, true)]
    [InlineData("param", true, true)]
    public async Task DictionaryModelBinder_DoesNotCreateCollection_IfNotIsTopLevelObject(
        string prefix,
        bool allowValidatingTopLevelNodes,
        bool isBindingRequired)
    {
        // Arrange
        var binder = new DictionaryModelBinder<int, int>(
            new SimpleTypeModelBinder(typeof(int), NullLoggerFactory.Instance),
            new SimpleTypeModelBinder(typeof(int), NullLoggerFactory.Instance),
            NullLoggerFactory.Instance,
            allowValidatingTopLevelNodes);

        var bindingContext = CreateContext();
        bindingContext.ModelName = ModelNames.CreatePropertyModelName(prefix, "ListProperty");

        var metadataProvider = new TestModelMetadataProvider();
        metadataProvider
            .ForProperty(
                typeof(ModelWithDictionaryProperties),
                nameof(ModelWithDictionaryProperties.DictionaryProperty))
            .BindingDetails(b => b.IsBindingRequired = isBindingRequired);
        bindingContext.ModelMetadata = metadataProvider.GetMetadataForProperty(
            typeof(ModelWithDictionaryProperties),
            nameof(ModelWithDictionaryProperties.DictionaryProperty));

        bindingContext.ValueProvider = new TestValueProvider(new Dictionary<string, object>());

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.False(bindingContext.Result.IsModelSet);
        Assert.Equal(0, bindingContext.ModelState.ErrorCount);
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
                    { typeof(IList<KeyValuePair<int, int>>), true },
                    { typeof(ISet<KeyValuePair<int, int>>), false },
                };
        }
    }

    [Theory]
    [MemberData(nameof(CanCreateInstanceData))]
    public void CanCreateInstance_ReturnsExpectedValue(Type modelType, bool expectedResult)
    {
        // Arrange
        var binder = new DictionaryModelBinder<int, int>(
            new SimpleTypeModelBinder(typeof(int), NullLoggerFactory.Instance),
            new SimpleTypeModelBinder(typeof(int), NullLoggerFactory.Instance),
            NullLoggerFactory.Instance);

        // Act
        var result = binder.CanCreateInstance(modelType);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    private static DefaultModelBindingContext CreateContext()
    {
        var actionContext = new ActionContext()
        {
            HttpContext = new DefaultHttpContext(),
        };
        var modelBindingContext = new DefaultModelBindingContext()
        {
            ActionContext = actionContext,
            ModelState = actionContext.ModelState,
            ValidationState = new ValidationStateDictionary(),
        };

        return modelBindingContext;
    }

    private static IValueProvider CreateEnumerableValueProvider(
        string keyFormat,
        IDictionary<string, string> dictionary)
    {
        // Convert to an IDictionary<string, StringValues> then wrap it up.
        var backingStore = dictionary.ToDictionary(
            kvp => string.Format(CultureInfo.InvariantCulture, keyFormat, kvp.Key),
            kvp => (StringValues)kvp.Value);

        var formCollection = new FormCollection(backingStore);

        return new FormValueProvider(
            BindingSource.Form,
            formCollection,
            CultureInfo.InvariantCulture);
    }

    // Like CreateEnumerableValueProvider except returned instance does not implement IEnumerableValueProvider.
    private static IValueProvider CreateTestValueProvider(string keyFormat, IDictionary<string, string> dictionary)
    {
        // Convert to an IDictionary<string, object> then wrap it up.
        var backingStore = dictionary.ToDictionary(
            kvp => string.Format(CultureInfo.InvariantCulture, keyFormat, kvp.Key),
            kvp => (object)kvp.Value);

        return new TestValueProvider(BindingSource.Form, backingStore);
    }

    private static DefaultModelBindingContext GetModelBindingContext(
        bool isReadOnly,
        IDictionary<string, string> values = null)
    {
        var metadataProvider = new TestModelMetadataProvider();
        metadataProvider
            .ForProperty<ModelWithIDictionaryProperty>(nameof(ModelWithIDictionaryProperty.DictionaryProperty))
            .BindingDetails(bd => bd.IsReadOnly = isReadOnly);
        var metadata = metadataProvider.GetMetadataForProperty(
            typeof(ModelWithIDictionaryProperty),
            nameof(ModelWithIDictionaryProperty.DictionaryProperty));

        var valueProvider = new SimpleValueProvider();
        foreach (var kvp in values)
        {
            valueProvider.Add(kvp.Key, string.Empty);
        }

        var bindingContext = CreateContext();
        bindingContext.ModelMetadata = metadata;
        bindingContext.ModelName = "someName";
        bindingContext.ValueProvider = valueProvider;

        return bindingContext;
    }

    private class ModelWithIDictionaryProperty
    {
        public IDictionary<int, string> DictionaryProperty { get; set; }
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
            return obj is ModelWithProperties other &&
                Id == other.Id &&
                string.Equals(Name, other.Name, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            var nameCode = Name == null ? 0 : Name.GetHashCode();
            return nameCode ^ Id.GetHashCode();
        }

        public override string ToString()
        {
            return $"{{{ Id }, '{ Name }'}}";
        }
    }
}
