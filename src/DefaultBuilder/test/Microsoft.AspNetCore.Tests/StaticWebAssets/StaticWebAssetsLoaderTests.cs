// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace Microsoft.AspNetCore.Tests
{
    public class StaticWebAssetsLoaderTests
    {
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
            StaticWebAssetsLoader.UseStaticWebAssetsCore(environment, manifest);

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
            StaticWebAssetsLoader.UseStaticWebAssetsCore(environment, manifest);

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
                ApplicationName = typeof(StaticWebAssetsReaderTests).Assembly.GetName().Name
            };

            // Act
            var manifest = StaticWebAssetsLoader.ResolveManifest(environment);

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
            var manifest = StaticWebAssetsLoader.ResolveManifest(environment);

            // Assert
            Assert.Equal(expectedManifest, new StreamReader(manifest).ReadToEnd());
        }

        private Stream CreateManifest(string manifestContent)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(manifestContent));
        }
    }
}
