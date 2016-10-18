// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
{
    public class AcceptedAtActionResultTests
    {
        public static TheoryData<object> ValuesData
        {
            get
            {
                return new TheoryData<object>
                {
                    null,
                    "Test string",
                    new object(),
                };
            }
        }

        [Theory]
        [MemberData(nameof(ValuesData))]
        public void Constructor_InitializesStatusCodeAndValue(object value)
        {
            // Arrange 
            var url = "testAction";

            // Act         
            var result = new AcceptedAtActionResult(
                actionName: url,
                controllerName: null,
                routeValues: null,
                value: value);

            // Assert
            Assert.Equal(StatusCodes.Status202Accepted, result.StatusCode);
            Assert.Same(value, result.Value);
        }

        [Theory]
        [MemberData(nameof(ValuesData))]
        public async Task ExecuteResultAsync_SetsObjectValueOfFormatter(object value)
        {
            // Arrange
            var url = "testAction";
            var formatter = CreateMockFormatter();
            var httpContext = GetHttpContext(formatter);
            object actual = null;
            formatter.Setup(f => f.WriteAsync(It.IsAny<OutputFormatterWriteContext>()))
                .Callback((OutputFormatterWriteContext context) => actual = context.Object)
                .Returns(Task.FromResult(0));

            var actionContext = GetActionContext(httpContext);
            var urlHelper = GetMockUrlHelper(url);

            // Act
            var result = new AcceptedAtActionResult(
                actionName: url,
                controllerName: null,
                routeValues: null,
                value: value);

            result.UrlHelper = urlHelper;
            await result.ExecuteResultAsync(actionContext);

            // Assert
            Assert.Same(value, actual);
        }

        [Fact]
        public async Task ExecuteResultAsync_SetsStatusCodeAndLocationHeader()
        {
            // Arrange
            var expectedUrl = "testAction";
            var formatter = CreateMockFormatter();
            var httpContext = GetHttpContext(formatter);
            var actionContext = GetActionContext(httpContext);
            var urlHelper = GetMockUrlHelper(expectedUrl);

            // Act
            var result = new AcceptedAtActionResult(
                actionName: expectedUrl,
                controllerName: null,
                routeValues: null,
                value: null);

            result.UrlHelper = urlHelper;
            await result.ExecuteResultAsync(actionContext);

            // Assert
            Assert.Equal(StatusCodes.Status202Accepted, httpContext.Response.StatusCode);
            Assert.Equal(expectedUrl, httpContext.Response.Headers["Location"]);
        }

        [Fact]
        public async Task ExecuteResultAsync_ThrowsIfActionUrlIsNull()
        {
            // Arrange
            var formatter = CreateMockFormatter();
            var httpContext = GetHttpContext(formatter);
            var actionContext = GetActionContext(httpContext);
            var urlHelper = GetMockUrlHelper(returnValue: null);

            // Act
            var result = new AcceptedAtActionResult(
                actionName: null,
                controllerName: null,
                routeValues: null,
                value: null);

            result.UrlHelper = urlHelper;

            // Assert
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(() =>
                result.ExecuteResultAsync(actionContext),
                "No route matches the supplied values.");
        }

        private static ActionContext GetActionContext(HttpContext httpContext)
        {
            var routeData = new RouteData();
            routeData.Routers.Add(Mock.Of<IRouter>());

            return new ActionContext(
                httpContext,
                routeData,
                new ActionDescriptor());
        }

        private static HttpContext GetHttpContext(Mock<IOutputFormatter> formatter)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = CreateServices(formatter);
            return httpContext;
        }

        private static Mock<IOutputFormatter> CreateMockFormatter()
        {
            var formatter = new Mock<IOutputFormatter>
            {
                CallBase = true
            };
            formatter.Setup(f => f.CanWriteResult(It.IsAny<OutputFormatterWriteContext>())).Returns(true);

            return formatter;
        }

        private static IServiceProvider CreateServices(Mock<IOutputFormatter> formatter)
        {
            var options = new TestOptionsManager<MvcOptions>();
            options.Value.OutputFormatters.Add(formatter.Object);
            var services = new ServiceCollection();
            services.AddSingleton(new ObjectResultExecutor(
                options,
                new TestHttpResponseStreamWriterFactory(),
                NullLoggerFactory.Instance));

            return services.BuildServiceProvider();
        }

        private static IUrlHelper GetMockUrlHelper(string returnValue)
        {
            var urlHelper = new Mock<IUrlHelper>();
            urlHelper.Setup(o => o.Action(It.IsAny<UrlActionContext>())).Returns(returnValue);

            return urlHelper.Object;
        }
    }
}