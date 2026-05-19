// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc.Formatters;

public class InputFormatterTest
{
    private class CatchAllFormatter : TestFormatter
    {
        public CatchAllFormatter()
        {
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("*/*"));
        }
    }

    [Theory]
    [InlineData("application/mathml-content+xml")]
    [InlineData("application/mathml-presentation+xml")]
    [InlineData("application/mathml+xml; undefined=ignored")]
    [InlineData("application/octet-stream; padding=3")]
    [InlineData("application/xml")]
    [InlineData("application/xml-dtd; undefined=ignored")]
    [InlineData("multipart/mixed; boundary=gc0p4Jq0M2Yt08j34c0p")]
    [InlineData("multipart/mixed; boundary=gc0p4Jq0M2Yt08j34c0p; undefined=ignored")]
    [InlineData("text/html")]
    public void CatchAll_CanRead_ReturnsTrueForSupportedMediaTypes(string requestContentType)
    {
        // Arrange
        var formatter = new CatchAllFormatter();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = requestContentType;

        var provider = new EmptyModelMetadataProvider();
        var metadata = provider.GetMetadataForType(typeof(void));
        var context = new InputFormatterContext(
            httpContext,
            modelName: string.Empty,
            modelState: new ModelStateDictionary(),
            metadata: metadata,
            readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader);

        // Act
        var result = formatter.CanRead(context);

        // Assert
        Assert.True(result);
    }

    private class MultipartFormatter : TestFormatter
    {
        public MultipartFormatter()
        {
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("multipart/*"));
        }
    }

    [Theory]
    [InlineData("multipart/mixed; boundary=gc0p4Jq0M2Yt08j34c0p")]
    [InlineData("multipart/mixed; boundary=gc0p4Jq0M2Yt08j34c0p; undefined=ignored")]
    public void MultipartFormatter_CanRead_ReturnsTrueForSupportedMediaTypes(string requestContentType)
    {
        // Arrange
        var formatter = new MultipartFormatter();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = requestContentType;

        var provider = new EmptyModelMetadataProvider();
        var metadata = provider.GetMetadataForType(typeof(void));
        var context = new InputFormatterContext(
            httpContext,
            modelName: string.Empty,
            modelState: new ModelStateDictionary(),
            metadata: metadata,
            readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader);

        // Act
        var result = formatter.CanRead(context);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("application/mathml-content+xml")]
    [InlineData("application/mathml-presentation+xml")]
    [InlineData("application/mathml+xml; undefined=ignored")]
    [InlineData("application/octet-stream; padding=3")]
    [InlineData("application/xml")]
    [InlineData("application/xml-dtd; undefined=ignored")]
    [InlineData("text/html")]
    public void MultipartFormatter_CanRead_ReturnsFalseForUnsupportedMediaTypes(string requestContentType)
    {
        // Arrange
        var formatter = new MultipartFormatter();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = requestContentType;

        var provider = new EmptyModelMetadataProvider();
        var metadata = provider.GetMetadataForType(typeof(void));
        var context = new InputFormatterContext(
            httpContext,
            modelName: string.Empty,
            modelState: new ModelStateDictionary(),
            metadata: metadata,
            readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader);

        // Act
        var result = formatter.CanRead(context);

        // Assert
        Assert.False(result);
    }

    private class MultipartMixedFormatter : TestFormatter
    {
        public MultipartMixedFormatter()
        {
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("multipart/mixed"));
        }
    }

    [Theory]
    [InlineData("multipart/mixed; boundary=gc0p4Jq0M2Yt08j34c0p")]
    [InlineData("multipart/mixed; boundary=gc0p4Jq0M2Yt08j34c0p; undefined=ignored")]
    public void MultipartMixedFormatter_CanRead_ReturnsTrueForSupportedMediaTypes(string requestContentType)
    {
        // Arrange
        var formatter = new MultipartMixedFormatter();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = requestContentType;

        var provider = new EmptyModelMetadataProvider();
        var metadata = provider.GetMetadataForType(typeof(void));
        var context = new InputFormatterContext(
            httpContext,
            modelName: string.Empty,
            modelState: new ModelStateDictionary(),
            metadata: metadata,
            readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader);

        // Act
        var result = formatter.CanRead(context);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("application/mathml-content+xml")]
    [InlineData("application/mathml-presentation+xml")]
    [InlineData("application/mathml+xml; undefined=ignored")]
    [InlineData("application/octet-stream; padding=3")]
    [InlineData("application/xml")]
    [InlineData("application/xml-dtd; undefined=ignored")]
    [InlineData("text/html")]
    public void MultipartMixedFormatter_CanRead_ReturnsFalseForUnsupportedMediaTypes(string requestContentType)
    {
        // Arrange
        var formatter = new MultipartMixedFormatter();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = requestContentType;

        var provider = new EmptyModelMetadataProvider();
        var metadata = provider.GetMetadataForType(typeof(void));
        var context = new InputFormatterContext(
            httpContext,
            modelName: string.Empty,
            modelState: new ModelStateDictionary(),
            metadata: metadata,
            readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader);

        // Act
        var result = formatter.CanRead(context);

        // Assert
        Assert.False(result);
    }

    private class MathMLFormatter : TestFormatter
    {
        public MathMLFormatter()
        {
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/mathml-content+xml"));
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/mathml-presentation+xml"));
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/mathml+xml"));
        }
    }

    [Theory]
    [InlineData("application/mathml-content+xml")]
    [InlineData("application/mathml-presentation+xml")]
    [InlineData("application/mathml+xml; undefined=ignored")]
    public void MathMLFormatter_CanRead_ReturnsTrueForSupportedMediaTypes(string requestContentType)
    {
        // Arrange
        var formatter = new MathMLFormatter();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = requestContentType;

        var provider = new EmptyModelMetadataProvider();
        var metadata = provider.GetMetadataForType(typeof(void));
        var context = new InputFormatterContext(
            httpContext,
            modelName: string.Empty,
            modelState: new ModelStateDictionary(),
            metadata: metadata,
            readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader);

        // Act
        var result = formatter.CanRead(context);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("application/octet-stream; padding=3")]
    [InlineData("application/xml")]
    [InlineData("application/xml-dtd; undefined=ignored")]
    [InlineData("multipart/mixed; boundary=gc0p4Jq0M2Yt08j34c0p")]
    [InlineData("multipart/mixed; boundary=gc0p4Jq0M2Yt08j34c0p; undefined=ignored")]
    [InlineData("text/html")]
    public void MathMLFormatter_CanRead_ReturnsFalseForUnsupportedMediaTypes(string requestContentType)
    {
        // Arrange
        var formatter = new MathMLFormatter();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = requestContentType;

        var provider = new EmptyModelMetadataProvider();
        var metadata = provider.GetMetadataForType(typeof(void));
        var context = new InputFormatterContext(
            httpContext,
            modelName: string.Empty,
            modelState: new ModelStateDictionary(),
            metadata: metadata,
            readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader);

        // Act
        var result = formatter.CanRead(context);

        // Assert
        Assert.False(result);
    }

    // IsSubsetOf does not follow XML media type conventions. This formatter does not support "application/*+xml".
    private class XmlFormatter : TestFormatter
    {
        public XmlFormatter()
        {
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/xml"));
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/xml"));
        }
    }

    [Theory]
    [InlineData("application/xml")]
    [InlineData("application/mathml-content+xml")]
    [InlineData("application/mathml-presentation+xml")]
    [InlineData("application/mathml+xml; test=value")]
    public void XMLFormatter_CanRead_ReturnsTrueForSupportedMediaTypes(string requestContentType)
    {
        // Arrange
        var formatter = new XmlFormatter();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = requestContentType;

        var provider = new EmptyModelMetadataProvider();
        var metadata = provider.GetMetadataForType(typeof(void));
        var context = new InputFormatterContext(
            httpContext,
            modelName: string.Empty,
            modelState: new ModelStateDictionary(),
            metadata: metadata,
            readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader);

        // Act
        var result = formatter.CanRead(context);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("application/octet-stream; padding=3")]
    [InlineData("application/xml-dtd; undefined=ignored")]
    [InlineData("multipart/mixed; boundary=gc0p4Jq0M2Yt08j34c0p")]
    [InlineData("multipart/mixed; boundary=gc0p4Jq0M2Yt08j34c0p; undefined=ignored")]
    [InlineData("text/html")]
    public void XMLFormatter_CanRead_ReturnsFalseForUnsupportedMediaTypes(string requestContentType)
    {
        // Arrange
        var formatter = new XmlFormatter();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = requestContentType;

        var provider = new EmptyModelMetadataProvider();
        var metadata = provider.GetMetadataForType(typeof(void));
        var context = new InputFormatterContext(
            httpContext,
            modelName: string.Empty,
            modelState: new ModelStateDictionary(),
            metadata: metadata,
            readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader);

        // Act
        var result = formatter.CanRead(context);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetSupportedContentTypes_UnsupportedObjectType_ReturnsNull()
    {
        // Arrange
        var formatter = new TestFormatter();
        formatter.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/xml"));
        formatter.SupportedTypes.Add(typeof(string));

        // Act
        var results = formatter.GetSupportedContentTypes(contentType: null, objectType: typeof(int));

        // Assert
        Assert.Null(results);
    }

    [Fact]
    public void GetSupportedContentTypes_SupportedObjectType_ReturnsContentTypes()
    {
        // Arrange
        var formatter = new TestFormatter();
        formatter.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/xml"));
        formatter.SupportedTypes.Add(typeof(string));

        // Act
        var results = formatter.GetSupportedContentTypes(contentType: null, objectType: typeof(string));

        // Assert
        Assert.Collection(results, c => Assert.Equal("text/xml", c));
    }

    [Fact]
    public void GetSupportedContentTypes_NullContentType_ReturnsAllContentTypes()
    {
        // Arrange
        var formatter = new TestFormatter();
        formatter.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/xml"));
        formatter.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/xml"));

        // Act
        var results = formatter.GetSupportedContentTypes(contentType: null, objectType: typeof(string));

        // Assert
        Assert.Collection(
            results.OrderBy(c => c.ToString()),
            c => Assert.Equal("application/xml", c),
            c => Assert.Equal("text/xml", c));
    }

    [Fact]
    public void GetSupportedContentTypes_NonNullContentType_FiltersContentTypes()
    {
        // Arrange
        var formatter = new TestFormatter();
        formatter.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/xml"));
        formatter.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/xml"));

        // Act
        var results = formatter.GetSupportedContentTypes("text/*", typeof(string));

        // Assert
        Assert.Collection(results, c => Assert.Equal("text/xml", c));
    }

    [Fact]
    public void CanRead_ThrowsInvalidOperationException_IfMediaTypesListIsEmpty()
    {
        // Arrange
        var formatter = new BadConfigurationFormatter();
        var context = new InputFormatterContext(
            new DefaultHttpContext(),
            string.Empty,
            new ModelStateDictionary(),
            new EmptyModelMetadataProvider().GetMetadataForType(typeof(object)),
            (s, e) => new StreamReader(s, e));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => formatter.CanRead(context));
    }

    [Fact]
    public void GetSupportedContentTypes_ThrowsInvalidOperationException_IfMediaTypesListIsEmpty()
    {
        // Arrange
        var formatter = new BadConfigurationFormatter();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(
            () => formatter.GetSupportedContentTypes("application/json", typeof(object)));
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task ReadAsync_WithEmptyRequest_ReturnsNoValueResultWhenExpected(bool allowEmptyInputValue, bool expectedIsModelSet)
    {
        // Arrange
        var formatter = new TestFormatter();
        var context = new InputFormatterContext(
            new DefaultHttpContext(),
            string.Empty,
            new ModelStateDictionary(),
            new EmptyModelMetadataProvider().GetMetadataForType(typeof(object)),
            (s, e) => new StreamReader(s, e),
            allowEmptyInputValue);
        context.HttpContext.Request.ContentLength = 0;

        // Act
        var result = await formatter.ReadAsync(context);

        // Assert
        Assert.False(result.HasError);
        Assert.Null(result.Model);
        Assert.Equal(expectedIsModelSet, result.IsModelSet);
    }

    private class BadConfigurationFormatter : InputFormatter
    {
        public override Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
        {
            throw new NotImplementedException();
        }
    }

    private class TestFormatter : InputFormatter
    {
        public IList<Type> SupportedTypes { get; } = new List<Type>();

        protected override bool CanReadType(Type type)
        {
            return SupportedTypes.Count == 0 ? true : SupportedTypes.Contains(type);
        }

        public override Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
        {
            throw new NotImplementedException();
        }
    }
}
