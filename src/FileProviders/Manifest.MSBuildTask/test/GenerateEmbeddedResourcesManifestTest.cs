// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Xunit;

namespace Microsoft.Extensions.FileProviders.Embedded.Manifest.Task
{
    public class GenerateEmbeddedResourcesManifestTest
    {
        [Fact]
        public void CreateEmbeddedItems_MapsMetadataFromEmbeddedResources_UsesTheTargetPath()
        {
            // Arrange
            var task = new TestGenerateEmbeddedResourcesManifest();
            var embeddedFiles = CreateEmbeddedResource(
                CreateMetadata(@"lib\js\jquery.validate.js"));

            var expectedItems = new[]
            {
                CreateEmbeddedItem(@"lib\js\jquery.validate.js","lib.js.jquery.validate.js")
            };

            // Act
            var embeddedItems = task.CreateEmbeddedItems(embeddedFiles);

            // Assert
            Assert.Equal(expectedItems, embeddedItems);
        }

        [Fact]
        public void CreateEmbeddedItems_MapsMetadataFromEmbeddedResources_WithLogicalName()
        {
            // Arrange
            var task = new TestGenerateEmbeddedResourcesManifest();
            var DirectorySeparator = (Path.DirectorySeparatorChar == '\\' ? '/' : '\\');
            var embeddedFiles = CreateEmbeddedResource(
                CreateMetadata("site.css", null, "site.css"),
                CreateMetadata("lib/jquery.validate.js", null, $"dist{DirectorySeparator}jquery.validate.js"));

            var expectedItems = new[]
            {
                CreateEmbeddedItem("site.css","site.css"),
                CreateEmbeddedItem(Path.Combine("dist","jquery.validate.js"),$"dist{DirectorySeparator}jquery.validate.js")
            };

            // Act
            var embeddedItems = task.CreateEmbeddedItems(embeddedFiles);

            // Assert
            Assert.Equal(expectedItems, embeddedItems);
        }

        [Fact]
        public void BuildManifest_CanCreatesManifest_ForTopLevelFiles()
        {
            // Arrange
            var task = new TestGenerateEmbeddedResourcesManifest();
            var embeddedFiles = CreateEmbeddedResource(
                CreateMetadata("jquery.validate.js"),
                CreateMetadata("jquery.min.js"),
                CreateMetadata("Site.css"));

            var manifestFiles = task.CreateEmbeddedItems(embeddedFiles);

            var expectedManifest = new Manifest()
            {
                Root = Entry.Directory("").AddRange(
                    Entry.File("jquery.validate.js", "jquery.validate.js"),
                    Entry.File("jquery.min.js", "jquery.min.js"),
                    Entry.File("Site.css", "Site.css"))
            };

            // Act
            var manifest = task.BuildManifest(manifestFiles);

            // Assert
            Assert.Equal(expectedManifest, manifest, ManifestComparer.Instance);
        }

        [Fact]
        public void BuildManifest_CanCreatesManifest_ForFilesWithinAFolder()
        {
            // Arrange
            var task = new TestGenerateEmbeddedResourcesManifest();
            var embeddedFiles = CreateEmbeddedResource(
                CreateMetadata(Path.Combine("wwwroot", "js", "jquery.validate.js")),
                CreateMetadata(Path.Combine("wwwroot", "js", "jquery.min.js")),
                CreateMetadata(Path.Combine("wwwroot", "css", "Site.css")),
                CreateMetadata(Path.Combine("Areas", "Identity", "Views", "Account", "Index.cshtml")));

            var manifestFiles = task.CreateEmbeddedItems(embeddedFiles);

            var expectedManifest = new Manifest()
            {
                Root = Entry.Directory("").AddRange(
                    Entry.Directory("wwwroot").AddRange(
                        Entry.Directory("js").AddRange(
                            Entry.File("jquery.validate.js", "wwwroot.js.jquery.validate.js"),
                            Entry.File("jquery.min.js", "wwwroot.js.jquery.min.js")),
                        Entry.Directory("css").AddRange(
                            Entry.File("Site.css", "wwwroot.css.Site.css"))),
                        Entry.Directory("Areas").AddRange(
                            Entry.Directory("Identity").AddRange(
                                Entry.Directory("Views").AddRange(
                                    Entry.Directory("Account").AddRange(
                                        Entry.File("Index.cshtml", "Areas.Identity.Views.Account.Index.cshtml"))))))
            };

            // Act
            var manifest = task.BuildManifest(manifestFiles);

            // Assert
            Assert.Equal(expectedManifest, manifest, ManifestComparer.Instance);
        }

