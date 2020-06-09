// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.SpaServices.Extensions.Tests
{
    public class SpaServicesExtensionsTests
    {
        [Fact]
        public void UseSpa_ThrowsInvalidOperationException_IfRootpathNotSet()
        {
            // Arrange
            var applicationbuilder = GetApplicationBuilder(GetServiceProvider());

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => applicationbuilder.UseSpa(rb => { }));

            Assert.Equal("No RootPath was set on the SpaStaticFilesOptions.", exception.Message);
        }

        private IApplicationBuilder GetApplicationBuilder(IServiceProvider serviceProvider = null)
        {
            if(serviceProvider == null)
            {
                serviceProvider = new Mock<IServiceProvider>(MockBehavior.Strict).Object;
            }

            var applicationbuilderMock = new Mock<IApplicationBuilder>();
            applicationbuilderMock
                .Setup(s => s.ApplicationServices)
                .Returns(serviceProvider);

            return applicationbuilderMock.Object;
        }

        private IServiceProvider GetServiceProvider()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSpaStaticFiles();

            return services.BuildServiceProvider();
        }
    }
}
