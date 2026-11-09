// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using Components.McpTestApp.E2E.Tests.Fixtures;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.AspNetCore.Components.Testing.Playwright;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace Components.McpTestApp.E2E.Tests.Tests;

[UITest]
public partial class McpBrowserTest : BrowserTest
{
    private readonly ConcurrentQueue<string> _errors = new();
    private readonly ConcurrentQueue<string> _webSockets = new();
    private Uri _rootUri = null!;

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task AuthoredResource_ExecutesRuntimeOnceAcrossInitialResultAndFormUpdates(bool preserveDom)
    {
        var page = await CreatePageAsync(preserveDom);
        using var http = CreateClient();
        await using var client = await ModelContextProtocol.Client.McpClient.CreateAsync(
            new ModelContextProtocol.Client.HttpClientTransport(new()
            {
                Endpoint = new Uri(_rootUri, "mcp"),
                TransportMode = ModelContextProtocol.Client.HttpTransportMode.StreamableHttp,
            }));
        var resource = await client.ReadResourceAsync("ui://blazor/component_form");
        var html = Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsInstanceOfType<ModelContextProtocol.Protocol.TextResourceContents>(
            Assert.ContainsSingle(resource.Contents)).Text;
        Assert.Contains("id=\"authored-shell\"", html);
        Assert.Contains("id=\"authored-runtime\" src=\"blazor.mcp.js\"", html);
        Assert.DoesNotContain("id=\"name\"", html);
        using var asset = await http.GetAsync("blazor.mcp.js");
        Assert.AreEqual(System.Net.HttpStatusCode.OK, asset.StatusCode);
        var runtimeFetches = new ConcurrentQueue<string>();
        var initialized = new ConcurrentQueue<string>();
        await page.AddInitScriptAsync("""
            if (window === top) addEventListener("message", event => {
                if (event.data?.method === "ui/notifications/initialized") {
                    console.info("MCP-AUTHORED-INITIALIZED");
                }
            });
            """);
        page.Request += (_, request) =>
        {
            if (new Uri(request.Url).AbsolutePath.EndsWith("/blazor.mcp.js", StringComparison.Ordinal))
            {
                runtimeFetches.Enqueue(request.Url);
            }
        };
        page.Console += (_, message) =>
        {
            if (message.Text == "MCP-AUTHORED-INITIALIZED")
            {
                initialized.Enqueue(message.Text);
            }
        };
        await ResetAsync(http, "mcp");
        var app = await OpenAsync(page);
        await Expect(app.Locator("#authored-runtime")).ToHaveAttributeAsync("src", "blazor.mcp.js");
        await AssertSubmissionsAsync(app);
        await AssertDisposedAsync(http, "mcp", 1, minimumOperations: 3);
        await CloseAsync(page);
        Assert.HasCount(1, runtimeFetches);
        Assert.HasCount(1, initialized);
        Assert.IsEmpty(_errors, string.Join(Environment.NewLine, _errors));
    }

