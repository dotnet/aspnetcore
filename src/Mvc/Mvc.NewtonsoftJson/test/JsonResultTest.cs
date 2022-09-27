// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Mvc.NewtonsoftJson;

public class JsonResultTest
{
    [Fact]
    public async Task ExecuteAsync_WritesJsonContent()
    {
        // Arrange
        var value = new { foo = "abcd" };
        var expected = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value));

        var context = GetActionContext();

        var result = new JsonResult(value);

        // Act
        await result.ExecuteResultAsync(context);

        // Assert
        var written = GetWrittenBytes(context.HttpContext);
        Assert.Equal(expected, written);
        Assert.Equal("application/json; charset=utf-8", context.HttpContext.Response.ContentType);
    }

    private static HttpContext GetHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        var executor = new NewtonsoftJsonResultExecutor(
            new TestHttpResponseStreamWriterFactory(),
            NullLogger<NewtonsoftJsonResultExecutor>.Instance,
            Options.Create(new MvcOptions()),
            Options.Create(new MvcNewtonsoftJsonOptions()),
            ArrayPool<char>.Shared);

        var services = new ServiceCollection();
        services.AddSingleton<IActionResultExecutor<JsonResult>>(executor);
        httpContext.RequestServices = services.BuildServiceProvider();

        return httpContext;
    }

    private static ActionContext GetActionContext()
    {
        return new ActionContext(GetHttpContext(), new RouteData(), new ActionDescriptor());
    }

    private static byte[] GetWrittenBytes(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        return Assert.IsType<MemoryStream>(context.Response.Body).ToArray();
    }
}
