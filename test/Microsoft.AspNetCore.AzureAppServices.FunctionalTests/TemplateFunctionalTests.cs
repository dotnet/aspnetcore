// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.AppService.Fluent.Models;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.AzureAppServices.FunctionalTests
{
    [Collection("Azure")]
    public class TemplateFunctionalTests
    {
        readonly AzureFixture _fixture;

        private readonly ITestOutputHelper _outputHelper;

        public TemplateFunctionalTests(AzureFixture fixture, ITestOutputHelper outputHelper)
        {
            _fixture = fixture;
            _outputHelper = outputHelper;
        }

        [Theory]
        [InlineData("web", "Hello World!")]
        [InlineData("razor", "Learn how to build ASP.NET apps that can run anywhere.")]
        [InlineData("mvc", "Learn how to build ASP.NET apps that can run anywhere.")]
        public async Task DotnetNewWebRunsInWebApp(string template, string expected)
        {
            var testId = nameof(DotnetNewWebRunsInWebApp) + template;

            using (var logger = GetLogger(testId))
            {
                var site = await _fixture.Deploy("Templates\\BasicAppServices.json", baseName: testId);
                var testDirectory = GetTestDirectory(testId);
                var dotnet = DotNet(logger, testDirectory);

                await dotnet.ExecuteAndAssertAsync("new " + template);

                await site.BuildPublishProfileAsync(testDirectory.FullName);

                await dotnet.ExecuteAndAssertAsync("publish /p:PublishProfile=Profile");

                using (var httpClient = site.CreateClient())
                {
                    var getResult = await httpClient.GetAsync("/");
                    getResult.EnsureSuccessStatusCode();
                    Assert.Contains(expected, await getResult.Content.ReadAsStringAsync());
                }
            }
        }

        [Theory]
        [InlineData("web", "Hello World!")]
        [InlineData("razor", "Learn how to build ASP.NET apps that can run anywhere.")]
        [InlineData("mvc", "Learn how to build ASP.NET apps that can run anywhere.")]
        public async Task DotnetNewWebRunsWebAppOnLatestRuntime(string template, string expected)
        {
            var testId = nameof(DotnetNewWebRunsWebAppOnLatestRuntime) + template;

            using (var logger = GetLogger(testId))
            {
                var site = await _fixture.Deploy("Templates\\AppServicesWithSiteExtensions.json",
                    baseName: testId,
                    additionalArguments: new Dictionary<string, string>
                    {
                        { "extensionFeed", AzureFixture.GetRequiredEnvironmentVariable("SiteExtensionFeed") },
                        { "extensionName", "AspNetCoreTestBundle" },
                        { "extensionVersion", GetAssemblyInformationalVersion() },
                    });

                var testDirectory = GetTestDirectory(testId);
                var dotnet = DotNet(logger, testDirectory);

                await dotnet.ExecuteAndAssertAsync("new " + template);

                FixAspNetCoreVersion(testDirectory);

                await dotnet.ExecuteAndAssertAsync("restore");

                await site.BuildPublishProfileAsync(testDirectory.FullName);

                await dotnet.ExecuteAndAssertAsync("publish /p:PublishProfile=Profile");

                using (var httpClient = site.CreateClient())
                {
                    var getResult = await httpClient.GetAsync("/");
                    getResult.EnsureSuccessStatusCode();
                    Assert.Contains(expected, await getResult.Content.ReadAsStringAsync());
                }
            }
        }

        private static void FixAspNetCoreVersion(DirectoryInfo testDirectory)
        {
            // TODO: Temporary workaround for broken templates in latest CLI

            var csproj = testDirectory.GetFiles("*.csproj").Single().FullName;
            var projectContents = XDocument.Load(csproj);
            var packageReference = projectContents
                .Descendants("PackageReference")
                .Single(element => (string) element.Attribute("Include") == "Microsoft.AspNetCore.All");

            // Detect what version of aspnet core was shipped with this CLI installation
            var aspnetCoreVersion =
                new DirectoryInfo(
                    Path.Combine(
                        Path.GetDirectoryName(TestCommand.DotnetPath),
                        "store", "x86", "netcoreapp2.0", "microsoft.aspnetcore"))
                .GetDirectories()
                .Single()
                .Name;

            packageReference.Attribute("Version").Value = aspnetCoreVersion;
            projectContents.Save(csproj);
        }

        private string GetAssemblyInformationalVersion()
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

        private TestCommand DotNet(TestLogger logger, DirectoryInfo workingDirectory)
        {
            return new TestCommand("dotnet")
            {
                Logger = logger,
                WorkingDirectory = workingDirectory.FullName
            };
        }

        private DirectoryInfo GetTestDirectory([CallerMemberName] string callerName = null)
        {
            if (Directory.Exists(callerName))
            {
                Directory.Delete(callerName, recursive:true);
            }
            return Directory.CreateDirectory(callerName);
        }
    }
}