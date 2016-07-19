// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Http
{
    public class HttpContextFactoryTests
    {
        [Fact]
        public void CreateHttpContextSetsHttpContextAccessor()
        {
            // Arrange
            var accessor = new HttpContextAccessor();
            var contextFactory = new HttpContextFactory(new DefaultObjectPoolProvider(), Options.Create(new FormOptions()), accessor);

            // Act
            var context = contextFactory.Create(new FeatureCollection());

            // Assert
            Assert.True(ReferenceEquals(context, accessor.HttpContext));
        }

        [Fact]
        public void AllowsCreatingContextWithoutSettingAccessor()
        {
            // Arrange
            var contextFactory = new HttpContextFactory(new DefaultObjectPoolProvider(), Options.Create(new FormOptions()));

            // Act & Assert
            var context = contextFactory.Create(new FeatureCollection());
            contextFactory.Dispose(context);
        }

#if NET451
        private static void DomainFunc()
        {
            var accessor = new HttpContextAccessor();
            Assert.Equal(null, accessor.HttpContext);
            accessor.HttpContext = new DefaultHttpContext();
        }

        [Fact]
        public void ChangingAppDomainsDoesNotBreak()
        {
            // Arrange
            var accessor = new HttpContextAccessor();
            var contextFactory = new HttpContextFactory(new DefaultObjectPoolProvider(), Options.Create(new FormOptions()), accessor);
            var domain = AppDomain.CreateDomain("newDomain");

            // Act
            var context = contextFactory.Create(new FeatureCollection());
            domain.DoCallBack(DomainFunc);
            AppDomain.Unload(domain);

            // Assert
            Assert.True(ReferenceEquals(context, accessor.HttpContext));
        }
#endif
    }
}