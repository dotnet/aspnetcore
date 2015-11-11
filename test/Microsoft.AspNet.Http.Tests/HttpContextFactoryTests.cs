// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http.Features;
using Xunit;

namespace Microsoft.AspNet.Http.Internal
{
    public class HttpContextFactoryTests
    {
        [Fact]
        public void CreateHttpContextSetsHttpContextAccessor()
        {
            // Arrange
            var accessor = new HttpContextAccessor();
            var contextFactory = new HttpContextFactory(accessor);

            // Act
            var context = contextFactory.Create(new FeatureCollection());

            // Assert
            Assert.True(ReferenceEquals(context, accessor.HttpContext));
        }

        [Fact]
        public void AllowsCreatingContextWithoutSettingAccessor()
        {
            // Arrange
            var contextFactory = new HttpContextFactory();

            // Act & Assert
            var context = contextFactory.Create(new FeatureCollection());
            contextFactory.Dispose(context);
        }
    }
}