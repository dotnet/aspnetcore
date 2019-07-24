using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace ProjectTemplates.Tests
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

                // data.Add(new Dictionary<string, string>(), "Bootstrap v4.3.1", Bootstrap4ContentFiles);

                return data;
            }
        }

        public static string[] Bootstrap3ContentFiles { get; } = new string[]
        {
            "Identity/css/site.css",
            "Identity/js/site.js",
            "Identity/lib/bootstrap/LICENSE",
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
            "favicon.ico",
            "css/site.css",
            "js/site.js",
            "lib/bootstrap/LICENSE",
            "lib/bootstrap/dist/css/bootstrap-grid.css",
            "lib/bootstrap/dist/css/bootstrap-grid.css.map",
            "lib/bootstrap/dist/css/bootstrap-grid.min.css",
            "lib/bootstrap/dist/css/bootstrap-grid.min.css.map",
            "lib/bootstrap/dist/css/bootstrap-reboot.css",
            "lib/bootstrap/dist/css/bootstrap-reboot.css.map",
            "lib/bootstrap/dist/css/bootstrap-reboot.min.css",
            "lib/bootstrap/dist/css/bootstrap-reboot.min.css.map",
            "lib/bootstrap/dist/css/bootstrap.css",
            "lib/bootstrap/dist/css/bootstrap.css.map",
            "lib/bootstrap/dist/css/bootstrap.min.css",
            "lib/bootstrap/dist/css/bootstrap.min.css.map",
            "lib/bootstrap/dist/js/bootstrap.bundle.js",
            "lib/bootstrap/dist/js/bootstrap.bundle.js.map",
            "lib/bootstrap/dist/js/bootstrap.bundle.min.js",
            "lib/bootstrap/dist/js/bootstrap.bundle.min.js.map",
            "lib/bootstrap/dist/js/bootstrap.js",
            "lib/bootstrap/dist/js/bootstrap.js.map",
            "lib/bootstrap/dist/js/bootstrap.min.js",
            "lib/bootstrap/dist/js/bootstrap.min.js.map",
            "lib/jquery/LICENSE.txt",
            "lib/jquery/dist/jquery.js",
            "lib/jquery/dist/jquery.min.js",
            "lib/jquery/dist/jquery.min.map",
            "lib/jquery-validation/LICENSE.md",
            "lib/jquery-validation/dist/additional-methods.js",
            "lib/jquery-validation/dist/additional-methods.min.js",
            "lib/jquery-validation/dist/jquery.validate.js",
            "lib/jquery-validation/dist/jquery.validate.min.js",
            "lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.js",
            "lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.min.js",
            "lib/jquery-validation-unobtrusive/LICENSE.txt",
        };

        [Theory]
        [MemberData(nameof(MSBuildIdentityUIPackageOptions))]
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
            // The output from publish will go into bin/Release/netcoreapp3.0/publish and won't be affected by calling build
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

                var response = await GetFile(aspNetProcess, "/Identity/lib/bootstrap/dist/css/bootstrap.css");
                Assert.Contains(versionValidator, await response.Content.ReadAsStringAsync());
            }

            using (var aspNetProcess = Project.StartPublishedProjectAsync())
            {
                Assert.False(
                    aspNetProcess.Process.HasExited,
                    ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", Project, aspNetProcess.Process));

                var response = await GetFile(aspNetProcess, "/Identity/lib/bootstrap/dist/css/bootstrap.css");
                Assert.Contains(versionValidator, await response.Content.ReadAsStringAsync());
                await ValidatePublishedFiles(aspNetProcess.ListeningUri, expectedFiles);
            }
        }

        private async Task ValidatePublishedFiles(Uri baseAddress, string[] expectedContentFiles)
        {
            using HttpClient httpClient = new HttpClient { BaseAddress = baseAddress };

            foreach (var file in expectedContentFiles)
            {
                var response = await GetFile(httpClient, file, assert: false);
                Assert.True(response?.StatusCode == HttpStatusCode.OK, $"Couldn't find file '{file}'");
            }
        }

        private static Task<HttpResponseMessage> GetFile(AspNetProcess aspNetProcess, string path, bool assert = true)
        {
            using HttpClient httpClient = new HttpClient { BaseAddress = aspNetProcess.ListeningUri };
            return GetFile(httpClient, path, assert);
        }

        private static async Task<HttpResponseMessage> GetFile(HttpClient httpClient, string path, bool assert = true)
        {
            HttpResponseMessage response = null;
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    response = await httpClient.GetAsync(path);
                    if (i + 1 == 3)
                    {
                        break;
                    }
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        return response;
                    }
                }
                catch (Exception)
                {
                    if (i + 1 == 3)
                    {
                        break;
                    }

                    await Task.Delay(3);
                }
            }

            if (assert)
            {
                Assert.NotNull(response);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }

            return response;
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
