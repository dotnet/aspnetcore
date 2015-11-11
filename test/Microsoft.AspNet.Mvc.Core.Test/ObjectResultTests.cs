// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Formatters;
using Microsoft.AspNet.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class ObjectResultTests
    {
        [Fact]
        public void ObjectResult_Constructor()
        {
            // Arrange
            var input = "testInput";

            // Act
            var result = new ObjectResult(input);

            // Assert
            Assert.Equal(input, result.Value);
            Assert.Empty(result.ContentTypes);
            Assert.Empty(result.Formatters);
            Assert.Null(result.StatusCode);
            Assert.Null(result.DeclaredType);
        }

        [Fact]
        public async Task ObjectResult_ExecuteResultAsync_SetsStatusCode()
        {
            // Arrange
            var result = new ObjectResult("Hello")
            {
                StatusCode = 404,
                Formatters = new FormatterCollection<IOutputFormatter>()
                {
                    new NoOpOutputFormatter(),
                },
            };

            var actionContext = new ActionContext()
            {
                HttpContext = new DefaultHttpContext()
                {
                    RequestServices = CreateServices(),
                }
            };

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            Assert.Equal(404, actionContext.HttpContext.Response.StatusCode);
        }

        private static IServiceProvider CreateServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton(new ObjectResultExecutor(
                new TestOptionsManager<MvcOptions>(),
                new TestHttpResponseStreamWriterFactory(),
                NullLoggerFactory.Instance));
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);

            return services.BuildServiceProvider();
        }

        private class NoOpOutputFormatter : IOutputFormatter
        {
            public bool CanWriteResult(OutputFormatterCanWriteContext context)
            {
                return true;
            }

            public Task WriteAsync(OutputFormatterWriteContext context)
            {
                return Task.FromResult(0);
            }
        }
    }
}