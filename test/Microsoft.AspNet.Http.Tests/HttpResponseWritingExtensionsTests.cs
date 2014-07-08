// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Builder.Extensions;
using Microsoft.AspNet.HttpFeature;
using Microsoft.AspNet.PipelineCore;
using Xunit;

namespace Microsoft.AspNet.Http
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
            return context;
        }
    }
}
