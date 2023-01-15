// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Formatters;

public class TextOutputFormatterTests
{
    public static IEnumerable<object[]> SelectResponseCharacterEncodingData
    {
        get
        {
            // string acceptEncodings, string requestEncoding, string[] supportedEncodings, string expectedEncoding
            yield return new object[] { "", new string[] { "utf-8", "utf-16" }, "utf-8" };

            yield return new object[] { "utf-8", new string[] { "utf-8", "utf-16" }, "utf-8" };
            yield return new object[] { "utf-16", new string[] { "utf-8", "utf-16" }, "utf-16" };
            yield return new object[] { "utf-16; q=0.5", new string[] { "utf-8", "utf-16" }, "utf-16" };

            yield return new object[] { "utf-8; q=0.0", new string[] { "utf-8", "utf-16" }, "utf-8" };
            yield return new object[] { "utf-8; q=0.0, utf-16; q=0.0", new string[] { "utf-8", "utf-16" }, "utf-8" };

            yield return new object[] { "*; q=0.0", new string[] { "utf-8", "utf-16" }, "utf-8" };
        }
    }

    [Theory]
    [MemberData(nameof(SelectResponseCharacterEncodingData))]
    public void SelectResponseCharacterEncoding_SelectsEncoding(
        string acceptCharsetHeaders,
        string[] supportedEncodings,
        string expectedEncoding)
    {
        // Arrange
        var httpContext = new Mock<HttpContext>();
        var httpRequest = new DefaultHttpContext().Request;
        httpRequest.Headers.AcceptCharset = acceptCharsetHeaders;
        httpRequest.Headers.Accept = "application/acceptCharset";
        httpContext.SetupGet(o => o.Request).Returns(httpRequest);

        var formatter = new TestOutputFormatter();
        foreach (string supportedEncoding in supportedEncodings)
        {
            formatter.SupportedEncodings.Add(Encoding.GetEncoding(supportedEncoding));
        }

        var context = new OutputFormatterWriteContext(
            httpContext.Object,
            new TestHttpResponseStreamWriterFactory().CreateWriter,
            typeof(string),
            "someValue")
        {
            ContentType = new StringSegment(httpRequest.Headers.Accept),
        };

        // Act
        var actualEncoding = formatter.SelectCharacterEncoding(context);

        // Assert
        Assert.Equal(Encoding.GetEncoding(expectedEncoding), actualEncoding);
    }

    [Theory]
    [InlineData("application/json; charset=utf-16", "application/json; charset=utf-32")]
    [InlineData("application/json; charset=utf-16; format=indent", "application/json; charset=utf-32; format=indent")]
    public void WriteResponse_OverridesCharset_IfDifferentFromContentTypeCharset(
        string contentType,
        string expectedContentType)
    {
        // Arrange
        var formatter = new OverrideEncodingFormatter(Encoding.UTF32);

        var formatterContext = new OutputFormatterWriteContext(
            new DefaultHttpContext(),
            new TestHttpResponseStreamWriterFactory().CreateWriter,
            objectType: null,
            @object: null)
        {
            ContentType = new StringSegment(contentType),
        };

        // Act
        formatter.WriteAsync(formatterContext);

        // Assert
        Assert.Equal(new StringSegment(expectedContentType), formatterContext.ContentType);
    }

    [Fact]
    public void WriteResponse_GetMediaTypeWithCharsetReturnsMediaTypeFromCache_IfEncodingIsUtf8()
    {
        // Arrange
        var formatter = new TestOutputFormatter();

        var formatterContext = new OutputFormatterWriteContext(
            new DefaultHttpContext(),
            new TestHttpResponseStreamWriterFactory().CreateWriter,
            objectType: null,
            @object: null)
        {
            ContentType = new StringSegment("application/json"),
        };

        formatter.SupportedMediaTypes.Add("application/json");
        formatter.SupportedEncodings.Add(Encoding.UTF8);

        // Act
        formatter.WriteAsync(formatterContext);
        var firstContentType = formatterContext.ContentType;

        formatterContext.ContentType = new StringSegment("application/json");

        formatter.WriteAsync(formatterContext);
        var secondContentType = formatterContext.ContentType;

        // Assert
        Assert.Same(firstContentType.Buffer, secondContentType.Buffer);
    }

