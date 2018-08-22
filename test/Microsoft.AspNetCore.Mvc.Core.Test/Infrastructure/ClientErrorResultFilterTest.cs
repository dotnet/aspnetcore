// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    public class ClientErrorResultFilterTest
    {
        private static readonly IActionResult Result = new EmptyResult();

        [Fact]
        public void OnResultExecuting_DoesNothing_IfActionIsNotClientErrorActionResult()
        {
            // Arrange
            var actionResult = new NotFoundObjectResult(new object());
            var context = GetContext(actionResult);
            var filter = GetFilter();

            // Act
            filter.OnResultExecuting(context);

            // Assert
            Assert.Same(actionResult, context.Result);
        }

        [Fact]
        public void OnResultExecuting_DoesNothing_IfStatusCodeDoesNotExistInApiBehaviorOptions()
        {
            // Arrange
            var actionResult = new NotFoundResult();
            var context = GetContext(actionResult);
            var filter = GetFilter(new ApiBehaviorOptions());

            // Act
            filter.OnResultExecuting(context);

            // Assert
            Assert.Same(actionResult, context.Result);
        }

        [Fact]
        public void OnResultExecuting_DoesNothing_IfResultDoesNotHaveStatusCode()
        {
            // Arrange
            var actionResult = new Mock<IActionResult>()
                .As<IClientErrorActionResult>()
                .Object;
            var context = GetContext(actionResult);
            var filter = GetFilter(new ApiBehaviorOptions());

            // Act
            filter.OnResultExecuting(context);

            // Assert
            Assert.Same(actionResult, context.Result);
        }

        [Fact]
        public void OnResultExecuting_TransformsClientErrors()
        {
            // Arrange
            var actionResult = new NotFoundResult();
            var context = GetContext(actionResult);
            var filter = GetFilter();

            // Act
            filter.OnResultExecuting(context);

            // Assert
            Assert.Same(Result, context.Result);
        }

        private static ClientErrorResultFilter GetFilter(ApiBehaviorOptions options = null)
        {
            var apiBehaviorOptions = options ?? GetOptions();
            var filter = new ClientErrorResultFilter(apiBehaviorOptions, NullLogger<ClientErrorResultFilter>.Instance);
            return filter;
        }

        private static ApiBehaviorOptions GetOptions()
        {
            var apiBehaviorOptions = new ApiBehaviorOptions();
            apiBehaviorOptions.ClientErrorFactory[404] = _ => Result;
            return apiBehaviorOptions;
        }

        private static ResultExecutingContext GetContext(IActionResult actionResult)
        {
            return new ResultExecutingContext(
                new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
                Array.Empty<IFilterMetadata>(),
                actionResult,
                new object());
        }
    }
}
