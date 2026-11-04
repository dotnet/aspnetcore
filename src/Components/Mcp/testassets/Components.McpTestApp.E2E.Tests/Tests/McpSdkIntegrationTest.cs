// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using Components.McpTestApp.E2E.Tests.Fixtures;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.AspNetCore.Components.Testing.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace Components.McpTestApp.E2E.Tests.Tests;

[UITest]
public partial class McpSdkIntegrationTest : UITest
{
    private Uri _rootUri = null!;

    protected override async Task InitializeCoreAsync()
    {
        await base.InitializeCoreAsync();
        var server = await StartServerAsync<McpTestAppMarker>(TestRoot.Servers);
        _rootUri = new Uri(server.AppUrl);
    }

    [TestMethod]
    public async Task ExistingTool_PreservesSdkContractAndReconstructsIndependentTypedParameters()
    {
        using var http = await ResetAsync();
        await using var first = await ConnectAsync();
        await using var second = await ConnectAsync();
        var tools = await first.ListToolsAsync();
        var associated = Assert.ContainsSingle(tools.Where(tool => tool.Name == "open_initial_form")).ProtocolTool;
        var baseline = Assert.ContainsSingle(tools.Where(tool => tool.Name == "unassociated_initial_form")).ProtocolTool;
        Assert.AreEqual(baseline.InputSchema.GetRawText(), associated.InputSchema.GetRawText());
        Assert.AreEqual("preserved", associated.Meta!["businessTool"]!.GetValue<string>());
        Assert.IsNull(baseline.Meta!["ui"]);
        var resourceUri = associated.Meta["ui"]!["resourceUri"]!.GetValue<string>();
        var resource = await first.ReadResourceAsync(resourceUri);
        var html = Assert.IsInstanceOfType<TextResourceContents>(Assert.ContainsSingle(resource.Contents));
        Assert.Contains("id=\"typed-shell\"", html.Text);
        Assert.Contains("src=\"blazor.mcp.js\"", html.Text);
        Assert.DoesNotContain("id=\"initial-label\"", html.Text);

        var firstResult = await first.CallToolAsync("open_initial_form", Arguments("first-view", 17));
        var firstView = GetView(firstResult);
        Assert.Contains("id=\"initial-label\">first-view", firstView["document"]!.GetValue<string>());
        Assert.Contains("id=\"initial-count\">17", firstView["document"]!.GetValue<string>());
        Assert.AreEqual("Business result: first-view.", Assert.IsInstanceOfType<TextContentBlock>(Assert.ContainsSingle(firstResult.Content)).Text);
        Assert.AreEqual("preserved", firstResult.Meta!["business"]!.GetValue<string>());
        Assert.AreEqual("preserved", firstResult.Meta["existingFilter"]!.GetValue<string>());

        var secondResult = await second.CallToolAsync("open_initial_form", Arguments("second-view", 29));
        var secondView = GetView(secondResult);
        Assert.AreNotEqual(firstView["viewId"]!.GetValue<string>(), secondView["viewId"]!.GetValue<string>());
        Assert.Contains("id=\"initial-label\">second-view", secondView["document"]!.GetValue<string>());
        Assert.Contains("id=\"initial-label\">first-view", firstView["document"]!.GetValue<string>());
        Assert.AreEqual("first-view", firstResult.StructuredContent!.Value.GetProperty("label").GetString());

        var posted = await first.CallToolAsync(firstView["dispatchTool"]!.GetValue<string>(), new Dictionary<string, object?>
        {
            ["viewId"] = firstView["viewId"]!.GetValue<string>(),
            ["location"] = firstView["location"]!.GetValue<string>(),
            ["method"] = "POST",
            ["fields"] = new Dictionary<string, string[]> { ["_handler"] = ["typed-form"], ["Input.Name"] = ["posted-value"] },
        });
        Assert.Contains("id=\"typed-value\">posted-value", GetView(posted)["document"]!.GetValue<string>());
        Assert.Contains("id=\"initial-label\">first-view:changed", GetView(posted)["document"]!.GetValue<string>());

        var fresh = await first.CallToolAsync(firstView["dispatchTool"]!.GetValue<string>(), new Dictionary<string, object?>
        {
            ["viewId"] = firstView["viewId"]!.GetValue<string>(),
            ["location"] = firstView["location"]!.GetValue<string>(),
            ["method"] = "GET",
        });
        Assert.Contains("id=\"initial-label\">first-view", GetView(fresh)["document"]!.GetValue<string>());
        Assert.DoesNotContain("first-view:changed", GetView(fresh)["document"]!.GetValue<string>());
        using var state = JsonDocument.Parse(await http.GetStringAsync("test/initial-observations"));
        Assert.AreEqual(2, state.RootElement.GetProperty("openingCalls").GetInt32());
        Assert.AreEqual(2, state.RootElement.GetProperty("projectionCalls").GetInt32());
        Assert.AreEqual(4, state.RootElement.GetProperty("renders").GetInt32());
        Assert.AreEqual(1, state.RootElement.GetProperty("submissions").GetInt32());
        Assert.IsFalse(state.RootElement.GetProperty("sharedResultHasViewMetadata").GetBoolean());
        await CloseAsync(first, firstView);
        await CloseAsync(second, secondView);
    }

