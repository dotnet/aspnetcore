// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http.HttpResults;

public class ContentResultTests
{
    [Fact]
    public async Task ContentResult_ExecuteAsync_Response_NullContent_SetsContentTypeAndEncoding()
    {
        // Arrange
        var contentType = new MediaTypeHeaderValue("text/plain")
        {
            Encoding = Encoding.Unicode
        }.ToString();

        var contentResult = new ContentHttpResult(null, contentType);
        var httpContext = GetHttpContext();

        // Act
        await contentResult.ExecuteAsync(httpContext);

        // Assert
        Assert.Equal("text/plain; charset=utf-16", httpContext.Response.ContentType);
    }

    public static TheoryData<MediaTypeHeaderValue, string, string, string, byte[]> ContentResultContentTypeData
    {
        get
        {
            // contentType, content, responseContentType, expectedContentType, expectedData
            return new TheoryData<MediaTypeHeaderValue, string, string, string, byte[]>
                {
                    {
                        null,
                        "κόσμε",
                        null,
                        "text/plain; charset=utf-8",
                        new byte[] { 206, 186, 225, 189, 185, 207, 131, 206, 188, 206, 181 } //utf-8 without BOM
                    },
                    {
                        new MediaTypeHeaderValue("text/foo"),
                        "κόσμε",
                        null,
                        "text/foo",
                        new byte[] { 206, 186, 225, 189, 185, 207, 131, 206, 188, 206, 181 } //utf-8 without BOM
                    },
                    {
                        MediaTypeHeaderValue.Parse("text/foo;p1=p1-value"),
                        "κόσμε",
                        null,
                        "text/foo; p1=p1-value",
                        new byte[] { 206, 186, 225, 189, 185, 207, 131, 206, 188, 206, 181 } //utf-8 without BOM
                    },
                    {
                        new MediaTypeHeaderValue("text/foo") { Encoding = Encoding.ASCII },
                        "abcd",
                        null,
                        "text/foo; charset=us-ascii",
                        new byte[] { 97, 98, 99, 100 }
                    },
                    {
                        null,
                        "abcd",
                        "text/bar",
                        "text/bar",
                        new byte[] { 97, 98, 99, 100 }
                    },
                    {
                        null,
                        "abcd",
                        "application/xml; charset=us-ascii",
                        "application/xml; charset=us-ascii",
                        new byte[] { 97, 98, 99, 100 }
                    },
                    {
                        null,
                        "abcd",
                        "Invalid content type",
                        "Invalid content type",
                        new byte[] { 97, 98, 99, 100 }
                    },
                    {
                        new MediaTypeHeaderValue("text/foo") { Charset = "us-ascii" },
                        "abcd",
                        "text/bar",
                        "text/foo; charset=us-ascii",
                        new byte[] { 97, 98, 99, 100 }
                    },
                };
        }
    }

    [Theory]
    [MemberData(nameof(ContentResultContentTypeData))]
    public async Task ContentResult_ExecuteAsync_SetContentTypeAndEncoding_OnResponse(
        MediaTypeHeaderValue contentType,
        string content,
        string responseContentType,
        string expectedContentType,
        byte[] expectedContentData)
    {
        // Arrange
        var contentResult = new ContentHttpResult(content, contentType?.ToString());
        var httpContext = GetHttpContext();
        var memoryStream = new MemoryStream();
        httpContext.Response.Body = memoryStream;
        httpContext.Response.ContentType = responseContentType;

        // Act
        await contentResult.ExecuteAsync(httpContext);

        // Assert
        var finalResponseContentType = httpContext.Response.ContentType;
        Assert.Equal(expectedContentType, finalResponseContentType);
        Assert.Equal(expectedContentData, memoryStream.ToArray());
        Assert.Equal(expectedContentData.Length, httpContext.Response.ContentLength);
    }

    [Fact]
    public void ExecuteAsync_ThrowsArgumentNullException_WhenHttpContextIsNull()
    {
        // Arrange
        var result = new ContentHttpResult("content", null);
        HttpContext httpContext = null;

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>("httpContext", () => result.ExecuteAsync(httpContext));
    }

    [Fact]
    public void ContentResult_Implements_IContentTypeHttpResult_Correctly()
    {
        // Arrange
        var contentType = "application/custom";

        // Act & Assert
        var result = Assert.IsAssignableFrom<IContentTypeHttpResult>(new ContentHttpResult("content", contentType));
        Assert.Equal(contentType, result.ContentType);
    }

    [Fact]
    public void ContentResult_Implements_IStatusCodeHttpResult_Correctly()
    {
        // Arrange
        var contentType = "application/custom";

        // Act & Assert
        var result = Assert.IsAssignableFrom<IStatusCodeHttpResult>(new ContentHttpResult("content", contentType, StatusCodes.Status202Accepted));
        Assert.Equal(StatusCodes.Status202Accepted, result.StatusCode);
    }

    [Fact]
    public void ContentResult_Implements_IStatusCodeHttpResult_Correctly_WithNullStatus()
    {
        // Arrange
        var contentType = "application/custom";

        // Act & Assert
        var result = Assert.IsAssignableFrom<IStatusCodeHttpResult>(new ContentHttpResult("content", contentType));
        Assert.Null(result.StatusCode);
    }

    private static IServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        return services;
    }

    private static HttpContext GetHttpContext()
    {
        var services = CreateServices();

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = services.BuildServiceProvider();

        return httpContext;
    }
}