        [Fact]
        public void BuildManifest_RespectsEntriesWithLogicalName()
        {
            // Arrange
            var task = new TestGenerateEmbeddedResourcesManifest();
            var embeddedFiles = CreateEmbeddedResource(
                CreateMetadata("jquery.validate.js", null, @"wwwroot\lib\js\jquery.validate.js"),
                CreateMetadata("jquery.min.js", null, @"wwwroot\lib/js\jquery.min.js"),
                CreateMetadata("Site.css", null, "wwwroot/lib/css/site.css"));
            var manifestFiles = task.CreateEmbeddedItems(embeddedFiles);

            var expectedManifest = new Manifest()
            {
                Root = Entry.Directory("").AddRange(
                    Entry.Directory("wwwroot").AddRange(
                        Entry.Directory("lib").AddRange(
                            Entry.Directory("js").AddRange(
                                Entry.File("jquery.validate.js", @"wwwroot\lib\js\jquery.validate.js"),
                                Entry.File("jquery.min.js", @"wwwroot\lib/js\jquery.min.js")),
                            Entry.Directory("css").AddRange(
                                Entry.File("site.css", "wwwroot/lib/css/site.css")))))
            };

            // Act
            var manifest = task.BuildManifest(manifestFiles);

            // Assert
            Assert.Equal(expectedManifest, manifest, ManifestComparer.Instance);
        }

        [Fact]
        public void BuildManifest_SupportsFilesAndFoldersWithDifferentCasing()
        {
            // Arrange
            var task = new TestGenerateEmbeddedResourcesManifest();
            var embeddedFiles = CreateEmbeddedResource(
                CreateMetadata(Path.Combine("A", "b", "c.txt")),
                CreateMetadata(Path.Combine("A", "B", "c.txt")),
                CreateMetadata(Path.Combine("A", "B", "C.txt")),
                CreateMetadata(Path.Combine("A", "b", "C.txt")),
                CreateMetadata(Path.Combine("A", "d")),
                CreateMetadata(Path.Combine("A", "D", "e.txt")));

            var manifestFiles = task.CreateEmbeddedItems(embeddedFiles);

            var expectedManifest = new Manifest()
            {
                Root = Entry.Directory("").AddRange(
                    Entry.Directory("A").AddRange(
                        Entry.Directory("b").AddRange(
                            Entry.File("c.txt", @"A.b.c.txt"),
                            Entry.File("C.txt", @"A.b.C.txt")),
                        Entry.Directory("B").AddRange(
                            Entry.File("c.txt", @"A.B.c.txt"),
                            Entry.File("C.txt", @"A.B.C.txt")),
                        Entry.Directory("D").AddRange(
                            Entry.File("e.txt", "A.D.e.txt")),
                        Entry.File("d", "A.d")))
            };

            // Act
            var manifest = task.BuildManifest(manifestFiles);

            // Assert
            Assert.Equal(expectedManifest, manifest, ManifestComparer.Instance);
        }

        [Fact]
        public void BuildManifest_ThrowsInvalidOperationException_WhenTryingToAddAFileWithTheSameNameAsAFolder()
        {
            // Arrange
            var task = new TestGenerateEmbeddedResourcesManifest();
            var embeddedFiles = CreateEmbeddedResource(
                CreateMetadata(Path.Combine("A", "b", "c.txt")),
                CreateMetadata(Path.Combine("A", "b")));

            var manifestFiles = task.CreateEmbeddedItems(embeddedFiles);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => task.BuildManifest(manifestFiles));
        }

