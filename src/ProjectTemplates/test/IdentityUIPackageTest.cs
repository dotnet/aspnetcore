// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test
{
    public class IdentityUIPackageTest
    {
        public IdentityUIPackageTest(ProjectFactoryFixture projectFactory, ITestOutputHelper output)
        {
            ProjectFactory = projectFactory;
            Output = output;
        }

        public Project Project { get; set; }

        public ProjectFactoryFixture ProjectFactory { get; set; }

        public ITestOutputHelper Output { get; }

        public static TheoryData<IDictionary<string, string>, string, string[]> MSBuildIdentityUIPackageOptions
        {
            get
            {
                var data = new TheoryData<IDictionary<string, string>, string, string[]>();

                data.Add(new Dictionary<string, string>
                {
                    ["IdentityUIFrameworkVersion"] = "Bootstrap3"
                },
                "Bootstrap v3.4.1",
                Bootstrap3ContentFiles);

                data.Add(new Dictionary<string, string>(), "Bootstrap v4.3.1", Bootstrap4ContentFiles);

                return data;
            }
        }

        public static string[] Bootstrap3ContentFiles { get; } = new string[]
        {
            "Identity/css/site.css",
            "Identity/js/site.js",
            "Identity/lib/bootstrap/dist/css/bootstrap-theme.css",
            "Identity/lib/bootstrap/dist/css/bootstrap-theme.css.map",
            "Identity/lib/bootstrap/dist/css/bootstrap-theme.min.css",
            "Identity/lib/bootstrap/dist/css/bootstrap-theme.min.css.map",
            "Identity/lib/bootstrap/dist/css/bootstrap.css",
            "Identity/lib/bootstrap/dist/css/bootstrap.css.map",
            "Identity/lib/bootstrap/dist/css/bootstrap.min.css",
            "Identity/lib/bootstrap/dist/css/bootstrap.min.css.map",
            "Identity/lib/bootstrap/dist/fonts/glyphicons-halflings-regular.eot",
            "Identity/lib/bootstrap/dist/fonts/glyphicons-halflings-regular.svg",
            "Identity/lib/bootstrap/dist/fonts/glyphicons-halflings-regular.ttf",
            "Identity/lib/bootstrap/dist/fonts/glyphicons-halflings-regular.woff",
            "Identity/lib/bootstrap/dist/fonts/glyphicons-halflings-regular.woff2",
            "Identity/lib/bootstrap/dist/js/bootstrap.js",
            "Identity/lib/bootstrap/dist/js/bootstrap.min.js",
            "Identity/lib/bootstrap/dist/js/npm.js",
            "Identity/lib/jquery/LICENSE.txt",
            "Identity/lib/jquery/dist/jquery.js",
            "Identity/lib/jquery/dist/jquery.min.js",
            "Identity/lib/jquery/dist/jquery.min.map",
            "Identity/lib/jquery-validation/LICENSE.md",
            "Identity/lib/jquery-validation/dist/additional-methods.js",
            "Identity/lib/jquery-validation/dist/additional-methods.min.js",
            "Identity/lib/jquery-validation/dist/jquery.validate.js",
            "Identity/lib/jquery-validation/dist/jquery.validate.min.js",
            "Identity/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.js",
            "Identity/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.min.js",
            "Identity/lib/jquery-validation-unobtrusive/LICENSE.txt",
        };

        public static string[] Bootstrap4ContentFiles { get; } = new string[]
        {
            "Identity/favicon.ico",
            "Identity/css/site.css",
            "Identity/js/site.js",
            "Identity/lib/bootstrap/dist/css/bootstrap-grid.css",
            "Identity/lib/bootstrap/dist/css/bootstrap-grid.css.map",
            "Identity/lib/bootstrap/dist/css/bootstrap-grid.min.css",
            "Identity/lib/bootstrap/dist/css/bootstrap-grid.min.css.map",
            "Identity/lib/bootstrap/dist/css/bootstrap-reboot.css",
            "Identity/lib/bootstrap/dist/css/bootstrap-reboot.css.map",
            "Identity/lib/bootstrap/dist/css/bootstrap-reboot.min.css",
            "Identity/lib/bootstrap/dist/css/bootstrap-reboot.min.css.map",
            "Identity/lib/bootstrap/dist/css/bootstrap.css",
            "Identity/lib/bootstrap/dist/css/bootstrap.css.map",
            "Identity/lib/bootstrap/dist/css/bootstrap.min.css",
            "Identity/lib/bootstrap/dist/css/bootstrap.min.css.map",
            "Identity/lib/bootstrap/dist/js/bootstrap.bundle.js",
            "Identity/lib/bootstrap/dist/js/bootstrap.bundle.js.map",
            "Identity/lib/bootstrap/dist/js/bootstrap.bundle.min.js",
            "Identity/lib/bootstrap/dist/js/bootstrap.bundle.min.js.map",
            "Identity/lib/bootstrap/dist/js/bootstrap.js",
            "Identity/lib/bootstrap/dist/js/bootstrap.js.map",
            "Identity/lib/bootstrap/dist/js/bootstrap.min.js",
            "Identity/lib/bootstrap/dist/js/bootstrap.min.js.map",
            "Identity/lib/jquery/LICENSE.txt",
            "Identity/lib/jquery/dist/jquery.js",
            "Identity/lib/jquery/dist/jquery.min.js",
            "Identity/lib/jquery/dist/jquery.min.map",
            "Identity/lib/jquery-validation/LICENSE.md",
            "Identity/lib/jquery-validation/dist/additional-methods.js",
            "Identity/lib/jquery-validation/dist/additional-methods.min.js",
            "Identity/lib/jquery-validation/dist/jquery.validate.js",
            "Identity/lib/jquery-validation/dist/jquery.validate.min.js",
            "Identity/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.js",
            "Identity/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.min.js",
            "Identity/lib/jquery-validation-unobtrusive/LICENSE.txt",
        };

        [ConditionalTheory(Skip = "This test run for over an hour")]
        [MemberData(nameof(MSBuildIdentityUIPackageOptions))]
        [SkipOnHelix("cert failure", Queues = "OSX.1014.Amd64;OSX.1014.Amd64.Open")]
        [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/19716")]
        public async Task IdentityUIPackage_WorksWithDifferentOptions(IDictionary<string, string> packageOptions, string versionValidator, string[] expectedFiles)
        {
            Project = await ProjectFactory.GetOrCreateProject("identityuipackage" + string.Concat(packageOptions.Values), Output);
            var useLocalDB = false;

            var createResult = await Project.RunDotNetNewAsync("razor", auth: "Individual", useLocalDB: useLocalDB, environmentVariables: packageOptions);
            Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage("create/restore", Project, createResult));

            var projectFileContents = ReadFile(Project.TemplateOutputDir, $"{Project.ProjectName}.csproj");
            Assert.Contains(".db", projectFileContents);

            var publishResult = await Project.RunDotNetPublishAsync(packageOptions: packageOptions);
            Assert.True(0 == publishResult.ExitCode, ErrorMessages.GetFailedProcessMessage("publish", Project, publishResult));

            // Run dotnet build after publish. The reason is that one uses Config = Debug and the other uses Config = Release
            // The output from publish will go into bin/Release/netcoreappX.Y/publish and won't be affected by calling build
            // later, while the opposite is not true.

            var buildResult = await Project.RunDotNetBuildAsync(packageOptions: packageOptions);
            Assert.True(0 == buildResult.ExitCode, ErrorMessages.GetFailedProcessMessage("build", Project, buildResult));

            var migrationsResult = await Project.RunDotNetEfCreateMigrationAsync("razorpages");
            Assert.True(0 == migrationsResult.ExitCode, ErrorMessages.GetFailedProcessMessage("run EF migrations", Project, migrationsResult));
            Project.AssertEmptyMigration("razorpages");

            using (var aspNetProcess = Project.StartBuiltProjectAsync())
            {
                Assert.False(
                    aspNetProcess.Process.HasExited,
                    ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", Project, aspNetProcess.Process));

                var response = await aspNetProcess.SendRequest("/Identity/lib/bootstrap/dist/css/bootstrap.css");
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Contains(versionValidator, await response.Content.ReadAsStringAsync());
                await ValidatePublishedFiles(aspNetProcess, expectedFiles);
            }

            using (var aspNetProcess = Project.StartPublishedProjectAsync())
            {
                Assert.False(
                    aspNetProcess.Process.HasExited,
                    ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", Project, aspNetProcess.Process));

                var response = await aspNetProcess.SendRequest("/Identity/lib/bootstrap/dist/css/bootstrap.css");
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Contains(versionValidator, await response.Content.ReadAsStringAsync());
                await ValidatePublishedFiles(aspNetProcess, expectedFiles);
            }
        }

        private async Task ValidatePublishedFiles(AspNetProcess aspNetProcess, string[] expectedContentFiles)
        {
            foreach (var file in expectedContentFiles)
            {
                var response = await aspNetProcess.SendRequest(file);
                Assert.True(response?.StatusCode == HttpStatusCode.OK, $"Couldn't find file '{file}'");
            }
        }

        private string ReadFile(string basePath, string path)
        {
            var fullPath = Path.Combine(basePath, path);
            var doesExist = File.Exists(fullPath);

            Assert.True(doesExist, $"Expected file to exist, but it doesn't: {path}");
            return File.ReadAllText(Path.Combine(basePath, path));
        }
    }
}
