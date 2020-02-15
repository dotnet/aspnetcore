// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MyNamespace;
using Moq;
using Xunit;

// This namespace intentionally matches the default assembly namespace.
namespace Microsoft.Extensions.Localization.Tests
{
    public class TestResourceManagerStringLocalizerFactory : ResourceManagerStringLocalizerFactory
    {
        private ResourceLocationAttribute _resourceLocationAttribute;

        private RootNamespaceAttribute _rootNamespaceAttribute;

        public Assembly Assembly { get; private set; }
        public string BaseName { get; private set; }

        public TestResourceManagerStringLocalizerFactory(
            IOptions<LocalizationOptions> localizationOptions,
            ResourceLocationAttribute resourceLocationAttribute,
            RootNamespaceAttribute rootNamespaceAttribute,
            ILoggerFactory loggerFactory)
            : base(localizationOptions, loggerFactory)
        {
            _resourceLocationAttribute = resourceLocationAttribute;
            _rootNamespaceAttribute = rootNamespaceAttribute;
        }

        protected override ResourceLocationAttribute GetResourceLocationAttribute(Assembly assembly)
        {
            return _resourceLocationAttribute;
        }

        protected override RootNamespaceAttribute GetRootNamespaceAttribute(Assembly assembly)
        {
            return _rootNamespaceAttribute;
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
            var locOptions = new LocalizationOptions();
            var options = new Mock<IOptions<LocalizationOptions>>();
            options.Setup(o => o.Value).Returns(locOptions);

            var resourceLocationAttribute = new ResourceLocationAttribute(Path.Combine("My", "Resources"));
            var loggerFactory = NullLoggerFactory.Instance;
            var typeFactory = new TestResourceManagerStringLocalizerFactory(
                options.Object,
                resourceLocationAttribute,
                rootNamespaceAttribute: null,
                loggerFactory: loggerFactory);
            var stringFactory = new TestResourceManagerStringLocalizerFactory(
                options.Object,
                resourceLocationAttribute,
                rootNamespaceAttribute: null,
                loggerFactory: loggerFactory);
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
            var locOptions = new LocalizationOptions();
            var options = new Mock<IOptions<LocalizationOptions>>();
            options.Setup(o => o.Value).Returns(locOptions);
            var loggerFactory = NullLoggerFactory.Instance;
            var factory = new ResourceManagerStringLocalizerFactory(localizationOptions: options.Object, loggerFactory: loggerFactory);

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
            var locOptions = new LocalizationOptions();
            var options = new Mock<IOptions<LocalizationOptions>>();
            options.Setup(o => o.Value).Returns(locOptions);
            var loggerFactory = NullLoggerFactory.Instance;
            var factory = new ResourceManagerStringLocalizerFactory(localizationOptions: options.Object, loggerFactory: loggerFactory);

            // Act
            var result1 = factory.Create(typeof(ResourceManagerStringLocalizerFactoryTest));
            var result2 = factory.Create(typeof(LocalizationOptions));

            // Assert
            Assert.NotSame(result1, result2);
        }

        [Fact]
        public void Create_ResourceLocationAttribute_RootNamespaceIgnoredWhenNoLocation()
        {
            // Arrange
            var locOptions = new LocalizationOptions();
            var options = new Mock<IOptions<LocalizationOptions>>();
            options.Setup(o => o.Value).Returns(locOptions);
            var loggerFactory = NullLoggerFactory.Instance;

            var resourcePath = Path.Combine("My", "Resources");
            var rootNamespace = nameof(MyNamespace);
            var rootNamespaceAttribute = new RootNamespaceAttribute(rootNamespace);

            var typeFactory = new TestResourceManagerStringLocalizerFactory(
                options.Object,
                resourceLocationAttribute: null,
                rootNamespaceAttribute: rootNamespaceAttribute,
                loggerFactory: loggerFactory);

            var type = typeof(Model);

            // Act
            typeFactory.Create(type);

            // Assert
            Assert.Equal($"{rootNamespace}.{nameof(Model)}", typeFactory.BaseName);
        }

