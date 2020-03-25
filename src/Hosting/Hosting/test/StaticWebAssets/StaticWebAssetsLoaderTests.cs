// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Microsoft.AspNetCore.Hosting.StaticWebAssets
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
        public void ResolveManifest_ManifestFromFile()
        {
            // Arrange
            var expectedManifest = @"<StaticWebAssets Version=""1.0"">
  <ContentRoot Path=""/Path"" BasePath=""/BasePath"" />
</StaticWebAssets>
";

            var environment = new HostingEnvironment()
            {
                ApplicationName = "Microsoft.AspNetCore.Hosting"
            };

            // Act
            var manifest = StaticWebAssetsLoader.ResolveManifest(environment, new ConfigurationBuilder().Build());

            // Assert
            Assert.Equal(expectedManifest, new StreamReader(manifest).ReadToEnd());
        }

        [Fact]
        public void ResolveManifest_UsesConfigurationKey_WhenProvided()
        {
            // Arrange
            var expectedManifest = @"<StaticWebAssets Version=""1.0"">
  <ContentRoot Path=""/Path"" BasePath=""/BasePath"" />
</StaticWebAssets>
";
            var path = Path.ChangeExtension(new Uri(typeof(StaticWebAssetsLoader).Assembly.CodeBase).LocalPath, ".StaticWebAssets.xml");
            var environment = new HostingEnvironment()
            {
                ApplicationName = "NonExistingDll"
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>() {
                    [WebHostDefaults.StaticWebAssetsKey] = path
                }).Build();

            // Act
            var manifest = StaticWebAssetsLoader.ResolveManifest(environment, configuration);

            // Assert
            Assert.Equal(expectedManifest, new StreamReader(manifest).ReadToEnd());
        }


        private Stream CreateManifest(string manifestContent)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(manifestContent));
        }
    }
}
