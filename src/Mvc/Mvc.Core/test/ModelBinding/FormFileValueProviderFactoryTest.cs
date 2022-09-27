// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using Moq;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

public class FormFileValueProviderFactoryTest
{
    [Fact]
    public async Task CreateValueProviderAsync_DoesNotAddValueProvider_IfRequestDoesNotHaveFormContent()
    {
        // Arrange
        var factory = new FormFileValueProviderFactory();
        var context = CreateContext("application/json");

        // Act
        await factory.CreateValueProviderAsync(context);

        // Assert
        Assert.Empty(context.ValueProviders);
    }

    [Fact]
    public async Task CreateValueProviderAsync_DoesNotAddValueProvider_IfFileCollectionIsEmpty()
    {
        // Arrange
        var factory = new FormFileValueProviderFactory();
        var context = CreateContext("multipart/form-data");

        // Act
        await factory.CreateValueProviderAsync(context);

        // Assert
        Assert.Empty(context.ValueProviders);
    }

    [Fact]
    public async Task CreateValueProviderAsync_AddsValueProvider()
    {
        // Arrange
        var factory = new FormFileValueProviderFactory();
        var context = CreateContext("multipart/form-data; boundary=----WebKitFormBoundarymx2fSWqWSd0OxQqq");
        var files = (FormFileCollection)context.ActionContext.HttpContext.Request.Form.Files;
        files.Add(new FormFile(Stream.Null, 0, 10, "some-name", "some-name"));

        // Act
        await factory.CreateValueProviderAsync(context);

        // Assert
        Assert.Collection(
            context.ValueProviders,
            v => Assert.IsType<FormFileValueProvider>(v));
    }

    [Fact]
    public async Task GetValueProviderAsync_ThrowsValueProviderException_IfReadingFormThrowsInvalidDataException()
    {
        // Arrange
        var exception = new InvalidDataException();
        var valueProviderContext = CreateThrowingContext(exception);

        var factory = new FormFileValueProviderFactory();

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

        var factory = new FormFileValueProviderFactory();

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

        var factory = new FormFileValueProviderFactory();

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
        context.Request.Form = new FormCollection(new Dictionary<string, StringValues>(), new FormFileCollection());
        var actionContext = new ActionContext(context, new RouteData(), new ActionDescriptor());

        return new ValueProviderFactoryContext(actionContext);
    }
}
