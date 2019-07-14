// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.HeaderPropagation.Tests
{
    public class HeaderPropagationMiddlewareTest
    {
        public HeaderPropagationMiddlewareTest()
        {
            Context = new DefaultHttpContext();
            Next = ctx => Task.CompletedTask;
            Processor = new HeaderPropagationProcessorMock();
            Middleware = new HeaderPropagationMiddleware(Next, Processor);
        }

        public DefaultHttpContext Context { get; set; }
        public RequestDelegate Next { get; set; }
        public HeaderPropagationProcessorMock Processor { get; set; }
        public HeaderPropagationMiddleware Middleware { get; set; }

        [Fact]
        public async Task Invoke_InvokesProcessorWithRequestHeaders()
        {
            // Act
            await Middleware.Invoke(Context);

            // Assert
            Assert.NotNull(Processor.ReceivedRequestHeaders);
            Assert.Same(Processor.ReceivedRequestHeaders, Context.Request.Headers);
        }

        [Fact]
        public async Task Invoke_InvokesNextMiddleware()
        {
            // Arrange
            var called = false;
            Next = ctx =>
            {
                called = true;
                return Task.CompletedTask;
            };

            // Act
            await Middleware.Invoke(Context);

            // Assert
            Assert.True(called);
        }

        public class HeaderPropagationProcessorMock : IHeaderPropagationProcessor
        {
            public IDictionary<string, StringValues> ReceivedRequestHeaders { get; private set; }

            public void ProcessRequest(IDictionary<string, StringValues> requestHeaders)
            {
                ReceivedRequestHeaders = requestHeaders;
            }
        }
    }
}
