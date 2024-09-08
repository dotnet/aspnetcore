// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.BrowserTesting;
using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Playwright;
using Templates.Test.Helpers;

namespace BlazorTemplates.Tests;

#pragma warning disable xUnit1041 // Fixture arguments to test classes must have fixture sources

public class BlazorWasmTemplateTest(ProjectFactoryFixture projectFactory) : BlazorTemplateTest(projectFactory)
{
    public override string ProjectType { get; } = "blazorwasm";

    [Theory]
    [InlineData(BrowserKind.Chromium)]
    public async Task BlazorWasmStandaloneTemplate_Works(BrowserKind browserKind)
    {
        var project = await CreateBuildPublishAsync();
        var appName = project.ProjectName;

        // The service worker assets manifest isn't generated for non-PWA projects
        var publishDir = Path.Combine(project.TemplatePublishDir, "wwwroot");
        Assert.False(File.Exists(Path.Combine(publishDir, "service-worker-assets.js")), "Non-PWA templates should not produce service-worker-assets.js");

        // Test the built project
        using (var aspNetProcess = project.StartBuiltProjectAsync())
        {
            Assert.False(
                aspNetProcess.Process.HasExited,
                ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, aspNetProcess.Process));

            await aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");
            await TestBasicInteractionInNewPageAsync(browserKind, aspNetProcess.ListeningUri.AbsoluteUri, appName);
        }

        // Test the published project
        var (serveProcess, listeningUri) = RunPublishedStandaloneBlazorProject(project);
        using (serveProcess)
        {
            await TestBasicInteractionInNewPageAsync(browserKind, listeningUri, appName);
        }
    }

    [Theory]
    [InlineData(BrowserKind.Chromium)]
    public async Task BlazorWasmStandalonePwaTemplate_Works(BrowserKind browserKind)
    {
        var project = await CreateBuildPublishAsync(args: ["--pwa"]);
        var appName = project.ProjectName;

        // Test the built project
        using (var aspNetProcess = project.StartBuiltProjectAsync())
        {
            Assert.False(
                aspNetProcess.Process.HasExited,
                ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, aspNetProcess.Process));

            await aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");
            await TestBasicInteractionInNewPageAsync(browserKind, aspNetProcess.ListeningUri.AbsoluteUri, appName);
        }

        ValidatePublishedServiceWorker(project);

        // Test the published project
        if (BrowserManager.IsAvailable(browserKind))
        {
            var (serveProcess, listeningUri) = RunPublishedStandaloneBlazorProject(project);
            await using var browser = await BrowserManager.GetBrowserInstance(browserKind, BrowserContextInfo);
            Output.WriteLine($"Opening browser at {listeningUri}...");
            var page = await browser.NewPageAsync();
            await page.GotoAsync(listeningUri, new() { WaitUntil = WaitUntilState.NetworkIdle });
            using (serveProcess)
            {
                await TestBasicInteractionAsync(page, project.ProjectName);
            }

            // The PWA template supports offline use. By now, the browser should have cached everything it needs,
            // so we can continue working even without the server.
            await page.GotoAsync("about:blank");
            await browser.SetOfflineAsync(true);
            await page.GotoAsync(listeningUri);
            await TestBasicInteractionAsync(page, project.ProjectName, pagesToExclude: BlazorTemplatePages.Weather);
            await page.CloseAsync();
        }
        else
        {
            EnsureBrowserAvailable(browserKind);
        }
    }

    [Theory]
    [MemberData(nameof(TemplateData))]
    public Task BlazorWasmStandaloneTemplate_AzureActiveDirectoryTemplate_Works(TemplateInstance instance)
        => CreateBuildPublishAsync(args: instance.Arguments, targetFramework: "netstandard2.1");

    private static void ValidatePublishedServiceWorker(Project project)
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

        static string ReadFile(string basePath, string path)
        {
            var fullPath = Path.Combine(basePath, path);
            var doesExist = File.Exists(fullPath);

            Assert.True(doesExist, $"Expected file to exist, but it doesn't: {path}");
            return File.ReadAllText(Path.Combine(basePath, path));
        }
    }

    public static TheoryData<TemplateInstance> TemplateData => new TheoryData<TemplateInstance>
        {
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

    private (ProcessEx, string url) RunPublishedStandaloneBlazorProject(Project project)
    {
        var publishDir = Path.Combine(project.TemplatePublishDir, "wwwroot");

        Output.WriteLine("Running dotnet serve on published output...");
        var command = DotNetMuxer.MuxerPathOrDefault();
        string args;
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HELIX_DIR")))
        {
            args = $"serve ";
        }
        else
        {
            command = "dotnet-serve";
            args = "--roll-forward LatestMajor"; // dotnet-serve targets net5.0 by default
        }

        var serveProcess = ProcessEx.Run(TestOutputHelper, publishDir, command, args);
        var listeningUri = ResolveListeningUrl(serveProcess);
        return (serveProcess, listeningUri);

        static string ResolveListeningUrl(ProcessEx process)
        {
            var buffer = new List<string>();
            try
            {
                foreach (var line in process.OutputLinesAsEnumerable)
                {
                    if (line != null)
                    {
                        buffer.Add(line);
                        if (line.Trim().Contains("https://", StringComparison.Ordinal) || line.Trim().Contains("http://", StringComparison.Ordinal))
                        {
                            return line.Trim();
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }

            throw new InvalidOperationException(
                $"Couldn't find listening url:\n{string.Join(Environment.NewLine, buffer.Append(process.Error))}");
        }
    }
}
