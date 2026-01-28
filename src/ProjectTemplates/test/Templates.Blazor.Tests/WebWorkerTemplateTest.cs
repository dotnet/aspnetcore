// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.BrowserTesting;
using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Playwright;
using Templates.Test.Helpers;

namespace BlazorTemplates.Tests;

#pragma warning disable xUnit1041 // Fixture arguments to test classes must have fixture sources

/// <summary>
/// E2E tests for the .NET Web Worker template that verify the worker
/// can be created and invoked from a Blazor WebAssembly application.
/// </summary>
public class WebWorkerTemplateTest(ProjectFactoryFixture projectFactory) : BlazorTemplateTest(projectFactory)
{
    public override string ProjectType { get; } = "webworker";

    [Theory]
    [InlineData(BrowserKind.Chromium)]
    public async Task WebWorker_CanBeUsedFromBlazorWasm(BrowserKind browserKind)
    {
        // Skip if browser is not available
        if (!BrowserManager.IsAvailable(browserKind))
        {
            EnsureBrowserAvailable(browserKind);
            return;
        }

        // Create the worker library project
        var workerLibProject = await ProjectFactory.CreateProject(Output);
        await workerLibProject.RunDotNetNewAsync("webworker");

        // Create a Blazor WASM app to host the worker
        var blazorProject = await ProjectFactory.CreateProject(Output);
        await blazorProject.RunDotNetNewAsync("blazorwasm");

        // Add reference to the worker library using ProcessEx.Run
        using var addRefExecution = ProcessEx.Run(
            Output,
            blazorProject.TemplateOutputDir,
            DotNetMuxer.MuxerPathOrDefault(),
            $"add reference \"{workerLibProject.TemplateOutputDir}\"");
        await addRefExecution.Exited;
        Assert.True(addRefExecution.ExitCode == 0, $"Failed to add project reference: {addRefExecution.Error}");

        // Copy the test worker class from TestAssets to the worker library
        var testAssetsDir = Path.Combine(AppContext.BaseDirectory, "TestAssets", "WorkerLibTest");
        var workerSourcePath = Path.Combine(testAssetsDir, "TestWorker.cs");
        var workerDestPath = Path.Combine(workerLibProject.TemplateOutputDir, "TestWorker.cs");
        File.Copy(workerSourcePath, workerDestPath, overwrite: true);

        // Enable unsafe blocks in the worker library (required for JSExport)
        var workerCsproj = Path.Combine(workerLibProject.TemplateOutputDir, $"{workerLibProject.ProjectName}.csproj");
        var csprojContent = File.ReadAllText(workerCsproj);
        csprojContent = csprojContent.Replace(
            "</PropertyGroup>",
            "  <AllowUnsafeBlocks>true</AllowUnsafeBlocks>\n  </PropertyGroup>");
        File.WriteAllText(workerCsproj, csprojContent);

        // Copy the test page from TestAssets to the Blazor app
        var testPageSourcePath = Path.Combine(testAssetsDir, "WorkerTest.razor");
        var pagesDir = Path.Combine(blazorProject.TemplateOutputDir, "Pages");
        var testPageDestPath = Path.Combine(pagesDir, "WorkerTest.razor");
        File.Copy(testPageSourcePath, testPageDestPath, overwrite: true);

        // Build and publish the Blazor project
        await blazorProject.RunDotNetPublishAsync(noRestore: false);

        // Test the published project
        var (serveProcess, listeningUri) = RunPublishedStandaloneBlazorProject(blazorProject);
        using (serveProcess)
        {
            await using var browser = await BrowserManager.GetBrowserInstance(browserKind, BrowserContextInfo);
            var page = await browser.NewPageAsync();

            Output.WriteLine($"Opening browser at {listeningUri}...");
            await page.GotoAsync(listeningUri, new() { WaitUntil = WaitUntilState.NetworkIdle });

            // Navigate to the worker test page
            await page.GotoAsync($"{listeningUri}workertest", new() { WaitUntil = WaitUntilState.NetworkIdle });

            // Wait for the page to load
            await page.WaitForSelectorAsync("h1 >> text=Worker Test");

            // Initialize the worker
            await page.ClickAsync("#init-worker");

            // Wait for worker to be ready (with retries for WebAssembly initialization)
            const int maxAttempts = 10;
            const int delayMs = 1000;
            var workerReady = false;

            for (var i = 0; i < maxAttempts; i++)
            {
                var status = await page.TextContentAsync("#status");
                if (status?.Contains("Worker ready") == true)
                {
                    workerReady = true;
                    break;
                }
                if (status?.Contains("Error") == true)
                {
                    Assert.Fail($"Worker initialization failed: {status}");
                }
                await Task.Delay(delayMs);
            }

            Assert.True(workerReady, "Worker did not initialize within the expected time");

            // Call the worker
            await page.ClickAsync("#call-worker");

            // Wait for result
            var resultReceived = false;
            for (var i = 0; i < maxAttempts; i++)
            {
                var result = await page.TextContentAsync("#result");
                if (result?.Contains("Worker received: Hello from test!") == true)
                {
                    resultReceived = true;
                    break;
                }
                if (result?.Contains("Error") == true)
                {
                    Assert.Fail($"Worker call failed: {result}");
                }
                await Task.Delay(delayMs);
            }

            Assert.True(resultReceived, "Worker did not return the expected result");

            await page.CloseAsync();
        }
    }

    private (ProcessEx, string url) RunPublishedStandaloneBlazorProject(Project project)
    {
        var publishDir = Path.Combine(project.TemplatePublishDir, "wwwroot");

        Output.WriteLine("Running dotnet serve on published output...");
        var command = DotNetMuxer.MuxerPathOrDefault();
        string args;
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HELIX_DIR")))
        {
            args = "serve";
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
