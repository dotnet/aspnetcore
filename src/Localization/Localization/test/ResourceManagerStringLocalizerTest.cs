// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Localization.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.Extensions.Localization
{
    public class ResourceManagerStringLocalizerTest
    {
        [Fact]
        public void EnumeratorCachesCultureWalkForSameAssembly()
        {
            // Arrange
            var resourceNamesCache = new ResourceNamesCache();
            var baseName = "test";
            var resourceAssembly = new TestAssemblyWrapper();
            var resourceManager = new TestResourceManager(baseName, resourceAssembly);
            var resourceStreamManager = new TestResourceStringProvider(
                resourceNamesCache,
                resourceManager,
                resourceAssembly.Assembly,
                baseName);
            var logger = Logger;
            var localizer1 = new ResourceManagerStringLocalizer(resourceManager,
                resourceStreamManager,
                baseName,
                resourceNamesCache,
                logger);
            var localizer2 = new ResourceManagerStringLocalizer(resourceManager,
                resourceStreamManager,
                baseName,
                resourceNamesCache,
                logger);

            // Act
            for (var i = 0; i < 5; i++)
            {
                localizer1.GetAllStrings().ToList();
                localizer2.GetAllStrings().ToList();
            }

            // Assert
            var expectedCallCount = GetCultureInfoDepth(CultureInfo.CurrentUICulture);
            Assert.Equal(expectedCallCount, resourceAssembly.ManifestResourceStreamCallCount);
        }

        [Fact]
        public void EnumeratorCacheIsScopedByAssembly()
        {
            // Arrange
            var resourceNamesCache = new ResourceNamesCache();
            var baseName = "test";
            var resourceAssembly1 = new TestAssemblyWrapper(typeof(ResourceManagerStringLocalizerTest));
            var resourceAssembly2 = new TestAssemblyWrapper(typeof(ResourceManagerStringLocalizer));
            var resourceManager1 = new TestResourceManager(baseName, resourceAssembly1);
            var resourceManager2 = new TestResourceManager(baseName, resourceAssembly2);
            var resourceStreamManager1 = new TestResourceStringProvider(resourceNamesCache, resourceManager1, resourceAssembly1.Assembly, baseName);
            var resourceStreamManager2 = new TestResourceStringProvider(resourceNamesCache, resourceManager2, resourceAssembly2.Assembly, baseName);
            var logger = Logger;
            var localizer1 = new ResourceManagerStringLocalizer(
                resourceManager1,
                resourceStreamManager1,
                baseName,
                resourceNamesCache,
                logger);
            var localizer2 = new ResourceManagerStringLocalizer(
                resourceManager2,
                resourceStreamManager2,
                baseName,
                resourceNamesCache,
                logger);

            // Act
            localizer1.GetAllStrings().ToList();
            localizer2.GetAllStrings().ToList();

            // Assert
            var expectedCallCount = GetCultureInfoDepth(CultureInfo.CurrentUICulture);
            Assert.Equal(expectedCallCount, resourceAssembly1.ManifestResourceStreamCallCount);
            Assert.Equal(expectedCallCount, resourceAssembly2.ManifestResourceStreamCallCount);
        }

        [Fact]
        public void GetString_PopulatesSearchedLocationOnLocalizedString()
        {
            // Arrange
            var baseName = "Resources.TestResource";
            var resourceNamesCache = new ResourceNamesCache();
            var resourceAssembly = new TestAssemblyWrapper();
            var resourceManager = new TestResourceManager(baseName, resourceAssembly);
            var resourceStreamManager = new TestResourceStringProvider(resourceNamesCache, resourceManager, resourceAssembly.Assembly, baseName);
            var logger = Logger;
            var localizer = new ResourceManagerStringLocalizer(
                resourceManager,
                resourceStreamManager,
                baseName,
                resourceNamesCache,
                logger);

            // Act
            var value = localizer["name"];

            // Assert
            Assert.Equal("Resources.TestResource", value.SearchedLocation);
        }

        [Fact]
        [ReplaceCulture("en-US", "en-US")]
        public void GetString_LogsLocationSearched()
        {
            // Arrange
            var baseName = "Resources.TestResource";
            var resourceNamesCache = new ResourceNamesCache();
            var resourceAssembly = new TestAssemblyWrapper();
            var resourceManager = new TestResourceManager(baseName, resourceAssembly);
            var resourceStreamManager = new TestResourceStringProvider(resourceNamesCache, resourceManager, resourceAssembly.Assembly, baseName);
            var logger = Logger;

            var localizer = new ResourceManagerStringLocalizer(
                resourceManager,
                resourceStreamManager,
                baseName,
                resourceNamesCache,
                logger);

            // Act
            var value = localizer["a key!"];

            // Assert
            var write = Assert.Single(Sink.Writes);
            Assert.Equal("ResourceManagerStringLocalizer searched for 'a key!' in 'Resources.TestResource' with culture 'en-US'.", write.State.ToString());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ResourceManagerStringLocalizer_GetAllStrings_ReturnsExpectedValue(bool includeParentCultures)
        {
            // Arrange
            var baseName = "test";
            var resourceNamesCache = new ResourceNamesCache();
            var resourceAssembly = new TestAssemblyWrapper();
            var resourceManager = new TestResourceManager(baseName, resourceAssembly);
            var resourceStreamManager = new TestResourceStringProvider(resourceNamesCache, resourceManager, resourceAssembly.Assembly, baseName);
            var logger = Logger;
            var localizer = new ResourceManagerStringLocalizer(
                resourceManager,
                resourceStreamManager,
                baseName,
                resourceNamesCache,
                logger);

            // Act
            // We have to access the result so it evaluates.
            var strings = localizer.GetAllStrings(includeParentCultures).ToList();

            // Assert
            var value = Assert.Single(strings);
            Assert.Equal("TestName", value.Value);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ResourceManagerStringLocalizer_GetAllStrings_MissingResourceThrows(bool includeParentCultures)
        {
            // Arrange
            var resourceNamesCache = new ResourceNamesCache();
            var baseName = "testington";
            var resourceAssembly = new TestAssemblyWrapper();
            resourceAssembly.HasResources = false;
            var resourceManager = new TestResourceManager(baseName, resourceAssembly);
            var logger = Logger;

            var localizer = new ResourceManagerStringLocalizer(
                resourceManager,
                resourceAssembly.Assembly,
                baseName,
                resourceNamesCache,
                logger);

            // Act & Assert
            var exception = Assert.Throws<MissingManifestResourceException>(() =>
            {
                // We have to access the result so it evaluates.
                localizer.GetAllStrings(includeParentCultures).ToArray();
            });

            var expectedTries = includeParentCultures ? 3 : 1;
            var expected = includeParentCultures
                ? "No manifests exist for the current culture."
                : $"The manifest 'testington.{CultureInfo.CurrentCulture}.resources' was not found.";
            Assert.Equal(expected, exception.Message);
            Assert.Equal(expectedTries, resourceAssembly.ManifestResourceStreamCallCount);
        }

        private static Stream MakeResourceStream()
        {
            var stream = new MemoryStream();
            var resourceWriter = new ResourceWriter(stream);
            resourceWriter.AddResource("TestName", "value");
            resourceWriter.Generate();
            stream.Position = 0;
            return stream;
        }

        private static int GetCultureInfoDepth(CultureInfo culture)
        {
            var result = 0;
            var currentCulture = culture;

            while (true)
            {
                result++;

                if (currentCulture == currentCulture.Parent)
                {
                    break;
                }

                currentCulture = currentCulture.Parent;
            }

            return result;
        }


        private TestSink Sink { get; } = new TestSink();

        private ILogger Logger => new TestLoggerFactory(Sink, enabled: true).CreateLogger<ResourceManagerStringLocalizer>();

        public class TestResourceManager : ResourceManager
        {
            private AssemblyWrapper _assemblyWrapper;

            public TestResourceManager(string baseName, AssemblyWrapper assemblyWrapper)
                : base(baseName, assemblyWrapper.Assembly)
            {
                _assemblyWrapper = assemblyWrapper;
            }

            public override string GetString(string name, CultureInfo culture) => null;

            public override ResourceSet GetResourceSet(CultureInfo culture, bool createIfNotExists, bool tryParents)
            {
                var resourceStream = _assemblyWrapper.GetManifestResourceStream(BaseName);

                return resourceStream != null ? new ResourceSet(resourceStream) : null;
            }
        }

        public class TestResourceStringProvider : ResourceManagerStringProvider
        {
            public TestResourceStringProvider(
                    IResourceNamesCache resourceCache,
                    TestResourceManager resourceManager,
                    Assembly assembly,
                    string resourceBaseName)
                : base(resourceCache, resourceManager, assembly, resourceBaseName)
            {
            }
        }

        public class TestAssemblyWrapper : AssemblyWrapper
        {
            public TestAssemblyWrapper()
                : this(typeof(TestAssemblyWrapper))
            {
            }

            public TestAssemblyWrapper(Type type)
                : base(type.GetTypeInfo().Assembly)
            {
            }

            public bool HasResources { get; set; } = true;

            public int ManifestResourceStreamCallCount { get; private set; }

            public override Stream GetManifestResourceStream(string name)
            {
                ManifestResourceStreamCallCount++;

                return HasResources ? MakeResourceStream() : null;
            }
        }
    }
}
