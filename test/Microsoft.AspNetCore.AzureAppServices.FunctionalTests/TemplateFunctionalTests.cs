// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.AzureAppServices.FunctionalTests
{
    [Collection("Azure")]
    public class TemplateFunctionalTests
    {
        private static readonly string RuntimeInformationMiddlewareType = "Microsoft.AspNetCore.AzureAppServices.FunctionalTests.RuntimeInformationMiddleware";

        private static readonly string RuntimeInformationMiddlewareFile = Asset("RuntimeInformationMiddleware.cs");

        private static readonly string AppServicesWithSiteExtensionsTemplate = Asset("AppServicesWithSiteExtensions.json");

        readonly AzureFixture _fixture;

        private readonly ITestOutputHelper _outputHelper;

        public TemplateFunctionalTests(AzureFixture fixture, ITestOutputHelper outputHelper)
        {
            _fixture = fixture;
            _outputHelper = outputHelper;
        }

        [Theory]
        [InlineData(WebAppDeploymentKind.Git, "1.0.5", "web", "Hello World!")]
        [InlineData(WebAppDeploymentKind.Git, "1.0.5", "mvc", "Learn how to build ASP.NET apps that can run anywhere")]
        [InlineData(WebAppDeploymentKind.Git, "1.1.2", "web", "Hello World!")]
        [InlineData(WebAppDeploymentKind.Git, "1.1.2", "mvc", "Learn how to build ASP.NET apps that can run anywhere")]
        [InlineData(WebAppDeploymentKind.Ftp, "1.0.5", "web", "Hello World!")]
        [InlineData(WebAppDeploymentKind.Ftp, "1.0.5", "mvc", "Learn how to build ASP.NET apps that can run anywhere")]
        [InlineData(WebAppDeploymentKind.Ftp, "1.1.2", "web", "Hello World!")]
        [InlineData(WebAppDeploymentKind.Ftp, "1.1.2", "mvc", "Learn how to build ASP.NET apps that can run anywhere")]
        public async Task LegacyTemplateRuns(WebAppDeploymentKind deploymentKind, string expectedRuntime, string template, string expected)
        {
            var testId =  ToFriendlyName(nameof(LegacyTemplateRuns), deploymentKind, template, expectedRuntime);

            using (var logger = GetLogger(testId))
            {
                var siteTask = _fixture.Deploy(AppServicesWithSiteExtensionsTemplate, GetSiteExtensionArguments(), testId);

                var testDirectory = GetTestDirectory(testId);

                var dotnet = DotNet(logger, testDirectory, "1.1");

                await dotnet.ExecuteAndAssertAsync($"new {template}");

                UpdateCSProj(testDirectory, Asset($"Legacy.{expectedRuntime}.{template}.csproj"));

                InjectMiddlware(testDirectory, RuntimeInformationMiddlewareType, RuntimeInformationMiddlewareFile);

                var site = await siteTask;
                await site.Deploy(deploymentKind, testDirectory, dotnet, logger);

                using (var httpClient = site.CreateClient())
                {
                    var getResult = await httpClient.GetAsync("/");
                    getResult.EnsureSuccessStatusCode();
                    Assert.Contains(expected, await getResult.Content.ReadAsStringAsync());

                    getResult = await httpClient.GetAsync("/runtimeInfo");
                    getResult.EnsureSuccessStatusCode();

                    var runtimeInfo = JsonConvert.DeserializeObject<RuntimeInfo>(await getResult.Content.ReadAsStringAsync());
                    ValidateLegacyRuntimeInfo(runtimeInfo, expectedRuntime, dotnet.Command);
                }
            }
        }

        private void ValidateLegacyRuntimeInfo(RuntimeInfo runtimeInfo, string expectedRuntime, string dotnetPath)
        {
            var cacheAssemblies = new HashSet<string>(File.ReadAllLines(Asset($"DotNetCache.{expectedRuntime}.txt")), StringComparer.InvariantCultureIgnoreCase);
            var runtimeModules = PathUtilities.GetLatestSharedRuntimeAssemblies(dotnetPath, out _);
            var modulesNotInCache = new List<string>();

            foreach (var runtimeInfoModule in runtimeInfo.Modules)
            {
                // Skip native
                if (runtimeInfoModule.Version == null)
                {
                    continue;
                }

                // Verify that modules that we expect to come from runtime actually come from there
                if (runtimeModules.Any(rutimeModule => runtimeInfoModule.ModuleName.Equals(rutimeModule, StringComparison.InvariantCultureIgnoreCase)))
                {
                    Assert.Contains($"shared\\Microsoft.NETCore.App\\{expectedRuntime}", runtimeInfoModule.FileName);
                    continue;
                }

                // Check if assembly that is in the cache is loaded from it
                if (cacheAssemblies.Contains(Path.GetFileNameWithoutExtension(runtimeInfoModule.ModuleName)))
                {
                    if (runtimeInfoModule.FileName.IndexOf("D:\\DotNetCache\\x86\\", StringComparison.CurrentCultureIgnoreCase) == -1)
                    {
                        modulesNotInCache.Add(runtimeInfoModule.FileName);
                    }
                    continue;
                }

                Assert.Contains("wwwroot\\", runtimeInfoModule.FileName);
            }

            Assert.Empty(modulesNotInCache);
        }

        [Theory]
        [InlineData(WebAppDeploymentKind.Git, "2.0", "web", "Hello World!")]
        [InlineData(WebAppDeploymentKind.Git, "2.0", "razor", "Learn how to build ASP.NET apps that can run anywhere.")]
        [InlineData(WebAppDeploymentKind.Git, "2.0", "mvc", "Learn how to build ASP.NET apps that can run anywhere.")]
        [InlineData(WebAppDeploymentKind.Git, "latest", "web", "Hello World!")]
        [InlineData(WebAppDeploymentKind.Git, "latest", "razor", "Learn how to build ASP.NET apps that can run anywhere.")]
        [InlineData(WebAppDeploymentKind.Git, "latest", "mvc", "Learn how to build ASP.NET apps that can run anywhere.")]
        [InlineData(WebAppDeploymentKind.WebDeploy, "2.0", "web", "Hello World!")]
        [InlineData(WebAppDeploymentKind.WebDeploy, "2.0", "razor", "Learn how to build ASP.NET apps that can run anywhere.")]
        [InlineData(WebAppDeploymentKind.WebDeploy, "2.0", "mvc", "Learn how to build ASP.NET apps that can run anywhere.")]
        [InlineData(WebAppDeploymentKind.WebDeploy, "latest", "web", "Hello World!")]
        [InlineData(WebAppDeploymentKind.WebDeploy, "latest", "razor", "Learn how to build ASP.NET apps that can run anywhere.")]
        [InlineData(WebAppDeploymentKind.WebDeploy, "latest", "mvc", "Learn how to build ASP.NET apps that can run anywhere.")]
        public async Task TemplateRuns(WebAppDeploymentKind deploymentKind, string dotnetVersion, string template, string expected)
        {
            var testId = ToFriendlyName(nameof(TemplateRuns), deploymentKind, template, dotnetVersion);

            using (var logger = GetLogger(testId))
            {
                var siteTask = _fixture.Deploy(AppServicesWithSiteExtensionsTemplate, GetSiteExtensionArguments(), testId);

                var testDirectory = GetTestDirectory(testId);
                var dotnet = DotNet(logger, testDirectory, dotnetVersion);

                await dotnet.ExecuteAndAssertAsync("new " + template);

                // We don't ship offline cache in site extension so we need to provider a feed to
                // restore from when doing kudu git deploy for version not published to Nuget
                if (deploymentKind == WebAppDeploymentKind.Git && dotnetVersion == "latest")
                {
                    CopyToProjectDirectory(testDirectory, Asset("Nuget.latest.config"), "NuGet.config");
                }

                InjectMiddlware(testDirectory, RuntimeInformationMiddlewareType, RuntimeInformationMiddlewareFile);
                FixAspNetCoreVersion(testDirectory, dotnet.Command);

                var site = await siteTask;
                await site.Deploy(deploymentKind, testDirectory, dotnet, logger);

                using (var httpClient = site.CreateClient())
                {
                    var getResult = await httpClient.GetAsync("/");
                    getResult.EnsureSuccessStatusCode();
                    Assert.Contains(expected, await getResult.Content.ReadAsStringAsync());

                    getResult = await httpClient.GetAsync("/runtimeInfo");
                    getResult.EnsureSuccessStatusCode();

                    var runtimeInfo = JsonConvert.DeserializeObject<RuntimeInfo>(await getResult.Content.ReadAsStringAsync());
                    ValidateStoreRuntimeInfo(runtimeInfo, dotnet.Command);
                }
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

        private static void CopyToProjectDirectory(DirectoryInfo projectRoot, string fileName, string desinationFileName = null)
        {
            // Copy implementation file to project directory
            var implementationFile = Path.Combine(Directory.GetCurrentDirectory(), fileName);
            File.Copy(implementationFile, Path.Combine(projectRoot.FullName, desinationFileName ?? Path.GetFileName(fileName)), true);
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

        private TestLogger GetLogger([CallerMemberName] string callerName = null)
        {
            _fixture.TestLog.StartTestLog(_outputHelper, nameof(TemplateFunctionalTests), out var factory, callerName);
            return new TestLogger(factory, factory.CreateLogger(callerName));
        }

        private TestCommand DotNet(TestLogger logger, DirectoryInfo workingDirectory, string suffix)
        {
            return new TestCommand(GetDotNetPath(suffix))
            {
                Logger = logger,
                WorkingDirectory = workingDirectory.FullName
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

        private DirectoryInfo GetTestDirectory([CallerMemberName] string callerName = null)
        {
            if (Directory.Exists(callerName))
            {
                try
                {
                    Directory.Delete(callerName, recursive: true);
                }
                catch { }
            }
            return Directory.CreateDirectory(callerName);
        }

        private static string Asset(string name)
        {
            return "Assets\\" + name;
        }
    }
}