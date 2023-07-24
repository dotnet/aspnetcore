// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class ClientRenderingMultpleComponentsTest : E2ETest.Infrastructure.ServerTestBase<BasicTestAppServerSiteFixture<MultipleComponents>>
{
    private const string MarkerPattern = ".*?<!--Blazor:(.*?)-->.*?";

    public ClientRenderingMultpleComponentsTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<MultipleComponents> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    public DateTime LastLogTimeStamp { get; set; } = DateTime.MinValue;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        // Capture the last log timestamp so that we can filter logs when we
        // check for duplicate connections.
        var lastLog = Browser.Manage().Logs.GetLog(LogType.Browser).LastOrDefault();
        if (lastLog != null)
        {
            LastLogTimeStamp = lastLog.Timestamp;
        }
    }

    [Fact]
    public void CanRenderMultipleRootComponents()
    {
        Navigate("/Client/multiple-components");

        var greets = Browser.FindElements(By.CssSelector(".greet-wrapper .greet")).Select(e => e.Text).ToArray();

        Assert.Equal(7, greets.Length); // 1 statically rendered + 5 prerendered + 1 server prerendered
        Assert.DoesNotContain("Hello Red fish", greets);
        Assert.Single(greets, "Hello John");
        Assert.Single(greets, "Hello Abraham");
        Assert.Equal(2, greets.Where(g => g == "Hello Blue fish").Count());
        Assert.Equal(3, greets.Where(g => string.Equals("Hello", g)).Count()); // 3 server prerendered without parameters
        var content = Browser.Exists(By.Id("test-container")).GetAttribute("innerHTML");
        var markers = ReadMarkers(content);
        var componentSequence = markers.Select(m => m.Item1.PrerenderId != null).ToArray();
        Assert.Equal(13, componentSequence.Length);

        // Once the app starts, output changes
        BeginInteractivity();

        Browser.Exists(By.CssSelector("h3.interactive"));
        var updatedGreets = Browser.FindElements(By.CssSelector(".greet-wrapper .greet")).Select(e => e.Text).ToArray();
        Assert.Equal(7, updatedGreets.Where(g => string.Equals("Hello Alfred", g)).Count());
        Assert.Equal(2, updatedGreets.Where(g => g == "Hello Red fish").Count());
        Assert.Equal(2, updatedGreets.Where(g => g == "Hello Blue fish").Count());
        Assert.Single(updatedGreets.Where(g => string.Equals("Hello Albert", g)));
        Assert.Single(updatedGreets.Where(g => string.Equals("Hello Abraham", g)));
    }

    private (ComponentMarker, ComponentMarker)[] ReadMarkers(string content)
    {
        content = content.Replace("\r\n", "");
        var matches = Regex.Matches(content, MarkerPattern);
        var markers = matches.Select(s => JsonSerializer.Deserialize<ComponentMarker>(
            s.Groups[1].Value,
            WebAssemblyComponentSerializationSettings.JsonSerializationOptions));

        var prerenderMarkers = markers.Where(m => m.PrerenderId != null).GroupBy(p => p.PrerenderId).Select(g => (g.First(), g.Skip(1).First())).ToArray();
        var nonPrerenderMarkers = markers.Where(m => m.PrerenderId == null).Select(g => (g, (ComponentMarker)default)).ToArray();

        return prerenderMarkers.Concat(nonPrerenderMarkers).ToArray();
    }

    private void BeginInteractivity()
    {
        Browser.Exists(By.Id("load-boot-script")).Click();
    }
}
