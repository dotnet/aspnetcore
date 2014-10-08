// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if ASPNET50
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.PipelineCore;
using Microsoft.AspNet.Routing;
using Moq;
using Xunit;

namespace System.Web.Http
{
    public class BadRequestErrorMessageResultTest
    {
        [Fact]
        public async Task BadRequestErrorMessageResult_SetsStatusCode()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = CreateServices();

            var stream = new MemoryStream();
            httpContext.Response.Body = stream;

            var context = new ActionContext(new RouteContext(httpContext), new ActionDescriptor());
            var result = new BadRequestErrorMessageResult("Error");

            // Act
            await result.ExecuteResultAsync(context);

            // Assert
            Assert.Equal(400, context.HttpContext.Response.StatusCode);
        }

        [Fact]
        public async Task BadRequestErrorMessageResult_WritesHttpError()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = CreateServices();

            var stream = new MemoryStream();
            httpContext.Response.Body = stream;

            var context = new ActionContext(new RouteContext(httpContext), new ActionDescriptor());
            var result = new BadRequestErrorMessageResult("Error");

            // Act
            await result.ExecuteResultAsync(context);

            // Assert
            using (var reader = new StreamReader(stream))
            {
                stream.Seek(0, SeekOrigin.Begin);
                var content = reader.ReadToEnd();
                Assert.Equal("{\"Message\":\"Error\"}", content);
            }
        }

        private IServiceProvider CreateServices()
        {
            var services = new Mock<IServiceProvider>(MockBehavior.Strict);

            var formatters = new Mock<IOutputFormattersProvider>(MockBehavior.Strict);
            formatters
                .SetupGet(f => f.OutputFormatters)
                .Returns(new List<IOutputFormatter>() { new JsonOutputFormatter(), });

            services
                .Setup(s => s.GetService(typeof(IOutputFormattersProvider)))
                .Returns(formatters.Object);

            return services.Object;
        }
    }
}
#endif