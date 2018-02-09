// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.FileProviders;
using System.IO;
using System.Linq;
using Xunit;
using System;
using AngleSharp.Parser.Html;
using Microsoft.AspNetCore.Blazor.Build.Core.FileSystem;

namespace Microsoft.AspNetCore.Blazor.Server.Test
{
    public class IndexHtmlFileProviderTest
    {
        [Fact]
        public void SuppliesNoIndexHtmlFileGivenNoTemplate()
        {
            // Arrange
            var instance = new IndexHtmlFileProvider(
                null, "fakeassembly", Enumerable.Empty<IFileInfo>());

            // Act
            var file = instance.GetFileInfo("/index.html");

            // Assert
            Assert.False(file.Exists);
        }

        [Fact]
        public void SuppliesIndexHtmlFileGivenTemplate()
        {
            // Arrange
            var htmlTemplate = "test";
            var htmlOutput = $"<html><head></head><body>{htmlTemplate}</body></html>";
            var instance = new IndexHtmlFileProvider(
                htmlTemplate, "fakeassembly", Enumerable.Empty<IFileInfo>());

            // Act
            var file = instance.GetFileInfo("/index.html");

            // Assert
            Assert.True(file.Exists);
            Assert.False(file.IsDirectory);
            Assert.Equal("/index.html", file.PhysicalPath);
            Assert.Equal("index.html", file.Name);
            Assert.Equal(htmlOutput, ReadString(file));
            Assert.Equal(htmlOutput.Length, file.Length);
        }

        [Fact]
        public void RootDirectoryContainsOnlyIndexHtml()
        {
            // Arrange
            var htmlTemplate = "test";
            var instance = new IndexHtmlFileProvider(
                htmlTemplate, "fakeassembly", Enumerable.Empty<IFileInfo>());

            // Act
            var directory = instance.GetDirectoryContents(string.Empty);

            // Assert
            Assert.True(directory.Exists);
            Assert.Collection(directory,
                item => Assert.Equal("/index.html", item.PhysicalPath));
        }

        [Fact]
        public void InjectsScriptTagReferencingAssemblyAndDependencies()
        {
            // Arrange
            var htmlTemplate = "<html><body><h1>Hello</h1>Some text</body><script type='blazor-boot'></script></html>";
            var dependencies = new IFileInfo[]
            {
                new TestFileInfo("System.Abc.dll"),
                new TestFileInfo("MyApp.ClassLib.dll"),
            };
            var instance = new IndexHtmlFileProvider(
                htmlTemplate, "MyApp.Entrypoint", dependencies);

            // Act
            var file = instance.GetFileInfo("/index.html");
            var parsedHtml = new HtmlParser().Parse(ReadString(file));
            var firstElem = parsedHtml.Body.FirstElementChild;
            var scriptElem = parsedHtml.Body.QuerySelector("script");

            // Assert
            Assert.Equal("h1", firstElem.TagName.ToLowerInvariant());
            Assert.Equal("script", scriptElem.TagName.ToLowerInvariant());
            Assert.False(scriptElem.HasChildNodes);
            Assert.Equal("/_framework/blazor.js", scriptElem.GetAttribute("src"));
            Assert.Equal("MyApp.Entrypoint.dll", scriptElem.GetAttribute("main"));
            Assert.Equal("System.Abc.dll,MyApp.ClassLib.dll", scriptElem.GetAttribute("references"));
            Assert.False(scriptElem.HasAttribute("type"));
        }

        [Fact]
        public void MissingBootScriptTagReferencingAssemblyAndDependencies()
        {
            // Arrange
            var htmlTemplate = "<html><body><h1>Hello</h1>Some text</body></html>";
            var dependencies = new IFileInfo[]
            {
                new TestFileInfo("System.Abc.dll"),
                new TestFileInfo("MyApp.ClassLib.dll"),
            };
            var instance = new IndexHtmlFileProvider(
                htmlTemplate, "MyApp.Entrypoint", dependencies);

            // Act
            var file = instance.GetFileInfo("/index.html");
            var parsedHtml = new HtmlParser().Parse(ReadString(file));
            var firstElem = parsedHtml.Body.FirstElementChild;
            var scriptElem = parsedHtml.Body.QuerySelector("script");


            // Assert
            Assert.Equal("h1", firstElem.TagName.ToLowerInvariant());
            Assert.Null(scriptElem);
        }

        private static string ReadString(IFileInfo file)
        {
            using (var stream = file.CreateReadStream())
            using (var sr = new StreamReader(stream))
            {
                return sr.ReadToEnd();
            }
        }

        class TestFileInfo : IFileInfo
        {
            public TestFileInfo(string physicalPath)
            {
                PhysicalPath = physicalPath;
            }

            public bool Exists => true;

            public long Length => throw new NotImplementedException();

            public string PhysicalPath { get; }

            public string Name => Path.GetFileName(PhysicalPath);

            public DateTimeOffset LastModified => throw new NotImplementedException();

            public bool IsDirectory => false;

            public Stream CreateReadStream() => throw new NotImplementedException();
        }
    }
}
