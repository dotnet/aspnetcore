// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if !DNXCORE50
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Routing;
using Moq;
#endif
using Xunit;

namespace Microsoft.AspNet.Mvc.WebApiCompatShim
{
    public class HttpResponseExceptionActionFilterTest
    {
        [Fact]
        public void OrderIsSetToMaxValue()
        {
            // Arrange
            var filter = new HttpResponseExceptionActionFilter();
            var expectedFilterOrder = int.MaxValue - 10;

            // Act & Assert
            Assert.Equal(expectedFilterOrder, filter.Order);
        }

#if !DNXCORE50

        [Fact]
        public void OnActionExecuting_IsNoOp()
        {
            // Arrange
            var filter = new HttpResponseExceptionActionFilter();

            var actionContext = new ActionContext(
                                new DefaultHttpContext(),
                                new RouteData(),
                                Mock.Of<ActionDescriptor>());

            var context = new ActionExecutingContext(
                actionContext,
                filters: new List<IFilter>(),
                actionArguments: new Dictionary<string, object>(),
                controller: new object());

            // Act
            filter.OnActionExecuting(context);

            // Assert
            Assert.Null(context.Result);
        }

        [Fact]
        public void OnActionExecuted_HandlesExceptionAndReturnsObjectResult()
        {
            // Arrange
            var filter = new HttpResponseExceptionActionFilter();
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = "GET";

            var actionContext = new ActionContext(
                                httpContext,
                                new RouteData(),
                                Mock.Of<ActionDescriptor>());

            var context = new ActionExecutedContext(
                actionContext,
                filters: new List<IFilter>(),
                controller: new object());

            context.Exception = new HttpResponseException(HttpStatusCode.BadRequest);

            // Act
            filter.OnActionExecuted(context);

            // Assert
            Assert.True(context.ExceptionHandled);
            var result = Assert.IsType<ObjectResult>(context.Result);
            Assert.Equal(typeof(HttpResponseMessage), result.DeclaredType);
            var response = Assert.IsType<HttpResponseMessage>(result.Value);
            Assert.NotNull(response.RequestMessage);
            Assert.Equal(context.HttpContext.GetHttpRequestMessage(), response.RequestMessage);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

#endif

    }
}