    [TestMethod]
    public async Task ExistingResponseFilter_EnrichesInitialParametersBeforeProjection()
    {
        using var http = await ResetAsync();
        await using var client = await ConnectAsync();
        var result = await client.CallToolAsync("open_initial_form", Arguments("filter-enrichment", 1));
        var view = GetView(result);
        try
        {
            Assert.AreEqual("enriched-by-filter", result.StructuredContent!.Value.GetProperty("label").GetString());
            Assert.AreEqual(73, result.StructuredContent.Value.GetProperty("count").GetInt32());
            Assert.Contains("id=\"initial-label\">enriched-by-filter", view["document"]!.GetValue<string>());
            Assert.Contains("id=\"initial-count\">73", view["document"]!.GetValue<string>());
            Assert.AreEqual("Business result: filter-enrichment.", Assert.IsInstanceOfType<TextContentBlock>(Assert.ContainsSingle(result.Content)).Text);
            Assert.AreEqual("preserved", result.Meta!["business"]!.GetValue<string>());
            Assert.AreEqual("preserved", result.Meta["existingFilter"]!.GetValue<string>());
            using var state = JsonDocument.Parse(await http.GetStringAsync("test/initial-observations"));
            Assert.AreEqual(1, state.RootElement.GetProperty("openingCalls").GetInt32());
            Assert.AreEqual(1, state.RootElement.GetProperty("projectionCalls").GetInt32());
            Assert.AreEqual(1, state.RootElement.GetProperty("renders").GetInt32());
            Assert.AreEqual(0, state.RootElement.GetProperty("submissions").GetInt32());
            Assert.IsFalse(state.RootElement.GetProperty("sharedResultHasViewMetadata").GetBoolean());
        }
        finally
        {
            await CloseAsync(client, view);
        }
    }

    [TestMethod]
    public async Task ExistingResponseFilter_ErrorDoesNotProjectRenderOrCreateAView()
    {
        using var http = await ResetAsync();
        await using var client = await ConnectAsync();

        var result = await client.CallToolAsync("open_initial_form", Arguments("filter-error", 1));

        Assert.IsTrue(result.IsError);
        Assert.AreEqual("Rejected by the existing SDK filter.", Assert.IsInstanceOfType<TextContentBlock>(Assert.ContainsSingle(result.Content)).Text);
        Assert.AreEqual("preserved", result.Meta!["business"]!.GetValue<string>());
        Assert.AreEqual("preserved", result.Meta["existingFilter"]!.GetValue<string>());
        using var state = JsonDocument.Parse(await http.GetStringAsync("test/initial-observations"));
        Assert.AreEqual(1, state.RootElement.GetProperty("openingCalls").GetInt32());
        Assert.AreEqual(0, state.RootElement.GetProperty("projectionCalls").GetInt32());
        Assert.AreEqual(0, state.RootElement.GetProperty("renders").GetInt32());
        Assert.AreEqual(0, state.RootElement.GetProperty("submissions").GetInt32());
        Assert.IsNull(result.Meta["blazor"]);
        Assert.IsFalse(state.RootElement.GetProperty("sharedResultHasViewMetadata").GetBoolean());
    }

