// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Media.Tests;

public class AudioTest
{
    [Fact]
    public async Task RendersAudioElement_WithControls()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IJSRuntime>(new NullJsRuntime());
        using var renderer = new NonInteractiveTestRenderer(services.BuildServiceProvider());
        var component = renderer.InstantiateComponent<Audio>();
        var componentId = renderer.AssignRootComponentId(component);

        await renderer.RenderRootComponentAsync(
            componentId,
            ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(Audio.Source)] = new MediaSource(
                    new byte[] { 1, 2, 3 },
                    "audio/webm",
                    "audio-test"),
                [nameof(Audio.AdditionalAttributes)] = new Dictionary<string, object>
                {
                    ["controls"] = true,
                },
            }));

        var frames = renderer.GetCurrentRenderTreeFrames(componentId);
        Assert.Contains(
            frames.Array.Take(frames.Count),
            frame => frame.FrameType == RenderTreeFrameType.Element &&
                frame.ElementName == "audio");
        Assert.Contains(
            frames.Array.Take(frames.Count),
            frame => frame.FrameType == RenderTreeFrameType.Attribute &&
                frame.AttributeName == "controls");
    }

    private sealed class NonInteractiveTestRenderer : TestRenderer
    {
        public NonInteractiveTestRenderer(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        protected internal override RendererInfo RendererInfo =>
            new("Test", isInteractive: false);
    }

    private sealed class NullJsRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            object?[]? args)
        {
            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
        {
            return ValueTask.FromResult(default(TValue)!);
        }
    }
}
