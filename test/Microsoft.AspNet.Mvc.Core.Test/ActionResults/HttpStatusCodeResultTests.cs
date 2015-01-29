// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http.Core;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.WebUtilities;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class HttpStatusCodeResultTests
    {
        [Fact]
        public void HttpStatusCodeResult_ExecuteResultSetsResponseStatusCode()
        {
            // Arrange
            var result = new HttpStatusCodeResult(StatusCodes.Status404NotFound);

            var httpContext = new DefaultHttpContext();
            var routeData = new RouteData();
            var actionDescriptor = new ActionDescriptor();

            var context = new ActionContext(httpContext, routeData, actionDescriptor);

            // Act
            result.ExecuteResult(context);

            // Assert
            Assert.Equal(StatusCodes.Status404NotFound, httpContext.Response.StatusCode);
        }
    }
}