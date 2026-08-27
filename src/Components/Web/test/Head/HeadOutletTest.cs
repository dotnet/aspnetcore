// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;

namespace Microsoft.AspNetCore.Components.Web;

public class HeadOutletTest
{
    [Fact]
    public async Task NonBrowserInProcessRuntimeDoesNotInvokeSynchronousDefaultInterfaceMethod()
    {
        var jsRuntime = new Mock<IJSInProcessRuntime>(MockBehavior.Strict);
        jsRuntime
            .Setup(runtime => runtime.InvokeAsync<string>(It.IsAny<string>(), It.IsAny<object[]>()))
            .Returns(new ValueTask<string>((string)null));

        var renderer = await RenderHeadOutletAsync(jsRuntime.Object);

        jsRuntime.Verify(runtime => runtime.InvokeAsync<string>(
            "Blazor._internal.PageTitle.getAndRemoveExistingTitle",
            It.Is<object[]>(args => args.Length == 0)), Times.Once);
        Assert.DoesNotContain(renderer.Batches.SelectMany(batch => batch.ReferenceFrames),
            frame => frame.FrameType == RenderTreeFrameType.Element && frame.ElementName == "title");
    }

    [Fact]
    public async Task NonInProcessRuntimeUsesStaticTitleAsDefault()
    {
        var jsRuntime = new Mock<IJSRuntime>(MockBehavior.Strict);
        jsRuntime
            .Setup(runtime => runtime.InvokeAsync<string>(It.IsAny<string>(), It.IsAny<object[]>()))
            .Returns(new ValueTask<string>("Static title"));

        var renderer = await RenderHeadOutletAsync(jsRuntime.Object);
        var frames = renderer.Batches.SelectMany(batch => batch.ReferenceFrames);

        jsRuntime.Verify(runtime => runtime.InvokeAsync<string>(
            "Blazor._internal.PageTitle.getAndRemoveExistingTitle",
            It.Is<object[]>(args => args.Length == 0)), Times.Once);
        Assert.Contains(frames, frame => frame.FrameType == RenderTreeFrameType.Element && frame.ElementName == "title");
        Assert.Contains(frames, frame => frame.FrameType == RenderTreeFrameType.Text && frame.TextContent == "Static title");
    }

    private static async Task<TestRenderer> RenderHeadOutletAsync(IJSRuntime jsRuntime)
    {
        var services = new ServiceCollection();
        services.AddSingleton(jsRuntime);
        var renderer = new TestRenderer(services.BuildServiceProvider());
        var headOutlet = renderer.InstantiateComponent<HeadOutlet>();
        var componentId = renderer.AssignRootComponentId(headOutlet);

        await renderer.RenderRootComponentAsync(componentId);

        return renderer;
    }
}
