// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.BrowserTesting;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.CommandLineUtils;
using Newtonsoft.Json.Linq;
using Microsoft.Playwright;
using Templates.Test.Helpers;

namespace BlazorTemplates.Tests;

public class BlazorWasmTemplateTest : BlazorTemplateTest
{
    public BlazorWasmTemplateTest(ProjectFactoryFixture projectFactory)
        : base(projectFactory) { }

    public override string ProjectType { get; } = "blazorwasm";

    [Theory]
    [InlineData(BrowserKind.Chromium)]
    public async Task BlazorWasmStandaloneTemplate_Works(BrowserKind browserKind)
    {
        var project = await CreateBuildPublishAsync();

        // The service worker assets manifest isn't generated for non-PWA projects
        var publishDir = Path.Combine(project.TemplatePublishDir, "wwwroot");
        Assert.False(File.Exists(Path.Combine(publishDir, "service-worker-assets.js")), "Non-PWA templates should not produce service-worker-assets.js");

        await BuildAndRunTest(project.ProjectName, project, browserKind);

        var (serveProcess, listeningUri) = RunPublishedStandaloneBlazorProject(project);
        using (serveProcess)
        {
            Output.WriteLine($"Opening browser at {listeningUri}...");
            if (BrowserManager.IsAvailable(browserKind))
            {
                await using var browser = await BrowserManager.GetBrowserInstance(browserKind, BrowserContextInfo);
                var page = await NavigateToPage(browser, listeningUri);
                await TestBasicNavigation(project.ProjectName, page);
            }
            else
            {
                EnsureBrowserAvailable(browserKind);
            }
        }
    }

    private static async Task<IPage> NavigateToPage(IBrowserContext browser, string listeningUri)
    {
        var page = await browser.NewPageAsync();
        await page.GotoAsync(listeningUri, new() { WaitUntil = WaitUntilState.NetworkIdle });
        return page;
    }

    [Theory(Skip="https://github.com/dotnet/aspnetcore/issues/46430")]
    [InlineData(BrowserKind.Chromium)]
    public async Task BlazorWasmHostedTemplate_Works(BrowserKind browserKind)
    {
        var project = await CreateBuildPublishAsync(args: new[] { "--hosted" }, serverProject: true);

        var serverProject = GetSubProject(project, "Server", $"{project.ProjectName}.Server");

        await BuildAndRunTest(project.ProjectName, serverProject, browserKind);

        using var aspNetProcess = serverProject.StartPublishedProjectAsync();

        Assert.False(
            aspNetProcess.Process.HasExited,
            ErrorMessages.GetFailedProcessMessageOrEmpty("Run published project", serverProject, aspNetProcess.Process));

        await aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");
        await AssertCompressionFormat(aspNetProcess, "br");

        if (BrowserManager.IsAvailable(browserKind))
        {
            await using var browser = await BrowserManager.GetBrowserInstance(browserKind, BrowserContextInfo);
            var page = await browser.NewPageAsync();
            await aspNetProcess.VisitInBrowserAsync(page);
            await TestBasicNavigation(project.ProjectName, page);
        }
        else
        {
            EnsureBrowserAvailable(browserKind);
        }
    }