    [TestMethod]
    public async Task OpeningError_DoesNotProjectParametersOrRenderComponents()
    {
        using var http = await ResetAsync();
        await using var client = await ConnectAsync();

        var result = await client.CallToolAsync("open_initial_form", Arguments("error", 0, error: true));

        Assert.IsTrue(result.IsError);
        Assert.AreEqual("Business error.", Assert.IsInstanceOfType<TextContentBlock>(Assert.ContainsSingle(result.Content)).Text);
        Assert.IsNull(result.Meta?["blazor"]);
        using var state = JsonDocument.Parse(await http.GetStringAsync("test/initial-observations"));
        Assert.AreEqual(1, state.RootElement.GetProperty("openingCalls").GetInt32());
        Assert.AreEqual(0, state.RootElement.GetProperty("projectionCalls").GetInt32());
        Assert.AreEqual(0, state.RootElement.GetProperty("renders").GetInt32());
    }

    [TestMethod]
    public async Task OpeningException_PreservesSdkErrorAndDoesNotProjectOrRender()
    {
        using var http = await ResetAsync();
        await using var client = await ConnectAsync();
        var arguments = new Dictionary<string, object?> { ["input"] = new { label = "exception", count = 0, @throw = true } };

        var baseline = await CaptureErrorAsync(client, "unassociated_initial_form", arguments);
        var associated = await CaptureErrorAsync(client, "open_initial_form", arguments);

        Assert.IsTrue(baseline.IsError);
        Assert.IsTrue(associated.IsError);
        Assert.AreEqual(baseline.Kind, associated.Kind);
        Assert.AreEqual("An error occurred invoking 'unassociated_initial_form': The business opening tool failed.", baseline.Message);
        Assert.AreEqual("An error occurred invoking 'open_initial_form': The business opening tool failed.", associated.Message);
        using var state = JsonDocument.Parse(await http.GetStringAsync("test/initial-observations"));
        Assert.AreEqual(2, state.RootElement.GetProperty("openingCalls").GetInt32());
        Assert.AreEqual(0, state.RootElement.GetProperty("projectionCalls").GetInt32());
        Assert.AreEqual(0, state.RootElement.GetProperty("renders").GetInt32());
    }

