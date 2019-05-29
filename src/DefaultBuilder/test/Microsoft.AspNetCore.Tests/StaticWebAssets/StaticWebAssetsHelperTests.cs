// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace Microsoft.AspNetCore.Tests.StaticWebAssets
{
    public class StaticWebAssetsHelperTests
    {
        [Fact]
        public void ParseManifest_ThrowsFor_EmptyManifest()
        {
            // Arrange
            var manifestContent = @"";
            var manifest = CreateManifest(manifestContent);

            // Act & Assert
            var exception = Assert.Throws<XmlException>(() => StaticWebAssetsHelper.Parse(manifest).ToArray());
            Assert.StartsWith("Root element is missing.", exception.Message);
        }

        [Fact]
        public void ParseManifest_ThrowsFor_UnknownRootElement()
        {
            // Arrange
            var manifestContent = @"<Invalid />";
            var manifest = CreateManifest(manifestContent);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => StaticWebAssetsHelper.Parse(manifest).ToArray());
            Assert.StartsWith("Invalid manifest", exception.Message);
        }

        [Fact]
        public void ParseManifest_ThrowsFor_MissingVersion()
        {
            // Arrange
            var manifestContent = @"<StaticWebAssets />";
            var manifest = CreateManifest(manifestContent);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => StaticWebAssetsHelper.Parse(manifest).ToArray());
            Assert.StartsWith("Invalid manifest", exception.Message);
        }

        [Fact]
        public void ParseManifest_ThrowsFor_UnknownVersion()
        {
            // Arrange
            var manifestContent = @"<StaticWebAssets Version=""2.0""/>";
            var manifest = CreateManifest(manifestContent);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => StaticWebAssetsHelper.Parse(manifest).ToArray());
            Assert.StartsWith("Unknown manifest version", exception.Message);
        }

        [Fact]
        public void ParseManifest_ThrowsFor_InvalidStaticWebAssetsChildren()
        {
            // Arrange
            var manifestContent = @"<StaticWebAssets Version=""1.0"">
    <Invalid />
</StaticWebAssets>";
            var manifest = CreateManifest(manifestContent);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => StaticWebAssetsHelper.Parse(manifest).ToArray());
            Assert.StartsWith("Invalid manifest", exception.Message);
        }

        [Fact]
        public void ParseManifest_ThrowsFor_MissingBasePath()
        {
            // Arrange
            var manifestContent = @"<StaticWebAssets Version=""1.0"">
    <ContentRoot Path=""/Path"" />
</StaticWebAssets>";

            var manifest = CreateManifest(manifestContent);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => StaticWebAssetsHelper.Parse(manifest).ToArray());
            Assert.StartsWith("Invalid manifest", exception.Message);
        }

        [Fact]
        public void ParseManifest_ThrowsFor_MissingPath()
        {
            // Arrange
            var manifestContent = @"<StaticWebAssets Version=""1.0"">
    <ContentRoot BasePath=""/BasePath"" />
</StaticWebAssets>";

            var manifest = CreateManifest(manifestContent);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => StaticWebAssetsHelper.Parse(manifest).ToArray());
            Assert.StartsWith("Invalid manifest", exception.Message);
        }

        [Fact]
        public void ParseManifest_ThrowsFor_ChildContentRootContent()
        {
            // Arrange
            var manifestContent = @"<StaticWebAssets Version=""1.0"">
    <ContentRoot Path=""/Path"" BasePath=""/BasePath"">
    </ContentRoot>
</StaticWebAssets>";

            var manifest = CreateManifest(manifestContent);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => StaticWebAssetsHelper.Parse(manifest).ToArray());
            Assert.StartsWith("Invalid manifest", exception.Message);
        }

        [Fact]
        public void ParseManifest_ParsesManifest_WithSingleItem()
        {
            // Arrange
            var manifestContent = @"<StaticWebAssets Version=""1.0"">
    <ContentRoot Path=""/Path"" BasePath=""/BasePath"" />
</StaticWebAssets>";

            var manifest = CreateManifest(manifestContent);

            // Act
            var mappings = StaticWebAssetsHelper.Parse(manifest).ToArray();

            // Assert
            var mapping = Assert.Single(mappings);
            Assert.Equal("/Path", mapping.Path);
            Assert.Equal("/BasePath", mapping.BasePath);
        }

        [Fact]
        public void UseStaticWebAssetsCore_CreatesCompositeRoot_WhenThereAreContentRootsInTheManifest()
        {
            // Arrange
            var manifestContent = @$"<StaticWebAssets Version=""1.0"">
    <ContentRoot Path=""{AppContext.BaseDirectory}"" BasePath=""/BasePath"" />
</StaticWebAssets>";

            var manifest = CreateManifest(manifestContent);
            var originalRoot = new NullFileProvider();
            var environment = new HostingEnvironment()
            {
                WebRootFileProvider = originalRoot
            };

            // Act
            StaticWebAssetsHelper.UseStaticWebAssetsCore(environment, manifest);

            // Assert
            var composite = Assert.IsType<CompositeFileProvider>(environment.WebRootFileProvider);
            Assert.Equal(2, composite.FileProviders.Count());
            Assert.Equal(originalRoot, composite.FileProviders.First());
        }

        [Fact]
        public void UseStaticWebAssetsCore_DoesNothing_WhenManifestDoesNotContainEntries()
        {
            // Arrange
            var manifestContent = @$"<StaticWebAssets Version=""1.0"">
</StaticWebAssets>";

            var manifest = CreateManifest(manifestContent);
            var originalRoot = new NullFileProvider();
            var environment = new HostingEnvironment()
            {
                WebRootFileProvider = originalRoot
            };

            // Act
            StaticWebAssetsHelper.UseStaticWebAssetsCore(environment, manifest);

            // Assert
            Assert.Equal(originalRoot, environment.WebRootFileProvider);
        }

        [Fact]
        public void ResolveManifest_FindsEmbeddedManifestProvider()
        {
            // Arrange
            var expectedManifest = @"<StaticWebAssets Version=""1.0"">
  <ContentRoot Path=""/Path"" BasePath=""/BasePath"" />
</StaticWebAssets>
";
            var originalRoot = new NullFileProvider();
            var environment = new HostingEnvironment()
            {
                ApplicationName = typeof(StaticWebAssetsHelperTests).Assembly.GetName().Name
            };

            // Act
            var manifest = StaticWebAssetsHelper.ResolveManifest(environment);

            // Assert
            Assert.Equal(expectedManifest, new StreamReader(manifest).ReadToEnd());
        }

        [Fact]
        public void ResolveManifest_ManifestFromFile()
        {
            // Arrange
            var expectedManifest = @"<StaticWebAssets Version=""1.0"">
  <ContentRoot Path=""/Path"" BasePath=""/BasePath"" />
</StaticWebAssets>
";

            var environment = new HostingEnvironment()
            {
                ApplicationName = "Microsoft.AspNetCore.TestHost"
            };

            // Act
            var manifest = StaticWebAssetsHelper.ResolveManifest(environment);

            // Assert
            Assert.Equal(expectedManifest, new StreamReader(manifest).ReadToEnd());
        }

        private Stream CreateManifest(string manifestContent)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(manifestContent));
        }
    }
}
