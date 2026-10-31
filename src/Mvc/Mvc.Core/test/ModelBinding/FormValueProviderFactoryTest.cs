// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using Moq;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Test;

public class FormValueProviderFactoryTest
{
    [Fact]
    public async Task GetValueProviderAsync_ReturnsNull_WhenContentTypeIsNotFormUrlEncoded()
    {
        // Arrange
        var context = CreateContext("some-content-type");
        var factory = new FormValueProviderFactory();

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
    public async Task GetValueProviderAsync_ReturnsValueProvider_WithCurrentCulture(string contentType)
    {
        // Arrange
        var context = CreateContext(contentType);
        var factory = new FormValueProviderFactory();

        // Act
        await factory.CreateValueProviderAsync(context);

        // Assert
        var valueProvider = Assert.IsType<FormValueProvider>(Assert.Single(context.ValueProviders));
        Assert.Equal(CultureInfo.CurrentCulture, valueProvider.Culture);
    }

    [Fact]
    public async Task GetValueProviderAsync_ThrowsValueProviderException_IfReadingFormThrowsInvalidDataException()
    {
        // Arrange
        var exception = new InvalidDataException();
        var valueProviderContext = CreateThrowingContext(exception);

        var factory = new FormValueProviderFactory();

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

        var factory = new FormValueProviderFactory();

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

        var factory = new FormValueProviderFactory();

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

    private static ValueProviderFactoryContext CreateContext(string contentType)
    {
        var context = new DefaultHttpContext();
        context.Request.ContentType = contentType;

        if (context.Request.HasFormContentType)
        {
            context.Request.Form = new FormCollection(new Dictionary<string, StringValues>());
        }

        var actionContext = new ActionContext(context, new RouteData(), new ActionDescriptor());

        return new ValueProviderFactoryContext(actionContext);
    }
}