    [TestMethod]
    public async Task ScriptOmission_LeavesTheAuthoredResourceWithoutAnInjectedRuntime()
    {
        var page = await CreatePageAsync();
        await using var client = await ModelContextProtocol.Client.McpClient.CreateAsync(
            new ModelContextProtocol.Client.HttpClientTransport(new()
            {
                Endpoint = new Uri(_rootUri, "mcp"),
                TransportMode = ModelContextProtocol.Client.HttpTransportMode.StreamableHttp,
            }));
        var resource = await client.ReadResourceAsync("ui://blazor/no_script");
        var html = Assert.IsInstanceOfType<ModelContextProtocol.Protocol.TextResourceContents>(
            Assert.ContainsSingle(resource.Contents)).Text;
        Assert.Contains("id=\"no-script-shell\"", html);
        Assert.DoesNotContain("<script", html);
        Assert.DoesNotContain("blazor.mcp.js", html);
        await page.GotoAsync(new Uri(_rootUri, "host?tool=no_script").AbsoluteUri);
        await page.Locator("#open").ClickAsync();
        var app = page.FrameLocator("#sandbox-frame").FrameLocator("#app-frame");
        await Expect(app.Locator("#no-script-shell")).ToBeVisibleAsync();
        await Expect(app.Locator("script")).ToHaveCountAsync(0);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task TypedAuthoredShell_DefersRequiredDataUntilTheOpeningResult(bool resultFirst)
    {
        var page = await CreatePageAsync();
        using var http = CreateClient();
        using var reset = await http.PostAsync("test/reset-initial", content: null);
        reset.EnsureSuccessStatusCode();
        await page.GotoAsync(new Uri(_rootUri, $"host?tool=open_initial_form&label=required-view&count=19&resultFirst={resultFirst.ToString().ToLowerInvariant()}").AbsoluteUri);
        await page.Locator("#open").ClickAsync();
        await Expect(page.Locator("#host-status")).ToHaveTextAsync("App ready");
        var app = page.FrameLocator("#sandbox-frame").FrameLocator("#app-frame");
        await Expect(app.Locator("#initial-label")).ToHaveTextAsync("required-view");
        await Expect(app.Locator("#initial-count")).ToHaveTextAsync("19");
        await Expect(app.Locator("#typed-shell")).ToHaveCountAsync(0);
        await Expect(app.Locator("#typed-runtime")).ToHaveAttributeAsync("src", "blazor.mcp.js");
        await app.Locator("input[name='Input.Name']").FillAsync("typed-post");
        await app.Locator("button[type=submit]").ClickAsync();
        await Expect(app.Locator("#typed-value")).ToHaveTextAsync("typed-post");
        await Expect(app.Locator("#initial-label")).ToHaveTextAsync("required-view:changed");
        await CloseAsync(page);
        using var state = JsonDocument.Parse(await http.GetStringAsync("test/initial-observations"));
        Assert.AreEqual(1, state.RootElement.GetProperty("openingCalls").GetInt32());
        Assert.AreEqual(1, state.RootElement.GetProperty("projectionCalls").GetInt32());
        Assert.AreEqual(2, state.RootElement.GetProperty("renders").GetInt32());
        Assert.AreEqual(1, state.RootElement.GetProperty("submissions").GetInt32());
        Assert.IsEmpty(_errors, string.Join(Environment.NewLine, _errors));
    }

    [TestMethod]
    public async Task FirstSubmission_BindsDispatchesAndRendersThroughMcp()
    {
        var page = await CreatePageAsync();
        using var http = CreateClient();
        await ResetAsync(http, "http");
        await ResetAsync(http, "mcp");
        await page.GotoAsync(new Uri(_rootUri, "mcp-form?case=http").AbsoluteUri);
        await AssertSubmissionsAsync(page.Locator("html"));
        await AssertNativeConstraintAsync(page.Locator("html"));
        await AssertDisposedAsync(http, "http", 1, minimumOperations: 3);

        var app = await OpenAsync(page);
        await AssertSubmissionsAsync(app);
        await AssertNativeConstraintAsync(app);
        await AssertDisposedAsync(http, "mcp", 1, minimumOperations: 3);
        await CloseAsync(page);
        Assert.IsEmpty(_errors, string.Join(Environment.NewLine, _errors));
        Assert.IsFalse(_webSockets.Any(uri => uri.Contains("/_blazor", StringComparison.Ordinal)));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public Task RelativeActions_UseTheDocumentBaseUri(bool useSubmitter)
        => AssertActionAsync(useSubmitter ? "submitter-action" : "form-action",
            useSubmitter ? "base-submitter" : "base-form", "base-target");

    [TestMethod]
    public Task EmptyAction_SubmitsToTheCurrentLogicalLocation()
        => AssertActionAsync("empty", "base-empty", "current-target");

    [TestMethod]
    public Task DefaultAction_SubmitsToTheCurrentLogicalLocation()
        => AssertActionAsync("missing", "base-missing", "current-target");

    [TestMethod]
    [DataRow("urlencoded", "upload-urlencoded", true, false, false)]
    [DataRow("multipart", "upload-multipart", true, false, true)]
    [DataRow("multipart", "upload-empty", false, false, false)]
    [DataRow("urlencoded", "upload-override", true, true, true)]
    public async Task FileControls_MatchHttpEncodingAndSubmitterSemantics(
        string encoding, string caseId, bool selectFile, bool useOverride, bool expectFile)
    {
        var page = await CreatePageAsync();
        using var http = CreateClient();
        await ResetAsync(http, "http-" + caseId);
        await ResetAsync(http, caseId);
        var file = new FilePayload { Name = "mcp-upload.txt", MimeType = "text/plain", Buffer = "Mcp upload payload"u8.ToArray() };
        await page.GotoAsync(new Uri(_rootUri, $"mcp-upload?encoding={encoding}&case=http-{caseId}").AbsoluteUri);
        await SubmitUploadAsync(page.Locator("html"));
        await AssertEffectAsync(http, "http-" + caseId, expectFile ? "file" : "none");

        var app = await OpenAsync(page);
        await app.Locator("#" + (useOverride ? "upload-override-link" : caseId)).ClickAsync();
        await SubmitUploadAsync(app);
        await AssertEffectAsync(http, caseId, expectFile ? "file" : "none");
        await CloseAsync(page);
        Assert.IsEmpty(_errors, string.Join(Environment.NewLine, _errors));

        async Task SubmitUploadAsync(ILocator root)
        {
            if (selectFile)
            {
                await root.Locator("#upload").SetInputFilesAsync(file);
            }

            await root.Locator(useOverride ? "#upload-override" : "#upload-submit").ClickAsync();
            await Expect(root.Locator("#upload-result")).ToHaveTextAsync("submitted");
            await Expect(root.Locator("#uploaded-kind")).ToHaveTextAsync(expectFile ? "file" : "none");
            await Expect(root.Locator("#uploaded-field")).ToHaveTextAsync(!selectFile ? "" : expectFile ? "(none)" : file.Name);
            if (expectFile)
            {
                await Expect(root.Locator("#uploaded-name")).ToHaveTextAsync(file.Name);
                await Expect(root.Locator("#uploaded-content-type")).ToHaveTextAsync(file.MimeType);
                await Expect(root.Locator("#uploaded-bytes")).ToHaveTextAsync("4D63702075706C6F6164207061796C6F6164");
            }
        }
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task DomPreservation_UsesMcpOptionsWhileConfigureBrowserIsNoOp(bool preserveDom)
    {
        var page = await CreatePageAsync(preserveDom);
        using var http = CreateClient();
        await ResetAsync(http, "mcp");
        var app = await OpenAsync(page);
        await app.Locator("#configure-browser-link").ClickAsync();
        await Expect(app.Locator("#http-browser-configuration")).ToBeAttachedAsync();
        await app.Locator("#permanent-input").FillAsync("retained browser state");
        await app.Locator("#name").FillAsync("ProofValue");
        await app.Locator("form button[type=submit]").First.ClickAsync();
        await Expect(app.Locator("#submitted-name")).ToHaveTextAsync("ProofValue");
        await Expect(app.Locator("#permanent-input")).ToHaveValueAsync(preserveDom ? "retained browser state" : "initial");
        await CloseAsync(page);
        Assert.IsEmpty(_errors, string.Join(Environment.NewLine, _errors));
    }

    [TestMethod]
    [DataRow(LogLevel.Information, true)]
    [DataRow(LogLevel.None, false)]
    public async Task LogLevel_ControlsFrameworkMessagesWithoutHidingVisibleErrors(LogLevel logLevel, bool expectConsoleError)
    {
        var page = await CreatePageAsync(logLevel: logLevel);
        var app = await OpenAsync(page);
        var reported = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        page.Console += (_, message) =>
        {
            if (message.Type == "error" && message.Text.Contains("exactly one named form handler", StringComparison.Ordinal))
            {
                reported.TrySetResult();
            }
        };
        await app.Locator("#framework-error").ClickAsync();
        await Expect(app.Locator("#blazor-mcp-error")).ToContainTextAsync("exactly one named form handler");
        if (expectConsoleError)
        {
            await reported.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.HasCount(1, _errors, string.Join(Environment.NewLine, _errors));
            Assert.Contains("exactly one named form handler", _errors.Single(), StringComparison.Ordinal);
        }
        else
        {
            Assert.IsEmpty(_errors, string.Join(Environment.NewLine, _errors));
        }

        await CloseAsync(page);
    }

    [TestMethod]
    public async Task SupersedingGet_DoesNotReplayThePendingPostOrReplaceTheNewDocument()
    {
        var page = await CreatePageAsync();
        using var http = CreateClient();
        await ResetAsync(http, "gated-navigation");
        await ResetAsync(http, "gated-next");
        var app = await OpenAsync(page);
        try
        {
            await app.Locator("#gated-navigation-link").ClickAsync();
            await app.Locator("#gated-submit").ClickAsync();
            await WaitForGateAsync(http, "gated-navigation", cancellationExpected: false);
            await app.Locator("#gated-next").ClickAsync();
            await Expect(app.Locator("#name")).ToBeVisibleAsync();
            await AssertPendingScopeAsync(http, "gated-navigation");
            await ReleaseAsync(http, "gated-navigation");
            await Expect(app).ToHaveAttributeAsync("aria-busy", "false");
            await app.Locator("#name").FillAsync("ProofValue");
            await app.Locator("form button[type=submit]").First.ClickAsync();
            await Expect(app.Locator("#submitted-name")).ToHaveTextAsync("ProofValue");
            await AssertDisposedAsync(http, "gated-navigation", 1);
            await AssertDisposedAsync(http, "gated-next", 1);
            await CloseAsync(page);
            Assert.IsEmpty(_errors, string.Join(Environment.NewLine, _errors));
        }
        finally
        {
            await ReleaseAsync(http, "gated-navigation");
        }
    }

    [TestMethod]
    public async Task Teardown_CancelsAndWaitsForTheAdmittedActionBeforeDisposingItsScope()
    {
        var page = await CreatePageAsync();
        using var http = CreateClient();
        await ResetAsync(http, "gated-close");
        var app = await OpenAsync(page);
        try
        {
            await app.Locator("#gated-close-link").ClickAsync();
            await app.Locator("#gated-submit").ClickAsync();
            await WaitForGateAsync(http, "gated-close", cancellationExpected: false);
            await page.Locator("#close").ClickAsync();
            await WaitForGateAsync(http, "gated-close", cancellationExpected: true);
            await AssertPendingScopeAsync(http, "gated-close");
            Assert.AreNotEqual("App closed", await page.Locator("#host-status").InnerTextAsync());
            await ReleaseAsync(http, "gated-close");
            await Expect(page.Locator("#host-status")).ToHaveTextAsync("App closed");
            await AssertDisposedAsync(http, "gated-close", 0);
            Assert.IsEmpty(_errors, string.Join(Environment.NewLine, _errors));
        }
        finally
        {
            await ReleaseAsync(http, "gated-close");
        }
    }

    private async Task<IPage> CreatePageAsync(bool preserveDom = true, LogLevel logLevel = LogLevel.Information)
    {
        var server = await StartServerAsync<McpTestAppMarker>(TestRoot.Servers, options =>
        {
            options.EnvironmentVariables["McpTest__PreserveDom"] = preserveDom.ToString();
            options.EnvironmentVariables["McpTest__LogLevel"] = logLevel.ToString();
        });
        _rootUri = new Uri(server.AppUrl);
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(server));
        var page = await context.NewPageAsync();
        page.Console += (_, message) =>
        {
            if (message.Type == "error")
            {
                _errors.Enqueue(message.Text);
            }
        };
        page.PageError += (_, error) => _errors.Enqueue(error);
        page.WebSocket += (_, socket) => _webSockets.Enqueue(socket.Url);
        return page;
    }

    private async Task<ILocator> OpenAsync(IPage page)
    {
        await page.GotoAsync(new Uri(_rootUri, "host").AbsoluteUri);
        await page.Locator("#open").ClickAsync();
        await Expect(page.Locator("#host-status")).ToHaveTextAsync("App ready");
        var app = page.FrameLocator("#sandbox-frame").FrameLocator("#app-frame").Locator("html");
        await Expect(app.Locator("#name")).ToBeVisibleAsync();
        return app;
    }

    private static async Task CloseAsync(IPage page)
    {
        await page.Locator("#close").ClickAsync();
        await Expect(page.Locator("#host-status")).ToHaveTextAsync("App closed");
    }

    private async Task AssertActionAsync(string mode, string caseId, string expected)
    {
        var page = await CreatePageAsync();
        using var http = CreateClient();
        await ResetAsync(http, "http-" + caseId);
        await ResetAsync(http, caseId);
        await page.GotoAsync(new Uri(_rootUri, $"forms/nested/edit?mode={mode}&case=http-{caseId}").AbsoluteUri);
        await page.Locator("#relative-submit").ClickAsync();
        await Expect(page.Locator("#action-result")).ToHaveTextAsync(expected);
        await AssertEffectAsync(http, "http-" + caseId, expected);

        var app = await OpenAsync(page);
        await app.Locator("#relative-" + mode).ClickAsync();
        await Expect(app.Locator("#relative-location")).ToContainTextAsync("forms/nested/edit");
        Assert.AreEqual(_rootUri.AbsoluteUri, await app.Locator("base").GetAttributeAsync("href"));
        await app.Locator("#relative-submit").ClickAsync();
        await Expect(app.Locator("#action-result")).ToHaveTextAsync(expected);
        await AssertEffectAsync(http, caseId, expected);
        await CloseAsync(page);
        Assert.IsEmpty(_errors, string.Join(Environment.NewLine, _errors));
    }

    private static async Task AssertSubmissionsAsync(ILocator root)
    {
        var originalScope = await root.Locator("#scope-id").InnerTextAsync();
        await root.Locator("#name").FillAsync("ProofValue");
        await root.Locator("form button[type=submit]").First.ClickAsync();
        await Expect(root.Locator("#submitted-name")).ToHaveTextAsync("ProofValue");
        await Expect(root.Locator("#effect-count")).ToHaveTextAsync("1");
        await Expect(root.Locator("#last-effect")).ToHaveTextAsync("ProofValue");
        Assert.AreNotEqual(originalScope, await root.Locator("#scope-id").InnerTextAsync());
        var validScope = await root.Locator("#scope-id").InnerTextAsync();
        await root.Locator("#name").FillAsync("");
        await root.Locator("form button[type=submit]").First.ClickAsync();
        await Expect(root.Locator(".validation-errors")).ToContainTextAsync("The Name field is required.");
        Assert.AreNotEqual(validScope, await root.Locator("#scope-id").InnerTextAsync());
        await Expect(root.Locator("#effect-count")).ToHaveTextAsync("1");
    }

    private static async Task AssertNativeConstraintAsync(ILocator root)
    {
        var scope = await root.Locator("#scope-id").InnerTextAsync();
        await root.Locator("#native-submit").ClickAsync();
        await Expect(root.Locator("#native-required")).Not.ToHaveJSPropertyAsync("validationMessage", "");
        Assert.AreEqual(scope, await root.Locator("#scope-id").InnerTextAsync());
    }

    private HttpClient CreateClient() => new() { BaseAddress = _rootUri };

    private static async Task ResetAsync(HttpClient client, string caseId)
    {
        using var response = await client.PostAsync("test/reset/" + caseId, content: null);
        response.EnsureSuccessStatusCode();
    }

    private static async Task ReleaseAsync(HttpClient client, string caseId)
    {
        using var response = await client.PostAsync($"test/gate/{caseId}/release", content: null);
        response.EnsureSuccessStatusCode();
    }

    private static async Task AssertEffectAsync(HttpClient client, string caseId, string expected)
    {
        using var state = JsonDocument.Parse(await client.GetStringAsync("observations/" + caseId));
        Assert.AreEqual(1, state.RootElement.GetProperty("count").GetInt32());
        Assert.AreEqual(expected, state.RootElement.GetProperty("value").GetString());
    }

    private static async Task AssertPendingScopeAsync(HttpClient client, string caseId)
    {
        using var state = JsonDocument.Parse(await client.GetStringAsync("observations/" + caseId));
        Assert.AreEqual(0, state.RootElement.GetProperty("count").GetInt32());
        Assert.AreEqual(2, state.RootElement.GetProperty("componentsCreated").GetInt32());
        Assert.AreEqual(1, state.RootElement.GetProperty("componentsDisposed").GetInt32());
        Assert.AreEqual(1, state.RootElement.GetProperty("scopesDisposed").GetInt32());
    }

    private static async Task WaitForGateAsync(HttpClient client, string caseId, bool cancellationExpected)
    {
        var elapsed = Stopwatch.StartNew();
        while (true)
        {
            using var state = JsonDocument.Parse(await client.GetStringAsync("test/gate/" + caseId));
            if (state.RootElement.GetProperty("started").GetBoolean()
                && state.RootElement.GetProperty("cancellationObserved").GetBoolean() == cancellationExpected)
            {
                return;
            }

            if (elapsed.Elapsed >= TimeSpan.FromSeconds(5))
            {
                Assert.Fail($"The real form callback did not reach its expected gate state: {state.RootElement}");
            }

            await Task.Delay(50);
        }
    }

    private static async Task AssertDisposedAsync(HttpClient client, string caseId, int expectedEffects, int minimumOperations = 2)
    {
        var elapsed = Stopwatch.StartNew();
        while (true)
        {
            using var state = JsonDocument.Parse(await client.GetStringAsync("observations/" + caseId));
            var value = state.RootElement;
            if (value.GetProperty("componentsCreated").GetInt32() >= minimumOperations
                && value.GetProperty("componentsCreated").GetInt32() == value.GetProperty("componentsDisposed").GetInt32()
                && value.GetProperty("componentsCreated").GetInt32() == value.GetProperty("scopesDisposed").GetInt32())
            {
                Assert.AreEqual(expectedEffects, value.GetProperty("count").GetInt32());
                return;
            }

            if (elapsed.Elapsed >= TimeSpan.FromSeconds(15))
            {
                Assert.Fail($"The operation did not dispose its complete render scope: {value}");
            }

            await Task.Delay(50);
        }
    }
}