        [Fact]
        public void BuildManifest_ThrowsInvalidOperationException_WhenTryingToAddAFolderWithTheSameNameAsAFile()
        {
            // Arrange
            var task = new TestGenerateEmbeddedResourcesManifest();
            var embeddedFiles = CreateEmbeddedResource(
                CreateMetadata(Path.Combine("A", "b")),
                CreateMetadata(Path.Combine("A", "b", "c.txt")));

            var manifestFiles = task.CreateEmbeddedItems(embeddedFiles);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => task.BuildManifest(manifestFiles));
        }

        [Fact]
        public void ToXmlDocument_GeneratesTheCorrectXmlDocument()
        {
            // Arrange
            var manifest = new Manifest()
            {
                Root = Entry.Directory("").AddRange(
                    Entry.Directory("A").AddRange(
                        Entry.Directory("b").AddRange(
                            Entry.File("c.txt", @"A.b.c.txt"),
                            Entry.File("C.txt", @"A.b.C.txt")),
                        Entry.Directory("B").AddRange(
                            Entry.File("c.txt", @"A.B.c.txt"),
                            Entry.File("C.txt", @"A.B.C.txt")),
                        Entry.Directory("D").AddRange(
                            Entry.File("e.txt", "A.D.e.txt")),
                        Entry.File("d", "A.d")))
            };

            var expectedDocument = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XElement("Manifest",
                    new XElement("ManifestVersion", "1.0"),
                    new XElement("FileSystem",
                        new XElement("Directory", new XAttribute("Name", "A"),
                            new XElement("Directory", new XAttribute("Name", "B"),
                                new XElement("File", new XAttribute("Name", "C.txt"), new XElement("ResourcePath", "A.B.C.txt")),
                                new XElement("File", new XAttribute("Name", "c.txt"), new XElement("ResourcePath", "A.B.c.txt"))),
                            new XElement("Directory", new XAttribute("Name", "D"),
                                new XElement("File", new XAttribute("Name", "e.txt"), new XElement("ResourcePath", "A.D.e.txt"))),
                            new XElement("Directory", new XAttribute("Name", "b"),
                                new XElement("File", new XAttribute("Name", "C.txt"), new XElement("ResourcePath", "A.b.C.txt")),
                                new XElement("File", new XAttribute("Name", "c.txt"), new XElement("ResourcePath", "A.b.c.txt"))),
                            new XElement("File", new XAttribute("Name", "d"), new XElement("ResourcePath", "A.d"))))));

            // Act
            var document = manifest.ToXmlDocument();

            // Assert
            Assert.Equal(expectedDocument.ToString(), document.ToString());
        }

        [Fact]
        public void Execute_WritesManifest_ToOutputFile()
        {
            // Arrange
            var task = new TestGenerateEmbeddedResourcesManifest();
            var embeddedFiles = CreateEmbeddedResource(
                    CreateMetadata(Path.Combine("A", "b", "c.txt")),
                    CreateMetadata(Path.Combine("A", "B", "c.txt")),
                    CreateMetadata(Path.Combine("A", "B", "C.txt")),
                    CreateMetadata(Path.Combine("A", "b", "C.txt")),
                    CreateMetadata(Path.Combine("A", "d")),
                    CreateMetadata(Path.Combine("A", "D", "e.txt")));

            task.EmbeddedFiles = embeddedFiles;
            task.ManifestFile = Path.Combine("obj", "debug", "netstandard2.0");

            var expectedDocument = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XElement("Manifest",
                    new XElement("ManifestVersion", "1.0"),
                    new XElement("FileSystem",
                        new XElement("Directory", new XAttribute("Name", "A"),
                            new XElement("Directory", new XAttribute("Name", "B"),
                                new XElement("File", new XAttribute("Name", "C.txt"), new XElement("ResourcePath", "A.B.C.txt")),
                                new XElement("File", new XAttribute("Name", "c.txt"), new XElement("ResourcePath", "A.B.c.txt"))),
                            new XElement("Directory", new XAttribute("Name", "D"),
                                new XElement("File", new XAttribute("Name", "e.txt"), new XElement("ResourcePath", "A.D.e.txt"))),
                            new XElement("Directory", new XAttribute("Name", "b"),
                                new XElement("File", new XAttribute("Name", "C.txt"), new XElement("ResourcePath", "A.b.C.txt")),
                                new XElement("File", new XAttribute("Name", "c.txt"), new XElement("ResourcePath", "A.b.c.txt"))),
                            new XElement("File", new XAttribute("Name", "d"), new XElement("ResourcePath", "A.d"))))));

            var expectedOutput = new MemoryStream();
            var writer = XmlWriter.Create(expectedOutput, new XmlWriterSettings { Encoding = Encoding.UTF8 });
            expectedDocument.WriteTo(writer);
            writer.Flush();
            expectedOutput.Seek(0, SeekOrigin.Begin);

            // Act
            task.Execute();

            // Assert
            task.Output.Seek(0, SeekOrigin.Begin);
            using (var expectedReader = new StreamReader(expectedOutput))
            {
                using (var reader = new StreamReader(task.Output))
                {
                    Assert.Equal(expectedReader.ReadToEnd(), reader.ReadToEnd());
                }
            }
        }

        private EmbeddedItem CreateEmbeddedItem(string manifestPath, string assemblyName) =>
            new EmbeddedItem
            {
                ManifestFilePath = manifestPath,
                AssemblyResourceName = assemblyName
            };


        public class TestGenerateEmbeddedResourcesManifest
            : GenerateEmbeddedResourcesManifest
        {
            public TestGenerateEmbeddedResourcesManifest()
                : this(new MemoryStream())
            {
            }

            public TestGenerateEmbeddedResourcesManifest(Stream output)
            {
                Output = output;
            }

            public Stream Output { get; }

            protected override XmlWriter GetXmlWriter(XmlWriterSettings settings)
            {
                settings.CloseOutput = false;
                return XmlWriter.Create(Output, settings);
            }
        }

        private ITaskItem[] CreateEmbeddedResource(params IDictionary<string, string>[] files) =>
            files.Select(f => CreateTaskItem(f)).ToArray();

        private ITaskItem CreateTaskItem(IDictionary<string, string> metadata)
        {
            var result = new TaskItem();
            foreach (var kvp in metadata)
            {
                result.SetMetadata(kvp.Key, kvp.Value);
            }

            return result;
        }

        private static IDictionary<string, string>
            CreateMetadata(
                string targetPath,
                string manifestResourceName = null,
                string logicalName = null) =>
            new Dictionary<string, string>
            {
                ["TargetPath"] = targetPath,
                ["ManifestResourceName"] = manifestResourceName ?? targetPath.Replace("/", ".").Replace("\\", "."),
                ["LogicalName"] = logicalName ?? targetPath.Replace("/", ".").Replace("\\", "."),
            };

        private class ManifestComparer : IEqualityComparer<Manifest>
        {
            public static IEqualityComparer<Manifest> Instance { get; } = new ManifestComparer();

            public bool Equals(Manifest x, Manifest y)
            {
                return x.Root.Equals(y.Root);
            }

            public int GetHashCode(Manifest obj)
            {
                return obj.Root.GetHashCode();
            }
        }
    }
}
