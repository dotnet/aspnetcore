// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Diagnostics;
using Microsoft.AspNet.Http.Core;
using Microsoft.AspNet.Routing;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core.Test
{
    public class SkipStatusCodePagesAttributeTest
    {
        [Fact]
        public void SkipStatusCodePagesAttribute_TurnsOfStatusCodePages()
        {
            // Arrange
            var skipStatusCodeAttribute = new SkipStatusCodePagesAttribute();
            var resultExecutingContext = CreateResultExecutingContext(new IFilter[] { skipStatusCodeAttribute });
            var statusCodePagesFeature = new TestStatusCodeFeature();
            resultExecutingContext.HttpContext.SetFeature<IStatusCodePagesFeature>(statusCodePagesFeature);

            // Act
            skipStatusCodeAttribute.OnResultExecuted(CreateResultExecutedContext(resultExecutingContext));

            // Assert
            Assert.False(statusCodePagesFeature.Enabled);
        }

        [Fact]
        public void SkipStatusCodePagesAttribute_Does_Not_Throw_If_Feature_Missing()
        {
            // Arrange
            var skipStatusCodeAttribute = new SkipStatusCodePagesAttribute();
            var resultExecutingContext = CreateResultExecutingContext(new IFilter[] { skipStatusCodeAttribute });

            // Act
            skipStatusCodeAttribute.OnResultExecuted(CreateResultExecutedContext(resultExecutingContext));
        }

        private static ResultExecutedContext CreateResultExecutedContext(ResultExecutingContext context)
        {
            return new ResultExecutedContext(context, context.Filters, context.Result, context.Controller);
        }

        private static ResultExecutingContext CreateResultExecutingContext(IFilter[] filters)
        {
            return new ResultExecutingContext(
                CreateActionContext(),
                filters,
                new ObjectResult("Some Value"),
                controller: new object());
        }

        private static ActionContext CreateActionContext()
        {
            return new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        }

        private class TestStatusCodeFeature : IStatusCodePagesFeature
        {
            public bool Enabled { get; set; } = true;
        }
    }
}