    [TestMethod]
    [DataRow("fields-shape", "Form fields must be an object containing arrays of strings.")]
    [DataRow("field-shape", "Form fields must be arrays of strings.")]
    [DataRow("handler-count", "exactly one named form handler")]
    [DataRow("empty-handler", "exactly one named form handler")]
    [DataRow("enctype", "Only URL-encoded and multipart component forms are supported.")]
    [DataRow("get-data", "GET component navigation cannot contain form data.")]
    [DataRow("base64", "A submitted file contains invalid base64 data.")]
    [DataRow("outside", "within the registered application")]
    [DataRow("view-id", "The component view is not available for this request.")]
    [DataRow("method", "within the registered application")]
    public async Task InvalidDispatch_DoesNotRenderOrExecuteTheOpeningToolAgain(string scenario, string expectedMessage)
    {
        using var http = await ResetAsync();
        await using var client = await ConnectAsync();
        var view = GetView(await client.CallToolAsync("open_initial_form", Arguments("input-errors", 3)));
        try
        {
            var arguments = new Dictionary<string, object?>
            {
                ["viewId"] = view["viewId"]!.GetValue<string>(),
                ["location"] = view["location"]!.GetValue<string>(),
                ["method"] = "POST",
                ["fields"] = new Dictionary<string, string[]> { ["_handler"] = ["typed-form"], ["Input.Name"] = ["must-not-submit"] },
            };
            switch (scenario)
            {
                case "fields-shape":
                    arguments["fields"] = "not-a-field-object";
                    break;
                case "field-shape":
                    arguments["fields"] = new Dictionary<string, object?> { ["_handler"] = "typed-form" };
                    break;
                case "handler-count":
                    arguments["fields"] = new Dictionary<string, string[]> { ["_handler"] = ["typed-form", "typed-form"] };
                    break;
                case "empty-handler":
                    arguments["fields"] = new Dictionary<string, string[]> { ["_handler"] = [""] };
                    break;
                case "enctype":
                    arguments["enctype"] = "text/plain";
                    break;
                case "get-data":
                    arguments["method"] = "GET";
                    break;
                case "base64":
                    arguments["enctype"] = "multipart/form-data";
                    arguments["files"] = new[] { new { name = "Upload", fileName = "upload.txt", contentType = "text/plain", data = "!", lastModified = 0 } };
                    break;
                case "outside":
                    arguments["location"] = "https://outside.example/form";
                    break;
                case "view-id":
                    arguments["viewId"] = Guid.NewGuid().ToString();
                    break;
                case "method":
                    arguments["method"] = "PATCH";
                    break;
                default:
                    throw new InvalidOperationException($"Unknown invalid-dispatch scenario '{scenario}'.");
            }

            var outcome = await CaptureErrorAsync(client, view["dispatchTool"]!.GetValue<string>(), arguments);

            Assert.IsTrue(outcome.IsError);
            Assert.Contains(expectedMessage, outcome.Message, StringComparison.Ordinal);
            using var state = JsonDocument.Parse(await http.GetStringAsync("test/initial-observations"));
            Assert.AreEqual(1, state.RootElement.GetProperty("openingCalls").GetInt32());
            Assert.AreEqual(1, state.RootElement.GetProperty("projectionCalls").GetInt32());
            Assert.AreEqual(1, state.RootElement.GetProperty("renders").GetInt32());
            Assert.AreEqual(0, state.RootElement.GetProperty("submissions").GetInt32());
        }
        finally
        {
            await CloseAsync(client, view);
        }
    }

    [TestMethod]
    public async Task WrongApplicationAndClosedView_DoNotRenderOrExecuteEffects()
    {
        using var http = await ResetAsync();
        await using var client = await ConnectAsync();
        var view = GetView(await client.CallToolAsync("open_initial_form", Arguments("view-errors", 4)));
        var arguments = new Dictionary<string, object?>
        {
            ["viewId"] = view["viewId"]!.GetValue<string>(),
            ["location"] = view["location"]!.GetValue<string>(),
            ["method"] = "GET",
        };

        var wrongApplication = await CaptureErrorAsync(client, "component_form_dispatch", arguments);
        await CloseAsync(client, view);
        var closed = await CaptureErrorAsync(client, view["dispatchTool"]!.GetValue<string>(), arguments);

        Assert.IsTrue(wrongApplication.IsError);
        Assert.IsTrue(closed.IsError);
        Assert.Contains("The component view is not available for this request.", wrongApplication.Message, StringComparison.Ordinal);
        Assert.Contains("The component view is not available for this request.", closed.Message, StringComparison.Ordinal);
        using var state = JsonDocument.Parse(await http.GetStringAsync("test/initial-observations"));
        Assert.AreEqual(1, state.RootElement.GetProperty("openingCalls").GetInt32());
        Assert.AreEqual(1, state.RootElement.GetProperty("projectionCalls").GetInt32());
        Assert.AreEqual(1, state.RootElement.GetProperty("renders").GetInt32());
        Assert.AreEqual(0, state.RootElement.GetProperty("submissions").GetInt32());
    }

