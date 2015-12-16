// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using Microsoft.Extensions.Localization.Internal;
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
            var localizer1 = new ResourceManagerStringLocalizer(resourceManager, resourceAssembly, baseName, resourceNamesCache);
            var localizer2 = new ResourceManagerStringLocalizer(resourceManager, resourceAssembly, baseName, resourceNamesCache);

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
            var localizer1 = new ResourceManagerStringLocalizer(resourceManager1, resourceAssembly1, baseName, resourceNamesCache);
            var localizer2 = new ResourceManagerStringLocalizer(resourceManager2, resourceAssembly2, baseName, resourceNamesCache);

            // Act
            localizer1.GetAllStrings().ToList();
            localizer2.GetAllStrings().ToList();

            // Assert
            var expectedCallCount = GetCultureInfoDepth(CultureInfo.CurrentUICulture);
            Assert.Equal(expectedCallCount, resourceAssembly1.GetManifestResourceStreamCallCount);
            Assert.Equal(expectedCallCount, resourceAssembly2.GetManifestResourceStreamCallCount);
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
            var localizer = new ResourceManagerStringLocalizer(resourceManager, resourceAssembly, baseName, resourceNamesCache);

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
            var localizer = new ResourceManagerWithCultureStringLocalizer(
                resourceManager,
                resourceAssembly.Assembly,
                baseName,
                resourceNamesCache,
                CultureInfo.CurrentCulture);

            // Act & Assert
            var exception = Assert.Throws<MissingManifestResourceException>(() =>
            {
                // We have to access the result so it evaluates.
                localizer.GetAllStrings(includeParentCultures).ToArray();
            });
            var expected = includeParentCultures
                ? "No manifests exist for the current culture."
                : "The manifest 'testington.en-US.resources' was not found.";
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

        public class TestResourceManager : ResourceManager
        {
            public TestResourceManager(string baseName, Assembly assembly)
                : base(baseName, assembly)
            {

            }

            public override string GetString(string name, CultureInfo culture) => null;
        }

        public class TestAssemblyWrapper : AssemblyWrapper
        {
            private readonly string _name;

            public TestAssemblyWrapper(string name = nameof(TestAssemblyWrapper))
                : base(typeof(TestAssemblyWrapper).GetTypeInfo().Assembly)
            {
                _name = name;
            }

            public int GetManifestResourceStreamCallCount { get; private set; }

            public override string FullName => _name;

            public override Stream GetManifestResourceStream(string name)
            {
                GetManifestResourceStreamCallCount++;
                return MakeResourceStream();
            }
        }
    }
}
