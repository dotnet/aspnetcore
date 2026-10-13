// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.WebAssembly.Server;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class DebugPageTest : ServerTestBase<DebugPageFixture>
{
    public DebugPageTest(
        BrowserFixture browserFixture,
        DebugPageFixture serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    [Theory]
    [InlineData("Browser unavailable")]
    [InlineData("</pre><span id=exception-probe>injected</span><pre>")]
    public async Task ExceptionTextIsDisplayedAsText(string reason)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var responseTask = RespondWithErrorAsync(listener, reason, cancellation.Token);

        Navigate($"/_framework/debug?port={((IPEndPoint)listener.LocalEndpoint).Port}");
        await responseTask;

        Assert.Contains(reason, Browser.Exists(By.TagName("pre")).Text);
        Assert.Empty(Browser.FindElements(By.Id("exception-probe")));
    }

    private static async Task RespondWithErrorAsync(TcpListener listener, string reason, CancellationToken cancellationToken)
    {
        using var client = await listener.AcceptTcpClientAsync(cancellationToken);
        await using var stream = client.GetStream();
        await stream.WriteAsync(
            Encoding.ASCII.GetBytes($"HTTP/1.1 400 {reason}\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"),
            cancellationToken);
    }
}

public class DebugPageFixture : WebHostServerFixture
{
    protected override IHost CreateWebHost()
    {
        return Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(builder => builder
                .UseUrls("http://127.0.0.1:0")
                .Configure(app => app.Map("/_framework/debug", debug => debug.Run(context =>
                    new TargetPickerUi("ws://127.0.0.1:1234", $"http://127.0.0.1:{context.Request.Query["port"]}").Display(context)))))
            .Build();
    }
}
