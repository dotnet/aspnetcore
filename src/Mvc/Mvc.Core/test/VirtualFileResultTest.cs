// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Net.Http.Headers;
using Moq;

namespace Microsoft.AspNetCore.Mvc;

public class VirtualFileResultTest : VirtualFileResultTestBase
{
    [Fact]
    public void Constructor_SetsFileName()
    {
        // Arrange
        var path = Path.GetFullPath("helllo.txt");

        // Act
        var result = new VirtualFileResult(path, "text/plain");

        // Assert
        Assert.Equal(path, result.FileName);
    }

    [Fact]
    public void Constructor_SetsContentTypeAndParameters()
    {
        // Arrange
        var path = Path.GetFullPath("helllo.txt");
        var contentType = "text/plain; charset=us-ascii; p1=p1-value";
        var expectedMediaType = contentType;

        // Act
        var result = new VirtualFileResult(path, contentType);

        // Assert
        Assert.Equal(path, result.FileName);
        MediaTypeAssert.Equal(expectedMediaType, result.ContentType);
    }

    [Fact]
    public void GetFileProvider_ReturnsFileProviderFromWebHostEnvironment()
    {
        // Arrange
        var webHostFileProvider = Mock.Of<IFileProvider>();
        var webHostEnvironment = Mock.Of<IWebHostEnvironment>(e => e.WebRootFileProvider == webHostFileProvider);

        var result = new VirtualFileResult("some-path", "text/plain");

        // Act
        var fileProvider = VirtualFileResultExecutor.GetFileProvider(result, webHostEnvironment);

        // Assert
        Assert.Same(webHostFileProvider, fileProvider);
    }

    [Fact]
    public void GetFileProvider_ReturnsFileProviderFromResult()
    {
        // Arrange
        var webHostFileProvider = Mock.Of<IFileProvider>();
        var fileProvider = Mock.Of<IFileProvider>();
        var webHostEnvironment = Mock.Of<IWebHostEnvironment>(e => e.WebRootFileProvider == webHostFileProvider);

        var result = new VirtualFileResult("some-path", "text/plain") { FileProvider = fileProvider };

        // Act
        var actual = VirtualFileResultExecutor.GetFileProvider(result, webHostEnvironment);

        // Assert
        Assert.Same(fileProvider, actual);
    }

    protected override Task ExecuteAsync(HttpContext httpContext, string path, string contentType, DateTimeOffset? lastModified = null, EntityTagHeaderValue entityTag = null, bool enableRangeProcessing = false)
    {
        var webHostEnvironment = httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        httpContext.RequestServices = new ServiceCollection()
            .AddSingleton(webHostEnvironment)
            .AddTransient<IActionResultExecutor<VirtualFileResult>, VirtualFileResultExecutor>()
            .AddTransient<ILoggerFactory, NullLoggerFactory>()
            .BuildServiceProvider();

        var actionContext = new ActionContext(httpContext, new(), new());
        var result = new VirtualFileResult(path, contentType)
        {
            LastModified = lastModified,
            EntityTag = entityTag,
            EnableRangeProcessing = enableRangeProcessing,
        };

        return result.ExecuteResultAsync(actionContext);
    }
}
