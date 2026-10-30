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

public class ServerInputFileLargeMessageTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
{
    public ServerInputFileLargeMessageTest(
        BrowserFixture browserFixture,
        ToggleExecutionModeServerFixture<Program> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture.WithServerExecution().WithAdditionalArguments(
            [$"--{nameof(HubOptions.MaximumReceiveMessageSize)}", "2097152"]), output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        Navigate($"{ServerPathBase}/input-file-small-messages");
    }

    [Theory]
    [InlineData(250000)]
    [InlineData(600000)]
    public void CanSelectFilesWithRaisedReceiveLimit(int nameLength)
    {
        Browser.Exists(By.Id("read-file-contents")).Click();

        // Native file pickers cannot select filenames this long, so create browser File objects.
        var metadataBytes = Browser.ExecuteJavaScript<long>("""
            const transfer = new DataTransfer();
            for (let index = 0; index < 2; index++) {
                transfer.items.add(new File(['x'], `${index}-${'m'.repeat(arguments[0])}.txt`, { type: 'text/plain' }));
            }
            const input = document.getElementById('input-file');
            input.files = transfer.files;
            input.dispatchEvent(new Event('change', { bubbles: true }));
            return new Blob([JSON.stringify(Object.values(input._blazorFilesById))]).size;
            """, nameLength);

        Assert.InRange(metadataBytes, 2L * nameLength, 2L * nameLength + 1024);
        Assert.True(metadataBytes < 2 * 1024 * 1024);
        Assert.Equal(nameLength == 600000, metadataBytes > 1024 * 1024);
        Browser.Equal("2", () => Browser.Exists(By.Id("file-count")).Text);
        Browser.Equal("1", () => Browser.Exists(By.Id("change-count")).Text);
        Browser.Equal(2L, () => Browser.ExecuteJavaScript<long>("""
            return [...document.querySelectorAll('#file-name')]
                .filter((element, index) => element.textContent === `${index}-${'m'.repeat(arguments[0])}.txt`).length;
            """, nameLength));
        Assert.All(Browser.FindElements(By.Id("file-size")), element => Assert.Equal("1", element.Text));

        Browser.ExecuteJavaScript("document.getElementById('input-file').dispatchEvent(new Event('cancel'));");
        Browser.Equal("0", () => Browser.Exists(By.Id("file-count")).Text);
        Browser.Equal("2", () => Browser.Exists(By.Id("change-count")).Text);
    }
}
