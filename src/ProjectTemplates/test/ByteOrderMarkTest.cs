// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test
{
    public class ByteOrderMarkTest
    {
        private readonly ITestOutputHelper _output;

        public ByteOrderMarkTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [ConditionalTheory]
        [SkipOnHelix("missing files")]
        [InlineData("Web.ProjectTemplates")]
        [InlineData("Web.Spa.ProjectTemplates")]
        public void JSAndJSONInAllTemplates_ShouldNotContainBOM(string projectName)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var projectTemplateDir = Directory.GetParent(currentDirectory).Parent.Parent.Parent.FullName;
            var path = Path.Combine(projectName, "content");
            var directories = Directory.GetDirectories(Path.Combine(projectTemplateDir, path), "*Sharp");

            var filesWithBOMCharactersPresent = false;
            foreach (var directory in directories)
            {
                var files = (IEnumerable<string>)Directory.GetFiles(directory, "*.json");
                files = files.Concat(Directory.GetFiles(directory, "*.js"));
                foreach (var file in files)
                {
                    var filePath = Path.GetFullPath(file);
                    using var fileStream = new FileStream(filePath, FileMode.Open);

                    var bytes = new byte[3];
                    fileStream.Read(bytes, 0, 3);

                    // Check for UTF8 BOM 0xEF,0xBB,0xBF
                    if (bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                    {
                        _output.WriteLine($"File {filePath} has UTF-8 BOM characters.");
                        filesWithBOMCharactersPresent = true;
                    }
                    // Check for UTF16 BOM 0xFF, 0xFE
                    if (bytes[0] == 0xFF && bytes[1] == 0xFE)
                    {
                        _output.WriteLine($"File {filePath} has UTF-16 BOM characters.");
                        filesWithBOMCharactersPresent = true;
                    }
                }
            }

            Assert.False(filesWithBOMCharactersPresent);
        }

        [ConditionalFact]
        [SkipOnHelix("missing files")]
        public void RazorFilesInWebProjects_ShouldContainBOM()
        {
            var projectName = "Web.ProjectTemplates";
            var currentDirectory = Directory.GetCurrentDirectory();
            var projectTemplateDir = Directory.GetParent(currentDirectory).Parent.Parent.Parent.FullName;
            var path = Path.Combine(projectName, "content");
            var directories = Directory.GetDirectories(Path.Combine(projectTemplateDir, path), "*Sharp");

            foreach (var directory in directories)
            {
                var files = (IEnumerable<string>)Directory.GetFiles(directory, "*.cshtml", SearchOption.AllDirectories);
                files = files.Concat(Directory.GetFiles(directory, "*.razor", SearchOption.AllDirectories));
                foreach (var file in files)
                {
                    var filePath = Path.GetFullPath(file);
                    using var fileStream = new FileStream(filePath, FileMode.Open);

                    var bytes = new byte[3];
                    fileStream.Read(bytes, 0, 3);

                    // Check for UTF8 BOM 0xEF,0xBB,0xBF
                    var expectedBytes = Encoding.UTF8.GetPreamble();
                    Assert.True(
                        bytes[0] == expectedBytes[0] && bytes[1] == expectedBytes[1] && bytes[2] == expectedBytes[2],
                        $"File {filePath} doesn't contains UTF-8 BOM characters.");
                }
            }
        }
    }
}
