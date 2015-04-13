// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if DNX451
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Core;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.WebUtilities;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Microsoft.Framework.Logging.Testing;
using Microsoft.Framework.OptionsModel;
using Moq;
using Xunit;

namespace System.Web.Http
{
    public class InvalidModelStateResultTest
    {
        [Fact]
        public async Task InvalidModelStateResult_SetsStatusCode()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = CreateServices();

            var stream = new MemoryStream();
            httpContext.Response.Body = stream;

            var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            var modelState = new ModelStateDictionary();
            modelState.AddModelError("product.Name", "Name is required.");

            var result = new InvalidModelStateResult(modelState, includeErrorDetail: false);

            // Act
            await result.ExecuteResultAsync(context);

            // Assert
            Assert.Equal(StatusCodes.Status400BadRequest, context.HttpContext.Response.StatusCode);
        }

        [Fact]
        public async Task InvalidModelStateResult_WritesHttpError()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = CreateServices();

            var stream = new MemoryStream();
            httpContext.Response.Body = stream;

            var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            var modelState = new ModelStateDictionary();
            modelState.AddModelError("product.Name", "Name is required.");

            var expected =
                "{\"Message\":\"The request is invalid.\"," +
                "\"ModelState\":{\"product.Name\":[\"Name is required.\"]}}";

            var result = new InvalidModelStateResult(modelState, includeErrorDetail: false);

            // Act
            await result.ExecuteResultAsync(context);

            // Assert
            using (var reader = new StreamReader(stream))
            {
                stream.Seek(0, SeekOrigin.Begin);
                var content = reader.ReadToEnd();
                Assert.Equal(expected, content);
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

            var mockActionBindingContext = new Mock<IScopedInstance<ActionBindingContext>>();

            var bindingContext = new ActionBindingContext { OutputFormatters = options.OutputFormatters };
            mockActionBindingContext
                .SetupGet(o => o.Value)
                .Returns(bindingContext);

            services.Setup(o => o.GetService(typeof(IScopedInstance<ActionBindingContext>)))
                    .Returns(mockActionBindingContext.Object);

            services.Setup(s => s.GetService(typeof(IOptions<MvcOptions>)))
                .Returns(optionsAccessor.Object);

            services.Setup(s => s.GetService(typeof(ILogger<ObjectResult>)))
                .Returns(new Mock<ILogger<ObjectResult>>().Object);

            return services.Object;
        }
    }
}
#endif
