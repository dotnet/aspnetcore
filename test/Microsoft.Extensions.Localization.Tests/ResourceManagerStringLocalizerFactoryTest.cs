// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Localization.Test
{
    public class ResourceManagerStringLocalizerFactoryTest
    {
        [Fact]
        public void Create_FromType_ReturnsCachedResultForSameType()
        {
            // Arrange
            var hostingEnvironment = new Mock<IHostingEnvironment>();
            hostingEnvironment.SetupGet(a => a.ApplicationName).Returns("TestApplication");
            var locOptions = new LocalizationOptions();
            var options = new Mock<IOptions<LocalizationOptions>>();
            options.Setup(o => o.Value).Returns(locOptions);
            var factory = new ResourceManagerStringLocalizerFactory(hostingEnvironment.Object, localizationOptions: options.Object);

            // Act
            var result1 = factory.Create(typeof(ResourceManagerStringLocalizerFactoryTest));
            var result2 = factory.Create(typeof(ResourceManagerStringLocalizerFactoryTest));

            // Assert
            Assert.Same(result1, result2);
        }

        [Fact]
        public void Create_FromType_ReturnsNewResultForDifferentType()
        {
            // Arrange
            var hostingEnvironment = new Mock<IHostingEnvironment>();
            hostingEnvironment.SetupGet(a => a.ApplicationName).Returns("TestApplication");
            var locOptions = new LocalizationOptions();
            var options = new Mock<IOptions<LocalizationOptions>>();
            options.Setup(o => o.Value).Returns(locOptions);
            var factory = new ResourceManagerStringLocalizerFactory(hostingEnvironment.Object, localizationOptions: options.Object);

            // Act
            var result1 = factory.Create(typeof(ResourceManagerStringLocalizerFactoryTest));
            var result2 = factory.Create(typeof(LocalizationOptions));

            // Assert
            Assert.NotSame(result1, result2);
        }

        [Fact]
        public void Create_FromNameLocation_ReturnsCachedResultForSameNameLocation()
        {
            // Arrange
            var hostingEnvironment = new Mock<IHostingEnvironment>();
            hostingEnvironment.SetupGet(a => a.ApplicationName).Returns("TestApplication");
            var locOptions = new LocalizationOptions();
            var options = new Mock<IOptions<LocalizationOptions>>();
            options.Setup(o => o.Value).Returns(locOptions);
            var factory = new ResourceManagerStringLocalizerFactory(hostingEnvironment.Object, localizationOptions: options.Object);
            var location = typeof(ResourceManagerStringLocalizer).GetTypeInfo().Assembly.FullName;

            // Act
            var result1 = factory.Create("baseName", location);
            var result2 = factory.Create("baseName", location);

            // Assert
            Assert.Same(result1, result2);
        }

        [Fact]
        public void Create_FromNameLocation_ReturnsNewResultForDifferentName()
        {
            // Arrange
            var hostingEnvironment = new Mock<IHostingEnvironment>();
            hostingEnvironment.SetupGet(a => a.ApplicationName).Returns("TestApplication");
            var locOptions = new LocalizationOptions();
            var options = new Mock<IOptions<LocalizationOptions>>();
            options.Setup(o => o.Value).Returns(locOptions);
            var factory = new ResourceManagerStringLocalizerFactory(hostingEnvironment.Object, localizationOptions: options.Object);
            var location = typeof(ResourceManagerStringLocalizer).GetTypeInfo().Assembly.FullName;

            // Act
            var result1 = factory.Create("baseName1", location);
            var result2 = factory.Create("baseName2", location);

            // Assert
            Assert.NotSame(result1, result2);
        }

        [Fact]
        public void Create_FromNameLocation_ReturnsNewResultForDifferentLocation()
        {
            // Arrange
            var hostingEnvironment = new Mock<IHostingEnvironment>();
            hostingEnvironment.SetupGet(a => a.ApplicationName).Returns("TestApplication");
            var locOptions = new LocalizationOptions();
            var options = new Mock<IOptions<LocalizationOptions>>();
            options.Setup(o => o.Value).Returns(locOptions);
            var factory = new ResourceManagerStringLocalizerFactory(hostingEnvironment.Object, localizationOptions: options.Object);
            var location1 = typeof(ResourceManagerStringLocalizer).GetTypeInfo().Assembly.FullName;
            var location2 = typeof(ResourceManagerStringLocalizerFactoryTest).GetTypeInfo().Assembly.FullName;

            // Act
            var result1 = factory.Create("baseName", location1);
            var result2 = factory.Create("baseName", location2);

            // Assert
            Assert.NotSame(result1, result2);
        }
    }
}
