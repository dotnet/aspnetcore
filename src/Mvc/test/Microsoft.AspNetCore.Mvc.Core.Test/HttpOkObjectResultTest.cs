// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
{
    public class HttpOkObjectResultTest
    {
        public static TheoryData<object> ValuesData
        {
            get
            {
                return new TheoryData<object>
                {
                    null,
                    "Test string",
                    new Person
                    {
                        Id = 274,
                        Name = "George",
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(ValuesData))]
        public void HttpOkObjectResult_InitializesStatusCodeAndValue(object value)
        {
            // Arrange & Act
            var result = new OkObjectResult(value);

            // Assert
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
            Assert.Same(value, result.Value);
        }

        [Theory]
        [MemberData(nameof(ValuesData))]
        public async Task HttpOkObjectResult_SetsStatusCode(object value)
        {
            // Arrange
            var result = new OkObjectResult(value);

            var httpContext = new DefaultHttpContext
            {
                RequestServices = CreateServices(),
            };
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
        }

        private static IServiceProvider CreateServices()
        {
            var options = Options.Create(new MvcOptions());
            options.Value.OutputFormatters.Add(new StringOutputFormatter());
            options.Value.OutputFormatters.Add(new JsonOutputFormatter(
                new JsonSerializerSettings(),
                ArrayPool<char>.Shared));

            var services = new ServiceCollection();
            services.AddSingleton<IActionResultExecutor<ObjectResult>>(new ObjectResultExecutor(
                new DefaultOutputFormatterSelector(options, NullLoggerFactory.Instance),
                new TestHttpResponseStreamWriterFactory(),
                NullLoggerFactory.Instance));

            return services.BuildServiceProvider();
        }

        private class Person
        {
            public int Id { get; set; }

            public string Name { get; set; }
        }
    }
}