        [Fact]
        public void Create_ResourceLocationAttribute_UsesRootNamespace()
        {
            // Arrange
            var locOptions = new LocalizationOptions();
            var options = new Mock<IOptions<LocalizationOptions>>();
            options.Setup(o => o.Value).Returns(locOptions);
            var loggerFactory = NullLoggerFactory.Instance;

            var resourcePath = Path.Combine("My", "Resources");
            var rootNamespace = nameof(MyNamespace);
            var resourceLocationAttribute = new ResourceLocationAttribute(resourcePath);
            var rootNamespaceAttribute = new RootNamespaceAttribute(rootNamespace);

            var typeFactory = new TestResourceManagerStringLocalizerFactory(
                options.Object,
                resourceLocationAttribute,
                rootNamespaceAttribute,
                loggerFactory);

            var type = typeof(Model);

            // Act
            typeFactory.Create(type);

            // Assert
            Assert.Equal($"{rootNamespace}.My.Resources.{nameof(Model)}", typeFactory.BaseName);
        }

        [Fact]
        public void Create_FromType_ResourcesPathDirectorySeperatorToDot()
        {
            // Arrange
            var locOptions = new LocalizationOptions();
            locOptions.ResourcesPath = Path.Combine("My", "Resources");
            var options = new Mock<IOptions<LocalizationOptions>>();
            options.Setup(o => o.Value).Returns(locOptions);
            var loggerFactory = NullLoggerFactory.Instance;
            var factory = new TestResourceManagerStringLocalizerFactory(
                options.Object,
                resourceLocationAttribute: null,
                rootNamespaceAttribute: null,
                loggerFactory: loggerFactory);

            // Act
            factory.Create(typeof(ResourceManagerStringLocalizerFactoryTest));

            // Assert
            Assert.Equal("Microsoft.Extensions.Localization.Tests.My.Resources." + nameof(ResourceManagerStringLocalizerFactoryTest), factory.BaseName);
        }

        [Fact]
        public void Create_FromNameLocation_ReturnsCachedResultForSameNameLocation()
        {
            // Arrange
            var locOptions = new LocalizationOptions();
            var options = new Mock<IOptions<LocalizationOptions>>();
            options.Setup(o => o.Value).Returns(locOptions);
            var loggerFactory = NullLoggerFactory.Instance;
            var factory = new ResourceManagerStringLocalizerFactory(localizationOptions: options.Object, loggerFactory: loggerFactory);
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
            var locOptions = new LocalizationOptions();
            var options = new Mock<IOptions<LocalizationOptions>>();
            options.Setup(o => o.Value).Returns(locOptions);
            var loggerFactory = NullLoggerFactory.Instance;
            var factory = new ResourceManagerStringLocalizerFactory(localizationOptions: options.Object, loggerFactory: loggerFactory);
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
            var locOptions = new LocalizationOptions();
            var options = new Mock<IOptions<LocalizationOptions>>();
            options.Setup(o => o.Value).Returns(locOptions);
            var loggerFactory = NullLoggerFactory.Instance;
            var factory = new ResourceManagerStringLocalizerFactory(localizationOptions: options.Object, loggerFactory: loggerFactory);
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
            var locOptions = new LocalizationOptions();
            locOptions.ResourcesPath = Path.Combine("My", "Resources");
            var options = new Mock<IOptions<LocalizationOptions>>();
            options.Setup(o => o.Value).Returns(locOptions);
            var loggerFactory = NullLoggerFactory.Instance;
            var factory = new TestResourceManagerStringLocalizerFactory(
                options.Object,
                resourceLocationAttribute: null,
                rootNamespaceAttribute: null,
                loggerFactory: loggerFactory);

            // Act
            var result1 = factory.Create("baseName", location: "Microsoft.Extensions.Localization.Tests");

            // Assert
            Assert.Equal("Microsoft.Extensions.Localization.Tests.My.Resources.baseName", factory.BaseName);
        }

        [Fact]
        public void Create_FromNameLocation_NullLocationThrows()
        {
            // Arrange
            var locOptions = new LocalizationOptions();
            var options = new Mock<IOptions<LocalizationOptions>>();
            options.Setup(o => o.Value).Returns(locOptions);
            var loggerFactory = NullLoggerFactory.Instance;
            var factory = new ResourceManagerStringLocalizerFactory(localizationOptions: options.Object, loggerFactory: loggerFactory);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => factory.Create("baseName", location: null));
        }
    }
}
