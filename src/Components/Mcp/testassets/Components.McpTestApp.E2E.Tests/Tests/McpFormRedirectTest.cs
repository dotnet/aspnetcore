// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Net.Http;
using System.Text.Json;
using Components.McpTestApp.E2E.Tests.Fixtures;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.AspNetCore.Components.Testing.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Client;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace Components.McpTestApp.E2E.Tests.Tests;

[UITest]
public partial class McpFormRedirectTest : UITest
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task SubmittedMutation_RedirectsToAFreshGetWithoutReplayingTheHandler(bool disableThrow)
    {
        var server = await StartServerAsync<McpTestAppMarker>(TestRoot.Servers, options =>
        {
            options.EnvironmentVariables["McpTest__DisableThrowNavigationException"] = disableThrow.ToString();
        });
        var root = new Uri(server.AppUrl);
        using var http = new HttpClient { BaseAddress = root };
        using var reset = await http.PostAsync("test/reset/redirect", content: null);
        reset.EnsureSuccessStatusCode();
        await using var client = await McpClient.CreateAsync(new HttpClientTransport(new()
        {
            Endpoint = new Uri(root, "mcp"),
            TransportMode = HttpTransportMode.StreamableHttp,
        }));
        var opened = await client.CallToolAsync("component_form");
        Assert.IsFalse(opened.IsError == true);
        var view = opened.Meta!["blazor"]!.AsObject();
        var dispatchTool = view["dispatchTool"]!.GetValue<string>();
        var viewId = view["viewId"]!.GetValue<string>();
        try
        {
            var formLocation = new Uri(root, "mcp-redirect?case=redirect").AbsoluteUri;
            var form = await client.CallToolAsync(dispatchTool, new Dictionary<string, object?>
            {
                ["viewId"] = viewId,
                ["location"] = formLocation,
                ["method"] = "GET",
            });
            Assert.IsFalse(form.IsError == true);
            Assert.Contains("id=\"redirect-submit\"", form.Meta!["blazor"]!["document"]!.GetValue<string>());

            var posted = await client.CallToolAsync(dispatchTool, new Dictionary<string, object?>
            {
                ["viewId"] = viewId,
                ["location"] = formLocation,
                ["method"] = "POST",
                ["fields"] = new Dictionary<string, string[]> { ["_handler"] = ["redirect"], ["Case"] = ["redirect"] },
            });

            Assert.IsFalse(posted.IsError == true);
            var result = posted.Meta!["blazor"]!.AsObject();
            Assert.IsTrue(result["redirected"]!.GetValue<bool>());
            Assert.AreEqual(new Uri(root, "mcp-redirect-result?case=redirect").AbsoluteUri, result["location"]!.GetValue<string>());
            Assert.Contains("id=\"redirect-count\">1", result["document"]!.GetValue<string>());
            Assert.Contains("id=\"redirect-form-data\">none", result["document"]!.GetValue<string>());

            var fresh = await client.CallToolAsync(dispatchTool, new Dictionary<string, object?>
            {
                ["viewId"] = viewId,
                ["location"] = result["location"]!.GetValue<string>(),
                ["method"] = "GET",
            });
            Assert.IsFalse(fresh.IsError == true);
            Assert.Contains("id=\"redirect-count\">1", fresh.Meta!["blazor"]!["document"]!.GetValue<string>());
            using var state = JsonDocument.Parse(await http.GetStringAsync("observations/redirect"));
            Assert.AreEqual(1, state.RootElement.GetProperty("count").GetInt32());
            Assert.AreEqual(4, state.RootElement.GetProperty("componentsCreated").GetInt32());
            Assert.AreEqual(4, state.RootElement.GetProperty("componentsDisposed").GetInt32());
            Assert.AreEqual(4, state.RootElement.GetProperty("scopesDisposed").GetInt32());
        }
        finally
        {
            await client.CallToolAsync(dispatchTool, new Dictionary<string, object?>
            {
                ["viewId"] = viewId,
                ["method"] = "CLOSE",
            });
        }
    }
}
