// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Routing
{
    public class RouteBuilderTest
    {
        [Fact]
        public void Ctor_SetsPropertyValues()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(typeof(RoutingMarkerService));
            var applicationServices = services.BuildServiceProvider();
            var applicationBuilderMock = new Mock<IApplicationBuilder>();
            applicationBuilderMock.Setup(a => a.ApplicationServices).Returns(applicationServices);
            var applicationBuilder = applicationBuilderMock.Object;
            var defaultHandler = Mock.Of<IRouter>();

            // Act
            var builder = new RouteBuilder(applicationBuilder, defaultHandler);

            // Assert
            Assert.Same(applicationBuilder, builder.ApplicationBuilder);
            Assert.Same(defaultHandler, builder.DefaultHandler);
            Assert.Same(applicationServices, builder.ServiceProvider);
        }

        [Fact]
        public void Ctor_ThrowsInvalidOperationException_IfRoutingMarkerServiceIsNotRegistered()
        {
            // Arrange
            var applicationBuilderMock = new Mock<IApplicationBuilder>();
            applicationBuilderMock
                .Setup(s => s.ApplicationServices)
                .Returns(Mock.Of<IServiceProvider>());

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => new RouteBuilder(applicationBuilderMock.Object));

            Assert.Equal(
                "Unable to find the required services. Please add all the required services by calling " +
                "'IServiceCollection.AddRouting' inside the call to 'ConfigureServices(...)'" +
                " in the application startup code.",
                exception.Message);
        }
    }
}
