// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Core.Test
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
                filters,
                new List<IValueProviderFactory>());
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