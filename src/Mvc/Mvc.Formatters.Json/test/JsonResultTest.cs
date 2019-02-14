// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Formatters.Json.Internal;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
{
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

            var executor = new JsonResultExecutor(
                new TestHttpResponseStreamWriterFactory(),
                NullLogger<JsonResultExecutor>.Instance,
                Options.Create(new MvcJsonOptions()),
                ArrayPool<char>.Shared);

            var services = new ServiceCollection();
            services.AddSingleton(executor);
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
}