    [TestMethod]
    [DataRow(true, "Cannot submit the form 'unknown' because no form on the page currently has that name.")]
    [DataRow(false, "The business form action failed.")]
    public async Task FormDispatchError_DoesNotReplayOpeningOrPoisonTheView(bool missingHandler, string expectedMessage)
    {
        using var http = await ResetAsync();
        await using var client = await ConnectAsync();
        var view = GetView(await client.CallToolAsync("open_initial_form", Arguments("form-errors", 5)));
        try
        {
            var dispatchTool = view["dispatchTool"]!.GetValue<string>();
            var outcome = await CaptureErrorAsync(client, dispatchTool, new Dictionary<string, object?>
            {
                ["viewId"] = view["viewId"]!.GetValue<string>(),
                ["location"] = view["location"]!.GetValue<string>(),
                ["method"] = "POST",
                ["fields"] = new Dictionary<string, string[]>
                {
                    ["_handler"] = [missingHandler ? "unknown" : "typed-form"],
                    ["Input.Name"] = ["throw-action"],
                },
            });

            Assert.IsTrue(outcome.IsError);
            Assert.Contains(expectedMessage, outcome.Message, StringComparison.Ordinal);
            var fresh = await client.CallToolAsync(dispatchTool, new Dictionary<string, object?>
            {
                ["viewId"] = view["viewId"]!.GetValue<string>(),
                ["location"] = view["location"]!.GetValue<string>(),
                ["method"] = "GET",
            });
            Assert.Contains("id=\"initial-label\">form-errors", GetView(fresh)["document"]!.GetValue<string>());
            using var state = JsonDocument.Parse(await http.GetStringAsync("test/initial-observations"));
            Assert.AreEqual(1, state.RootElement.GetProperty("openingCalls").GetInt32());
            Assert.AreEqual(1, state.RootElement.GetProperty("projectionCalls").GetInt32());
            Assert.AreEqual(3, state.RootElement.GetProperty("renders").GetInt32());
            Assert.AreEqual(0, state.RootElement.GetProperty("submissions").GetInt32());
        }
        finally
        {
            await CloseAsync(client, view);
        }
    }

    private async Task<HttpClient> ResetAsync()
    {
        var http = new HttpClient { BaseAddress = _rootUri };
        using var reset = await http.PostAsync("test/reset-initial", content: null);
        reset.EnsureSuccessStatusCode();
        return http;
    }

    private Task<McpClient> ConnectAsync()
        => McpClient.CreateAsync(new HttpClientTransport(new()
        {
            Endpoint = new Uri(_rootUri, "mcp"),
            TransportMode = HttpTransportMode.StreamableHttp,
        }));

    private static Dictionary<string, object?> Arguments(string label, int count, bool error = false)
        => new()
        {
            ["input"] = new { label, count, error },
        };

    private static JsonObject GetView(CallToolResult result)
    {
        Assert.IsFalse(result.IsError == true);
        return result.Meta!["blazor"]!.AsObject();
    }

    private static ValueTask<CallToolResult> CloseAsync(McpClient client, JsonObject view)
        => client.CallToolAsync(view["dispatchTool"]!.GetValue<string>(), new Dictionary<string, object?>
        {
            ["viewId"] = view["viewId"]!.GetValue<string>(),
            ["method"] = "CLOSE",
        });

    private static async Task<ErrorOutcome> CaptureErrorAsync(McpClient client, string tool, Dictionary<string, object?> arguments)
    {
        try
        {
            var result = await client.CallToolAsync(tool, arguments);
            return new(result.IsError == true, "tool-result", string.Join("\n", result.Content.OfType<TextContentBlock>().Select(block => block.Text)));
        }
        catch (McpException exception)
        {
            return new(true, exception.GetType().FullName!, exception.Message);
        }
    }

    private sealed record ErrorOutcome(bool IsError, string Kind, string Message);
}
