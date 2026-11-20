// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests;

public class RemoteJSDataStreamTest : ServerTestBase<BasicTestAppServerSiteFixture<ServerStartup>>
{
    public RemoteJSDataStreamTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<ServerStartup> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
        serverFixture.AdditionalArguments.AddRange("--JSInteropDefaultCallTimeoutMilliseconds", "500");
    }

    protected override void InitializeAsyncCore()
    {
        Navigate(ServerPathBase);
        Browser.MountTestComponent<InteropComponent>();
    }

    [Fact]
    public void ConcurrentLargeStreamsCanBeConsumedSequentially()
    {
        Browser.Exists(By.Id("btn-concurrent-js-streams")).Click();

        Browser.Equal("Success", () => Browser.Exists(By.Id("concurrent-js-streams-result")).Text);
    }
}
