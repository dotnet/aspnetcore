// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.InternalTesting;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test;

public class NewlineEndingTest : LoggedTest
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

    [Theory]
    [InlineData("Web.ProjectTemplates")]
    [InlineData("Web.ItemTemplates")]
    [InlineData("Web.Client.ItemTemplates")]
    public void TemplateFiles_ShouldEndWithNewline(string projectName)
    {
        var templateDirectoryPath = GetTemplateDirectoryPath(projectName);

        var filesWithoutNewlineEnding = new List<string>();

        // Get all template source files (excluding third-party libraries and auto-generated localization files)
        var files = Directory.GetFiles(templateDirectoryPath, "*.cs", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(templateDirectoryPath, "*.fs", SearchOption.AllDirectories))
            .Concat(Directory.GetFiles(templateDirectoryPath, "*.razor", SearchOption.AllDirectories))
            .Concat(Directory.GetFiles(templateDirectoryPath, "*.cshtml", SearchOption.AllDirectories))
            .Concat(Directory.GetFiles(templateDirectoryPath, "*.css", SearchOption.AllDirectories))
            .Concat(Directory.GetFiles(templateDirectoryPath, "*.js", SearchOption.AllDirectories))
            .Concat(Directory.GetFiles(templateDirectoryPath, "*.ts", SearchOption.AllDirectories))
            .Concat(Directory.GetFiles(templateDirectoryPath, "*.tsx", SearchOption.AllDirectories))
            .Concat(Directory.GetFiles(templateDirectoryPath, "*.html", SearchOption.AllDirectories))
            .Concat(Directory.GetFiles(templateDirectoryPath, "*.json", SearchOption.AllDirectories))
            .Concat(Directory.GetFiles(templateDirectoryPath, "*.xml", SearchOption.AllDirectories))
            .Concat(Directory.GetFiles(templateDirectoryPath, "*.csproj", SearchOption.AllDirectories))
            .Concat(Directory.GetFiles(templateDirectoryPath, "*.fsproj", SearchOption.AllDirectories))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}wwwroot{Path.DirectorySeparatorChar}lib{Path.DirectorySeparatorChar}")) // Exclude third-party libraries
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}.template.config{Path.DirectorySeparatorChar}localize{Path.DirectorySeparatorChar}")); // Exclude auto-generated localization files in localize directory

        foreach (var file in files)
        {
            var filePath = Path.GetFullPath(file);

            // Skip empty files before opening the stream
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length == 0)
            {
                continue;
            }

            // Check if file ends with newline (0x0a)
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            fileStream.Seek(-1, SeekOrigin.End);
            var lastByte = fileStream.ReadByte();

            if (lastByte != 0x0a) // LF
            {
                Output.WriteLine($"File {filePath} does not end with a newline.");
                filesWithoutNewlineEnding.Add(filePath);
            }
        }

        Assert.False(filesWithoutNewlineEnding.Any(), $"Found {filesWithoutNewlineEnding.Count} file(s) without newline ending.");
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
