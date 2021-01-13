#pragma warning disable CS0618 // Type or member is obsolete
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Http
{
    public class HttpContextFactoryTests
    {
        [Fact]
        public void ConstructorWithoutServiceScopeFactoryThrows()
        {
            // Arrange
            var accessor = new HttpContextAccessor();
            var exception1 = Assert.Throws<ArgumentNullException>(() => new HttpContextFactory(Options.Create(new FormOptions()), accessor));
            var exception2 = Assert.Throws<ArgumentNullException>(() => new HttpContextFactory(Options.Create(new FormOptions())));

            Assert.Equal("serviceScopeFactory", exception1.ParamName);
            Assert.Equal("serviceScopeFactory", exception2.ParamName);
        }

        [Fact]
        public void CreateHttpContextSetsHttpContextAccessor()
        {
            // Arrange
            var accessor = new HttpContextAccessor();
            var contextFactory = new HttpContextFactory(Options.Create(new FormOptions()), new MyServiceScopeFactory(), accessor);

            // Act
            var context = contextFactory.Create(new FeatureCollection());

            // Assert
            Assert.Same(context, accessor.HttpContext);
        }

        [Fact]
        public void DisposeHttpContextSetsHttpContextAccessorToNull()
        {
            // Arrange
            var accessor = new HttpContextAccessor();
            var contextFactory = new HttpContextFactory(Options.Create(new FormOptions()), new MyServiceScopeFactory(), accessor);

            // Act
            var context = contextFactory.Create(new FeatureCollection());

            // Assert
            Assert.Same(context, accessor.HttpContext);

            contextFactory.Dispose(context);

            Assert.Null(accessor.HttpContext);
        }

        [Fact]
        public void AllowsCreatingContextWithoutSettingAccessor()
        {
            // Arrange
            var contextFactory = new HttpContextFactory(Options.Create(new FormOptions()), new MyServiceScopeFactory());

            // Act & Assert
            var context = contextFactory.Create(new FeatureCollection());
            contextFactory.Dispose(context);
        }

        private class MyServiceScopeFactory : IServiceScopeFactory
        {
            public IServiceScope CreateScope() => null;
        }
    }
}
#pragma warning restore CS0618 // Type or member is obsolete
