// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

public class HeaderModelBinderTests
{
    [Fact]
    public async Task HeaderBinder_BindsHeaders_ToStringCollection_WithoutInnerModelBinder()
    {
        // Arrange
        var binder = new HeaderModelBinder(NullLoggerFactory.Instance);
        var type = typeof(string[]);
        var header = "Accept";
        var headerValue = "application/json,text/json";

        var bindingContext = CreateContext(type);

        bindingContext.FieldName = header;
        bindingContext.HttpContext.Request.Headers.Add(header, new[] { headerValue });

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        Assert.Equal(headerValue.Split(','), bindingContext.Result.Model);
    }

    [Fact]
    public async Task HeaderBinder_BindsHeaders_ToStringType_WithoutInnerModelBinder()
    {
        // Arrange
        var type = typeof(string);
        var header = "User-Agent";
        var headerValue = "UnitTest";
        var bindingContext = CreateContext(type);

        var binder = new HeaderModelBinder(NullLoggerFactory.Instance);

        bindingContext.FieldName = header;
        bindingContext.HttpContext.Request.Headers.Add(header, new[] { headerValue });

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        Assert.Equal(headerValue, bindingContext.Result.Model);
    }

    [Theory]
    [InlineData(typeof(IEnumerable<string>))]
    [InlineData(typeof(ICollection<string>))]
    [InlineData(typeof(IList<string>))]
    [InlineData(typeof(List<string>))]
    [InlineData(typeof(LinkedList<string>))]
    [InlineData(typeof(StringList))]
    public async Task HeaderBinder_BindsHeaders_ForCollectionsItCanCreate_WithoutInnerModelBinder(
        Type destinationType)
    {
        // Arrange
        var header = "Accept";
        var headerValue = "application/json,text/json";
        var binder = new HeaderModelBinder(NullLoggerFactory.Instance);
        var bindingContext = CreateContext(destinationType);

        bindingContext.FieldName = header;
        bindingContext.HttpContext.Request.Headers.Add(header, new[] { headerValue });

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        Assert.IsAssignableFrom(destinationType, bindingContext.Result.Model);
        Assert.Equal(headerValue.Split(','), bindingContext.Result.Model as IEnumerable<string>);
    }

    [Fact]
    public async Task HeaderBinder_BindsHeaders_ToStringCollection()
    {
        // Arrange
        var type = typeof(string[]);
        var headerValue = "application/json,text/json";
        var bindingContext = CreateContext(type);
        var binder = CreateBinder(bindingContext);
        bindingContext.HttpContext.Request.Headers.Add("Header", new[] { headerValue });

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        Assert.Equal(headerValue.Split(','), bindingContext.Result.Model);
    }

    public static TheoryData<string, Type, object> BinderHeaderToSimpleTypesData
    {
        get
        {
            var guid = new Guid("3916A5B1-5FE4-4E09-9812-5CDC127FA5B1");

            return new TheoryData<string, Type, object>()
                {
                    { "10", typeof(int), 10 },
                    { "10.50", typeof(double), 10.50 },
                    { "10.50", typeof(IEnumerable<double>), new List<double>() { 10.50 } },
                    { "Sedan", typeof(CarType), CarType.Sedan },
                    { "", typeof(CarType?), null },
                    { "", typeof(string[]), Array.Empty<string>() },
                    { null, typeof(string[]), Array.Empty<string>() },
                    { "", typeof(IEnumerable<string>), new List<string>() },
                    { null, typeof(IEnumerable<string>), new List<string>() },
                    { guid.ToString(), typeof(Guid), guid },
                    { "foo", typeof(string), "foo" },
                    { "foo, bar", typeof(string), "foo, bar" },
                    { "foo, bar", typeof(string[]), new[]{ "foo", "bar" } },
                    { "foo, \"bar\"", typeof(string[]), new[]{ "foo", "bar" } },
                    { "\"foo,bar\"", typeof(string[]), new[]{ "foo,bar" } }
                };
        }
    }

    [Theory]
    [MemberData(nameof(BinderHeaderToSimpleTypesData))]
    public async Task HeaderBinder_BindsHeaders_ToSimpleTypes(
        string headerValue,
        Type modelType,
        object expectedModel)
    {
        // Arrange
        var bindingContext = CreateContext(modelType);
        var binder = CreateBinder(bindingContext);

