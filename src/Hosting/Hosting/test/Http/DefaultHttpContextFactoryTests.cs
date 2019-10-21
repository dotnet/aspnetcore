// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Http
{
    public class DefaultHttpContextFactoryTests
    {
        [Fact]
        public void CreateHttpContextSetsHttpContextAccessor()
        {
            // Arrange
            var services = new ServiceCollection()
                .AddOptions()
                .AddHttpContextAccessor()
                .BuildServiceProvider();
            var accessor = services.GetRequiredService<IHttpContextAccessor>();
            var contextFactory = new DefaultHttpContextFactory(services);

            // Act
            var context = contextFactory.Create(new FeatureCollection());

            // Assert
            Assert.Same(context, accessor.HttpContext);
        }

        [Fact]
        public void DisposeHttpContextSetsHttpContextAccessorToNull()
        {
            // Arrange
            var services = new ServiceCollection()
                .AddOptions()
                .AddHttpContextAccessor()
                .BuildServiceProvider();
            var accessor = services.GetRequiredService<IHttpContextAccessor>();
            var contextFactory = new DefaultHttpContextFactory(services);

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
            var services = new ServiceCollection()
                .AddOptions()
                .BuildServiceProvider();
            var contextFactory = new DefaultHttpContextFactory(services);

            // Act & Assert
            var context = contextFactory.Create(new FeatureCollection());
            contextFactory.Dispose(context);
        }

        [Fact]
        public void SetsDefaultPropertiesOnHttpContext()
        {
            // Arrange
            var services = new ServiceCollection()
                .AddOptions()
                .BuildServiceProvider();
            var contextFactory = new DefaultHttpContextFactory(services);

            // Act & Assert
            var context = contextFactory.Create(new FeatureCollection()) as DefaultHttpContext;
            Assert.NotNull(context);
            Assert.NotNull(context.FormOptions);
            Assert.NotNull(context.ServiceScopeFactory);

            Assert.Same(services.GetRequiredService<IServiceScopeFactory>(), context.ServiceScopeFactory);
        }
    }
}
