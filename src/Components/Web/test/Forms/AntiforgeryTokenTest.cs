// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Microsoft.AspNetCore.Components.Forms;

public class AntiforgeryTokenTest
{
    [Fact]
    public async Task RendersHiddenFieldWhenTokenIsAvailable()
    {
        var provider = new Mock<AntiforgeryStateProvider>(MockBehavior.Strict);
        provider.Setup(state => state.GetAntiforgeryToken()).Returns(new AntiforgeryRequestToken("value", "custom-field"));
        using var services = new ServiceCollection().AddSingleton(provider.Object).BuildServiceProvider();
        await using var renderer = new TestRenderer(services);
        var id = renderer.AssignRootComponentId(renderer.InstantiateComponent<AntiforgeryToken>());

        await renderer.RenderRootComponentAsync(id);

        var frames = renderer.GetCurrentRenderTreeFrames(id).AsEnumerable().ToArray();
        Assert.Collection(frames,
            frame => AssertFrame.Element(frame, "input", 4),
            frame => AssertFrame.Attribute(frame, "type", "hidden"),
            frame => AssertFrame.Attribute(frame, "name", "custom-field"),
            frame => AssertFrame.Attribute(frame, "value", "value"));
        provider.Verify(state => state.GetAntiforgeryToken(), Times.Once);
    }

    [Fact]
    public async Task RendersPlaceholderWhenTokenIsNotAvailable()
    {
        using var services = new ServiceCollection()
            .AddSingleton<AntiforgeryStateProvider, DefaultAntiforgeryStateProvider>().BuildServiceProvider();
        await using var renderer = new TestRenderer(services);
        var id = renderer.AssignRootComponentId(renderer.InstantiateComponent<AntiforgeryToken>());

        await renderer.RenderRootComponentAsync(id);

        Assert.Collection(renderer.GetCurrentRenderTreeFrames(id).AsEnumerable(),
            frame => AssertFrame.Element(frame, "blazor-antiforgery", 1));
    }

    [Fact]
    public async Task RendersNothingWithoutProvider()
    {
        using var services = new ServiceCollection().BuildServiceProvider();
        await using var renderer = new TestRenderer(services);
        var id = renderer.AssignRootComponentId(renderer.InstantiateComponent<AntiforgeryToken>());

        await renderer.RenderRootComponentAsync(id);

        Assert.Empty(renderer.GetCurrentRenderTreeFrames(id).AsEnumerable());
    }
}
