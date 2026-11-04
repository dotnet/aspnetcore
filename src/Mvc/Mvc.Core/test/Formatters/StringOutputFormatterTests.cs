// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Formatters;

public class StringOutputFormatterTests
{
    public static IEnumerable<object[]> CanWriteStringsData
    {
        get
        {
            // object value, bool useDeclaredTypeAsString
            yield return new object[] { "declared and runtime type are same", true };
            yield return new object[] { "declared and runtime type are different", false };
            yield return new object[] { null, true };
        }
    }

    public static TheoryData<object> CannotWriteNonStringsData
    {
        get
        {
            return new TheoryData<object>()
                {
                    null,
                    new object()
                };
        }
    }

    [Theory]
    [InlineData("application/json")]
    [InlineData("application/xml")]
    public void CannotWriteUnsupportedMediaType(string contentType)
    {
        // Arrange
        var formatter = new StringOutputFormatter();
        var expectedContentType = new StringSegment(contentType);

        var context = new OutputFormatterWriteContext(
            new DefaultHttpContext(),
            new TestHttpResponseStreamWriterFactory().CreateWriter,
            typeof(string),
            "Thisisastring");
        context.ContentType = new StringSegment(contentType);

        // Act
        var result = formatter.CanWriteResult(context);

        // Assert
        Assert.False(result);
        Assert.Equal(expectedContentType, context.ContentType);
    }

    [Fact]
    public void CanWriteResult_DefaultContentType()
    {
        // Arrange
        var formatter = new StringOutputFormatter();
        var context = new OutputFormatterWriteContext(
            new DefaultHttpContext(),
            new TestHttpResponseStreamWriterFactory().CreateWriter,
            typeof(string),
            "Thisisastring");

        // Act
        var result = formatter.CanWriteResult(context);

        // Assert
        Assert.True(result);
        Assert.Equal(new StringSegment("text/plain"), context.ContentType);
    }

    [Theory]
    [MemberData(nameof(CanWriteStringsData))]
    public void CanWriteStrings(
        object value,
        bool useDeclaredTypeAsString)
    {
        // Arrange
        var expectedContentType = new StringSegment("text/plain");

        var formatter = new StringOutputFormatter();
        var type = useDeclaredTypeAsString ? typeof(string) : typeof(object);

        var context = new OutputFormatterWriteContext(
            new DefaultHttpContext(),
            new TestHttpResponseStreamWriterFactory().CreateWriter,
            type,
            value);
        context.ContentType = new StringSegment("text/plain");

        // Act
        var result = formatter.CanWriteResult(context);

        // Assert
        Assert.True(result);
        Assert.Equal(expectedContentType, context.ContentType);
    }

    [Theory]
    [MemberData(nameof(CannotWriteNonStringsData))]
    public void CannotWriteNonStrings(object value)
    {
        // Arrange
        var expectedContentType = new StringSegment("text/plain");
        var formatter = new StringOutputFormatter();
        var context = new OutputFormatterWriteContext(
            new DefaultHttpContext(),
            new TestHttpResponseStreamWriterFactory().CreateWriter,
            typeof(object),
            value);
        context.ContentType = new StringSegment("text/plain");

        // Act
        var result = formatter.CanWriteResult(context);

        // Assert
        Assert.False(result);
        Assert.Equal(expectedContentType, context.ContentType);
    }

    [Fact]
    public async Task WriteAsync_DoesNotWriteNullStrings()
    {
        // Arrange
        Encoding encoding = Encoding.UTF8;
        var memoryStream = new MemoryStream();
        var response = new Mock<HttpResponse>();
        response.SetupProperty(o => o.ContentLength);
        response.SetupGet(r => r.Body).Returns(memoryStream);
        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(o => o.Response).Returns(response.Object);

        var formatter = new StringOutputFormatter();
        var context = new OutputFormatterWriteContext(
            httpContext.Object,
            new TestHttpResponseStreamWriterFactory().CreateWriter,
            typeof(string),
            @object: null);

        // Act
        await formatter.WriteResponseBodyAsync(context, encoding);

        // Assert
        Assert.Equal(0, memoryStream.Length);
        response.VerifySet(r => r.ContentLength = It.IsAny<long?>(), Times.Never());
    }
}
