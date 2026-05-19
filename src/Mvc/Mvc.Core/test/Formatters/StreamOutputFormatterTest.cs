// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.Formatters;

public class StreamOutputFormatterTest
{
    [Theory]
    [InlineData(typeof(Stream), "text/plain")]
    [InlineData(typeof(Stream), null)]
    public void CanWriteResult_ReturnsTrue_ForStreams(Type type, string contentType)
    {
        // Arrange
        var formatter = new StreamOutputFormatter();
        var contentTypeHeader = new StringSegment(contentType);

        var context = new OutputFormatterWriteContext(
            new DefaultHttpContext(),
            new TestHttpResponseStreamWriterFactory().CreateWriter,
            type,
            new MemoryStream())
        {
            ContentType = contentTypeHeader,
        };

        // Act
        var canWrite = formatter.CanWriteResult(context);

        // Assert
        Assert.True(canWrite);
    }

    [Theory]
    [InlineData(typeof(SimplePOCO), "text/plain")]
    [InlineData(typeof(SimplePOCO), null)]
    public void CanWriteResult_OnlyActsOnStreams_IgnoringContentType(Type type, string contentType)
    {
        // Arrange
        var formatter = new StreamOutputFormatter();
        var contentTypeHeader = contentType == null ? new StringSegment() : new StringSegment(contentType);

        var context = new OutputFormatterWriteContext(
            new DefaultHttpContext(),
            new TestHttpResponseStreamWriterFactory().CreateWriter,
            type,
            new SimplePOCO())
        {
            ContentType = contentTypeHeader,
        };

        // Act
        var canWrite = formatter.CanWriteResult(context);

        // Assert
        Assert.False(canWrite);
    }

    [Theory]
    [InlineData(typeof(object))]
    [InlineData(typeof(SimplePOCO))]
    [InlineData(null)]
    public void CanWriteResult_OnlyActsOnStreams(Type type)
    {
        // Arrange
        var formatter = new StreamOutputFormatter();
        var @object = type != null ? Activator.CreateInstance(type) : null;

        var context = new OutputFormatterWriteContext(
            new DefaultHttpContext(),
            new TestHttpResponseStreamWriterFactory().CreateWriter,
            type,
            @object);

        // Act
        var result = formatter.CanWriteResult(context);

        // Assert
        Assert.False(result);
    }

    private class SimplePOCO
    {
        public int Id { get; set; }
    }
}
