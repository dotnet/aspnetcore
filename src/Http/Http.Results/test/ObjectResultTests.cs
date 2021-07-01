// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.AspNetCore.Http.Result
{
    public class ObjectResultTests
    {
        [Fact]
        public async Task ObjectResult_ExecuteAsync_SetsStatusCode()
        {
            // Arrange
            var result = new ObjectResult("Hello", 407);

            var httpContext = new DefaultHttpContext()
            {
                RequestServices = CreateServices(),
            };

            // Act
            await result.ExecuteAsync(httpContext);

            // Assert
            Assert.Equal(407, httpContext.Response.StatusCode);
        }

        [Fact]
        public async Task ObjectResult_ExecuteAsync_JsonSerializesBody()
        {
            // Arrange
            var result = new ObjectResult("Hello", 407);
            var stream = new MemoryStream();
            var httpContext = new DefaultHttpContext()
            {
                RequestServices = CreateServices(),
                Response =
                {
                    Body = stream,
                },
            };

            // Act
            await result.ExecuteAsync(httpContext);

            // Assert
            Assert.Equal("\"Hello\"", Encoding.UTF8.GetString(stream.ToArray()));
        }

        private static IServiceProvider CreateServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);

            return services.BuildServiceProvider();
        }
    }
}
