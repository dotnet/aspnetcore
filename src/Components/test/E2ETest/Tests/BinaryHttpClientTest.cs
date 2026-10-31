// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp.HttpClientTest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class BinaryHttpClientTest : BrowserTestBase,
    IClassFixture<BasicTestAppServerSiteFixture<CorsStartup>>,
    IClassFixture<BlazorWasmTestAppFixture<BasicTestApp.Program>>
{
    private readonly BlazorWasmTestAppFixture<BasicTestApp.Program> _devHostServerFixture;
    readonly ServerFixture _apiServerFixture;
    IWebElement _appElement;
    IWebElement _responseStatus;
    IWebElement _responseStatusText;
    IWebElement _testOutcome;

    public BinaryHttpClientTest(
        BrowserFixture browserFixture,
        BlazorWasmTestAppFixture<BasicTestApp.Program> devHostServerFixture,
        BasicTestAppServerSiteFixture<CorsStartup> apiServerFixture,
        ITestOutputHelper output)
        : base(browserFixture, output)
    {
        _devHostServerFixture = devHostServerFixture;
        _devHostServerFixture.PathBase = "/subdir";
        _apiServerFixture = apiServerFixture;
    }

    protected override void InitializeAsyncCore()
    {
        Browser.Navigate(_devHostServerFixture.RootUri, "/subdir");
        _appElement = Browser.MountTestComponent<BinaryHttpRequestsComponent>();
    }

    public override Task InitializeAsync() => base.InitializeAsync(Guid.NewGuid().ToString());

    [Fact]
    public void CanSendAndReceiveBytes()
    {
        IssueRequest("/subdir/api/data");
        Assert.Equal("OK", _responseStatus.Text);
        Assert.Equal("OK", _responseStatusText.Text);
        Assert.Equal("", _testOutcome.Text);
    }

    private void IssueRequest(string relativeUri)
    {
        var targetUri = new Uri(_apiServerFixture.RootUri, relativeUri);
        SetValue("request-uri", targetUri.AbsoluteUri);

        _appElement.FindElement(By.Id("send-request")).Click();

        _responseStatus = Browser.Exists(By.Id("response-status"));
        _responseStatusText = _appElement.FindElement(By.Id("response-status-text"));
        _testOutcome = _appElement.FindElement(By.Id("test-outcome"));
    }

    private void SetValue(string elementId, string value)
    {
        var element = Browser.Exists(By.Id(elementId));
        element.Clear();
        element.SendKeys(value);
    }
}