    [Fact]
    public void WriteResponse_GetMediaTypeWithCharsetReplacesCharset_IfDifferentThanEncoding()
    {
        // Arrange
        var formatter = new TestOutputFormatter();

        var formatterContext = new OutputFormatterWriteContext(
            new DefaultHttpContext(),
            new TestHttpResponseStreamWriterFactory().CreateWriter,
            objectType: null,
            @object: null)
        {
            ContentType = new StringSegment("application/json; charset=utf-7"),
        };

        formatter.SupportedMediaTypes.Add("application/json");
        formatter.SupportedEncodings.Add(Encoding.UTF8);

        // Act
        formatter.WriteAsync(formatterContext);

        // Assert
        Assert.Equal(new StringSegment("application/json; charset=utf-8"), formatterContext.ContentType);
    }

    [Fact]
    public void WriteResponse_GetMediaTypeWithCharsetReturnsSameString_IfCharsetEqualToEncoding()
    {
        // Arrange
        var formatter = new TestOutputFormatter();

        var contentType = "application/json; charset=utf-16";
        var formatterContext = new OutputFormatterWriteContext(
            new DefaultHttpContext(),
            new TestHttpResponseStreamWriterFactory().CreateWriter,
            objectType: null,
            @object: null)
        {
            ContentType = new StringSegment(contentType),
        };

        formatter.SupportedMediaTypes.Add("application/json");
        formatter.SupportedEncodings.Add(Encoding.Unicode);

        // Act
        formatter.WriteAsync(formatterContext);

        // Assert
        Assert.Same(contentType, formatterContext.ContentType.Buffer);
    }

    [Fact]
    public void WriteResponseContentHeaders_NoSupportedEncodings_NoEncodingIsSet()
    {
        // Arrange
        var formatter = new TestOutputFormatter();

        var testContentType = new StringSegment("text/json");

        formatter.SupportedEncodings.Clear();
        formatter.SupportedMediaTypes.Clear();
        formatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/json"));

        var context = new OutputFormatterWriteContext(
            new DefaultHttpContext(),
            new TestHttpResponseStreamWriterFactory().CreateWriter,
            objectType: null,
            @object: null)
        {
            ContentType = testContentType,
        };

        // Act
        formatter.WriteResponseHeaders(context);

        // Assert
        Assert.Null(MediaTypeHeaderValue.Parse(context.ContentType.Value).Encoding);
        Assert.Equal(testContentType, context.ContentType);

        // If we had set an encoding, it would be part of the content type header
        Assert.Equal(MediaTypeHeaderValue.Parse(testContentType.Value), context.HttpContext.Response.GetTypedHeaders().ContentType);
    }

    [Fact]
    public async Task WriteAsync_ReturnsNotAcceptable_IfSelectCharacterEncodingReturnsNull()
    {
        // Arrange
        var formatter = new OverrideEncodingFormatter(encoding: null);

        var testContentType = new StringSegment("text/json");
        formatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/json"));

        var context = new OutputFormatterWriteContext(
            new DefaultHttpContext() { RequestServices = new ServiceCollection().BuildServiceProvider() },
            new TestHttpResponseStreamWriterFactory().CreateWriter,
            objectType: null,
            @object: null)
        {
            ContentType = testContentType,
        };

        // Act
        await formatter.WriteAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status406NotAcceptable, context.HttpContext.Response.StatusCode);
    }

    [Fact]
    public void GetAcceptCharsetHeaderValues_ReadsHeaderAndParsesValues()
    {
        // Arrange
        const string expectedValue = "expected";

        var formatter = new OverrideEncodingFormatter(encoding: null);
        var context = new DefaultHttpContext();
        context.Request.Headers.AcceptCharset = expectedValue;

        var writerContext = new OutputFormatterWriteContext(
            context,
            new TestHttpResponseStreamWriterFactory().CreateWriter,
            objectType: null,
            @object: null);

        // Act
        var result = TextOutputFormatter.GetAcceptCharsetHeaderValues(writerContext);

        //Assert
        Assert.Equal(expectedValue, Assert.Single(result).Value.Value);
    }

    private class TestOutputFormatter : TextOutputFormatter
    {
        public TestOutputFormatter()
        {
            SupportedMediaTypes.Add("application/acceptCharset");
        }

        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            return Task.FromResult(true);
        }
    }

    private class OverrideEncodingFormatter : TextOutputFormatter
    {
        private readonly Encoding _encoding;

        public OverrideEncodingFormatter(Encoding encoding)
        {
            _encoding = encoding;
        }

        public override Encoding SelectCharacterEncoding(OutputFormatterWriteContext context)
        {
            return _encoding;
        }

        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            return Task.FromResult(true);
        }
    }
}
