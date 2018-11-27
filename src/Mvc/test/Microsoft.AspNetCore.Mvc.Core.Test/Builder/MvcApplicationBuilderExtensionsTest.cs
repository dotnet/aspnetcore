// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Core.Builder
{
    public class MvcApplicationBuilderExtensionsTest
    {
        [Fact]
        public void UseMvc_ThrowsInvalidOperationException_IfMvcMarkerServiceIsNotRegistered()
        {
            // Arrange
            var applicationBuilderMock = new Mock<IApplicationBuilder>();
            applicationBuilderMock
                .Setup(s => s.ApplicationServices)
                .Returns(Mock.Of<IServiceProvider>());

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => applicationBuilderMock.Object.UseMvc(rb => { }));

            Assert.Equal(
                "Unable to find the required services. Please add all the required services by calling " +
                "'IServiceCollection.AddMvc' inside the call to 'ConfigureServices(...)' " +
                "in the application startup code.",
                exception.Message);
        }
    }
}
