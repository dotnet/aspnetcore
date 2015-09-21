// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.Formatters;
using Microsoft.AspNet.Mvc.Infrastructure;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class HttpNotFoundObjectResultTest
    {
        [Fact]
        public void HttpNotFoundObjectResult_InitializesStatusCode()
        {
            // Arrange & act
            var notFound = new HttpNotFoundObjectResult(null);

            // Assert
            Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
        }

        [Fact]
        public void HttpNotFoundObjectResult_InitializesStatusCodeAndResponseContent()
        {
            // Arrange & act
            var notFound = new HttpNotFoundObjectResult("Test Content");

            // Assert
            Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
            Assert.Equal("Test Content", notFound.Value);
        }

        [Fact]
        public async Task HttpNotFoundObjectResult_ExecuteSuccessful()
        {
            // Arrange
            var input = "Test Content";
            var stream = new MemoryStream();

            var httpResponse = new Mock<HttpResponse>();
            var tempContentType = string.Empty;
            httpResponse.SetupProperty(o => o.ContentType);
            httpResponse.SetupGet(r => r.Body).Returns(stream);

            var actionContext = CreateMockActionContext(httpResponse.Object);
            var notFound = new HttpNotFoundObjectResult(input);

            // Act
            await notFound.ExecuteResultAsync(actionContext);

            // Assert
            httpResponse.VerifySet(r => r.StatusCode = StatusCodes.Status404NotFound);
            Assert.Equal(input.Length, httpResponse.Object.Body.Length);
        }

        private static ActionContext CreateMockActionContext(
            HttpResponse response = null,
            string requestAcceptHeader = "application/*",
            string requestContentType = "application/json",
            string requestAcceptCharsetHeader = "",
            bool respectBrowserAcceptHeader = false)
        {
            var httpContext = new Mock<HttpContext>();
            if (response != null)
            {
                httpContext.Setup(o => o.Response).Returns(response);
            }

            var content = "{name: 'Person Name', Age: 'not-an-age'}";
            var contentBytes = Encoding.UTF8.GetBytes(content);

            var request = new DefaultHttpContext().Request;
            request.Headers["Accept-Charset"] = requestAcceptCharsetHeader;
            request.Headers["Accept"] = requestAcceptHeader;
            request.ContentType = requestContentType;
            request.Body = new MemoryStream(contentBytes);

            httpContext.Setup(o => o.Request).Returns(request);
            httpContext.Setup(o => o.RequestServices).Returns(GetServiceProvider());
            var optionsAccessor = new TestOptionsManager<MvcOptions>();
            optionsAccessor.Value.OutputFormatters.Add(new StringOutputFormatter());
            optionsAccessor.Value.OutputFormatters.Add(new JsonOutputFormatter());
            optionsAccessor.Value.RespectBrowserAcceptHeader = respectBrowserAcceptHeader;
            var actionBindingContextAccessor = new ActionBindingContextAccessor()
            {
                ActionBindingContext = new ActionBindingContext()
                {
                    OutputFormatters = optionsAccessor.Value.OutputFormatters
                }
            };

            httpContext.Setup(o => o.RequestServices.GetService(typeof(IActionBindingContextAccessor)))
                       .Returns(actionBindingContextAccessor);
            httpContext.Setup(o => o.RequestServices.GetService(typeof(IOptions<MvcOptions>)))
                .Returns(optionsAccessor);
            httpContext.Setup(o => o.RequestServices.GetService(typeof(ILogger<ObjectResult>)))
                .Returns(new Mock<ILogger<ObjectResult>>().Object);

            return new ActionContext(httpContext.Object, new RouteData(), new ActionDescriptor());
        }

        private static IServiceProvider GetServiceProvider()
        {
            var options = new MvcOptions();
            var optionsAccessor = new Mock<IOptions<MvcOptions>>();
            optionsAccessor.SetupGet(o => o.Value).Returns(options);

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddInstance(optionsAccessor.Object);
            return serviceCollection.BuildServiceProvider();
        }
    }
}