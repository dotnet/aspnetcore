// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Components.RenderTree;
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
    public async Task WebAssembly_NoStaticTitleFound_DoesNotClearSynchronouslySeededTitle()
    {
        // Guards the OnAfterRenderAsync fix: an empty/null async lookup result must not overwrite the
        // title already seeded synchronously in OnInitialized, or the title-flash bug (#68346) reappears.
        var jsRuntime = new Mock<IJSInProcessRuntime>(MockBehavior.Strict);
        jsRuntime.Setup(x => x.GetValue<string>("document.title")).Returns("Seeded title");
        jsRuntime.Setup(x => x.InvokeAsync<string>(It.IsAny<string>(), It.IsAny<object[]>())).Returns(new ValueTask<string>((string)null));

        var headOutlet = await RenderWithAfterRenderSupportAsync(jsRuntime.Object);

        Assert.Equal("Seeded title", GetDefaultTitle(headOutlet));
    }

    private static HtmlRenderer CreateHtmlRenderer(IJSRuntime jsRuntime)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(jsRuntime);
        return new HtmlRenderer(services.BuildServiceProvider(), NullLoggerFactory.Instance);
    }

    // HtmlRenderer's UpdateDisplayAsync task is deliberately canceled, so it never invokes OnAfterRenderAsync.
    // Exercising the async static-title lookup requires a renderer whose display update actually completes.
    private static async Task<HeadOutlet> RenderWithAfterRenderSupportAsync(IJSRuntime jsRuntime)
    {
        var services = new ServiceCollection();
        services.AddSingleton(jsRuntime);
        var renderer = new AfterRenderCapableRenderer(services.BuildServiceProvider());

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var headOutlet = (HeadOutlet)renderer.InstantiateComponent(typeof(HeadOutlet));
            var componentId = renderer.AssignRootComponentId(headOutlet);

            await renderer.RenderRootComponentAsync(componentId);

            return headOutlet;
        });
    }

    private static string GetDefaultTitle(HeadOutlet headOutlet)
        => (string)typeof(HeadOutlet).GetField("_defaultTitle", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(headOutlet);

    private sealed class AfterRenderCapableRenderer : Renderer
    {
        public AfterRenderCapableRenderer(IServiceProvider serviceProvider)
            : base(serviceProvider, NullLoggerFactory.Instance)
        {
            Dispatcher = Dispatcher.CreateDefault();
        }

        public override Dispatcher Dispatcher { get; }

        public new IComponent InstantiateComponent(Type componentType)
            => base.InstantiateComponent(componentType);

        public new int AssignRootComponentId(IComponent component)
            => base.AssignRootComponentId(component);

        public new Task RenderRootComponentAsync(int componentId)
            => base.RenderRootComponentAsync(componentId);

        protected override void HandleException(Exception exception)
            => ExceptionDispatchInfo.Capture(exception).Throw();

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
            => Task.CompletedTask;
    }
}