        if (headerValue != null)
        {
            bindingContext.HttpContext.Request.Headers.Add("Header", headerValue);
        }

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        Assert.Equal(expectedModel, bindingContext.Result.Model);
    }

    [Theory]
    [InlineData(typeof(CarType?))]
    [InlineData(typeof(int?))]
    public async Task HeaderBinder_DoesNotSetModel_ForHeaderNotPresentOnRequest(Type modelType)
    {
        // Arrange
        var bindingContext = CreateContext(modelType);
        var binder = CreateBinder(bindingContext);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.False(bindingContext.Result.IsModelSet);
        Assert.Null(bindingContext.Result.Model);
    }

    [Theory]
    [InlineData(typeof(string[]))]
    [InlineData(typeof(IEnumerable<string>))]
    public async Task HeaderBinder_DoesNotCreateEmptyCollection_ForNonTopLevelObjects(Type modelType)
    {
        // Arrange
        var bindingContext = CreateContext(modelType);
        bindingContext.IsTopLevelObject = false;
        var binder = CreateBinder(bindingContext);
        // No header on the request that the header value provider is looking for

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.False(bindingContext.Result.IsModelSet);
        Assert.Null(bindingContext.Result.Model);
    }

    [Theory]
    [InlineData(typeof(IEnumerable<string>))]
    [InlineData(typeof(ICollection<string>))]
    [InlineData(typeof(IList<string>))]
    [InlineData(typeof(List<string>))]
    [InlineData(typeof(LinkedList<string>))]
    [InlineData(typeof(StringList))]
    public async Task HeaderBinder_BindsHeaders_ForCollectionsItCanCreate(Type destinationType)
    {
        // Arrange
        var headerValue = "application/json,text/json";
        var bindingContext = CreateContext(destinationType);
        var binder = CreateBinder(bindingContext);
        bindingContext.HttpContext.Request.Headers.Add("Header", new[] { headerValue });

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        Assert.IsAssignableFrom(destinationType, bindingContext.Result.Model);
        Assert.Equal(headerValue.Split(','), bindingContext.Result.Model as IEnumerable<string>);
    }

    [Fact]
    public async Task HeaderBinder_ReturnsResult_ForReadOnlyDestination()
    {
        // Arrange
        var bindingContext = CreateContext(GetMetadataForReadOnlyArray());
        var binder = CreateBinder(bindingContext);
        bindingContext.HttpContext.Request.Headers.Add("Header", "application/json,text/json");

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        Assert.NotNull(bindingContext.Result.Model);
    }

    [Fact]
    public async Task HeaderBinder_ResetsTheBindingScope_GivingOriginalValueProvider()
    {
        // Arrange
        var expectedValueProvider = Mock.Of<IValueProvider>();
        var bindingContext = CreateContext(GetMetadataForType(typeof(string)), expectedValueProvider);
        var binder = CreateBinder(bindingContext);
        bindingContext.HttpContext.Request.Headers.Add("Header", "application/json,text/json");

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        Assert.Equal("application/json,text/json", bindingContext.Result.Model);
        Assert.Same(expectedValueProvider, bindingContext.ValueProvider);
    }

    [Fact]
    public async Task HeaderBinder_UsesValues_OnlyFromHeaderValueProvider()
    {
        // Arrange
        var testValueProvider = new Mock<IValueProvider>();
        testValueProvider
            .Setup(vp => vp.ContainsPrefix(It.IsAny<string>()))
            .Returns(true);
        testValueProvider
            .Setup(vp => vp.GetValue(It.IsAny<string>()))
            .Returns(new ValueProviderResult(new StringValues("foo,bar")));
        var bindingContext = CreateContext(GetMetadataForType(typeof(string)), testValueProvider.Object);
        var binder = CreateBinder(bindingContext);
        bindingContext.HttpContext.Request.Headers.Add("Header", "application/json,text/json");

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        Assert.Equal("application/json,text/json", bindingContext.Result.Model);
        Assert.Same(testValueProvider.Object, bindingContext.ValueProvider);
    }

    [Theory]
    [InlineData(typeof(int), "not-an-integer")]
    [InlineData(typeof(double), "not-an-double")]
    [InlineData(typeof(CarType?), "boo")]
    public async Task HeaderBinder_BindModelAsync_AddsErrorToModelState_OnInvalidInput(
        Type modelType,
        string headerValue)
    {
        // Arrange
        var bindingContext = CreateContext(modelType);
        var binder = CreateBinder(bindingContext);
        bindingContext.HttpContext.Request.Headers.Add("Header", headerValue);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        var entry = bindingContext.ModelState["someprefix.Header"];
        Assert.NotNull(entry);
        var error = Assert.Single(entry.Errors);
        Assert.Equal($"The value '{headerValue}' is not valid.", error.ErrorMessage);
    }

