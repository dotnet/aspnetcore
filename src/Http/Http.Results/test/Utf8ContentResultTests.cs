// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.HttpResults;

public class Utf8ContentResultTests
{
    [Fact]
    public void Utf8ContentResultSetsProperties()
    {
        // Arrange
        var data = """{ "name" : "Hello" }"""u8.ToArray();
        var result = new Utf8ContentHttpResult(data, contentType: "application/json charst=utf-8", statusCode: 401);

        // Act
        Assert.Equal(data, result.ResponseContent.ToArray());
        Assert.Equal("application/json charst=utf-8", result.ContentType);
        Assert.Equal(401, result.StatusCode);
    }

    [Fact]
    public async Task Utf8ContentExecutionWorks()
    {
        // Arrange
        var data = "Hello"u8.ToArray();
        var context = GetHttpContext();
        var ms = new MemoryStream();
        context.Response.Body = ms;
        var result = new Utf8ContentHttpResult("Hello"u8, contentType: null, statusCode: null);

        await result.ExecuteAsync(context);

        // Act
        Assert.Equal(data, ms.ToArray());
        Assert.Equal(data.Length, context.Response.ContentLength);
        Assert.Equal("text/plain; charset=utf-8", context.Response.ContentType);
    }

    [Fact]
    public async Task Utf8TextResultCopiesData()
    {
        // Arrange
        var data = "Hello"u8.ToArray();
        var context = GetHttpContext();
        var ms = new MemoryStream();
        context.Response.Body = ms;
        var result = new Utf8ContentHttpResult(stackalloc byte[5] { 72, 101, 108, 108, 111 }, contentType: null, statusCode: null);

        await result.ExecuteAsync(context);

        // Act
        Assert.Equal(data, ms.ToArray());
        Assert.Equal(data.Length, context.Response.ContentLength);
        Assert.Equal("text/plain; charset=utf-8", context.Response.ContentType);
    }

    [Fact]
    public void Utf8TextResult_Implements_IContentTypeHttpResult_Correctly()
    {
        // Arrange
        var contentType = "application/custom";

        // Act & Assert
        var result = Assert.IsAssignableFrom<IContentTypeHttpResult>(new Utf8ContentHttpResult("Hello"u8, contentType, statusCode: null));
        Assert.Equal(contentType, result.ContentType);
    }

    [Fact]
    public void Utf8TextResult_Implements_IStatusCodeHttpResult_Correctly()
    {
        // Arrange
        var contentType = "application/custom";

        // Act & Assert
        var result = Assert.IsAssignableFrom<IStatusCodeHttpResult>(new Utf8ContentHttpResult("Hello"u8, contentType, statusCode: StatusCodes.Status202Accepted));
        Assert.Equal(StatusCodes.Status202Accepted, result.StatusCode);
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
