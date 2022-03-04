// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Xunit;

namespace Microsoft.AspNetCore.Hosting.StaticWebAssets
{
    public class StaticWebAssetsReaderTests
    {
        [Fact]
        public void ParseManifest_ThrowsFor_EmptyManifest()
        {
            // Arrange
            var manifestContent = @"";
            var manifest = CreateManifest(manifestContent);

            // Act & Assert
            var exception = Assert.Throws<XmlException>(() => StaticWebAssetsReader.Parse(manifest).ToArray());
            Assert.StartsWith("Root element is missing.", exception.Message);
        }

        [Fact]
        public void ParseManifest_ThrowsFor_UnknownRootElement()
        {
            // Arrange
            var manifestContent = @"<Invalid />";
            var manifest = CreateManifest(manifestContent);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => StaticWebAssetsReader.Parse(manifest).ToArray());
            Assert.StartsWith("Invalid manifest", exception.Message);
        }

        [Fact]
        public void ParseManifest_ThrowsFor_MissingVersion()
        {
            // Arrange
            var manifestContent = @"<StaticWebAssets />";
            var manifest = CreateManifest(manifestContent);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => StaticWebAssetsReader.Parse(manifest).ToArray());
            Assert.StartsWith("Invalid manifest", exception.Message);
        }

        [Fact]
        public void ParseManifest_ThrowsFor_UnknownVersion()
        {
            // Arrange
            var manifestContent = @"<StaticWebAssets Version=""2.0""/>";
            var manifest = CreateManifest(manifestContent);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => StaticWebAssetsReader.Parse(manifest).ToArray());
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
            var exception = Assert.Throws<InvalidOperationException>(() => StaticWebAssetsReader.Parse(manifest).ToArray());
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
            var exception = Assert.Throws<InvalidOperationException>(() => StaticWebAssetsReader.Parse(manifest).ToArray());
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
            var exception = Assert.Throws<InvalidOperationException>(() => StaticWebAssetsReader.Parse(manifest).ToArray());
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
            var exception = Assert.Throws<InvalidOperationException>(() => StaticWebAssetsReader.Parse(manifest).ToArray());
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
            var mappings = StaticWebAssetsReader.Parse(manifest).ToArray();

            // Assert
            var mapping = Assert.Single(mappings);
            Assert.Equal("/Path", mapping.Path);
            Assert.Equal("/BasePath", mapping.BasePath);
        }

        private Stream CreateManifest(string manifestContent)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(manifestContent));
        }
    }
}
