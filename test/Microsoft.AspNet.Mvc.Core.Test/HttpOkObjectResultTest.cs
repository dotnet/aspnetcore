// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.Formatters;
using Microsoft.AspNet.Mvc.Infrastructure;
using Microsoft.AspNet.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.OptionsModel;
using Xunit;

namespace Microsoft.AspNet.Mvc
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
            var result = new HttpOkObjectResult(value);

            // Assert
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
            Assert.Same(value, result.Value);
        }

        [Theory]
        [MemberData(nameof(ValuesData))]
        public async Task HttpOkObjectResult_SetsStatusCode(object value)
        {
            // Arrange
            var result = new HttpOkObjectResult(value);

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
            var options = new TestOptionsManager<MvcOptions>();
            options.Value.OutputFormatters.Add(new StringOutputFormatter());
            options.Value.OutputFormatters.Add(new JsonOutputFormatter());

            var services = new ServiceCollection();
            services.AddSingleton(new ObjectResultExecutor(
                options,
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
