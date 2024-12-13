// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerRenderingTests;

public partial class ResourceCollectionTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>>
{
    public ResourceCollectionTest(BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    [Fact]
    public void StaticRendering_CanUseFingerprintedResources()
    {
        var url = $"{ServerPathBase}/resource-collection";
        Navigate(url);

        Browser.True(() => AppStylesRegex().IsMatch(Browser.Exists(By.Id("basic-app-styles")).Text));

        Browser.Exists(By.Id("import-module")).Click();

        Browser.True(() => JsModuleRegex().IsMatch(Browser.Exists(By.Id("js-module")).Text));
    }

    [Theory]
    [InlineData("Server")]
    [InlineData("WebAssembly")]
    public void StaticRendering_CanUseFingerprintedResources_InteractiveModes(string renderMode)
    {
        var url = $"{ServerPathBase}/resource-collection?render-mode={renderMode}";
        Navigate(url);

        Browser.Equal(renderMode, () => Browser.Exists(By.Id("platform-name")).Text);

        Browser.True(() => AppStylesRegex().IsMatch(Browser.Exists(By.Id("basic-app-styles")).Text));

        Browser.Exists(By.Id("import-module")).Click();

        Browser.True(() => JsModuleRegex().IsMatch(Browser.Exists(By.Id("js-module")).Text));
    }

    [GeneratedRegex("""BasicTestApp\.[a-zA-Z0-9]{10}\.styles\.css""")]
    private static partial Regex AppStylesRegex();
    [GeneratedRegex(""".*Index\.[a-zA-Z0-9]{10}\.mjs""")]
    private static partial Regex JsModuleRegex();
}
