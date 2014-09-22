// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class EmptyResultTests
    {
        [Fact]
        public void EmptyResult_ExecuteResult_IsANoOp()
        {
            // Arrange
            var emptyResult = new EmptyResult();

            var httpContext = new Mock<HttpContext>(MockBehavior.Strict);
            var routeData = new RouteData();
            var actionDescriptor = new ActionDescriptor();

            var context = new ActionContext(httpContext.Object, routeData, actionDescriptor);

            // Act & Assert
            Assert.DoesNotThrow(() => emptyResult.ExecuteResult(context));
        }
    }
}