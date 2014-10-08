// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.PipelineCore;
using Microsoft.AspNet.Routing;
using Xunit;

namespace System.Web.Http
{
    public class ApiControllerTest
    {
        [Fact]
        public void AccessDependentProperties()
        {
            // Arrange
            var controller = new ConcreteApiController();

            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal();

            var routeContext = new RouteContext(httpContext);
            var actionContext = new ActionContext(routeContext, new ActionDescriptor());

            // Act
            controller.ActionContext = actionContext;

            // Assert
            Assert.Same(httpContext, controller.Context);
            Assert.Same(actionContext.ModelState, controller.ModelState);
            Assert.Same(httpContext.User, controller.User);
        }

        [Fact]
        public void AccessDependentProperties_UnsetContext()
        {
            // Arrange
            var controller = new ConcreteApiController();

            // Act & Assert
            Assert.Null(controller.Context);
            Assert.Null(controller.ModelState);
            Assert.Null(controller.User);
        }

        private class ConcreteApiController : ApiController
        {
        }
    }
}