    [Theory]
    [InlineData(typeof(int[]), "a, b")]
    [InlineData(typeof(IEnumerable<double>), "a, b")]
    [InlineData(typeof(ICollection<CarType>), "a, b")]
    public async Task HeaderBinder_BindModelAsync_AddsErrorToModelState_OnInvalid_CollectionInput(
        Type modelType,
        string headerValue)
    {
        // Arrange
        var headerValues = headerValue.Split(',').Select(s => s.Trim()).ToArray();
        var bindingContext = CreateContext(modelType);
        var binder = CreateBinder(bindingContext);
        bindingContext.HttpContext.Request.Headers.Add("Header", headerValue);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        var entry = bindingContext.ModelState["someprefix.Header"];
        Assert.NotNull(entry);
        Assert.Equal(2, entry.Errors.Count);
        Assert.Equal($"The value '{headerValues[0]}' is not valid.", entry.Errors[0].ErrorMessage);
        Assert.Equal($"The value '{headerValues[1]}' is not valid.", entry.Errors[1].ErrorMessage);
    }

    private static DefaultModelBindingContext CreateContext(Type modelType)
    {
        return CreateContext(metadata: GetMetadataForType(modelType), valueProvider: null);
    }

    private static DefaultModelBindingContext CreateContext(
        ModelMetadata metadata,
        IValueProvider valueProvider = null)
    {
        if (valueProvider == null)
        {
            valueProvider = Mock.Of<IValueProvider>();
        }

        var options = new MvcOptions();
        var setup = new MvcCoreMvcOptionsSetup(new TestHttpRequestStreamReaderFactory());
        setup.Configure(options);

        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton(Options.Create(options));
        var serviceProvider = services.BuildServiceProvider();

        var headerName = "Header";

        return new DefaultModelBindingContext()
        {
            IsTopLevelObject = true,
            ModelMetadata = metadata,
            BinderModelName = metadata.BinderModelName,
            BindingSource = metadata.BindingSource,

            // HeaderModelBinder must always use the field name when getting the values from header value provider
            // but add keys into ModelState using the ModelName. This is for back compat reasons.
            ModelName = $"somePrefix.{headerName}",
            FieldName = headerName,

            ValueProvider = valueProvider,
            ModelState = new ModelStateDictionary(),
            ActionContext = new ActionContext()
            {
                HttpContext = new DefaultHttpContext()
                {
                    RequestServices = serviceProvider
                }
            },
        };
    }

    private static IModelBinder CreateBinder(DefaultModelBindingContext bindingContext)
    {
        var factory = TestModelBinderFactory.Create(bindingContext.HttpContext.RequestServices);
        var metadata = bindingContext.ModelMetadata;
        return factory.CreateBinder(new ModelBinderFactoryContext()
        {
            Metadata = metadata,
            BindingInfo = new BindingInfo()
            {
                BinderModelName = metadata.BinderModelName,
                BinderType = metadata.BinderType,
                BindingSource = metadata.BindingSource,
                PropertyFilterProvider = metadata.PropertyFilterProvider,
            },
        });
    }

    private static ModelMetadata GetMetadataForType(Type modelType)
    {
        var metadataProvider = new TestModelMetadataProvider();
        metadataProvider.ForType(modelType).BindingDetails(d => d.BindingSource = BindingSource.Header);
        return metadataProvider.GetMetadataForType(modelType);
    }

    private static ModelMetadata GetMetadataForReadOnlyArray()
    {
        var metadataProvider = new TestModelMetadataProvider();
        metadataProvider
            .ForProperty<ModelWithReadOnlyArray>(nameof(ModelWithReadOnlyArray.ArrayProperty))
            .BindingDetails(bd => bd.BindingSource = BindingSource.Header);
        return metadataProvider.GetMetadataForProperty(
            typeof(ModelWithReadOnlyArray),
            nameof(ModelWithReadOnlyArray.ArrayProperty));
    }

    private class ModelWithReadOnlyArray
    {
        public string[] ArrayProperty { get; }
    }

    private class StringList : List<string>
    {
    }

    private enum CarType
    {
        Sedan,
        Coupe
    }
}
