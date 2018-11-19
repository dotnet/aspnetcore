// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.AzureAppServices.FunctionalTests
{
    public class TemplateFunctionalTests
    {

        private static readonly string RuntimeInformationMiddlewareType = "Microsoft.AspNetCore.AzureAppServices.FunctionalTests.RuntimeInformationMiddleware";

        private static readonly string RuntimeInformationMiddlewareFile = Asset("RuntimeInformationMiddleware.cs");

        private static readonly string AppServicesWithSiteExtensionsTemplate = Asset("AppServicesWithSiteExtensions.json");

        readonly AzureFixture _fixture;

        private readonly ILogger _logger;

        public TemplateFunctionalTests(AzureFixture fixture, ILogger logger)
        {
            _fixture = fixture;
            _logger = logger;
        }

        public async Task LegacyTemplateRuns(WebAppDeploymentKind deploymentKind, string templateVersion, string expectedRuntime, string template, string expected)
        {
            var testId = ToFriendlyName(nameof(LegacyTemplateRuns), deploymentKind, template, templateVersion);

            var siteTask = _fixture.Deploy(AppServicesWithSiteExtensionsTemplate, GetSiteExtensionArguments(), testId);

            var testDirectory = GetTestDirectory(testId);

            // we are going to deploy with 2.0 dotnet to enable WebDeploy
            var dotnet20 = DotNet(_logger, testDirectory, "2.0");

            CopyFilesToProjectDirectory(testDirectory, Asset($"AspNetCore1x{template}"));
            CopyToProjectDirectory(testDirectory, Asset($"NuGet.{templateVersion}.config"), "NuGet.config", false);
            CopyToProjectDirectory(testDirectory, Asset($"Legacy.{templateVersion}.{template}.csproj"));
            InjectMiddlware(testDirectory, RuntimeInformationMiddlewareType, RuntimeInformationMiddlewareFile);

            await dotnet20.ExecuteAndAssertAsync("restore");

            var site = await siteTask;
            await site.Deploy(deploymentKind, testDirectory, dotnet20, _logger);

            using (var httpClient = site.CreateClient())
            {
                var getResult = await httpClient.GetAsync("/");
                getResult.EnsureSuccessStatusCode();
                Assert.Contains(expected, await getResult.Content.ReadAsStringAsync());

                getResult = await httpClient.GetAsync("/runtimeInfo");
                getResult.EnsureSuccessStatusCode();

                var runtimeInfoJson = await getResult.Content.ReadAsStringAsync();
                _logger.LogTrace("Runtime info: {Info}", runtimeInfoJson);

                var runtimeInfo = JsonConvert.DeserializeObject<RuntimeInfo>(runtimeInfoJson);
                ValidateLegacyRuntimeInfo(deploymentKind, runtimeInfo, templateVersion);
            }
        }

        private void ValidateLegacyRuntimeInfo(WebAppDeploymentKind deploymentKind, RuntimeInfo runtimeInfo, string templateVersion)
        {
            var cacheAssemblies = new HashSet<string>(File.ReadAllLines(Asset($"DotNetCache.{deploymentKind}.{templateVersion}.txt")), StringComparer.InvariantCultureIgnoreCase);
            var modulesNotInCache = new List<string>();

            foreach (var runtimeInfoModule in runtimeInfo.Modules)
            {
                // Skip native
                if (runtimeInfoModule.Version == null)
                {
                    continue;
                }


                // Check if assembly that is in the cache is loaded from it
                var moduleName = Path.GetFileNameWithoutExtension(runtimeInfoModule.ModuleName);
                if (cacheAssemblies.Contains(moduleName))
                {
                    if (runtimeInfoModule.FileName.IndexOf("\\DotNetCache\\x86\\", StringComparison.CurrentCultureIgnoreCase) == -1)
                    {
                        modulesNotInCache.Add(moduleName);
                    }
                    continue;
                }
            }

            Assert.Empty(modulesNotInCache);
        }

        public async Task TemplateRuns(WebAppDeploymentKind deploymentKind, string dotnetVersion, string template, string expected)
        {
            var testId = ToFriendlyName(nameof(TemplateRuns), deploymentKind, template, dotnetVersion);

            var siteTask = _fixture.Deploy(AppServicesWithSiteExtensionsTemplate, GetSiteExtensionArguments(), testId);

            var testDirectory = GetTestDirectory(testId);
            var dotnet = DotNet(_logger, testDirectory, dotnetVersion);

            await dotnet.ExecuteAndAssertAsync($"--info");
            await dotnet.ExecuteAndAssertAsync("new " + template);

            InjectMiddlware(testDirectory, RuntimeInformationMiddlewareType, RuntimeInformationMiddlewareFile);
            FixAspNetCoreVersion(testDirectory, dotnet.Command);

            var site = await siteTask;

            // There is no feed with packages included in lastes so we have to enable first run experience
            if (deploymentKind == WebAppDeploymentKind.Git && dotnetVersion == "latest")
            {
                await site.Update().WithAppSetting("DOTNET_SKIP_FIRST_TIME_EXPERIENCE", "false").ApplyAsync();
            }

            await site.Deploy(deploymentKind, testDirectory, dotnet, _logger);

            using (var httpClient = site.CreateClient())
            {
                var getResult = await httpClient.GetAsync("/");
                getResult.EnsureSuccessStatusCode();
                Assert.Contains(expected, await getResult.Content.ReadAsStringAsync());

                getResult = await httpClient.GetAsync("/runtimeInfo");
                getResult.EnsureSuccessStatusCode();

                var runtimeInfoJson = await getResult.Content.ReadAsStringAsync();
                _logger.LogTrace("Runtime info: {Info}", runtimeInfoJson);

                var runtimeInfo = JsonConvert.DeserializeObject<RuntimeInfo>(runtimeInfoJson);
                ValidateStoreRuntimeInfo(runtimeInfo, dotnet.Command);
            }
        }

        private void ValidateStoreRuntimeInfo(RuntimeInfo runtimeInfo, string dotnetPath)
        {
            var storeModules = PathUtilities.GetStoreModules(dotnetPath);
            var runtimeModules = PathUtilities.GetLatestSharedRuntimeAssemblies(dotnetPath, out var runtimeVersion);

            foreach (var runtimeInfoModule in runtimeInfo.Modules)
            {
                // Skip native
                if (runtimeInfoModule.Version == null)
                {
                    continue;
                }

                var moduleName = Path.GetFileNameWithoutExtension(runtimeInfoModule.ModuleName);

                // Check if module should come from the store, verify that one of the expected versions is loaded
                var storeModule = storeModules.SingleOrDefault(f => moduleName.Equals(f.Name, StringComparison.InvariantCultureIgnoreCase));
                if (storeModule != null)
                {
                    var expectedVersion = false;
                    foreach (var version in storeModule.Versions)
                    {
                        var expectedModulePath = $"store\\x86\\netcoreapp2.0\\{storeModule.Name}\\{version}";

                        if (runtimeInfoModule.FileName.IndexOf(expectedModulePath, StringComparison.InvariantCultureIgnoreCase) != -1)
                        {
                            expectedVersion = true;
                            break;
                        }
                    }

                    Assert.True(expectedVersion, $"{runtimeInfoModule.FileName} doesn't match expected versions: {string.Join(",", storeModule.Versions)}");
                }

                // Verify that modules that we expect to come from runtime actually come from there
                // Native modules would prefer to be loaded from windows folder, skip them
                if (runtimeModules.Any(rutimeModule => runtimeInfoModule.ModuleName.Equals(rutimeModule, StringComparison.InvariantCultureIgnoreCase)))
                {
                    Assert.Contains($"shared\\Microsoft.NETCore.App\\{runtimeVersion}", runtimeInfoModule.FileName);
                }
            }
        }

        private string ToFriendlyName(params object[] parts)
        {
            return new string(string.Join(string.Empty, parts).Where(char.IsLetterOrDigit).ToArray());
        }

        private static Dictionary<string, string> GetSiteExtensionArguments()
        {
            return new Dictionary<string, string>
            {
                { "extensionFeed", AzureFixture.GetRequiredEnvironmentVariable("SiteExtensionFeed") },
                { "extensionName", "AspNetCoreTestBundle" },
                { "extensionVersion", GetAssemblyInformationalVersion() },
            };
        }

        private static void UpdateCSProj(DirectoryInfo projectRoot, string fileName)
        {
            var csproj = projectRoot.GetFiles("*.csproj").Single().FullName;

            // Copy implementation file to project directory
            var implementationFile = Path.Combine(Directory.GetCurrentDirectory(), fileName);
            File.Copy(implementationFile, csproj, true);
        }

        private static void InjectMiddlware(DirectoryInfo projectRoot, string typeName, string fileName)
        {
            // Copy implementation file to project directory
            var implementationFile = Path.Combine(Directory.GetCurrentDirectory(), fileName);
            var destinationImplementationFile = Path.Combine(projectRoot.FullName, Path.GetFileName(fileName));
            File.Copy(implementationFile, destinationImplementationFile, true);

            // Register middleware in Startup.cs/Configure
            var startupFile = Path.Combine(projectRoot.FullName, "Startup.cs");
            var startupText = File.ReadAllText(startupFile);
            startupText = Regex.Replace(startupText, "public void Configure\\([^{]+{", match => match.Value + $" app.UseMiddleware<{typeName}>();");
            File.WriteAllText(startupFile, startupText);
        }

        private static void FixAspNetCoreVersion(DirectoryInfo testDirectory, string dotnetPath)
        {
            // TODO: Temporary workaround for broken templates in latest CLI

            // Detect what version of aspnet core was shipped with this CLI installation
            var aspnetCoreVersion = PathUtilities.GetBundledAspNetCoreVersion(dotnetPath);

            var csproj = testDirectory.GetFiles("*.csproj").Single().FullName;
            var projectContents = XDocument.Load(csproj);
            var packageReferences = projectContents
                .Descendants("PackageReference")
                .Concat(projectContents.Descendants("DotNetCliToolReference"));

            foreach (var packageReference in packageReferences)
            {
                var packageName = (string)packageReference.Attribute("Include");

                if (packageName == "Microsoft.AspNetCore.All" ||
                    packageName == "Microsoft.VisualStudio.Web.CodeGeneration.Tools")
                {
                    packageReference.Attribute("Version").Value = aspnetCoreVersion;
                }
            }

            projectContents.Save(csproj);
        }

        private static string GetAssemblyInformationalVersion()
        {
            var assemblyInformationalVersionAttribute = typeof(TemplateFunctionalTests).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (assemblyInformationalVersionAttribute == null)
            {
                throw new InvalidOperationException("Tests assembly lacks AssemblyInformationalVersionAttribute");
            }
            return assemblyInformationalVersionAttribute.InformationalVersion;
        }

        private static void CopyFilesToProjectDirectory(DirectoryInfo projectRoot, string directory)
        {
            var source = directory;
            var dest = Path.GetFullPath(projectRoot.FullName);

            foreach (string path in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(path.Replace(source, dest));
            }

            foreach (string path in Directory.GetFiles(source, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(path, path.Replace(source, dest), true);
            }
        }


        private static void CopyToProjectDirectory(DirectoryInfo projectRoot, string fileName, string desinationFileName = null, bool required = true)
        {
            // Copy implementation file to project directory
            var implementationFile = Path.Combine(Directory.GetCurrentDirectory(), fileName);
            if (!required && !File.Exists(implementationFile))
            {
                return;
            }

            File.Copy(implementationFile, Path.Combine(projectRoot.FullName, desinationFileName ?? Path.GetFileName(fileName)), true);
        }

        private TestCommand DotNet(ILogger logger, DirectoryInfo workingDirectory, string suffix)
        {
            var packages = Path.Combine(Program.ArtifactsPath, "packages", workingDirectory.Name);
            Directory.CreateDirectory(packages);
            var path = string.Join(";",
                Environment.GetEnvironmentVariable("PATH")
                    .Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries)
                    .Where(e => !e.Contains("dotnet")));

            return new TestCommand(GetDotNetPath(suffix))
            {
                Logger = logger,
                WorkingDirectory = workingDirectory.FullName,
                Environment = {
                    { "NUGET_PACKAGES", packages},
                    { "PATH", path }
                }
            };
        }

        private static string GetDotNetPath(string suffix)
        {
            var current = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (current != null)
            {
                var dotnetSubdir = new DirectoryInfo(Path.Combine(current.FullName, ".test-dotnet", suffix));
                if (dotnetSubdir.Exists)
                {
                    var dotnetName = Path.Combine(dotnetSubdir.FullName, "dotnet.exe");
                    if (!File.Exists(dotnetName))
                    {
                        throw new InvalidOperationException("dotnet directory was found but dotnet.exe is not in it");
                    }
                    return dotnetName;
                }
                current = current.Parent;
            }

            throw new InvalidOperationException("dotnet executable was not found");
        }

        private DirectoryInfo GetTestDirectory(string callerName)
        {
            var testDirectory = Path.Combine(Program.ArtifactsPath, "apps", callerName);
            if (Directory.Exists(testDirectory))
            {
                try
                {
                    Directory.Delete(testDirectory, recursive: true);
                }
                catch { }
            }
            return Directory.CreateDirectory(testDirectory);
        }

        private static string Asset(string name)
        {
            return Path.Combine("Assets", name);
        }
    }
}
