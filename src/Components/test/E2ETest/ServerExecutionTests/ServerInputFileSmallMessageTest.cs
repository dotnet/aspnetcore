// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.AspNetCore.SignalR;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.Extensions;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests;

public class ServerInputFileSmallMessageTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
{
    private readonly string _tempFile = Path.GetTempFileName();

    public ServerInputFileSmallMessageTest(
        BrowserFixture browserFixture,
        ToggleExecutionModeServerFixture<Program> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture.WithServerExecution().WithAdditionalArguments(
            [$"--{nameof(HubOptions.MaximumReceiveMessageSize)}", "512"]), output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        File.WriteAllText(_tempFile, "x");
        Navigate($"{ServerPathBase}/input-file-small-messages");
    }

    [Fact]
    public void CanSelectAndCancelFilesWhenMessageLimitDoesNotSupportStreaming()
    {
        Browser.Exists(By.Id("read-file-contents")).Click();
        Browser.Exists(By.Id("input-file")).SendKeys(_tempFile);

        Browser.Equal("1", () => Browser.Exists(By.Id("file-count")).Text);
        Browser.Equal("1", () => Browser.Exists(By.Id("change-count")).Text);
        Browser.Equal(Path.GetFileName(_tempFile), () => Browser.Exists(By.Id("file-name")).Text);
        Browser.Equal("1", () => Browser.Exists(By.Id("file-size")).Text);

        Browser.ExecuteJavaScript("document.getElementById('input-file').dispatchEvent(new Event('cancel'));");
        Browser.Equal("0", () => Browser.Exists(By.Id("file-count")).Text);
        Browser.Equal("2", () => Browser.Exists(By.Id("change-count")).Text);
    }

    public override async Task DisposeAsync()
    {
        try
        {
            await base.DisposeAsync();
        }
        finally
        {
            File.Delete(_tempFile);
        }
    }
}
