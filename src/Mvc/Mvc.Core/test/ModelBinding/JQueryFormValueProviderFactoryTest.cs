// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using Moq;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Test;

public class JQueryFormValueProviderFactoryTest
{
    private static readonly Dictionary<string, StringValues> _backingStore = new Dictionary<string, StringValues>
        {
            { "[]", new[] { "found" } },
            { "[]property1", new[] { "found" } },
            { "property2[]", new[] { "found" } },
            { "[]property3[]", new[] { "found" } },
            { "property[]Value", new[] { "found" } },
            { "[10]", new[] { "found" } },
            { "[11]property", new[] { "found" } },
            { "property4[10]", new[] { "found" } },
            { "[12]property[][13]", new[] { "found" } },
            { "[14][]property1[15]property2", new[] { "found" } },
            { "prefix[11]property1", new[] { "found" } },
            { "prefix[12][][property2]", new[] { "found" } },
            { "prefix[property1][13]", new[] { "found" } },
            { "prefix[14][][15]", new[] { "found" } },
            { "[property5][]", new[] { "found" } },
            { "[][property6]Value", new[] { "found" } },
            { "prefix[property2]", new[] { "found" } },
            { "prefix[][property]Value", new[] { "found" } },
            { "[property7][property8]", new[] { "found" } },
            { "[property9][][property10]Value", new[] { "found" } },
        };

    [Fact]
    public async Task GetValueProvider_ReturnsNull_WhenContentTypeIsNotFormUrlEncoded()
    {
        // Arrange
        var context = CreateContext("some-content-type", formValues: null);
        var factory = new JQueryFormValueProviderFactory();

        // Act
        await factory.CreateValueProviderAsync(context);

        // Assert
        Assert.Empty(context.ValueProviders);
    }

    [Theory]
    [InlineData("application/x-www-form-urlencoded")]
    [InlineData("application/x-www-form-urlencoded;charset=utf-8")]
    [InlineData("multipart/form-data; boundary=----WebKitFormBoundarymx2fSWqWSd0OxQqq")]
    [InlineData("multipart/form-data; boundary=----WebKitFormBoundarymx2fSWqWSd0OxQqq; charset=utf-8")]
    public async Task CreateValueProviderAsync_ReturnsValueProvider_WithCurrentCulture(string contentType)
    {
        // Arrange
        var context = CreateContext(contentType, formValues: null);
        var factory = new JQueryFormValueProviderFactory();

        // Act
        await factory.CreateValueProviderAsync(context);

        // Assert
        var valueProvider = Assert.IsType<JQueryFormValueProvider>(Assert.Single(context.ValueProviders));
        Assert.Equal(CultureInfo.CurrentCulture, valueProvider.Culture);
    }

    public static TheoryData<string> SuccessDataSet
    {
        get
        {
            return new TheoryData<string>
                {
                    string.Empty,
                    "property1",
                    "property2",
                    "property3",
                    "propertyValue",
                    "[10]",
                    "[11]property",
                    "property4[10]",
                    "[12]property[13]",
                    "[14]property1[15]property2",
                    "prefix.property1[13]",
                    "prefix[14][15]",
                    "property5",
                    "property6Value",
                    "prefix.property2",
                    "prefix.propertyValue",
                    "property7.property8",
                    "property9.property10Value",
                };
        }
    }

    [Theory]
    [MemberData(nameof(SuccessDataSet))]
    public async Task GetValueProvider_ReturnsValueProvider_ContainingExpectedKeys(string key)
    {
        // Arrange
        var context = CreateContext("application/x-www-form-urlencoded", formValues: _backingStore);
        var factory = new JQueryFormValueProviderFactory();

        // Act
        await factory.CreateValueProviderAsync(context);

        // Assert
        var valueProvider = Assert.Single(context.ValueProviders);
        var result = valueProvider.GetValue(key);
        Assert.Equal("found", (string)result);
    }

    [Fact]
    public async Task CreatesValueProvider_WithCurrentCulture()
    {
        // Arrange
        var context = CreateContext("application/x-www-form-urlencoded", formValues: _backingStore);
        var factory = new JQueryFormValueProviderFactory();

        // Act
        await factory.CreateValueProviderAsync(context);

        // Assert
        var valueProvider = Assert.Single(context.ValueProviders);
        var jqueryFormValueProvider = Assert.IsType<JQueryFormValueProvider>(valueProvider);
        Assert.Equal(CultureInfo.CurrentCulture, jqueryFormValueProvider.Culture);
    }

    [Fact]
    public async Task GetValueProviderAsync_ThrowsValueProviderException_IfReadingFormThrowsInvalidDataException()
    {
        // Arrange
        var exception = new InvalidDataException();
        var valueProviderContext = CreateThrowingContext(exception);

        var factory = new JQueryFormValueProviderFactory();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValueProviderException>(() => factory.CreateValueProviderAsync(valueProviderContext));
        Assert.Same(exception, ex.InnerException);
    }

    [Fact]
    public async Task GetValueProviderAsync_ThrowsValueProviderException_IfReadingFormThrowsInvalidOperationException()
    {
        // Arrange
        var exception = new IOException();
        var valueProviderContext = CreateThrowingContext(exception);

        var factory = new JQueryFormValueProviderFactory();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValueProviderException>(() => factory.CreateValueProviderAsync(valueProviderContext));
        Assert.Same(exception, ex.InnerException);
    }

    [Fact]
    public async Task GetValueProviderAsync_ThrowsOriginalException_IfReadingFormThrows()
    {
        // Arrange
        var exception = new TimeZoneNotFoundException();
        var valueProviderContext = CreateThrowingContext(exception);

        var factory = new JQueryFormValueProviderFactory();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<TimeZoneNotFoundException>(() => factory.CreateValueProviderAsync(valueProviderContext));
        Assert.Same(exception, ex);
    }

    private static ValueProviderFactoryContext CreateThrowingContext(Exception exception)
    {
        var context = new Mock<HttpContext>();
        context.Setup(c => c.Request.ContentType).Returns("application/x-www-form-urlencoded");
        context.Setup(c => c.Request.HasFormContentType).Returns(true);
        context.Setup(c => c.Request.ReadFormAsync(It.IsAny<CancellationToken>())).ThrowsAsync(exception);
        var actionContext = new ActionContext(context.Object, new RouteData(), new ActionDescriptor());
        var valueProviderContext = new ValueProviderFactoryContext(actionContext);
        return valueProviderContext;
    }

    private static ValueProviderFactoryContext CreateContext(string contentType, Dictionary<string, StringValues> formValues)
    {
        var context = new DefaultHttpContext();
        context.Request.ContentType = contentType;

        if (context.Request.HasFormContentType)
        {
            context.Request.Form = new FormCollection(formValues ?? new Dictionary<string, StringValues>());
        }

        var actionContext = new ActionContext(context, new RouteData(), new ActionDescriptor());

        return new ValueProviderFactoryContext(actionContext);
    }
}
