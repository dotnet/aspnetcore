// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if DNX451
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.Formatters;
using Microsoft.AspNet.Mvc.Infrastructure;
using Microsoft.AspNet.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.OptionsModel;
using Xunit;

namespace System.Web.Http
{
    public class NegotiatedContentResultTest
    {
        [Fact]
        public async Task NegotiatedContentResult_SetsStatusCode()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = CreateServices();

            var stream = new MemoryStream();
            httpContext.Response.Body = stream;

            var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var result = new NegotiatedContentResult<Product>(HttpStatusCode.Ambiguous, new Product());

            // Act
            await result.ExecuteResultAsync(context);

            // Assert
            Assert.Equal(300, context.HttpContext.Response.StatusCode);
        }

        [Fact]
        public async Task NegotiatedContentResult_WritesHttpError()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = CreateServices();

            var stream = new MemoryStream();
            httpContext.Response.Body = stream;

            var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var result = new NegotiatedContentResult<Product>(HttpStatusCode.Ambiguous, new Product());

            // Act
            await result.ExecuteResultAsync(context);

            // Assert
            using (var reader = new StreamReader(stream))
            {
                stream.Seek(0, SeekOrigin.Begin);
                var content = reader.ReadToEnd();
                Assert.Equal("{\"Id\":0,\"Name\":null}", content);
            }
        }

        private static IServiceProvider CreateServices()
        {
            var options = new OptionsManager<MvcOptions>(new IConfigureOptions<MvcOptions>[] { });
            options.Value.OutputFormatters.Add(new StringOutputFormatter());
            options.Value.OutputFormatters.Add(new JsonOutputFormatter());

            var services = new ServiceCollection();
            services.AddSingleton(new ObjectResultExecutor(
                options,
                new TestHttpResponseStreamWriterFactory(),
                NullLoggerFactory.Instance));

            return services.BuildServiceProvider();
        }

        private class Product
        {
            public int Id { get; set; }

            public string Name { get; set; }
        };
    }
}
#endif
