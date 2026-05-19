// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc.Formatters;

public class OutputFormatterTests
{
    [Fact]
    public void CanWriteResult_ForNullContentType_UsesFirstEntryInSupportedContentTypes()
    {
        // Arrange
        var context = new OutputFormatterWriteContext(
            new DefaultHttpContext(),
            new TestHttpResponseStreamWriterFactory().CreateWriter,
            objectType: null,
            @object: null);

        var formatter = new TestOutputFormatter();

        // Act
        var result = formatter.CanWriteResult(context);

        // Assert
        Assert.True(result);
        Assert.Equal(formatter.SupportedMediaTypes[0].ToString(), context.ContentType.ToString());
    }

    [Fact]
    public void GetSupportedContentTypes_ReturnsNull_ForUnsupportedType()
    {
        // Arrange
        var formatter = new TypeSpecificFormatter();
        formatter.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/json"));
        formatter.SupportedTypes.Add(typeof(int));

        // Act
        var contentTypes = formatter.GetSupportedContentTypes(
            contentType: null,
            objectType: typeof(string));

        // Assert
        Assert.Null(contentTypes);
    }

    [Fact]
    public void CanWrite_ReturnsFalse_ForUnsupportedType()
    {
        // Arrange
        var formatter = new TypeSpecificFormatter();
        formatter.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/json"));
        formatter.SupportedTypes.Add(typeof(int));

        var context = new OutputFormatterWriteContext(
            new DefaultHttpContext(),
            new TestHttpResponseStreamWriterFactory().CreateWriter,
            typeof(string),
            "Hello, world!")
        {
            ContentType = new StringSegment(formatter.SupportedMediaTypes[0].ToString()),
        };

        // Act
        var result = formatter.CanWriteResult(context);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void CanWriteResult_MatchesWildcardsOnlyWhenContentTypeProvidedByServer(
        bool contentTypeProvidedByServer, bool shouldMatchWildcards)
    {
        // Arrange
        var formatter = new TypeSpecificFormatter();
        formatter.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/*+xml"));
        formatter.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/*+json"));
        formatter.SupportedTypes.Add(typeof(string));

        var requestedContentType = "application/vnd.test.entity+json;v=2";
        var context = new OutputFormatterWriteContext(
            new DefaultHttpContext(),
            new TestHttpResponseStreamWriterFactory().CreateWriter,
            typeof(string),
            "Hello, world!")
        {
            ContentType = new StringSegment(requestedContentType),
            ContentTypeIsServerDefined = contentTypeProvidedByServer,
        };

        // Act
        var result = formatter.CanWriteResult(context);

        // Assert
        Assert.Equal(shouldMatchWildcards, result);
        Assert.Equal(requestedContentType, context.ContentType.ToString());
    }

    [Fact]
    public void GetSupportedContentTypes_ReturnsAllNonWildcardContentTypes_WithContentTypeNull()
    {
        // Arrange
        var formatter = new TestOutputFormatter();
        formatter.SupportedMediaTypes.Clear();
        formatter.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/json"));
        formatter.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("*/*"));
        formatter.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/*"));
        formatter.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/plain;*"));
        formatter.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/xml"));

        // Act
        var contentTypes = formatter.GetSupportedContentTypes(
            contentType: null,
            objectType: typeof(string));

        // Assert
        Assert.Equal(2, contentTypes.Count);
        Assert.Single(contentTypes, ct => ct.ToString() == "application/json");
        Assert.Single(contentTypes, ct => ct.ToString() == "application/xml");
    }

    [Fact]
    public void GetSupportedContentTypes_ReturnsMoreSpecificMatchingContentTypes_WithContentType()
    {
        // Arrange
        var formatter = new TestOutputFormatter();

        formatter.SupportedMediaTypes.Clear();
        formatter.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/json"));
        formatter.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/xml"));

        // Act
        var contentTypes = formatter.GetSupportedContentTypes(
            "application/*",
            typeof(int));

        // Assert
        var contentType = Assert.Single(contentTypes);
        Assert.Equal("application/json", contentType.ToString());
    }

    [Fact]
    public void GetSupportedContentTypes_ReturnsMatchingWildcardContentTypes_WithContentType()
    {
        // Arrange
        var formatter = new TestOutputFormatter();

        formatter.SupportedMediaTypes.Clear();
        formatter.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/json"));
        formatter.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/*+json"));
        formatter.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/xml"));

        // Act
        var contentTypes = formatter.GetSupportedContentTypes(
            "application/vnd.test+json;v=2",
            typeof(int));

        // Assert
        var contentType = Assert.Single(contentTypes);
        Assert.Equal("application/vnd.test+json;v=2", contentType.ToString());
    }

    [Fact]
    public void GetSupportedContentTypes_ReturnsMatchingContentTypes_NoMatches()
    {
        // Arrange
        var formatter = new TestOutputFormatter();

        formatter.SupportedMediaTypes.Clear();
        formatter.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/*+xml"));
        formatter.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/xml"));

        // Act
        var contentTypes = formatter.GetSupportedContentTypes(
            "application/vnd.test+bson",
            typeof(int));

        // Assert
        Assert.Null(contentTypes);
    }

    private class TypeSpecificFormatter : OutputFormatter
    {
        public List<Type> SupportedTypes { get; } = new List<Type>();

        protected override bool CanWriteType(Type type)
        {
            return SupportedTypes.Contains(type);
        }

        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
        {
            throw new NotImplementedException();
        }
    }

    [Fact]
    public void CanWrite_ThrowsInvalidOperationException_IfMediaTypesListIsEmpty()
    {
        // Arrange
        var formatter = new TestOutputFormatter();
        formatter.SupportedMediaTypes.Clear();

        var context = new OutputFormatterWriteContext(
            new DefaultHttpContext(),
            (s, e) => new StreamWriter(s, e),
            typeof(object),
            new object());

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => formatter.CanWriteResult(context));
    }

    [Fact]
    public void GetSupportedContentTypes_ThrowsInvalidOperationException_IfMediaTypesListIsEmpty()
    {
        // Arrange
        var formatter = new TestOutputFormatter();
        formatter.SupportedMediaTypes.Clear();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(
            () => formatter.GetSupportedContentTypes("application/json", typeof(object)));
    }

    private class TestOutputFormatter : OutputFormatter
    {
        public TestOutputFormatter()
        {
            SupportedMediaTypes.Add("application/acceptCharset");
        }

        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
        {
            return Task.FromResult(true);
        }
    }
}
