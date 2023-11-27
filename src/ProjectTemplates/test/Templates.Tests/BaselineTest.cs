// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.InternalTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test;

public class BaselineTest : LoggedTest
{
    private static readonly string BaselineDefinitionFileResourceName = "Templates.Tests.template-baselines.json";

    public BaselineTest(ProjectFactoryFixture projectFactory)
    {
        ProjectFactory = projectFactory;
    }

    public Project Project { get; set; }

    public static TheoryData<string, string[]> TemplateBaselines
    {
        get
        {
            using (var stream = typeof(BaselineTest).Assembly.GetManifestResourceStream(BaselineDefinitionFileResourceName))
            {
                using (var jsonReader = new JsonTextReader(new StreamReader(stream)))
                {
                    var baseline = JObject.Load(jsonReader);
                    var data = new TheoryData<string, string[]>();
                    foreach (var template in baseline)
                    {
                        foreach (var scenarioName in (JObject)template.Value)
                        {
                            data.Add(
                                (string)scenarioName.Value["Arguments"],
                                ((JArray)scenarioName.Value["Files"]).Select(s => (string)s).ToArray());
                        }
                    }

                    return data;
                }
            }
        }
    }

    public ProjectFactoryFixture ProjectFactory { get; }
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

    // This test should generally not be quarantined as it only is checking that the expected files are on disk
    // and that the namespace declarations in the generated .cs files start with the project name
    [Theory]
    [MemberData(nameof(TemplateBaselines))]
    public async Task Template_Produces_The_Right_Set_Of_FilesAsync(string arguments, string[] expectedFiles)
    {
        Project = await ProjectFactory.CreateProject(Output);
        await Project.RunDotNetNewRawAsync(arguments);

        expectedFiles = expectedFiles.Select(f => f.Replace("{ProjectName}", Project.ProjectName)).ToArray();

        foreach (var file in expectedFiles)
        {
            AssertFileExists(Project.TemplateOutputDir, file, shouldExist: true);
        }

        var filesInFolder = Directory.EnumerateFiles(Project.TemplateOutputDir, "*", SearchOption.AllDirectories).ToArray();
        foreach (var file in filesInFolder)
        {
            var relativePath = file.Replace(Project.TemplateOutputDir, "").Replace("\\", "/").Trim('/');
            if (relativePath.EndsWith(".csproj", StringComparison.Ordinal) ||
                relativePath.EndsWith(".fsproj", StringComparison.Ordinal) ||
                relativePath.EndsWith(".props", StringComparison.Ordinal) ||
                relativePath.EndsWith(".sln", StringComparison.Ordinal) ||
                relativePath.EndsWith(".targets", StringComparison.Ordinal) ||
                relativePath.StartsWith("bin/", StringComparison.Ordinal) ||
                relativePath.StartsWith("obj/", StringComparison.Ordinal) ||
                relativePath.Contains("/bin/", StringComparison.Ordinal) ||
                relativePath.Contains("/obj/", StringComparison.Ordinal))
            {
                continue;
            }
            Assert.Contains(relativePath, expectedFiles);

            if (relativePath.EndsWith(".cs", StringComparison.Ordinal) && !relativePath.EndsWith("Extensions.cs", StringComparison.Ordinal))
            {
                var namespaceDeclarationPrefix = "namespace ";
                var namespaceDeclaration = File.ReadLines(file)
                    .SingleOrDefault(line => line.StartsWith(namespaceDeclarationPrefix, StringComparison.Ordinal))
                    ?.Substring(namespaceDeclarationPrefix.Length);

                // nullable because Program.cs with top-level statements doesn't have a namespace declaration
                if (namespaceDeclaration is not null)
                {
                    Assert.StartsWith(Project.ProjectName, namespaceDeclaration, StringComparison.Ordinal);
                }
            }
        }
    }

    private void AssertFileExists(string basePath, string path, bool shouldExist)
    {
        var fullPath = Path.Combine(basePath, path);
        var doesExist = File.Exists(fullPath);

        if (shouldExist)
        {
            Assert.True(doesExist, "Expected file to exist, but it doesn't: " + path);
        }
        else
        {
            Assert.False(doesExist, "Expected file not to exist, but it does: " + path);
        }
    }
}
