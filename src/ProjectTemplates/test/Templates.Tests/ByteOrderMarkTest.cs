// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.InternalTesting;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test;

public class ByteOrderMarkTest : LoggedTest
{
    private ITestOutputHelper _output;
    public ITestOutputHelper Output
    {
        get
        {
            if (_output == null)
            {
                _output = new TestOutputLogger(Logger);
            }
            return _output;
        }
    }

    public ByteOrderMarkTest()
    {
    }

    [Theory]
    [InlineData("Web.ProjectTemplates")]
    [InlineData("Web.ItemTemplates")]
    [InlineData("Web.Client.ItemTemplates")]
    public void JSAndJSONInAllTemplates_ShouldNotContainBOM(string projectName)
    {
        var templateDirectoryPath = GetTemplateDirectoryPath(projectName);

        var filesWithBOMCharactersPresent = false;
        var files = (IEnumerable<string>)Directory.GetFiles(templateDirectoryPath, "*.json");
        files = files.Concat(Directory.GetFiles(templateDirectoryPath, "*.js"));

        foreach (var file in files)
        {
            var filePath = Path.GetFullPath(file);
            using var fileStream = new FileStream(filePath, FileMode.Open);

            var bytes = new byte[3];
            fileStream.Read(bytes, 0, 3);

            // Check for UTF8 BOM 0xEF,0xBB,0xBF
            if (bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                Output.WriteLine($"File {filePath} has UTF-8 BOM characters.");
                filesWithBOMCharactersPresent = true;
            }
            // Check for UTF16 BOM 0xFF, 0xFE
            if (bytes[0] == 0xFF && bytes[1] == 0xFE)
            {
                Output.WriteLine($"File {filePath} has UTF-16 BOM characters.");
                filesWithBOMCharactersPresent = true;
            }
        }

        Assert.False(filesWithBOMCharactersPresent);
    }

    [Theory]
    [InlineData("Web.ProjectTemplates")]
    [InlineData("Web.ItemTemplates")]
    [InlineData("Web.Client.ItemTemplates")]
    public void RazorFilesInWebProjects_ShouldContainBOM(string projectName)
    {
        var templateDirectoryPath = GetTemplateDirectoryPath(projectName);

        var nonBOMFilesPresent = false;

        var files = (IEnumerable<string>)Directory.GetFiles(templateDirectoryPath, "*.cshtml", SearchOption.AllDirectories);
        files = files.Concat(Directory.GetFiles(templateDirectoryPath, "*.razor", SearchOption.AllDirectories));

        foreach (var file in files)
        {
            var filePath = Path.GetFullPath(file);
            using var fileStream = new FileStream(filePath, FileMode.Open);

            var bytes = new byte[3];
            fileStream.Read(bytes, 0, 3);

            // Check for UTF8 BOM 0xEF,0xBB,0xBF
            var expectedBytes = Encoding.UTF8.GetPreamble();
            if (bytes[0] != expectedBytes[0] || bytes[1] != expectedBytes[1] || bytes[2] != expectedBytes[2])
            {
                Output.WriteLine($"File {filePath} does not have UTF-8 BOM characters.");
                nonBOMFilesPresent = true;
            }
        }

        Assert.False(nonBOMFilesPresent);
    }

    private string GetTemplateDirectoryPath(string projectName)
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var AssetsDir = Path.Combine(currentDirectory, "Assets");
        var path = Path.Combine(projectName, "content");
        var templateDirectoryPath = Path.Combine(AssetsDir, path);

        return templateDirectoryPath;
    }
}
