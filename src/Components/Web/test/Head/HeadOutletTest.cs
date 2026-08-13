// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Moq;

namespace Microsoft.AspNetCore.Components.Web;

public class HeadOutletTest
{
    [Fact]
    public async Task WebAssembly_SeedsTitleFromDocumentSynchronously_AndKeepsItWhenNoStaticTitleIsFound()
    {
        var jsRuntime = new Mock<IJSInProcessRuntime>(MockBehavior.Strict);
        jsRuntime.Setup(x => x.GetValue<string>("document.title")).Returns("Existing title");
        jsRuntime.Setup(x => x.InvokeAsync<string>(It.IsAny<string>(), It.IsAny<object[]>())).Returns(new ValueTask<string>((string)null));

        var htmlRenderer = CreateHtmlRenderer(jsRuntime.Object);

        await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            var result = await htmlRenderer.RenderComponentAsync<HeadOutlet>();
            await result.QuiescenceTask;

            Assert.Equal("<title>Existing title</title>", result.ToHtmlString());
        });
    }

    [Fact]
    public async Task Server_DoesNotSeedTitleSynchronously()
    {
        var jsRuntime = new Mock<IJSRuntime>(MockBehavior.Strict);
        jsRuntime.Setup(x => x.InvokeAsync<string>(It.IsAny<string>(), It.IsAny<object[]>())).Returns(new ValueTask<string>((string)null));

        var htmlRenderer = CreateHtmlRenderer(jsRuntime.Object);

        await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            var result = await htmlRenderer.RenderComponentAsync<HeadOutlet>();
            await result.QuiescenceTask;

            Assert.Equal(string.Empty, result.ToHtmlString());
        });
    }

    private static HtmlRenderer CreateHtmlRenderer(IJSRuntime jsRuntime)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(jsRuntime);
        return new HtmlRenderer(services.BuildServiceProvider(), NullLoggerFactory.Instance);
    }
}
