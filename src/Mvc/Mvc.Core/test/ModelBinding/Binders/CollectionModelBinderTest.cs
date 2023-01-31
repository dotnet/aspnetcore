// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

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
        var binder = new CollectionModelBinder<int>(CreateIntBinder(), NullLoggerFactory.Instance);

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
        var binder = new CollectionModelBinder<int>(CreateIntBinder(), NullLoggerFactory.Instance);

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
        var binder = new CollectionModelBinder<int>(CreateIntBinder(), NullLoggerFactory.Instance);

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
        var binder = new CollectionModelBinder<int>(CreateIntBinder(), NullLoggerFactory.Instance);

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
        var binder = new CollectionModelBinder<int>(CreateIntBinder(), NullLoggerFactory.Instance);

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
        var binder = new CollectionModelBinder<int>(CreateIntBinder(), NullLoggerFactory.Instance);

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
        var binder = new CollectionModelBinder<int>(CreateIntBinder(), NullLoggerFactory.Instance);
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
        var binder = new CollectionModelBinder<int>(CreateIntBinder(), NullLoggerFactory.Instance);
        var context = GetModelBindingContext(new SimpleValueProvider());

        // Act
        var boundCollection = await binder.BindSimpleCollection(context, new ValueProviderResult(new string[0]));

        // Assert
        Assert.NotNull(boundCollection.Model);
        Assert.Empty(boundCollection.Model);
    }

    [Fact]
    public async Task BindSimpleCollection_RawValueWithNull_ReturnsListWithoutNull()
    {
        // Arrange
        var binder = new CollectionModelBinder<int>(CreateIntBinder(), NullLoggerFactory.Instance);
        var valueProvider = new SimpleValueProvider
            {
                { "someName", "420" },
            };
        var context = GetModelBindingContext(valueProvider);
        var valueProviderResult = new ValueProviderResult(new[] { null, "42", "", "100", null, "200" });

        // Act
        var boundCollection = await binder.BindSimpleCollection(context, valueProviderResult);

        // Assert
        Assert.NotNull(boundCollection.Model);
        Assert.Equal(new[] { 420, 42, 100, 420, 200 }, boundCollection.Model);
    }

    private IActionResult ActionWithListParameter(List<string> parameter) => null;
    private IActionResult ActionWithListParameterDefaultValue(List<string> parameter = null) => null;

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task CollectionModelBinder_CreatesEmptyCollection_IfIsTopLevelObject(
        bool allowValidatingTopLevelNodes,
        bool isBindingRequired)
    {
        // Arrange
        var binder = new CollectionModelBinder<string>(
            new StubModelBinder(result: ModelBindingResult.Failed()),
            NullLoggerFactory.Instance,
            allowValidatingTopLevelNodes);

        var bindingContext = CreateContext();
        bindingContext.IsTopLevelObject = true;

        // Lack of prefix and non-empty model name both ignored.
        bindingContext.ModelName = "modelName";

        var metadataProvider = new TestModelMetadataProvider();
        var parameter = typeof(CollectionModelBinderTest)
            .GetMethod(nameof(ActionWithListParameter), BindingFlags.Instance | BindingFlags.NonPublic)
            .GetParameters()[0];
        metadataProvider
            .ForParameter(parameter)
            .BindingDetails(b => b.IsBindingRequired = isBindingRequired);
        bindingContext.ModelMetadata = metadataProvider.GetMetadataForParameter(parameter);

        bindingContext.ValueProvider = new TestValueProvider(new Dictionary<string, object>());

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.Empty(Assert.IsType<List<string>>(bindingContext.Result.Model));
        Assert.True(bindingContext.Result.IsModelSet);
        Assert.Equal(0, bindingContext.ModelState.ErrorCount);
    }

    [Fact]
    public async Task CollectionModelBinder_CreatesEmptyCollectionAndAddsError_IfIsTopLevelObject()
    {
        // Arrange
        var binder = new CollectionModelBinder<string>(
            new StubModelBinder(result: ModelBindingResult.Failed()),
            NullLoggerFactory.Instance,
            allowValidatingTopLevelNodes: true);

        var bindingContext = CreateContext();
        bindingContext.IsTopLevelObject = true;
        bindingContext.FieldName = "fieldName";
        bindingContext.ModelName = "modelName";

        var metadataProvider = new TestModelMetadataProvider();
        var parameter = typeof(CollectionModelBinderTest)
            .GetMethod(nameof(ActionWithListParameter), BindingFlags.Instance | BindingFlags.NonPublic)
            .GetParameters()[0];
        metadataProvider
            .ForParameter(parameter)
            .BindingDetails(b => b.IsBindingRequired = true);
        bindingContext.ModelMetadata = metadataProvider.GetMetadataForParameter(parameter);

        bindingContext.ValueProvider = new TestValueProvider(new Dictionary<string, object>());

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.Empty(Assert.IsType<List<string>>(bindingContext.Result.Model));
        Assert.True(bindingContext.Result.IsModelSet);

        var keyValuePair = Assert.Single(bindingContext.ModelState);
        Assert.Equal("modelName", keyValuePair.Key);
        var error = Assert.Single(keyValuePair.Value.Errors);
        Assert.Equal("A value for the 'fieldName' parameter or property was not provided.", error.ErrorMessage);
    }

    // Setup like CollectionModelBinder_CreatesEmptyCollection_IfIsTopLevelObject except
    // Model has a default value.
    [Fact]
    public async Task CollectionModelBinder_DoesNotCreateEmptyCollection_IfModelHasDefaultValue()
    {
        // Arrange
        var binder = new CollectionModelBinder<string>(
            new StubModelBinder(result: ModelBindingResult.Failed()),
            NullLoggerFactory.Instance,
            allowValidatingTopLevelNodes: true);

        var bindingContext = CreateContext();
        bindingContext.IsTopLevelObject = true;

        // Lack of prefix and non-empty model name both ignored.
        bindingContext.ModelName = "modelName";

        var metadataProvider = new TestModelMetadataProvider();
        var parameter = typeof(CollectionModelBinderTest)
            .GetMethod(nameof(ActionWithListParameterDefaultValue), BindingFlags.Instance | BindingFlags.NonPublic)
            .GetParameters()[0];
        metadataProvider
            .ForParameter(parameter)
            .BindingDetails(b => b.IsBindingRequired = false);
        bindingContext.ModelMetadata = metadataProvider.GetMetadataForParameter(parameter);

        bindingContext.ValueProvider = new TestValueProvider(new Dictionary<string, object>());

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.Null(bindingContext.Result.Model);
        Assert.False(bindingContext.Result.IsModelSet);
        Assert.Equal(0, bindingContext.ModelState.ErrorCount);
    }

    // Setup like CollectionModelBinder_CreatesEmptyCollection_IfIsTopLevelObject  except
    // Model already has a value.
    [Fact]
    public async Task CollectionModelBinder_DoesNotCreateEmptyCollection_IfModelNonNull()
    {
        // Arrange
        var binder = new CollectionModelBinder<string>(
            new StubModelBinder(result: ModelBindingResult.Failed()),
            NullLoggerFactory.Instance);

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
    [InlineData("", false, false)]
    [InlineData("", true, false)]
    [InlineData("", false, true)]
    [InlineData("", true, true)]
    [InlineData("param", false, false)]
    [InlineData("param", true, false)]
    [InlineData("param", false, true)]
    [InlineData("param", true, true)]
    public async Task CollectionModelBinder_DoesNotCreateCollection_IfNotIsTopLevelObject(
        string prefix,
        bool allowValidatingTopLevelNodes,
        bool isBindingRequired)
    {
        // Arrange
        var binder = new CollectionModelBinder<string>(
            new StubModelBinder(result: ModelBindingResult.Failed()),
            NullLoggerFactory.Instance,
            allowValidatingTopLevelNodes);

        var bindingContext = CreateContext();
        bindingContext.ModelName = ModelNames.CreatePropertyModelName(prefix, "ListProperty");

        var metadataProvider = new TestModelMetadataProvider();
        metadataProvider
            .ForProperty(typeof(ModelWithListProperty), nameof(ModelWithListProperty.ListProperty))
            .BindingDetails(b => b.IsBindingRequired = isBindingRequired);
        bindingContext.ModelMetadata = metadataProvider.GetMetadataForProperty(
            typeof(ModelWithListProperty),
            nameof(ModelWithListProperty.ListProperty));

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
        var binder = new CollectionModelBinder<int>(CreateIntBinder(), NullLoggerFactory.Instance);

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

        var modelBinder = new CollectionModelBinder<int>(elementBinder, NullLoggerFactory.Instance);

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

        var bindingContext = CreateContext();
        bindingContext.FieldName = "testfieldname";
        bindingContext.ModelName = "someName";
        bindingContext.ModelMetadata = metadata;
        bindingContext.ValueProvider = valueProvider;

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
