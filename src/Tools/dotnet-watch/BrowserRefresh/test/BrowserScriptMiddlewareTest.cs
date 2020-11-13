// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Microsoft.AspNetCore.Watch.BrowserRefresh
{
    public class BrowserScriptMiddlewareTest
    {
        [Fact]
        public async Task InvokeAsync_ReturnsScript()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var stream = new MemoryStream();
            context.Response.Body = stream;
            var middleware = new BrowserScriptMiddleware("some-host");

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            stream.Position = 0;
            var script = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Contains("// dotnet-watch browser reload script", script);
            Assert.Contains("'some-host'", script);
        }

        [Fact]
        public async Task InvokeAsync_ConfiguresHeaders()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            var middleware = new BrowserScriptMiddleware("some-host");

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            var response = context.Response;
            Assert.Collection(
                response.Headers.OrderBy(h => h.Key),
                kvp =>
                {
                    Assert.Equal("Cache-Control", kvp.Key);
                    Assert.Equal("no-store", kvp.Value);
                },
                kvp =>
                {
                    Assert.Equal("Content-Length", kvp.Key);
                    Assert.NotEmpty(kvp.Value);
                },
                kvp =>
                {
                    Assert.Equal("Content-Type", kvp.Key);
                    Assert.Equal("application/javascript; charset=utf-8", kvp.Value);
                });
        }
    }
}
