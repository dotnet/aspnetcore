// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProjectTemplates.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test
{
    public class BaselineTest
    {
        public BaselineTest(ProjectFactoryFixture projectFactory, ITestOutputHelper output)
        {
            Project = projectFactory.CreateProject(output);
        }

        public Project Project { get; set; }

        public static TheoryData<string, string[]> TemplateBaselines
        {
            get
            {
                using (var stream = typeof(BaselineTest).Assembly.GetManifestResourceStream("ProjectTemplates.Tests.template-baselines.json"))
                {
                    using (var jsonReader = new JsonTextReader(new StreamReader(stream)))
                    {
                        var baseline = JObject.Load(jsonReader);
                        var data = new TheoryData<string, string[]>();
                        foreach (var template in baseline)
                        {
                            foreach (var authOption in (JObject)template.Value)
                            {
                                data.Add(
                                    (string)authOption.Value["Arguments"],
                                    ((JArray)authOption.Value["Files"]).Select(s => (string)s).ToArray());
                            }
                        }

                        return data;
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(TemplateBaselines))]
        public void Template_Produces_The_Right_Set_Of_Files(string arguments, string[] expectedFiles)
        {
            Project.RunDotNet(arguments);
            foreach (var file in expectedFiles)
            {
                Project.AssertFileExists(file, shouldExist: true);
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
                    relativePath.StartsWith("obj/", StringComparison.Ordinal))
                {
                    continue;
                }
                Assert.Contains(relativePath, expectedFiles);
            }
        }
    }
}
