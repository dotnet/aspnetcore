// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Routing
{
    public class RouteBuilderTest
    {
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
