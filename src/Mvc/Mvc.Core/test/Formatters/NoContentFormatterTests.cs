// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.Formatters;

public class NoContentFormatterTests
{
    public static IEnumerable<object[]> OutputFormatterContextValues_CanWriteType
    {
        get
        {
            // object value, bool useDeclaredTypeAsString, bool expectedCanWriteResult, bool useNonNullContentType
            yield return new object[] { "valid value", true, false, true };
            yield return new object[] { "valid value", false, false, true };
            yield return new object[] { "", false, false, true };
            yield return new object[] { "", true, false, true };
            yield return new object[] { null, true, true, true };
            yield return new object[] { null, false, true, true };
            yield return new object[] { null, false, true, false };
            yield return new object[] { new object(), false, false, true };
            yield return new object[] { 1232, false, false, true };
            yield return new object[] { 1232, false, false, false };
        }
    }

    [Theory]
    [MemberData(nameof(OutputFormatterContextValues_CanWriteType))]
    public void CanWriteResult_ByDefault_ReturnsTrue_IfTheValueIsNull(
        object value,
        bool declaredTypeAsString,
        bool expected,
        bool useNonNullContentType)
    {
        // Arrange
        var type = declaredTypeAsString ? typeof(string) : typeof(object);
        var contentType = useNonNullContentType ? new StringSegment("text/plain") : new StringSegment();

        var context = new OutputFormatterWriteContext(
            new DefaultHttpContext(),
            new TestHttpResponseStreamWriterFactory().CreateWriter,
            type,
            value)
        {
            ContentType = contentType,
        };

        var formatter = new HttpNoContentOutputFormatter();

        // Act
        var result = formatter.CanWriteResult(context);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(typeof(void))]
    [InlineData(typeof(Task))]
    public void CanWriteResult_ReturnsTrue_IfReturnTypeIsVoidOrTask(Type declaredType)
    {
        // Arrange
        var context = new OutputFormatterWriteContext(
            new DefaultHttpContext(),
            new TestHttpResponseStreamWriterFactory().CreateWriter,
            declaredType,
            "Something non null.")
        {
            ContentType = new StringSegment("text/plain"),
        };

        var formatter = new HttpNoContentOutputFormatter();

        // Act
        var result = formatter.CanWriteResult(context);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(null, true, true)]
    [InlineData(null, false, false)]
    [InlineData("some value", true, false)]
    public void CanWriteResult_ReturnsTrue_IfReturnValueIsNullAndTreatNullValueAsNoContentIsNotSet(
        string value,
        bool treatNullValueAsNoContent,
        bool expected)
    {
        // Arrange
        var context = new OutputFormatterWriteContext(
            new DefaultHttpContext(),
            new TestHttpResponseStreamWriterFactory().CreateWriter,
            typeof(string),
            value)
        {
            ContentType = new StringSegment("text/plain"),
        };

        var formatter = new HttpNoContentOutputFormatter()
        {
            TreatNullValueAsNoContent = treatNullValueAsNoContent
        };

        // Act
        var result = formatter.CanWriteResult(context);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task WriteAsync_WritesTheStatusCode204()
    {
        // Arrange
        var context = new OutputFormatterWriteContext(
            new DefaultHttpContext(),
            new TestHttpResponseStreamWriterFactory().CreateWriter,
            typeof(string),
            @object: null);

        var formatter = new HttpNoContentOutputFormatter();

        // Act
        await formatter.WriteAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status204NoContent, context.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task WriteAsync_DoesNotHaveContentLengthSet()
    {
        // Arrange
        var context = new OutputFormatterWriteContext(
            new DefaultHttpContext(),
            new TestHttpResponseStreamWriterFactory().CreateWriter,
            typeof(string),
            @object: null);

        var formatter = new HttpNoContentOutputFormatter();

        // Act
        await formatter.WriteAsync(context);

        // Assert
        // No Content responses shouldn't have a Content-Length.
        Assert.Null(context.HttpContext.Response.ContentLength);
    }

    [Fact]
    public async Task WriteAsync_ContextStatusCodeSet_WritesSameStatusCode()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Response.StatusCode = StatusCodes.Status201Created;

        var context = new OutputFormatterWriteContext(
            httpContext,
            new TestHttpResponseStreamWriterFactory().CreateWriter,
            typeof(string),
            @object: null);

        var formatter = new HttpNoContentOutputFormatter();

        // Act
        await formatter.WriteAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status201Created, httpContext.Response.StatusCode);
    }
}
