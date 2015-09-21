// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Diagnostics;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.Filters;
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
            var resourceExecutingContext = CreateResourceExecutingContext(new IFilterMetadata[] { skipStatusCodeAttribute });
            var statusCodePagesFeature = new TestStatusCodeFeature();
            resourceExecutingContext.HttpContext.Features.Set<IStatusCodePagesFeature>(statusCodePagesFeature);

            // Act
            skipStatusCodeAttribute.OnResourceExecuting(resourceExecutingContext);

            // Assert
            Assert.False(statusCodePagesFeature.Enabled);
        }

        [Fact]
        public void SkipStatusCodePagesAttribute_Does_Not_Throw_If_Feature_Missing()
        {
            // Arrange
            var skipStatusCodeAttribute = new SkipStatusCodePagesAttribute();
            var resourceExecutingContext = CreateResourceExecutingContext(new IFilterMetadata[] { skipStatusCodeAttribute });

            // Act
            skipStatusCodeAttribute.OnResourceExecuting(resourceExecutingContext);
        }

        private static ResourceExecutingContext CreateResourceExecutingContext(IFilterMetadata[] filters)
        {
            return new ResourceExecutingContext(
                CreateActionContext(),
                filters);
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