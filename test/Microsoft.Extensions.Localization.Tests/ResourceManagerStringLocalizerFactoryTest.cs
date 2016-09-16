// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Localization.Tests
{
    public class TestResourceManagerStringLocalizerFactory : ResourceManagerStringLocalizerFactory
    {
        private ResourceLocationAttribute _resourceLocationAttribute;

        public Assembly Assembly { get; private set; }
        public string BaseName { get; private set; }

        public TestResourceManagerStringLocalizerFactory(
            IHostingEnvironment hostingEnvironment,
            IOptions<LocalizationOptions> localizationOptions,
            ResourceLocationAttribute resourceLocationAttribute)
            : base(hostingEnvironment, localizationOptions)
        {
            _resourceLocationAttribute = resourceLocationAttribute;
        }

        protected override ResourceLocationAttribute GetResourceLocationAttribute(Assembly assembly)
        {
            return _resourceLocationAttribute;
        }

        protected override ResourceManagerStringLocalizer CreateResourceManagerStringLocalizer(Assembly assembly, string baseName)
        {
            BaseName = baseName;
            Assembly = assembly;

            return base.CreateResourceManagerStringLocalizer(assembly, baseName);
        }
    }

    public class ResourceManagerStringLocalizerFactoryTest
    {
        [Fact]
        public void Create_OverloadsProduceSameResult()
        {
            // Arrange
            var hostingEnvironment = new Mock<IHostingEnvironment>();
            hostingEnvironment.SetupGet(a => a.ApplicationName).Returns("TestApplication");
            var locOptions = new LocalizationOptions();
            var options = new Mock<IOptions<LocalizationOptions>>();
            options.Setup(o => o.Value).Returns(locOptions);

            var resourceLocationAttribute = new ResourceLocationAttribute(Path.Combine("My", "Resources"));
            var typeFactory = new TestResourceManagerStringLocalizerFactory(
                hostingEnvironment.Object,
                options.Object,
                resourceLocationAttribute);
            var stringFactory = new TestResourceManagerStringLocalizerFactory(
                hostingEnvironment.Object,
                options.Object,
                resourceLocationAttribute);
            var type = typeof(ResourceManagerStringLocalizerFactoryTest);
            var assemblyName = new AssemblyName(type.GetTypeInfo().Assembly.FullName);

            // Act
            typeFactory.Create(type);
            stringFactory.Create(type.Name, assemblyName.Name);

            // Assert
            Assert.Equal(typeFactory.BaseName, stringFactory.BaseName);
            Assert.Equal(typeFactory.Assembly.FullName, stringFactory.Assembly.FullName);
        }

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
        public void Create_FromType_ResourcesPathDirectorySeperatorToDot()
        {
            // Arrange
            var hostingEnvironment = new Mock<IHostingEnvironment>();
            var locOptions = new LocalizationOptions();
            locOptions.ResourcesPath = Path.Combine("My", "Resources");
            var options = new Mock<IOptions<LocalizationOptions>>();
            options.Setup(o => o.Value).Returns(locOptions);
            var factory = new TestResourceManagerStringLocalizerFactory(
                hostingEnvironment.Object,
                options.Object,
                resourceLocationAttribute: null);

            // Act
            factory.Create(typeof(ResourceManagerStringLocalizerFactoryTest));

            // Assert
            Assert.Equal("Microsoft.Extensions.Localization.Tests.My.Resources." + nameof(ResourceManagerStringLocalizerFactoryTest), factory.BaseName);
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
            var location1 = new AssemblyName(typeof(ResourceManagerStringLocalizer).GetTypeInfo().Assembly.FullName).Name;
            var location2 = new AssemblyName(typeof(ResourceManagerStringLocalizerFactoryTest).GetTypeInfo().Assembly.FullName).Name;

            // Act
            var result1 = factory.Create("baseName", location1);
            var result2 = factory.Create("baseName", location2);

            // Assert
            Assert.NotSame(result1, result2);
        }

        [Fact]
        public void Create_FromNameLocation_ResourcesPathDirectorySeparatorToDot()
        {
            // Arrange
            var hostingEnvironment = new Mock<IHostingEnvironment>();
            hostingEnvironment.SetupGet(a => a.ApplicationName).Returns("Microsoft.Extensions.Localization.Tests");
            var locOptions = new LocalizationOptions();
            locOptions.ResourcesPath = Path.Combine("My", "Resources");
            var options = new Mock<IOptions<LocalizationOptions>>();
            options.Setup(o => o.Value).Returns(locOptions);
            var factory = new TestResourceManagerStringLocalizerFactory(
                hostingEnvironment.Object,
                options.Object,
                resourceLocationAttribute: null);

            // Act
            var result1 = factory.Create("baseName", location: null);

            // Assert
            Assert.Equal("Microsoft.Extensions.Localization.Tests.My.Resources.baseName", factory.BaseName);
        }

        [Fact]
        public void Create_FromNameLocation_NullLocationUsesApplicationPath()
        {
            // Arrange
            var hostingEnvironment = new Mock<IHostingEnvironment>();
            hostingEnvironment.SetupGet(a => a.ApplicationName).Returns("Microsoft.Extensions.Localization.Tests");
            var locOptions = new LocalizationOptions();
            var options = new Mock<IOptions<LocalizationOptions>>();
            options.Setup(o => o.Value).Returns(locOptions);
            var factory = new ResourceManagerStringLocalizerFactory(hostingEnvironment.Object, localizationOptions: options.Object);

            // Act
            var result1 = factory.Create("baseName", location: null);
            var result2 = factory.Create("baseName", location: null);

            // Assert
            Assert.Same(result1, result2);
        }
    }
}
