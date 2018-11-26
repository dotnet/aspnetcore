// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Http
{
    public class HttpResponseWritingExtensionsTests
    {
        [Fact]
        public async Task WritingText_WriteText()
        {
            HttpContext context = CreateRequest();
            await context.Response.WriteAsync("Hello World");

            Assert.Equal(11, context.Response.Body.Length);
        }

        [Fact]
        public async Task WritingText_MultipleWrites()
        {
            HttpContext context = CreateRequest();
            await context.Response.WriteAsync("Hello World");
            await context.Response.WriteAsync("Hello World");

            Assert.Equal(22, context.Response.Body.Length);
        }

        private HttpContext CreateRequest()
        {
            HttpContext context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            return context;
        }
    }
}