    private static async Task AssertCompressionFormat(AspNetProcess aspNetProcess, string expectedEncoding)
    {
        var response = await aspNetProcess.SendRequest(() =>
        {
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri(aspNetProcess.ListeningUri, "/_framework/blazor.boot.json"));
            // These are the same as chrome
            request.Headers.AcceptEncoding.Clear();
            request.Headers.AcceptEncoding.Add(StringWithQualityHeaderValue.Parse("gzip"));
            request.Headers.AcceptEncoding.Add(StringWithQualityHeaderValue.Parse("deflate"));
            request.Headers.AcceptEncoding.Add(StringWithQualityHeaderValue.Parse("br"));

            return request;
        });
        Assert.Equal(expectedEncoding, response.Content.Headers.ContentEncoding.Single());
    }

    [Theory(Skip = "https://github.com/dotnet/aspnetcore/issues/45736")]
    [InlineData(BrowserKind.Chromium)]
    public async Task BlazorWasmStandalonePwaTemplate_Works(BrowserKind browserKind)
    {
        var project = await CreateBuildPublishAsync(args: new[] { "--pwa" });

        await BuildAndRunTest(project.ProjectName, project, browserKind);

        ValidatePublishedServiceWorker(project);

        if (BrowserManager.IsAvailable(browserKind))
        {
            var (serveProcess, listeningUri) = RunPublishedStandaloneBlazorProject(project);
            await using var browser = await BrowserManager.GetBrowserInstance(browserKind, BrowserContextInfo);
            Output.WriteLine($"Opening browser at {listeningUri}...");
            var page = await NavigateToPage(browser, listeningUri);
            using (serveProcess)
            {
                await TestBasicNavigation(project.ProjectName, page);
            }

            // The PWA template supports offline use. By now, the browser should have cached everything it needs,
            // so we can continue working even without the server.
            await page.GotoAsync("about:blank");
            await browser.SetOfflineAsync(true);
            await page.GotoAsync(listeningUri);
            await TestBasicNavigation(project.ProjectName, page, skipFetchData: true);
            await page.CloseAsync();
        }
        else
        {
            EnsureBrowserAvailable(browserKind);
        }
    }

    [Theory(Skip = "https://github.com/dotnet/aspnetcore/issues/45736")]
    [InlineData(BrowserKind.Chromium)]
    public async Task BlazorWasmHostedPwaTemplate_Works(BrowserKind browserKind)
    {
        var project = await CreateBuildPublishAsync(args: new[] { "--hosted", "--pwa" }, serverProject: true);

        var serverProject = GetSubProject(project, "Server", $"{project.ProjectName}.Server");

        await BuildAndRunTest(project.ProjectName, serverProject, browserKind);

        ValidatePublishedServiceWorker(serverProject);

        string listeningUri = null;
        if (BrowserManager.IsAvailable(browserKind))
        {
            await using var browser = await BrowserManager.GetBrowserInstance(browserKind, BrowserContextInfo);
            IPage page = null;
            using (var aspNetProcess = serverProject.StartPublishedProjectAsync())
            {
                Assert.False(
                    aspNetProcess.Process.HasExited,
                    ErrorMessages.GetFailedProcessMessageOrEmpty("Run published project", serverProject, aspNetProcess.Process));

                await aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");
                page = await browser.NewPageAsync();
                await aspNetProcess.VisitInBrowserAsync(page);
                await TestBasicNavigation(project.ProjectName, page);

                // Note: we don't want to use aspNetProcess.ListeningUri because that isn't necessarily the HTTPS URI
                listeningUri = new Uri(page.Url).GetLeftPart(UriPartial.Authority);
            }

            // The PWA template supports offline use. By now, the browser should have cached everything it needs,
            // so we can continue working even without the server.
            // Since this is the hosted project, backend APIs won't work offline, so we need to skip "fetchdata"
            await page.GotoAsync("about:blank");
            await browser.SetOfflineAsync(true);
            await page.GotoAsync(listeningUri);
            await TestBasicNavigation(project.ProjectName, page, skipFetchData: true);
            await page.CloseAsync();
        }
        else
        {
            EnsureBrowserAvailable(browserKind);
        }
    }

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
    }

    [ConditionalTheory(Skip="https://github.com/dotnet/aspnetcore/issues/46430")]
    [InlineData(BrowserKind.Chromium)]
    // LocalDB doesn't work on non Windows platforms
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
    public Task BlazorWasmHostedTemplate_IndividualAuth_Works_WithLocalDB(BrowserKind browserKind)
        => BlazorWasmHostedTemplate_IndividualAuth_Works(browserKind, true);

    // This test depends on BlazorWasmTemplate_CreateBuildPublish_IndividualAuthNoLocalDb running first
    [Theory(Skip="https://github.com/dotnet/aspnetcore/issues/46430")]
    [InlineData(BrowserKind.Chromium)]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/30825", Queues = "All.OSX")]
    public Task BlazorWasmHostedTemplate_IndividualAuth_Works_WithOutLocalDB(BrowserKind browserKind)
        => BlazorWasmHostedTemplate_IndividualAuth_Works(browserKind, false);

    private async Task<Project> CreateBuildPublishIndividualAuthProject(bool useLocalDb)
    {
        // Additional arguments are needed. See: https://github.com/dotnet/aspnetcore/issues/24278
        Environment.SetEnvironmentVariable("EnableDefaultScopedCssItems", "true");

        var project = await CreateBuildPublishAsync(args: new[] { "--hosted", "-au", "Individual", useLocalDb ? "-uld" : "" });

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

        await serverProject.RunDotNetPublishAsync();

        // Run dotnet build after publish. The reason is that one uses Config = Debug and the other uses Config = Release
        // The output from publish will go into bin/Release/netcoreappX.Y/publish and won't be affected by calling build
        // later, while the opposite is not true.

        await serverProject.RunDotNetBuildAsync();

        await serverProject.RunDotNetEfCreateMigrationAsync("blazorwasm");
        serverProject.AssertEmptyMigration("blazorwasm");

        if (useLocalDb)
        {
            await serverProject.RunDotNetEfUpdateDatabaseAsync();
        }

        return project;
    }

    private async Task BlazorWasmHostedTemplate_IndividualAuth_Works(BrowserKind browserKind, bool useLocalDb)
    {
        var project = await CreateBuildPublishIndividualAuthProject(useLocalDb: useLocalDb);

        var serverProject = GetSubProject(project, "Server", $"{project.ProjectName}.Server");

        await BuildAndRunTest(project.ProjectName, serverProject, browserKind, usesAuth: true);

        UpdatePublishedSettings(serverProject);

        if (BrowserManager.IsAvailable(browserKind))
        {
            using var aspNetProcess = serverProject.StartPublishedProjectAsync();

            Assert.False(
                aspNetProcess.Process.HasExited,
                ErrorMessages.GetFailedProcessMessageOrEmpty("Run published project", serverProject, aspNetProcess.Process));

            await aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");

            await using var browser = await BrowserManager.GetBrowserInstance(browserKind, BrowserContextInfo);
            var page = await browser.NewPageAsync();
            await aspNetProcess.VisitInBrowserAsync(page);
            await TestBasicNavigation(project.ProjectName, page, usesAuth: true);
            await page.CloseAsync();
        }
        else
        {
            EnsureBrowserAvailable(browserKind);
        }
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

    [Theory(Skip = "https://github.com/dotnet/aspnetcore/issues/37782")]
    [MemberData(nameof(TemplateData))]
    public Task BlazorWasmHostedTemplate_AzureActiveDirectoryTemplate_Works(TemplateInstance instance)
        => CreateBuildPublishAsync(args: instance.Arguments, targetFramework: "netstandard2.1");

    protected async Task BuildAndRunTest(string appName, Project project, BrowserKind browserKind, bool usesAuth = false)
    {
        using var aspNetProcess = project.StartBuiltProjectAsync();

        Assert.False(
            aspNetProcess.Process.HasExited,
            ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, aspNetProcess.Process));

        await aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");
        if (BrowserManager.IsAvailable(browserKind))
        {
            await using var browser = await BrowserManager.GetBrowserInstance(browserKind, BrowserContextInfo);
            var page = await browser.NewPageAsync();
            await aspNetProcess.VisitInBrowserAsync(page);
            await TestBasicNavigation(appName, page, usesAuth);
            await page.CloseAsync();
        }
        else
        {
            EnsureBrowserAvailable(browserKind);
        }
    }

    private static async Task TestBasicNavigation(string appName, IPage page, bool usesAuth = false, bool skipFetchData = false)
    {
        await page.WaitForSelectorAsync("nav");

        // Initially displays the home page
        await page.WaitForSelectorAsync("h1 >> text=Hello, world!");

        Assert.Equal("Index", (await page.TitleAsync()).Trim());

        // Can navigate to the counter page
        await Task.WhenAll(
            page.WaitForNavigationAsync(new() { UrlString = "**/counter" }),
            page.WaitForSelectorAsync("h1 >> text=Counter"),
            page.WaitForSelectorAsync("p >> text=Current count: 0"),
            page.ClickAsync("a[href=counter]"));

        // Clicking the counter button works
        await Task.WhenAll(
            page.WaitForSelectorAsync("p >> text=Current count: 1"),
            page.ClickAsync("p+button >> text=Click me"));

        if (usesAuth)
        {
            await Task.WhenAll(
                page.WaitForNavigationAsync(new() { UrlString = "**/Identity/Account/Login**", WaitUntil = WaitUntilState.NetworkIdle }),
                page.ClickAsync("text=Log in"));

            await Task.WhenAll(
                page.WaitForSelectorAsync("[name=\"Input.Email\"]"),
                page.WaitForNavigationAsync(new() { UrlString = "**/Identity/Account/Register**", WaitUntil = WaitUntilState.NetworkIdle }),
                page.ClickAsync("text=Register as a new user"));

            var userName = $"{Guid.NewGuid()}@example.com";
            var password = "[PLACEHOLDER]-1a";

            await page.TypeAsync("[name=\"Input.Email\"]", userName);
            await page.TypeAsync("[name=\"Input.Password\"]", password);
            await page.TypeAsync("[name=\"Input.ConfirmPassword\"]", password);

            // We will be redirected to the RegisterConfirmation
            await Task.WhenAll(
                page.WaitForNavigationAsync(new() { UrlString = "**/Identity/Account/RegisterConfirmation**", WaitUntil = WaitUntilState.NetworkIdle }),
                page.ClickAsync("#registerSubmit"));

            // We will be redirected to the ConfirmEmail
            await Task.WhenAll(
                page.WaitForNavigationAsync(new() { UrlString = "**/Identity/Account/ConfirmEmail**", WaitUntil = WaitUntilState.NetworkIdle }),
                page.ClickAsync("text=Click here to confirm your account"));

            // Now we can login
            await page.ClickAsync("text=Login");
            await page.WaitForSelectorAsync("[name=\"Input.Email\"]");
            await page.TypeAsync("[name=\"Input.Email\"]", userName);
            await page.TypeAsync("[name=\"Input.Password\"]", password);
            await page.ClickAsync("#login-submit");

            // Need to navigate to fetch page
            await page.GotoAsync(new Uri(page.Url).GetLeftPart(UriPartial.Authority));
            Assert.Equal(appName.Trim(), (await page.TitleAsync()).Trim());
        }

        if (!skipFetchData)
        {
            // Can navigate to the 'fetch data' page
            await Task.WhenAll(
                page.WaitForNavigationAsync(new() { UrlString = "**/fetchdata" }),
                page.WaitForSelectorAsync("h1 >> text=Weather forecast"),
                page.ClickAsync("text=Fetch data"));

            // Asynchronously loads and displays the table of weather forecasts
            await page.WaitForSelectorAsync("table>tbody>tr");
            Assert.Equal(5, await page.Locator("p+table>tbody>tr").CountAsync());
        }
    }

    private static string ReadFile(string basePath, string path)
    {
        var fullPath = Path.Combine(basePath, path);
        var doesExist = File.Exists(fullPath);

        Assert.True(doesExist, $"Expected file to exist, but it doesn't: {path}");
        return File.ReadAllText(Path.Combine(basePath, path));
    }

    private static void UpdatePublishedSettings(Project serverProject)
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

    private (ProcessEx, string url) RunPublishedStandaloneBlazorProject(Project project)
    {
        var publishDir = Path.Combine(project.TemplatePublishDir, "wwwroot");

        Output.WriteLine("Running dotnet serve on published output...");
        var developmentCertificate = DevelopmentCertificate.Create(project.TemplateOutputDir);
        var args = $"-S --pfx \"{developmentCertificate.CertificatePath}\" --pfx-pwd \"{developmentCertificate.CertificatePassword}\" --port 0";
        var command = DotNetMuxer.MuxerPathOrDefault();
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HELIX_DIR")))
        {
            args = $"serve " + args;
        }
        else
        {
            command = "dotnet-serve";
            args = "--roll-forward LatestMajor " + args; // dotnet-serve targets net5.0 by default
        }

        var serveProcess = ProcessEx.Run(TestOutputHelper, publishDir, command, args);
        var listeningUri = ResolveListeningUrl(serveProcess);
        return (serveProcess, listeningUri);
    }

    private static string ResolveListeningUrl(ProcessEx process)
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

        throw new InvalidOperationException(@$"Couldn't find listening url:
{string.Join(Environment.NewLine, buffer.Append(process.Error))}");
    }
}
