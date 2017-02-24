// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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

namespace Microsoft.Extensions.Localization.Tests
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
            var resourceManager = new TestResourceManager(baseName, resourceAssembly.Assembly);
            var resourceStreamManager = new TestResourceStringProvider(resourceNamesCache, resourceAssembly, baseName);
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
            for (int i = 0; i < 5; i++)
            {
                localizer1.GetAllStrings().ToList();
                localizer2.GetAllStrings().ToList();
            }

            // Assert
            var expectedCallCount = GetCultureInfoDepth(CultureInfo.CurrentUICulture);
            Assert.Equal(expectedCallCount, resourceAssembly.GetManifestResourceStreamCallCount);
        }

        [Fact]
        public void EnumeratorCacheIsScopedByAssembly()
        {
            // Arrange
            var resourceNamesCache = new ResourceNamesCache();
            var baseName = "test";
            var resourceAssembly1 = new TestAssemblyWrapper("Assembly1");
            var resourceAssembly2 = new TestAssemblyWrapper("Assembly2");
            var resourceManager1 = new TestResourceManager(baseName, resourceAssembly1.Assembly);
            var resourceManager2 = new TestResourceManager(baseName, resourceAssembly2.Assembly);
            var resourceStreamManager1 = new TestResourceStringProvider(resourceNamesCache, resourceAssembly1, baseName);
            var resourceStreamManager2 = new TestResourceStringProvider(resourceNamesCache, resourceAssembly2, baseName);
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
            Assert.Equal(expectedCallCount, resourceAssembly1.GetManifestResourceStreamCallCount);
            Assert.Equal(expectedCallCount, resourceAssembly2.GetManifestResourceStreamCallCount);
        }

        [Fact]
        public void GetString_PopulatesSearchedLocationOnLocalizedString()
        {
            // Arrange
            var baseName = "Resources.TestResource";
            var resourceNamesCache = new ResourceNamesCache();
            var resourceAssembly = new TestAssemblyWrapper();
            var resourceManager = new TestResourceManager(baseName, resourceAssembly.Assembly);
            var resourceStreamManager = new TestResourceStringProvider(resourceNamesCache, resourceAssembly, baseName);
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
            var resourceManager = new TestResourceManager(baseName, resourceAssembly.Assembly);
            var resourceStreamManager = new TestResourceStringProvider(resourceNamesCache, resourceAssembly, baseName);
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
            Assert.Equal(1, Sink.Writes.Count);
            Assert.Equal("ResourceManagerStringLocalizer searched for 'a key!' in 'Resources.TestResource' with culture 'en-US'.", Sink.Writes.First().State.ToString());
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
            var resourceManager = new TestResourceManager(baseName, resourceAssembly.Assembly);
            var resourceStreamManager = new TestResourceStringProvider(resourceNamesCache, resourceAssembly, baseName);
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
            var resourceAssembly = new TestAssemblyWrapper("Assembly1");
            var resourceManager = new TestResourceManager(baseName, resourceAssembly.Assembly);
            var logger = Logger;

            var localizer = new ResourceManagerWithCultureStringLocalizer(
                resourceManager,
                resourceAssembly.Assembly,
                baseName,
                resourceNamesCache,
                CultureInfo.CurrentCulture,
                logger);

            // Act & Assert
            var exception = Assert.Throws<MissingManifestResourceException>(() =>
            {
                // We have to access the result so it evaluates.
                localizer.GetAllStrings(includeParentCultures).ToArray();
            });
            var expected = includeParentCultures
                ? "No manifests exist for the current culture."
                : $"The manifest 'testington.{CultureInfo.CurrentCulture}.resources' was not found.";
            Assert.Equal(expected, exception.Message);
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

        private ILogger Logger
        {
            get
            {
                return new TestLoggerFactory(Sink, true).CreateLogger<ResourceManagerStringLocalizer>();
            }
        }

        public class TestResourceManager : ResourceManager
        {
            public TestResourceManager(string baseName, Assembly assembly)
                : base(baseName, assembly)
            {
            }

            public override string GetString(string name, CultureInfo culture) => null;
        }

        public class TestResourceStringProvider : AssemblyResourceStringProvider
        {
            private TestAssemblyWrapper _assemblyWrapper;

            public TestResourceStringProvider(
                    IResourceNamesCache resourceCache,
                    TestAssemblyWrapper assemblyWrapper,
                    string resourceBaseName)
                : base(resourceCache, assemblyWrapper, resourceBaseName)
            {
                _assemblyWrapper = assemblyWrapper;
            }

            protected override AssemblyWrapper GetAssembly(CultureInfo culture)
            {
                return _assemblyWrapper;
            }
        }

        public class TestAssemblyWrapper : AssemblyWrapper
        {
            public TestAssemblyWrapper(string name = nameof(TestAssemblyWrapper))
                : base(typeof(TestAssemblyWrapper).GetTypeInfo().Assembly)
            {
                FullName = name;
            }

            public int GetManifestResourceStreamCallCount { get; private set; }

            public override string FullName { get; }

            public override Stream GetManifestResourceStream(string name)
            {
                GetManifestResourceStreamCallCount++;
                return MakeResourceStream();
            }
        }
    }
}
