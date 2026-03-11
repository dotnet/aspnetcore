// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.BrowserTesting;
using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Playwright;
using Templates.Test.Helpers;
using Microsoft.AspNetCore.InternalTesting;
using Xunit.Abstractions;

namespace BlazorTemplates.Tests;

#pragma warning disable xUnit1041 // Fixture arguments to test classes must have fixture sources

public class WebWorkerTemplateE2ETest(ProjectFactoryFixture projectFactory) : BlazorTemplateTest(projectFactory)
{
    public override string ProjectType => "blazorwasm";

    private static readonly string TestAssetsPath = Path.Combine(
        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
        "TestAssets",
        "WebWorker");

    private static Project _sharedHostProject;
    private static bool _hostInitialized;

    protected override async Task InitializeCoreAsync(TestContext context)
    {
        await base.InitializeCoreAsync(context);

        if (!_hostInitialized)
        {
            _sharedHostProject = await CreateBuildPublishAsync(onlyCreate: true);
            CopyTestAssets(_sharedHostProject);
            AddHostProjectSettings(_sharedHostProject);
            _hostInitialized = true;
        }
    }

    [Theory]
    [InlineData(BrowserKind.Chromium)]
    public async Task WebWorkerTemplate_CanInvokeMethods(BrowserKind browserKind)
    {
        await using var testRun = await SetupWorkerLibAndBuild(_sharedHostProject);

        using var aspNetProcess = _sharedHostProject.StartBuiltProjectAsync();
        Assert.False(
            aspNetProcess.Process.HasExited,
            ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", _sharedHostProject, aspNetProcess.Process));

        await aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");
        await TestWebWorkerInteraction(browserKind, aspNetProcess.ListeningUri.AbsoluteUri + "webworker-test");
    }

    [Theory]
    [InlineData(BrowserKind.Chromium)]
    public async Task WebWorkerTemplate_HandlesErrors(BrowserKind browserKind)
    {
        await using var testRun = await SetupWorkerLibAndBuild(_sharedHostProject);

        using var aspNetProcess = _sharedHostProject.StartBuiltProjectAsync();
        Assert.False(
            aspNetProcess.Process.HasExited,
            ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", _sharedHostProject, aspNetProcess.Process));

        await aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");
        await TestWebWorkerErrorHandling(browserKind, aspNetProcess.ListeningUri.AbsoluteUri + "webworker-test");
    }

    [Theory]
    [InlineData(BrowserKind.Chromium)]
    public async Task WebWorkerTemplate_CanDisposeWorker(BrowserKind browserKind)
    {
        await using var testRun = await SetupWorkerLibAndBuild(_sharedHostProject);

        using var aspNetProcess = _sharedHostProject.StartBuiltProjectAsync();
        Assert.False(
            aspNetProcess.Process.HasExited,
            ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", _sharedHostProject, aspNetProcess.Process));

        await aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");
        await TestWebWorkerDisposal(browserKind, aspNetProcess.ListeningUri.AbsoluteUri + "webworker-test");
    }

    private async Task<WorkerLibTestRun> SetupWorkerLibAndBuild(Project hostProject)
    {
        var parentDir = Path.GetDirectoryName(hostProject.TemplateOutputDir);
        var workerLibDir = Path.Combine(parentDir, "WorkerLib");

        if (Directory.Exists(workerLibDir))
        {
            Directory.Delete(workerLibDir, recursive: true);
        }
        Directory.CreateDirectory(workerLibDir);

        await CreateWebWorkerLibrary(workerLibDir);
        await AddWorkerLibReferenceAsync(hostProject);
        await hostProject.RunDotNetBuildAsync();

        return new WorkerLibTestRun(workerLibDir, hostProject, Output);
    }

    private sealed class WorkerLibTestRun(string workerLibDir, Project hostProject, ITestOutputHelper output) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            using var result = ProcessEx.Run(
                output,
                hostProject.TemplateOutputDir,
                DotNetMuxer.MuxerPathOrDefault(),
                "remove reference ../WorkerLib/WorkerLib.csproj");
            await result.Exited;

            if (Directory.Exists(workerLibDir))
            {
                try { Directory.Delete(workerLibDir, recursive: true); }
                catch { /* Best effort cleanup */ }
            }
        }
    }

    private async Task CreateWebWorkerLibrary(string workerLibDir)
    {
        var hiveArg = $"--debug:disable-sdk-templates --debug:custom-hive \"{TemplatePackageInstaller.CustomHivePath}\"";
        var args = $"new webworker {hiveArg} -n WorkerLib -o \"{workerLibDir}\"";

        using var result = ProcessEx.Run(Output, AppContext.BaseDirectory, DotNetMuxer.MuxerPathOrDefault(), args);
        await result.Exited;
        Assert.True(result.ExitCode == 0, $"Failed to create webworker template: {result.Output}\n{result.Error}");

        ModifyWorkerLibProjectFile(workerLibDir);

        using var restoreResult = ProcessEx.Run(Output, workerLibDir, DotNetMuxer.MuxerPathOrDefault(), "restore");
        await restoreResult.Exited;
        Assert.True(restoreResult.ExitCode == 0, $"Failed to restore webworker library: {restoreResult.Output}\n{restoreResult.Error}");
    }

    private static void ModifyWorkerLibProjectFile(string workerLibDir)
    {
        var csprojPath = Path.Combine(workerLibDir, "WorkerLib.csproj");
        var content = File.ReadAllText(csprojPath);

        if (!content.Contains("AllowUnsafeBlocks"))
        {
            content = content.Replace(
                "</PropertyGroup>",
                "    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>\n  </PropertyGroup>");
            File.WriteAllText(csprojPath, content);
        }
    }

    private static void AddHostProjectSettings(Project hostProject)
    {
        var csprojPath = Path.Combine(hostProject.TemplateOutputDir, $"{hostProject.ProjectName}.csproj");
        var content = File.ReadAllText(csprojPath);

        var settings = @"
  <PropertyGroup>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <BlazorWebAssemblyLoadAllGlobalizationData>true</BlazorWebAssemblyLoadAllGlobalizationData>
  </PropertyGroup>

  <ItemGroup>
    <SupportedPlatform Include=""browser"" />
  </ItemGroup>
";
        content = content.Replace("</Project>", settings + "</Project>");
        File.WriteAllText(csprojPath, content);
    }

    private async Task AddWorkerLibReferenceAsync(Project hostProject)
    {
        using var result = ProcessEx.Run(
            Output,
            hostProject.TemplateOutputDir,
            DotNetMuxer.MuxerPathOrDefault(),
            "add reference ../WorkerLib/WorkerLib.csproj");
        await result.Exited;
        Assert.True(result.ExitCode == 0, $"Failed to add WorkerLib reference: {result.Output}\n{result.Error}");
    }

    private void CopyTestAssets(Project hostProject)
    {
        var testComponentSource = Path.Combine(TestAssetsPath, "WebWorkerTest.razor");
        var testComponentContent = File.ReadAllText(testComponentSource);

        var pagesDir = Path.Combine(hostProject.TemplateOutputDir, "Components", "Pages");
        if (!Directory.Exists(pagesDir))
        {
            pagesDir = Path.Combine(hostProject.TemplateOutputDir, "Pages");
        }
        File.WriteAllText(
            Path.Combine(pagesDir, "WebWorkerTest.razor"),
            testComponentContent);

        var workerMethodsSource = Path.Combine(TestAssetsPath, "TestWorkerMethods.cs");
        File.Copy(workerMethodsSource, Path.Combine(hostProject.TemplateOutputDir, "TestWorkerMethods.cs"), overwrite: true);
    }

    private async Task TestWebWorkerInteraction(BrowserKind browserKind, string baseUri)
    {
        if (!BrowserManager.IsAvailable(browserKind))
        {
            EnsureBrowserAvailable(browserKind);
            return;
        }

        await using var browser = await BrowserManager.GetBrowserInstance(browserKind, BrowserContextInfo);
        var page = await browser.NewPageAsync();

        await page.GotoAsync(baseUri);
        await page.WaitForSelectorAsync("#webworker-test", new() { Timeout = 15000 });

        await page.ClickAsync("#btn-init");
        await WaitForWorkerInit(page);

        await page.ClickAsync("#btn-add");
        await WaitForElementText(page, "#add-result", "8", timeout: 30000);

        await page.ClickAsync("#btn-echo");
        await WaitForElementText(page, "#echo-result", "Hello Worker", timeout: 30000);

        await page.ClickAsync("#btn-dispose");
        await WaitForElementText(page, "#dispose-status", "Disposed");

        await page.CloseAsync();
    }

    private async Task TestWebWorkerErrorHandling(BrowserKind browserKind, string baseUri)
    {
        if (!BrowserManager.IsAvailable(browserKind))
        {
            EnsureBrowserAvailable(browserKind);
            return;
        }

        await using var browser = await BrowserManager.GetBrowserInstance(browserKind, BrowserContextInfo);
        var page = await browser.NewPageAsync();

        await page.GotoAsync(baseUri);
        await page.WaitForSelectorAsync("#webworker-test", new() { Timeout = 15000 });

        await page.ClickAsync("#btn-init");
        await WaitForWorkerInit(page);

        await page.ClickAsync("#btn-error");
        await WaitForElementText(page, "#error-result", "Caught expected error", timeout: 30000);

        await page.ClickAsync("#btn-dispose");
        await WaitForElementText(page, "#dispose-status", "Disposed");

        await page.CloseAsync();
    }

    private async Task TestWebWorkerDisposal(BrowserKind browserKind, string baseUri)
    {
        if (!BrowserManager.IsAvailable(browserKind))
        {
            EnsureBrowserAvailable(browserKind);
            return;
        }

        await using var browser = await BrowserManager.GetBrowserInstance(browserKind, BrowserContextInfo);
        var page = await browser.NewPageAsync();

        await page.GotoAsync(baseUri);
        await page.WaitForSelectorAsync("#webworker-test", new() { Timeout = 15000 });

        await page.ClickAsync("#btn-init");
        await WaitForWorkerInit(page);

        await page.ClickAsync("#btn-dispose");
        await WaitForElementText(page, "#dispose-status", "Disposed");

        await page.CloseAsync();
    }

    private async Task WaitForElementText(IPage page, string selector, string expectedText, int timeout = 5000)
    {
        try
        {
            await page.WaitForFunctionAsync(
                $"() => document.querySelector('{selector}')?.textContent === '{expectedText}'",
                new PageWaitForFunctionOptions { Timeout = timeout });
        }
        catch (TimeoutException)
        {
            var actualText = await page.EvaluateAsync<string>($"document.querySelector('{selector}')?.textContent ?? 'element not found'");
            throw new TimeoutException($"Timeout waiting for '{selector}' to have text '{expectedText}'. Actual text: '{actualText}'");
        }
    }

    private async Task WaitForWorkerInit(IPage page, int timeout = 60000)
    {
        try
        {
            await page.WaitForFunctionAsync(
                """
                () => {
                    const status = document.querySelector('#init-status')?.textContent;
                    return status === 'Ready' || (status && status.startsWith('Error:'));
                }
                """,
                new PageWaitForFunctionOptions { Timeout = timeout });
        }
        catch (TimeoutException)
        {
            var actualStatus = await page.EvaluateAsync<string>("document.querySelector('#init-status')?.textContent ?? 'element not found'");
            throw new TimeoutException($"Worker initialization timed out after {timeout}ms. Init status: '{actualStatus}'");
        }

        var initStatus = await page.EvaluateAsync<string>("document.querySelector('#init-status')?.textContent");
        if (initStatus?.StartsWith("Error:", StringComparison.Ordinal) == true)
        {
            throw new InvalidOperationException($"Worker initialization failed: {initStatus}");
        }

        Output.WriteLine($"Worker initialized successfully. Status: {initStatus}");
    }
}
