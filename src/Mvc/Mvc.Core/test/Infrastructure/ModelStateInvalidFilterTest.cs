// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    public class ModelStateInvalidFilterTest
    {
        [Fact]
        public void OnActionExecuting_NoOpsIfResultIsAlreadySet()
        {
            // Arrange
            var options = new ApiBehaviorOptions
            {
                InvalidModelStateResponseFactory = _ => new BadRequestResult(),
            };
            var filter = new ModelStateInvalidFilter(options, NullLogger.Instance);
            var context = GetActionExecutingContext();
            var expected = new OkResult();
            context.Result = expected;

            // Act
            filter.OnActionExecuting(context);

            // Assert
            Assert.Same(expected, context.Result);
        }

        [Fact]
        public void OnActionExecuting_NoOpsIfModelStateIsValid()
        {
            // Arrange
            var options = new ApiBehaviorOptions
            {
                InvalidModelStateResponseFactory = _ => new BadRequestResult(),
            };
            var filter = new ModelStateInvalidFilter(options, NullLogger.Instance);
            var context = GetActionExecutingContext();

            // Act
            filter.OnActionExecuting(context);

            // Assert
            Assert.Null(context.Result);
        }

        [Fact]
        public void OnActionExecuting_InvokesResponseFactoryIfModelStateIsInvalid()
        {
            // Arrange
            var expected = new BadRequestResult();
            var options = new ApiBehaviorOptions
            {
                InvalidModelStateResponseFactory = _ => expected,
            };
            var filter = new ModelStateInvalidFilter(options, NullLogger.Instance);
            var context = GetActionExecutingContext();
            context.ModelState.AddModelError("some-key", "some-error");

            // Act
            filter.OnActionExecuting(context);

            // Assert
            Assert.Same(expected, context.Result);
        }

        private static ActionExecutingContext GetActionExecutingContext()
        {
            return new ActionExecutingContext(
                new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
                Array.Empty<IFilterMetadata>(),
                new Dictionary<string, object>(),
                new object());
        }
    }
}
