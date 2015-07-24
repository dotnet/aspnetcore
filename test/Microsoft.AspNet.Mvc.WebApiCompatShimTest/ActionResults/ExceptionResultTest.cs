// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if DNX451
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;
using Moq;
using Xunit;

namespace System.Web.Http
{
    public class ExceptionResultTest
    {
        [Fact]
        public async Task ExceptionResult_SetsStatusCode()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = CreateServices();

            var stream = new MemoryStream();
            httpContext.Response.Body = stream;

            var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var result = new ExceptionResult(new Exception("hello, world!"), includeErrorDetail: false);

            // Act
            await result.ExecuteResultAsync(context);

            // Assert
            Assert.Equal(StatusCodes.Status500InternalServerError, context.HttpContext.Response.StatusCode);
        }

        [Fact]
        public async Task ExceptionResult_WritesHttpError()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = CreateServices();

            var stream = new MemoryStream();
            httpContext.Response.Body = stream;

            var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var result = new ExceptionResult(new Exception("hello, world!"), includeErrorDetail: false);

            // Act
            await result.ExecuteResultAsync(context);

            // Assert
            using (var reader = new StreamReader(stream))
            {
                stream.Seek(0, SeekOrigin.Begin);
                var content = reader.ReadToEnd();
                Assert.Equal("{\"Message\":\"An error has occurred.\"}", content);
            }
        }

        private IServiceProvider CreateServices()
        {
            var services = new Mock<IServiceProvider>(MockBehavior.Strict);

            var options = new MvcOptions();
            options.OutputFormatters.Add(new JsonOutputFormatter());

            var optionsAccessor = new Mock<IOptions<MvcOptions>>();
            optionsAccessor.SetupGet(o => o.Options)
                .Returns(options);

            var actionBindingContext = new ActionBindingContext { OutputFormatters = options.OutputFormatters };
            services.Setup(o => o.GetService(typeof(IActionBindingContextAccessor)))
                    .Returns(new ActionBindingContextAccessor() { ActionBindingContext = actionBindingContext });

            services.Setup(s => s.GetService(typeof(IOptions<MvcOptions>)))
                .Returns(optionsAccessor.Object);

            services.Setup(s => s.GetService(typeof(ILogger<ObjectResult>)))
                .Returns(new Mock<ILogger<ObjectResult>>().Object);

            return services.Object;
        }
    }
}
#endif
