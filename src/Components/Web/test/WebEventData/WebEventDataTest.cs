// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Test.Helpers;

namespace Microsoft.AspNetCore.Components.Web;

public class WebEventDataTest
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        IncludeFields = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
    };

    [Fact]
    public void ParseEventArgsJson_DeserializesCustomEventArgs_UsingHandlerParameterType()
    {
        var renderer = new TestRenderer();
        var component = new CustomEventComponent
        {
            Handler = (Func<CustomEventArgs, Task>)(_ => Task.CompletedTask),
        };
        renderer.AssignRootComponentId(component);
        component.TriggerRender();

        var eventHandlerId = renderer.Batches.Single()
            .ReferenceFrames
            .First(frame => frame.AttributeValue != null)
            .AttributeEventHandlerId;

        var descriptor = new WebEventDescriptor
        {
            EventHandlerId = eventHandlerId,
            EventName = "oncustomevent",
        };

        using var eventArgsJson = JsonDocument.Parse("{\"value\":\"hello\"}");

        var webEventData = WebEventData.Parse(renderer, _jsonOptions, descriptor, eventArgsJson.RootElement);

        var customArgs = Assert.IsType<CustomEventArgs>(webEventData.EventArgs);
        Assert.Equal("hello", customArgs.Value);
        Assert.Equal(eventHandlerId, webEventData.EventHandlerId);
    }

    [Fact]
    public void ParseEventArgsJson_ReturnsEmptyEventArgs_ForParameterlessHandler()
    {
        // Arrange: a handler that takes no EventArgs resolves to EventArgs.
        var renderer = new TestRenderer();
        var component = new CustomEventComponent
        {
            Handler = (Func<Task>)(() => Task.CompletedTask),
        };
        renderer.AssignRootComponentId(component);
        component.TriggerRender();

        var eventHandlerId = renderer.Batches.Single()
            .ReferenceFrames
            .First(frame => frame.AttributeValue != null)
            .AttributeEventHandlerId;

        var descriptor = new WebEventDescriptor
        {
            EventHandlerId = eventHandlerId,
            EventName = "oncustomevent",
        };

        using var eventArgsJson = JsonDocument.Parse("{}");

        var webEventData = WebEventData.Parse(renderer, _jsonOptions, descriptor, eventArgsJson.RootElement);

        Assert.NotNull(webEventData.EventArgs);
        Assert.IsType<EventArgs>(webEventData.EventArgs);
    }

    private sealed class CustomEventArgs : EventArgs
    {
        public string Value { get; set; }
    }

    private sealed class CustomEventComponent : AutoRenderComponent, IHandleEvent
    {
        [Parameter]
        public Delegate Handler { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "element");
            builder.AddAttribute(1, "oncustomevent", Handler);
            builder.CloseElement();
        }

        public Task HandleEventAsync(EventCallbackWorkItem callback, object arg)
            => callback.InvokeAsync(arg);
    }
}
