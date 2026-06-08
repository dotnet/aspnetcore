// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.InternalTesting;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test;

public class TemplateConfigJsonTest : LoggedTest
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

    // Mirrors the leniency used by the .NET template engine when it loads
    // template.json and dotnetcli.host.json, so the test only fails for files
    // that the engine would also reject (e.g. trailing content after the JSON).
    private static readonly JsonDocumentOptions JsonOptions = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    [Theory]
    [InlineData("Web.ProjectTemplates")]
    [InlineData("Web.ItemTemplates")]
    [InlineData("Web.Client.ItemTemplates")]
    [InlineData("McpServer.ProjectTemplates")]
    public void TemplateConfigJsonFiles_ShouldBeValidJson(string projectName)
    {
        var templateDirectoryPath = GetTemplateDirectoryPath(projectName);

        var invalidFiles = new List<string>();

        // Validate every JSON file under a .template.config directory (template.json,
        // dotnetcli.host.json, localized strings, ...). Malformed JSON here causes
        // "dotnet new" to fail loading the template host data with errors such as
        // "'g' is invalid after a single JSON value. Expected end of data.".
        var files = Directory.GetFiles(templateDirectoryPath, "*.json", SearchOption.AllDirectories)
            .Where(f => f.Contains($"{Path.DirectorySeparatorChar}.template.config{Path.DirectorySeparatorChar}"));

        foreach (var file in files)
        {
            var filePath = Path.GetFullPath(file);

            try
            {
                // File.ReadAllText strips a leading byte-order mark when present.
                using var _ = JsonDocument.Parse(File.ReadAllText(filePath), JsonOptions);
            }
            catch (JsonException ex)
            {
                Output.WriteLine($"File {filePath} is not valid JSON: {ex.Message}");
                invalidFiles.Add(filePath);
            }
        }

        Assert.False(invalidFiles.Any(), $"Found {invalidFiles.Count} invalid JSON file(s) under .template.config.");
    }

    private static string GetTemplateDirectoryPath(string projectName)
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var assetsDir = Path.Combine(currentDirectory, "Assets");
        var path = Path.Combine(projectName, "content");
        var templateDirectoryPath = Path.Combine(assetsDir, path);

        return templateDirectoryPath;
    }
}
