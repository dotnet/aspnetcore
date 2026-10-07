// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.Extensions.Localization;

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
        var writes = Sink.Writes.ToArray();
        Assert.Equal(2, writes.Length);
        Assert.Equal("ResourceManagerStringLocalizer searched for 'a key!' in 'Resources.TestResource' with culture 'en-US'.", writes[0].State.ToString());
        Assert.Equal("A resource for 'a key!' with culture 'en-US' was not found.", writes[1].State.ToString());
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
#pragma warning disable CA1304 // Specify CultureInfo
        var strings = localizer.GetAllStrings(includeParentCultures).ToList();
#pragma warning restore CA1304 // Specify CultureInfo

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
#pragma warning disable CA1304 // Specify CultureInfo
            localizer.GetAllStrings(includeParentCultures).ToArray();
#pragma warning restore CA1304 // Specify CultureInfo
        });

        var expectedTries = includeParentCultures ? GetCultureInfoDepth(CultureInfo.CurrentUICulture) : 1;
        string cultureName = CultureInfo.CurrentCulture.ToString();
        string expectedManifestFileName = cultureName.Length > 0 ? $"testington.{cultureName}.resources" : $"testington.resources";
        var expected = includeParentCultures
            ? "No manifests exist for the current culture."
            : $"The manifest '{expectedManifestFileName}' was not found.";
        Assert.Equal(expected, exception.Message);
        Assert.Equal(expectedTries, resourceAssembly.ManifestResourceStreamCallCount);
    }

    [Fact]
    [ReplaceCulture("en-US", "en-US")]
    public void GetString_LogsResourceNotFound_WhenResourceIsMissing()
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
        Assert.True(value.ResourceNotFound);

        var write = Assert.Single(Sink.Writes, w => w.EventId.Name == "ResourceNotFound");

        Assert.Equal(LogLevel.Debug, write.LogLevel);
        Assert.Equal("A resource for 'a key!' with culture 'en-US' was not found.", write.State.ToString());
    }

    [Fact]
    [ReplaceCulture("fr-FR", "fr-FR")]
    public void GetString_LogsResourceNotFound_IncludesCurrentUICulture()
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
        var write = Assert.Single(Sink.Writes, w => w.EventId.Name == "ResourceNotFound");
        Assert.Equal("A resource for 'a key!' with culture 'fr-FR' was not found.", write.State.ToString());
    }

    [Fact]
    [ReplaceCulture("en-US", "en-US")]
    public void GetString_DoesNotLogResourceNotFound_WhenResourceIsFound()
    {
        // Arrange
        var baseName = "Resources.TestResource";
        var resourceNamesCache = new ResourceNamesCache();
        var resourceAssembly = new TestAssemblyWrapper();
        var resourceManager = new TestResourceManager(baseName, resourceAssembly, new Dictionary<string, string> { ["a key!"] = "a value!" });
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
        Assert.Equal("a value!", value.Value);
        Assert.False(value.ResourceNotFound);
        Assert.DoesNotContain(Sink.Writes, w => w.EventId.Name == "ResourceNotFound");
    }

    [Fact]
    [ReplaceCulture("en-US", "en-US")]
    public void GetString_LogsResourceNotFoundOnce_WhenManifestIsMissing()
    {
        // Arrange
        var baseName = "Resources.TestResource";
        var resourceNamesCache = new ResourceNamesCache();
        var resourceAssembly = new TestAssemblyWrapper();
        var resourceManager = new TestResourceManager(baseName, resourceAssembly) { ThrowMissingManifest = true };
        var resourceStreamManager = new TestResourceStringProvider(resourceNamesCache, resourceManager, resourceAssembly.Assembly, baseName);
        var logger = Logger;

        var localizer = new ResourceManagerStringLocalizer(
            resourceManager,
            resourceStreamManager,
            baseName,
            resourceNamesCache,
            logger);

        // Act
        _ = localizer["a key!"];
        _ = localizer["a key!"];

        // Assert
        var writes = Sink.Writes.Where(w => w.EventId.Name == "ResourceNotFound").ToList();

        Assert.Equal(2, writes.Count);
        Assert.All(writes, w => Assert.Equal("A resource for 'a key!' with culture 'en-US' was not found.", w.State.ToString()));
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

            // Under LC_ALL=C on Linux, the current culture is an invariant culture, but its Parent does
            // not refer to itself (https://github.com/dotnet/runtime/issues/94505).
            // Avoid counting it as 2 cultures by directly checking for equality against the InvariantCulture.
            if (CultureInfo.InvariantCulture.Equals(currentCulture))
            {
                break;
            }

            currentCulture = currentCulture.Parent;
        }

        return result;
    }

    private TestSink Sink { get; } = new TestSink();

    private ILogger Logger => new TestLoggerFactory(Sink, enabled: true).CreateLogger<ResourceManagerStringLocalizer>();

    internal class TestResourceManager(string baseName, AssemblyWrapper assemblyWrapper, IDictionary<string, string>? strings) : ResourceManager(baseName, assemblyWrapper.Assembly)
    {
        public TestResourceManager(string baseName, AssemblyWrapper assemblyWrapper)
            : this(baseName, assemblyWrapper, strings: null)
        {
        }

        public bool ThrowMissingManifest { get; set; }

        public override string? GetString(string name, CultureInfo? culture)
        {
            if (ThrowMissingManifest)
            {
                throw new MissingManifestResourceException();
            }

            return strings is not null && strings.TryGetValue(name, out var value) ? value : null;
        }

        public override ResourceSet? GetResourceSet(CultureInfo culture, bool createIfNotExists, bool tryParents)
        {
            var resourceStream = assemblyWrapper.GetManifestResourceStream(BaseName);

            return resourceStream != null ? new ResourceSet(resourceStream) : null;
        }
    }

    internal class TestResourceStringProvider : ResourceManagerStringProvider
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

    internal class TestAssemblyWrapper : AssemblyWrapper
    {
        public TestAssemblyWrapper()
            : this(typeof(TestAssemblyWrapper))
        {
        }

        public TestAssemblyWrapper(Type type)
            : base(type.Assembly)
        {
        }

        public bool HasResources { get; set; } = true;

        public int ManifestResourceStreamCallCount { get; private set; }

        public override Stream? GetManifestResourceStream(string name)
        {
            ManifestResourceStreamCallCount++;

            return HasResources ? MakeResourceStream() : null;
        }
    }
}
