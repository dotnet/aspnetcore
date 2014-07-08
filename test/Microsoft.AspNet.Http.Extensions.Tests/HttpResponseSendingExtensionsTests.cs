// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.PipelineCore;
using Xunit;

namespace Microsoft.AspNet.Http.Extensions
{
    public class HttpResponseSendingExtensionsTests
    {
        [Fact]
        public async Task SendData_SendBytes()
        {
            HttpContext context = CreateRequest();
            await context.Response.SendAsync(new byte[10]);

            Assert.Equal(10, context.Response.Body.Length);
            Assert.Equal(10, context.Response.ContentLength);
            Assert.Null(context.Response.ContentType);
        }

        [Fact]
        public async Task SendData_SendBytesAndContentType()
        {
            HttpContext context = CreateRequest();
            await context.Response.SendAsync(new byte[10], "text/html");

            Assert.Equal(10, context.Response.Body.Length);
            Assert.Equal(10, context.Response.ContentLength);
            Assert.Equal("text/html", context.Response.ContentType);
        }

        [Fact]
        public async Task SendData_SendText()
        {
            HttpContext context = CreateRequest();
            await context.Response.SendAsync("Hello World");

            Assert.Equal(11, context.Response.Body.Length);
            Assert.Equal(11, context.Response.ContentLength);
            Assert.Null(context.Response.ContentType);
        }

        [Fact]
        public async Task SendData_SendTextWithContentType()
        {
            HttpContext context = CreateRequest();
            await context.Response.SendAsync("Hello World", "text/html");

            Assert.Equal(11, context.Response.Body.Length);
            Assert.Equal(11, context.Response.ContentLength);
            Assert.Equal("text/html; charset=utf-8", context.Response.ContentType);
        }

        [Fact]
        public async Task SendData_SendTextWithEncoding()
        {
            HttpContext context = CreateRequest();
            await context.Response.SendAsync("Hello World", Encoding.UTF32);

            Assert.Equal(44, context.Response.Body.Length);
            Assert.Equal(44, context.Response.ContentLength);
            Assert.Null(context.Response.ContentType);
        }

        [Fact]
        public async Task SendData_SendTextWithContentTypeAndEncoding()
        {
            HttpContext context = CreateRequest();
            await context.Response.SendAsync("Hello World", Encoding.UTF32, "text/html");

            Assert.Equal(44, context.Response.Body.Length);
            Assert.Equal(44, context.Response.ContentLength);
            Assert.Equal("text/html; charset=utf-32", context.Response.ContentType);
        }

        private HttpContext CreateRequest()
        {
            HttpContext context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            return context;
        }
    }
}
