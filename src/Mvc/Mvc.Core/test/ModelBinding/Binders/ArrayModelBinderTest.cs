// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

public class ArrayModelBinderTest
{
    [Fact]
    public async Task BindModelAsync_ValueProviderContainPrefix_Succeeds()
    {
        // Arrange
        var valueProvider = new SimpleValueProvider
            {
                { "someName[0]", "42" },
                { "someName[1]", "84" },
            };

        var bindingContext = GetBindingContext(valueProvider);
        var metadataProvider = new TestModelMetadataProvider();
        bindingContext.ModelMetadata = metadataProvider.GetMetadataForProperty(
            typeof(ModelWithIntArrayProperty),
            nameof(ModelWithIntArrayProperty.ArrayProperty));

        var binder = new ArrayModelBinder<int>(
            new SimpleTypeModelBinder(typeof(int), NullLoggerFactory.Instance),
            NullLoggerFactory.Instance);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);

        var array = Assert.IsType<int[]>(bindingContext.Result.Model);
        Assert.Equal(new[] { 42, 84 }, array);
    }

    private IActionResult ActionWithArrayParameter(string[] parameter) => null;

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ArrayModelBinder_CreatesEmptyCollection_IfIsTopLevelObject(
        bool allowValidatingTopLevelNodes,
        bool isBindingRequired)
    {
        // Arrange
        var expectedErrorCount = isBindingRequired ? 1 : 0;
        var binder = new ArrayModelBinder<string>(
            new SimpleTypeModelBinder(typeof(string), NullLoggerFactory.Instance),
            NullLoggerFactory.Instance,
            allowValidatingTopLevelNodes);

        var bindingContext = CreateContext();
        bindingContext.IsTopLevelObject = true;

        // Lack of prefix and non-empty model name both ignored.
        bindingContext.ModelName = "modelName";

        var metadataProvider = new TestModelMetadataProvider();
        var parameter = typeof(ArrayModelBinderTest)
            .GetMethod(nameof(ActionWithArrayParameter), BindingFlags.Instance | BindingFlags.NonPublic)
            .GetParameters()[0];
        metadataProvider
            .ForParameter(parameter)
            .BindingDetails(b => b.IsBindingRequired = isBindingRequired);
        bindingContext.ModelMetadata = metadataProvider.GetMetadataForParameter(parameter);

        bindingContext.ValueProvider = new TestValueProvider(new Dictionary<string, object>());

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.Empty(Assert.IsType<string[]>(bindingContext.Result.Model));
        Assert.True(bindingContext.Result.IsModelSet);
        Assert.Equal(expectedErrorCount, bindingContext.ModelState.ErrorCount);
    }

    [Fact]
    public async Task ArrayModelBinder_CreatesEmptyCollectionAndAddsError_IfIsTopLevelObject()
    {
        // Arrange
        var binder = new ArrayModelBinder<string>(
            new SimpleTypeModelBinder(typeof(string), NullLoggerFactory.Instance),
            NullLoggerFactory.Instance,
            allowValidatingTopLevelNodes: true);

        var bindingContext = CreateContext();
        bindingContext.IsTopLevelObject = true;
        bindingContext.FieldName = "fieldName";
        bindingContext.ModelName = "modelName";

        var metadataProvider = new TestModelMetadataProvider();
        var parameter = typeof(ArrayModelBinderTest)
            .GetMethod(nameof(ActionWithArrayParameter), BindingFlags.Instance | BindingFlags.NonPublic)
            .GetParameters()[0];
        metadataProvider
            .ForParameter(parameter)
            .BindingDetails(b => b.IsBindingRequired = true);
        bindingContext.ModelMetadata = metadataProvider.GetMetadataForParameter(parameter);

        bindingContext.ValueProvider = new TestValueProvider(new Dictionary<string, object>());

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.Empty(Assert.IsType<string[]>(bindingContext.Result.Model));
        Assert.True(bindingContext.Result.IsModelSet);

        var keyValuePair = Assert.Single(bindingContext.ModelState);
        Assert.Equal("modelName", keyValuePair.Key);
        var error = Assert.Single(keyValuePair.Value.Errors);
        Assert.Equal("A value for the 'fieldName' parameter or property was not provided.", error.ErrorMessage);
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
    public async Task ArrayModelBinder_DoesNotCreateCollection_IfNotIsTopLevelObject(
        string prefix,
        bool allowValidatingTopLevelNodes,
        bool isBindingRequired)
    {
        // Arrange
        var binder = new ArrayModelBinder<string>(
            new SimpleTypeModelBinder(typeof(string), NullLoggerFactory.Instance),
            NullLoggerFactory.Instance,
            allowValidatingTopLevelNodes);

        var bindingContext = CreateContext();
        bindingContext.ModelName = ModelNames.CreatePropertyModelName(prefix, "ArrayProperty");

        var metadataProvider = new TestModelMetadataProvider();
        metadataProvider
            .ForProperty(typeof(ModelWithArrayProperty), nameof(ModelWithArrayProperty.ArrayProperty))
            .BindingDetails(b => b.IsBindingRequired = isBindingRequired);
        bindingContext.ModelMetadata = metadataProvider.GetMetadataForProperty(
            typeof(ModelWithArrayProperty),
            nameof(ModelWithArrayProperty.ArrayProperty));

        bindingContext.ValueProvider = new TestValueProvider(new Dictionary<string, object>());

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.False(bindingContext.Result.IsModelSet);
        Assert.Equal(0, bindingContext.ModelState.ErrorCount);
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

    // Here "fails silently" means the call does not update the array but also does not throw or set an error.
    [Theory]
    [MemberData(nameof(ArrayModelData))]
    public async Task BindModelAsync_ModelMetadataNotReadOnly_ModelNonNull_FailsSilently(int[] model)
    {
        // Arrange
        var arrayLength = model.Length;
        var valueProvider = new SimpleValueProvider
            {
                { "someName[0]", "42" },
                { "someName[1]", "84" },
            };

        var bindingContext = GetBindingContext(valueProvider);
        bindingContext.Model = model;

        var metadataProvider = new TestModelMetadataProvider();
        metadataProvider.ForProperty(
            typeof(ModelWithIntArrayProperty),
            nameof(ModelWithIntArrayProperty.ArrayProperty)).BindingDetails(bd => bd.IsReadOnly = false);
        bindingContext.ModelMetadata = metadataProvider.GetMetadataForProperty(
            typeof(ModelWithIntArrayProperty),
            nameof(ModelWithIntArrayProperty.ArrayProperty));

        var binder = new ArrayModelBinder<int>(
            new SimpleTypeModelBinder(typeof(int), NullLoggerFactory.Instance),
            NullLoggerFactory.Instance);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        Assert.Same(model, bindingContext.Result.Model);

        for (var i = 0; i < arrayLength; i++)
        {
            // Array should be unchanged.
            Assert.Equal(357, model[i]);
        }
    }

    private static DefaultModelBindingContext GetBindingContext(IValueProvider valueProvider)
    {
        var bindingContext = CreateContext();
        bindingContext.ModelName = "someName";
        bindingContext.ValueProvider = valueProvider;

        return bindingContext;
    }

    private static DefaultModelBindingContext CreateContext()
    {
        var actionContext = new ActionContext
        {
            HttpContext = new DefaultHttpContext(),
        };
        var modelBindingContext = new DefaultModelBindingContext
        {
            ActionContext = actionContext,
            ModelState = actionContext.ModelState,
        };

        return modelBindingContext;
    }

    private class ModelWithArrayProperty
    {
        public string[] ArrayProperty { get; set; }
    }

    private class ModelWithIntArrayProperty
    {
        public int[] ArrayProperty { get; set; }
    }
}
