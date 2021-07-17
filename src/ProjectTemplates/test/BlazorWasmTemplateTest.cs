// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Newtonsoft.Json.Linq;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Templates.Test
{
    public class BlazorWasmTemplateTest : BlazorTemplateTest
    {
        public BlazorWasmTemplateTest(ProjectFactoryFixture projectFactory)
            : base(projectFactory) { }

        public override string ProjectType { get; } = "blazorwasm";

        [Fact]
        public async Task BlazorWasmStandaloneTemplateCanCreateBuildPublish()
        {
            var project = await CreateBuildPublishAsync("blazorstandalone");

            // The service worker assets manifest isn't generated for non-PWA projects
            var publishDir = Path.Combine(project.TemplatePublishDir, "wwwroot");
            Assert.False(File.Exists(Path.Combine(publishDir, "service-worker-assets.js")), "Non-PWA templates should not produce service-worker-assets.js");
        }

        [Fact]
        public Task BlazorWasmHostedTemplateCanCreateBuildPublish() => CreateBuildPublishAsync("blazorhosted", args: new[] { "--hosted" }, serverProject: true);

        [Fact]
        public Task BlazorWasmStandalonePwaTemplateCanCreateBuildPublish() => CreateBuildPublishAsync("blazorstandalonepwa", args: new[] { "--pwa" });

        [Fact]
        public async Task BlazorWasmHostedPwaTemplateCanCreateBuildPublish()
        {
            var project = await CreateBuildPublishAsync("blazorhostedpwa", args: new[] { "--hosted", "--pwa" }, serverProject: true);

            var serverProject = GetSubProject(project, "Server", $"{project.ProjectName}.Server");

            ValidatePublishedServiceWorker(serverProject);
        }

        private void ValidatePublishedServiceWorker(Project project)
        {
            var publishDir = Path.Combine(project.TemplatePublishDir, "wwwroot");

            // When publishing the PWA template, we generate an assets manifest
            // and move service-worker.published.js to overwrite service-worker.js
            Assert.False(File.Exists(Path.Combine(publishDir, "service-worker.published.js")), "service-worker.published.js should not be published");
            Assert.True(File.Exists(Path.Combine(publishDir, "service-worker.js")), "service-worker.js should be published");
            Assert.True(File.Exists(Path.Combine(publishDir, "service-worker-assets.js")), "service-worker-assets.js should be published");

            // We automatically append the SWAM version as a comment in the published service worker file
            var serviceWorkerAssetsManifestContents = ReadFile(publishDir, "service-worker-assets.js");
            var serviceWorkerContents = ReadFile(publishDir, "service-worker.js");

            // Parse the "version": "..." value from the SWAM, and check it's in the service worker
            var serviceWorkerAssetsManifestVersionMatch = new Regex(@"^\s*\""version\"":\s*(\""[^\""]+\"")", RegexOptions.Multiline)
                .Match(serviceWorkerAssetsManifestContents);
            Assert.True(serviceWorkerAssetsManifestVersionMatch.Success);
            var serviceWorkerAssetsManifestVersionJson = serviceWorkerAssetsManifestVersionMatch.Groups[1].Captures[0].Value;
            var serviceWorkerAssetsManifestVersion = JsonSerializer.Deserialize<string>(serviceWorkerAssetsManifestVersionJson);
            Assert.True(serviceWorkerContents.Contains($"/* Manifest version: {serviceWorkerAssetsManifestVersion} */", StringComparison.Ordinal));
        }

        [ConditionalFact]
        // LocalDB doesn't work on non Windows platforms
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public Task BlazorWasmHostedTemplate_IndividualAuth_Works_WithLocalDB()
            => BlazorWasmHostedTemplate_IndividualAuth_Works(true);

        [Fact]
        [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/30825", Queues = "All.OSX")]
        public Task BlazorWasmHostedTemplate_IndividualAuth_Works_WithOutLocalDB()
            => BlazorWasmHostedTemplate_IndividualAuth_Works(false);

        private async Task<Project> CreateBuildPublishIndividualAuthProject(bool useLocalDb)
        {
            // Additional arguments are needed. See: https://github.com/dotnet/aspnetcore/issues/24278
            Environment.SetEnvironmentVariable("EnableDefaultScopedCssItems", "true");

            var project = await CreateBuildPublishAsync("blazorhostedindividual" + (useLocalDb ? "uld" : ""),
                args: new[] { "--hosted", "-au", "Individual", useLocalDb ? "-uld" : "" });

            var serverProject = GetSubProject(project, "Server", $"{project.ProjectName}.Server");

            var serverProjectFileContents = ReadFile(serverProject.TemplateOutputDir, $"{serverProject.ProjectName}.csproj");
            if (!useLocalDb)
            {
                Assert.Contains(".db", serverProjectFileContents);
            }

            var appSettings = ReadFile(serverProject.TemplateOutputDir, "appsettings.json");
            var element = JsonSerializer.Deserialize<JsonElement>(appSettings);
            var clientsProperty = element.GetProperty("IdentityServer").EnumerateObject().Single().Value.EnumerateObject().Single();
            var replacedSection = element.GetRawText().Replace(clientsProperty.Name, serverProject.ProjectName.Replace(".Server", ".Client"));
            var appSettingsPath = Path.Combine(serverProject.TemplateOutputDir, "appsettings.json");
            File.WriteAllText(appSettingsPath, replacedSection);

            var publishResult = await serverProject.RunDotNetPublishAsync();
            Assert.True(0 == publishResult.ExitCode, ErrorMessages.GetFailedProcessMessage("publish", serverProject, publishResult));

            // Run dotnet build after publish. The reason is that one uses Config = Debug and the other uses Config = Release
            // The output from publish will go into bin/Release/netcoreappX.Y/publish and won't be affected by calling build
            // later, while the opposite is not true.

            var buildResult = await serverProject.RunDotNetBuildAsync();
            Assert.True(0 == buildResult.ExitCode, ErrorMessages.GetFailedProcessMessage("build", serverProject, buildResult));

            var migrationsResult = await serverProject.RunDotNetEfCreateMigrationAsync("blazorwasm");
            Assert.True(0 == migrationsResult.ExitCode, ErrorMessages.GetFailedProcessMessage("run EF migrations", serverProject, migrationsResult));
            serverProject.AssertEmptyMigration("blazorwasm");

            return project;
        }

        private async Task BlazorWasmHostedTemplate_IndividualAuth_Works(bool useLocalDb)
        {
            var project = await CreateBuildPublishIndividualAuthProject(useLocalDb: useLocalDb);

            var serverProject = GetSubProject(project, "Server", $"{project.ProjectName}.Server");
        }

        [Fact]
        public async Task BlazorWasmStandaloneTemplate_IndividualAuth_CreateBuildPublish()
        {
            var project = await CreateBuildPublishAsync("blazorstandaloneindividual", args: new[] {
                "-au",
                "Individual",
                "--authority",
                "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration",
                "--client-id",
                "sample-client-id"
            });
        }

        public static TheoryData<TemplateInstance> TemplateData => new TheoryData<TemplateInstance>
        {
            new TemplateInstance(
                "blazorwasmhostedaadb2c", "-ho",
                "-au", "IndividualB2C",
                "--aad-b2c-instance", "example.b2clogin.com",
                "-ssp", "b2c_1_siupin",
                "--client-id", "clientId",
                "--domain", "my-domain",
                "--default-scope", "full",
                "--app-id-uri", "ApiUri",
                "--api-client-id", "1234123413241324"),
            new TemplateInstance(
                "blazorwasmhostedaad", "-ho",
                "-au", "SingleOrg",
                "--domain", "my-domain",
                "--tenant-id", "tenantId",
                "--client-id", "clientId",
                "--default-scope", "full",
                "--app-id-uri", "ApiUri",
                "--api-client-id", "1234123413241324"),
            new TemplateInstance(
                "blazorwasmhostedaadgraph", "-ho",
                "-au", "SingleOrg",
                "--calls-graph",
                "--domain", "my-domain",
                "--tenant-id", "tenantId",
                "--client-id", "clientId",
                "--default-scope", "full",
                "--app-id-uri", "ApiUri",
                "--api-client-id", "1234123413241324"),
            new TemplateInstance(
                "blazorwasmhostedaadapi", "-ho",
                "-au", "SingleOrg",
                "--called-api-url", "\"https://graph.microsoft.com\"",
                "--called-api-scopes", "user.readwrite",
                "--domain", "my-domain",
                "--tenant-id", "tenantId",
                "--client-id", "clientId",
                "--default-scope", "full",
                "--app-id-uri", "ApiUri",
                "--api-client-id", "1234123413241324"),
            new TemplateInstance(
                "blazorwasmstandaloneaadb2c",
                "-au", "IndividualB2C",
                "--aad-b2c-instance", "example.b2clogin.com",
                "-ssp", "b2c_1_siupin",
                "--client-id", "clientId",
                "--domain", "my-domain"),
            new TemplateInstance(
                "blazorwasmstandaloneaad",
                "-au", "SingleOrg",
                "--domain", "my-domain",
                "--tenant-id", "tenantId",
                "--client-id", "clientId"),
        };

        public class TemplateInstance
        {
            public TemplateInstance(string name, params string[] arguments)
            {
                Name = name;
                Arguments = arguments;
            }

            public string Name { get; }
            public string[] Arguments { get; }
        }

        [Theory]
        [MemberData(nameof(TemplateData))]
        public Task BlazorWasmHostedTemplate_AzureActiveDirectoryTemplate_Works(TemplateInstance instance)
            => CreateBuildPublishAsync(instance.Name, args: instance.Arguments, targetFramework: "netstandard2.1");

        private string ReadFile(string basePath, string path)
        {
            var fullPath = Path.Combine(basePath, path);
            var doesExist = File.Exists(fullPath);

            Assert.True(doesExist, $"Expected file to exist, but it doesn't: {path}");
            return File.ReadAllText(Path.Combine(basePath, path));
        }

        private void UpdatePublishedSettings(Project serverProject)
        {
            // Hijack here the config file to use the development key during publish.
            var appSettings = JObject.Parse(File.ReadAllText(Path.Combine(serverProject.TemplateOutputDir, "appsettings.json")));
            var appSettingsDevelopment = JObject.Parse(File.ReadAllText(Path.Combine(serverProject.TemplateOutputDir, "appsettings.Development.json")));
            ((JObject)appSettings["IdentityServer"]).Merge(appSettingsDevelopment["IdentityServer"]);
            ((JObject)appSettings["IdentityServer"]).Merge(new
            {
                IdentityServer = new
                {
                    Key = new
                    {
                        FilePath = "./tempkey.json"
                    }
                }
            });
            var testAppSettings = appSettings.ToString();
            File.WriteAllText(Path.Combine(serverProject.TemplatePublishDir, "appsettings.json"), testAppSettings);
        }
    }
}
