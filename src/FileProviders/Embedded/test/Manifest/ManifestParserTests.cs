// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.Extensions.FileProviders.Embedded.Manifest
{
    public class ManifestParserTests
    {
        [Fact]
        public void Parse_UsesDefaultManifestNameForManifest()
        {
            // Arrange
            var assembly = new TestAssembly(
                TestEntry.Directory("unused",
                    TestEntry.File("sample.txt")));

            // Act
            var manifest = ManifestParser.Parse(assembly);

            // Assert
            Assert.NotNull(manifest);
        }

        [Fact]
        public void Parse_FindsManifestWithCustomName()
        {
            // Arrange
            var assembly = new TestAssembly(
                TestEntry.Directory("unused",
                    TestEntry.File("sample.txt")),
                manifestName: "Manifest.xml");

            // Act
            var manifest = ManifestParser.Parse(assembly, "Manifest.xml");

            // Assert
            Assert.NotNull(manifest);
        }

        [Fact]
        public void Parse_ThrowsForEntriesWithDifferentCasing()
        {
            // Arrange
            var assembly = new TestAssembly(
                TestEntry.Directory("unused",
                    TestEntry.File("sample.txt"),
                    TestEntry.File("SAMPLE.TXT")));

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => ManifestParser.Parse(assembly));
        }

        [Theory]
        [MemberData(nameof(MalformedManifests))]
        public void Parse_ThrowsForInvalidManifests(string invalidManifest)
        {
            // Arrange
            var assembly = new TestAssembly(invalidManifest);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => ManifestParser.Parse(assembly));
        }

        public static TheoryData<string> MalformedManifests =>
            new TheoryData<string>
            {
                "<Manifest></Manifest>",
                "<Manifest><ManifestVersion></ManifestVersion></Manifest>",
                "<Manifest><ManifestVersion /></Manifest>",
                "<Manifest><ManifestVersion><Version>2.0</Version></ManifestVersion></Manifest>",
                "<Manifest><ManifestVersion>2.0</ManifestVersion></Manifest>",
                @"<Manifest><ManifestVersion>1.0</ManifestVersion>
<FileSystem><File><ResourcePath>path</ResourcePath></File></FileSystem></Manifest>",

                @"<Manifest><ManifestVersion>1.0</ManifestVersion>
<FileSystem><File Name=""sample.txt""><ResourcePath></ResourcePath></File></FileSystem></Manifest>",

                @"<Manifest><ManifestVersion>1.0</ManifestVersion>
<FileSystem><File Name=""sample.txt"">sample.txt</File></FileSystem></Manifest>",

                @"<Manifest><ManifestVersion>1.0</ManifestVersion>
<FileSystem><Directory></Directory></FileSystem></Manifest>",

                @"<Manifest><ManifestVersion>1.0</ManifestVersion>
<FileSystem><Directory Name=""wwwroot""><Unknown /></Directory></FileSystem></Manifest>"
            };

        [Theory]
        [MemberData(nameof(ManifestsWithAdditionalData))]
        public void Parse_IgnoresAdditionalDataOnFileAndDirectoryNodes(string manifest)
        {
            // Arrange
            var assembly = new TestAssembly(manifest);

            // Act
            var result = ManifestParser.Parse(assembly);

            // Assert
            Assert.NotNull(result);
        }

        public static TheoryData<string> ManifestsWithAdditionalData =>
            new TheoryData<string>
            {
                @"<Manifest><ManifestVersion>1.0</ManifestVersion>
<FileSystem><Directory Name=""wwwroot"" AdditionalAttribute=""value""></Directory></FileSystem></Manifest>",

                @"<Manifest><ManifestVersion>1.0</ManifestVersion>
<FileSystem><Directory Name=""wwwroot"" AdditionalAttribute=""value"">
<File Name=""sample.txt"" AdditionalValue=""value""><ResourcePath something=""abc"">path</ResourcePath><hash>1234</hash></File>
</Directory></FileSystem></Manifest>"
            };
    }
}
