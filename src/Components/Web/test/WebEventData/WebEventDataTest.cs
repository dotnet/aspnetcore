// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;

namespace Microsoft.AspNetCore.Components.Web;

public partial class WebEventDataTest
{
    [Fact]
    public void CustomEventWithBaseEventArgsNeedsNoJsonContract()
    {
        using var renderer = new TestRenderer();
        var eventHandlerId = RenderEventHandler(renderer, (Action<EventArgs>)(_ => { }));
        var options = new JsonSerializerOptions { TypeInfoResolver = new NullResolver() };

        var result = WebEventData.Parse(
            renderer,
            options,
            CreateDescriptor(eventHandlerId),
            ParseElement("""{"ignored":true}"""));

        Assert.Same(EventArgs.Empty, result.EventArgs);
    }

    [Fact]
    public void CustomEventUsesApplicationJsonContract()
    {
        using var renderer = new TestRenderer();
        var eventHandlerId = RenderEventHandler(renderer, (Action<CustomEventArgs>)(_ => { }));
        var options = new JsonSerializerOptions { TypeInfoResolver = CustomEventArgsJsonContext.Default };

        var result = WebEventData.Parse(
            renderer,
            options,
            CreateDescriptor(eventHandlerId),
            ParseElement("""{"Value":"from-json"}"""));

        var eventArgs = Assert.IsType<CustomEventArgs>(result.EventArgs);
        Assert.Equal("from-json", eventArgs.Value);
    }

    [Fact]
    public void MissingCustomEventContractIncludesEventIdAndInnerFailure()
    {
        using var renderer = new TestRenderer();
        var eventHandlerId = RenderEventHandler(renderer, (Action<CustomEventArgs>)(_ => { }));
        var options = new JsonSerializerOptions { TypeInfoResolver = new NullResolver() };

        var exception = Assert.Throws<InvalidOperationException>(() => WebEventData.Parse(
            renderer,
            options,
            CreateDescriptor(eventHandlerId),
            ParseElement("""{"Value":"from-json"}""")));

        Assert.Contains($"EventId: '{eventHandlerId}'", exception.Message);
        Assert.IsType<NotSupportedException>(exception.InnerException);
    }

    private static ulong RenderEventHandler(TestRenderer renderer, Delegate handler)
    {
        var component = new EventHandlerComponent(handler);
        renderer.AssignRootComponentId(component);
        component.TriggerRender();

        return renderer.Batches.Single()
            .ReferenceFrames
            .Single(frame => frame.AttributeName == "oncustom")
            .AttributeEventHandlerId;
    }

    private static WebEventDescriptor CreateDescriptor(ulong eventHandlerId)
        => new()
        {
            EventHandlerId = eventHandlerId,
            EventName = "custom",
        };

    private static JsonElement ParseElement(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private sealed class EventHandlerComponent(Delegate handler) : AutoRenderComponent
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "oncustom", handler);
            builder.CloseElement();
        }
    }

    private sealed class NullResolver : IJsonTypeInfoResolver
    {
        public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
            => null;
    }

    private sealed class CustomEventArgs : EventArgs
    {
        public string? Value { get; set; }
    }

    [JsonSerializable(typeof(CustomEventArgs))]
    private sealed partial class CustomEventArgsJsonContext : JsonSerializerContext;
}
