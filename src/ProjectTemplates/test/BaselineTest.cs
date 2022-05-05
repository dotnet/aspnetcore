// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test
{
    public class BaselineTest : LoggedTest
    {
        private static readonly string BaselineDefinitionFileResourceName = "ProjectTemplates.Tests.template-baselines.json";

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
        [Theory]
        [MemberData(nameof(TemplateBaselines))]
        public async Task Template_Produces_The_Right_Set_Of_FilesAsync(string arguments, string[] expectedFiles)
        {
            Project = await ProjectFactory.GetOrCreateProject(CreateProjectKey(arguments), Output);
            var createResult = await Project.RunDotNetNewRawAsync(arguments);
            Assert.True(createResult.ExitCode == 0, createResult.GetFormattedOutput());

            foreach (var file in expectedFiles)
            {
                AssertFileExists(Project.TemplateOutputDir, file, shouldExist: true);
            }

            var filesInFolder = Directory.EnumerateFiles(Project.TemplateOutputDir, "*", SearchOption.AllDirectories);
            foreach (var file in filesInFolder)
            {
                var relativePath = file.Replace(Project.TemplateOutputDir, "").Replace("\\", "/").Trim('/');
                if (relativePath.EndsWith(".csproj", StringComparison.Ordinal) ||
                    relativePath.EndsWith(".fsproj", StringComparison.Ordinal) ||
                    relativePath.EndsWith(".props", StringComparison.Ordinal) ||
                    relativePath.EndsWith(".targets", StringComparison.Ordinal) ||
                    relativePath.StartsWith("bin/", StringComparison.Ordinal) ||
                    relativePath.StartsWith("obj/", StringComparison.Ordinal) ||
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

                if (relativePath.EndsWith(".cs", StringComparison.Ordinal))
                {
                    var namespaceDeclarationPrefix = "namespace ";
                    var namespaceDeclaration = File.ReadLines(Path.Combine(Project.TemplateOutputDir, relativePath))
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

        private static ConcurrentDictionary<string, object> _projectKeys = new();

        private string CreateProjectKey(string arguments)
        {
            var text = "baseline";

            // Turn string like "new templatename -minimal -au SingleOrg --another-option OptionValue"
            // into array like [ "new templatename", "minimal", "au SingleOrg", "another-option OptionValue" ]
            var argumentsArray = arguments
                .Split(new[] { " --", " -" }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .ToArray();

            // Add template name, value has form of "new name"
            text += argumentsArray[0].Substring("new ".Length);

            // Sort arguments to ensure definitions that differ only by arguments order are caught
            Array.Sort(argumentsArray, StringComparer.Ordinal);

            foreach (var argValue in argumentsArray)
            {
                var argSegments = argValue.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                if (argSegments.Length == 0)
                {
                    continue;
                }
                else if (argSegments.Length == 1)
                {
                    text += argSegments[0] switch
                    {
                        "ho" => "hosted",
                        "p" => "pwa",
                        _ => argSegments[0].Replace("-","")
                    };
                }
                else
                {
                    text += argSegments[0] switch
                    {
                        "au" => argSegments[1],
                        "uld" => "uld",
                        "language" => argSegments[1].Replace("#", "Sharp"),
                        "support-pages-and-views" when argSegments[1] == "true" => "supportpagesandviewstrue",
                        _ => ""
                    };
                }
            }

            if (!_projectKeys.TryAdd(text, null))
            {
                throw new InvalidOperationException(
                    $"Project key for template with args '{arguments}' already exists. " +
                    $"Check that the metadata specified in {BaselineDefinitionFileResourceName} is correct and that " +
                    $"the {nameof(CreateProjectKey)} method is considering enough template arguments to ensure uniqueness.");
            }

            return text;
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
}
