// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if !ASPNETCORE50
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Microsoft.AspNet.PipelineCore;
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

#if !ASPNETCORE50

        [Fact]
        public void OnActionExecuting_IsNoOp()
        {
            // Arrange
            var filter = new HttpResponseExceptionActionFilter();
            var context = new ActionExecutingContext(new ActionContext(
                            new DefaultHttpContext(),
                            new RouteData(),
                            actionDescriptor: Mock.Of<ActionDescriptor>()),
                            filters: Mock.Of<IList<IFilter>>(),
                            actionArguments: new Dictionary<string, object>());

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

            var context = new ActionExecutedContext(
                new ActionContext(
                            httpContext, 
                            new RouteData(),
                            actionDescriptor: Mock.Of<ActionDescriptor>()),
                filters: null);
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