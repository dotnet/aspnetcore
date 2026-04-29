// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text.Json;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class RenderFragmentSerializerTest
{
    [Fact]
    public void Serialize_EmptyFragment_ReturnsEmptyList()
    {
        RenderFragment fragment = builder => { };

        var result = RenderFragmentSerializer.Serialize(fragment);

        Assert.Empty(result);
    }

    [Fact]
    public void Serialize_TextContent_ProducesTextFrame()
    {
        RenderFragment fragment = builder =>
        {
            builder.AddContent(0, "Hello world");
        };

        var result = RenderFragmentSerializer.Serialize(fragment);

        var frame = Assert.Single(result);
        Assert.Equal(RenderTreeFrameType.Text, frame.Type);
        Assert.Equal("Hello world", frame.TextContent);
    }

    [Fact]
    public void Serialize_MarkupContent_ProducesMarkupFrame()
    {
        RenderFragment fragment = builder =>
        {
            builder.AddMarkupContent(0, "<b>bold</b>");
        };

        var result = RenderFragmentSerializer.Serialize(fragment);

        var frame = Assert.Single(result);
        Assert.Equal(RenderTreeFrameType.Markup, frame.Type);
        Assert.Equal("<b>bold</b>", frame.MarkupContent);
    }

    [Fact]
    public void Serialize_ElementWithAttributes_ProducesCorrectFrames()
    {
        RenderFragment fragment = builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "container");
            builder.AddAttribute(2, "id", "test");
            builder.AddContent(3, "content");
            builder.CloseElement();
        };

        var result = RenderFragmentSerializer.Serialize(fragment);

        Assert.Equal(5, result.Count);

        Assert.Equal(RenderTreeFrameType.Element, result[0].Type);
        Assert.Equal("div", result[0].ElementName);
        Assert.False(result[0].IsClosingFrame);
        Assert.Equal(RenderTreeFrameType.Attribute, result[1].Type);
        Assert.Equal("class", result[1].AttributeName);
        Assert.Equal("container", result[1].AttributeValue);
        Assert.Equal(RenderTreeFrameType.Attribute, result[2].Type);
        Assert.Equal("id", result[2].AttributeName);
        Assert.Equal("test", result[2].AttributeValue);
        Assert.Equal(RenderTreeFrameType.Text, result[3].Type);
        Assert.Equal("content", result[3].TextContent);
        Assert.Equal(RenderTreeFrameType.Element, result[4].Type);
        Assert.True(result[4].IsClosingFrame);
    }

    [Fact]
    public void Serialize_NestedElements_ProducesCloseMarkers()
    {
        RenderFragment fragment = builder =>
        {
            builder.OpenElement(0, "div");
            builder.OpenElement(1, "p");
            builder.AddContent(2, "text");
            builder.CloseElement();
            builder.CloseElement();
        };

        var result = RenderFragmentSerializer.Serialize(fragment);

        Assert.Equal(5, result.Count);
        Assert.Equal("div", result[0].ElementName);
        Assert.False(result[0].IsClosingFrame);
        Assert.Equal("p", result[1].ElementName);
        Assert.False(result[1].IsClosingFrame);
        Assert.Equal("text", result[2].TextContent);
        Assert.True(result[3].IsClosingFrame);
        Assert.Equal(RenderTreeFrameType.Element, result[3].Type);
        Assert.True(result[4].IsClosingFrame);
        Assert.Equal(RenderTreeFrameType.Element, result[4].Type);
    }

    [Fact]
    public void Serialize_Component_ProducesComponentFrameWithAssemblyQualifiedType()
    {
        RenderFragment fragment = builder =>
        {
            builder.OpenComponent<TestComponent>(0);
            builder.AddComponentParameter(1, "Title", "Hello");
            builder.CloseComponent();
        };

        var result = RenderFragmentSerializer.Serialize(fragment);

        Assert.Equal(3, result.Count);
        Assert.Equal(RenderTreeFrameType.Component, result[0].Type);
        Assert.Contains(nameof(TestComponent), result[0].ComponentType);
        Assert.Equal(RenderTreeFrameType.Attribute, result[1].Type);
        Assert.Equal("Title", result[1].AttributeName);
        Assert.Equal("Hello", result[1].AttributeValue);
        Assert.True(result[2].IsClosingFrame);
        Assert.Equal(RenderTreeFrameType.Component, result[2].Type);
    }

    [Fact]
    public void Serialize_Region_ProducesRegionFrame()
    {
        RenderFragment fragment = builder =>
        {
            builder.OpenRegion(0);
            builder.AddContent(1, "inside region");
            builder.CloseRegion();
        };

        var result = RenderFragmentSerializer.Serialize(fragment);

        Assert.Equal(3, result.Count);
        Assert.Equal(RenderTreeFrameType.Region, result[0].Type);
        Assert.False(result[0].IsClosingFrame);
        Assert.Equal("inside region", result[1].TextContent);
        Assert.True(result[2].IsClosingFrame);
        Assert.Equal(RenderTreeFrameType.Region, result[2].Type);
    }

    [Fact]
    public void Serialize_DelegateAttribute_IsSkipped()
    {
        RenderFragment fragment = builder =>
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "onclick", (Action)(() => { }));
            builder.AddContent(2, "Click me");
            builder.CloseElement();
        };

        var result = RenderFragmentSerializer.Serialize(fragment);

        Assert.Equal(3, result.Count);
        Assert.Equal(RenderTreeFrameType.Element, result[0].Type);
        Assert.Equal(RenderTreeFrameType.Text, result[1].Type);
        Assert.DoesNotContain(result, f => f.AttributeName == "onclick");
        Assert.True(result[2].IsClosingFrame);
    }

    [Fact]
    public void Serialize_SkippedFrames_SiblingStructurePreserved()
    {
        RenderFragment fragment = builder =>
        {
            builder.OpenElement(0, "div");
            builder.OpenElement(1, "button");
            builder.AddAttribute(2, "onclick", (Action)(() => { }));
            builder.AddContent(3, "Click");
            builder.CloseElement();
            builder.OpenElement(4, "span");
            builder.AddContent(5, "sibling");
            builder.CloseElement();
            builder.CloseElement();
        };

        var result = RenderFragmentSerializer.Serialize(fragment);

        Assert.Equal(8, result.Count);
        Assert.Equal("div", result[0].ElementName);
        Assert.False(result[0].IsClosingFrame);
        Assert.Equal("button", result[1].ElementName);
        Assert.False(result[1].IsClosingFrame);
        Assert.Equal("Click", result[2].TextContent);
        Assert.True(result[3].IsClosingFrame);
        Assert.Equal("span", result[4].ElementName);
        Assert.False(result[4].IsClosingFrame);
        Assert.Equal("sibling", result[5].TextContent);
        Assert.True(result[6].IsClosingFrame);
        Assert.True(result[7].IsClosingFrame);
    }

    [Fact]
    public void Serialize_NestedRenderFragment_IsRecursivelySerialized()
    {
        RenderFragment inner = builder =>
        {
            builder.AddContent(0, "nested content");
        };

        RenderFragment fragment = builder =>
        {
            builder.OpenComponent<TestComponent>(0);
            builder.AddAttribute(1, "ChildContent", inner);
            builder.CloseComponent();
        };

        var result = RenderFragmentSerializer.Serialize(fragment);

        Assert.Equal(3, result.Count);
        Assert.Equal(RenderTreeFrameType.Component, result[0].Type);
        Assert.Equal(RenderTreeFrameType.Attribute, result[1].Type);
        Assert.Equal("ChildContent", result[1].AttributeName);
        Assert.Null(result[1].AttributeValue);
        Assert.NotNull(result[1].NestedRenderFragment);
        Assert.True(result[2].IsClosingFrame);
        Assert.Equal(RenderTreeFrameType.Component, result[2].Type);

        var nested = result[1].NestedRenderFragment;
        var nestedFrame = Assert.Single(nested!);
        Assert.Equal(RenderTreeFrameType.Text, nestedFrame.Type);
        Assert.Equal("nested content", nestedFrame.TextContent);
    }

    [Fact]
    public void Serialize_BooleanAttribute_PreservesValue()
    {
        RenderFragment fragment = builder =>
        {
            builder.OpenElement(0, "input");
            builder.AddAttribute(1, "disabled", true);
            builder.CloseElement();
        };

        var result = RenderFragmentSerializer.Serialize(fragment);

        var attr = result.Single(f => f.Type == RenderTreeFrameType.Attribute);
        Assert.Equal("disabled", attr.AttributeName);
        Assert.Equal(true, attr.AttributeValue);
        Assert.True(result.Last().IsClosingFrame);
    }

    [Fact]
    public void Deserialize_EmptyList_ProducesEmptyFragment()
    {
        var frameDTOs = new List<RenderTreeFrameDTO>();

        var fragment = RenderFragmentSerializer.Deserialize(frameDTOs);

        var builder = new RenderTreeBuilder();
        fragment(builder);
        var frames = builder.GetFrames();
        Assert.Equal(0, frames.Count);
    }

    [Fact]
    public void Deserialize_TextFrame_ProducesTextContent()
    {
        var frameDTOs = new List<RenderTreeFrameDTO>
        {
            new() { Type = RenderTreeFrameType.Text, Sequence = 0, TextContent = "Hello" }
        };

        var fragment = RenderFragmentSerializer.Deserialize(frameDTOs);

        var builder = new RenderTreeBuilder();
        fragment(builder);
        var frames = builder.GetFrames();
        Assert.Equal(1, frames.Count);
        Assert.Equal(RenderTreeFrameType.Text, frames.Array[0].FrameType);
        Assert.Equal("Hello", frames.Array[0].TextContent);
    }

    [Fact]
    public void Deserialize_ElementWithContent_ProducesCorrectTree()
    {
        var frameDTOs = new List<RenderTreeFrameDTO>
        {
            new() { Type = RenderTreeFrameType.Element, Sequence = 0, ElementName = "div" },
            new() { Type = RenderTreeFrameType.Attribute, Sequence = 1, AttributeName = "class", AttributeValue = "test" },
            new() { Type = RenderTreeFrameType.Text, Sequence = 2, TextContent = "content" },
            new() { Type = RenderTreeFrameType.Element, IsClosingFrame = true },
        };

        var fragment = RenderFragmentSerializer.Deserialize(frameDTOs);

        var builder = new RenderTreeBuilder();
        fragment(builder);
        var frames = builder.GetFrames();

        Assert.Equal(RenderTreeFrameType.Element, frames.Array[0].FrameType);
        Assert.Equal("div", frames.Array[0].ElementName);
        Assert.Equal(RenderTreeFrameType.Attribute, frames.Array[1].FrameType);
        Assert.Equal("class", frames.Array[1].AttributeName);
        Assert.Equal("test", frames.Array[1].AttributeValue);
        Assert.Equal(RenderTreeFrameType.Text, frames.Array[2].FrameType);
        Assert.Equal("content", frames.Array[2].TextContent);
    }

    [Fact]
    public void Deserialize_NestedRenderFragment_ProducesRenderFragmentAttribute()
    {
        var frameDTOs = new List<RenderTreeFrameDTO>
        {
            new()
            {
                Type = RenderTreeFrameType.Component,
                Sequence = 0,
                ComponentType = $"{typeof(TestComponent).FullName}, {typeof(TestComponent).Assembly.GetName().Name}",
            },
            new()
            {
                Type = RenderTreeFrameType.Attribute,
                Sequence = 1,
                AttributeName = "ChildContent",
                NestedRenderFragment = new List<RenderTreeFrameDTO>
                {
                    new() { Type = RenderTreeFrameType.Text, Sequence = 0, TextContent = "nested" }
                }
            },
            new() { Type = RenderTreeFrameType.Component, IsClosingFrame = true },
        };

        var fragment = RenderFragmentSerializer.Deserialize(frameDTOs);

        var builder = new RenderTreeBuilder();
        fragment(builder);
        var frames = builder.GetFrames();

        Assert.Equal(RenderTreeFrameType.Component, frames.Array[0].FrameType);
        Assert.Equal(RenderTreeFrameType.Attribute, frames.Array[1].FrameType);
        Assert.Equal("ChildContent", frames.Array[1].AttributeName);
        Assert.IsType<RenderFragment>(frames.Array[1].AttributeValue);
    }

    [Fact]
    public void Roundtrip_ComplexFragment_PreservesStructure()
    {
        RenderFragment original = builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "wrapper");
            builder.OpenElement(2, "p");
            builder.AddContent(3, "Hello ");
            builder.OpenElement(4, "strong");
            builder.AddContent(5, "world");
            builder.CloseElement();
            builder.CloseElement();
            builder.AddMarkupContent(6, "<hr />");
            builder.CloseElement();
        };

        var serialized = RenderFragmentSerializer.Serialize(original);
        var deserialized = RenderFragmentSerializer.Deserialize(serialized);

        var originalBuilder = new RenderTreeBuilder();
        original(originalBuilder);
        var originalFrames = originalBuilder.GetFrames();

        var roundtripBuilder = new RenderTreeBuilder();
        deserialized(roundtripBuilder);
        var roundtripFrames = roundtripBuilder.GetFrames();

        Assert.Equal(originalFrames.Count, roundtripFrames.Count);

        for (var i = 0; i < originalFrames.Count; i++)
        {
            Assert.Equal(originalFrames.Array[i].FrameType, roundtripFrames.Array[i].FrameType);
            Assert.Equal(originalFrames.Array[i].Sequence, roundtripFrames.Array[i].Sequence);

            switch (originalFrames.Array[i].FrameType)
            {
                case RenderTreeFrameType.Element:
                    Assert.Equal(originalFrames.Array[i].ElementName, roundtripFrames.Array[i].ElementName);
                    break;
                case RenderTreeFrameType.Text:
                    Assert.Equal(originalFrames.Array[i].TextContent, roundtripFrames.Array[i].TextContent);
                    break;
                case RenderTreeFrameType.Markup:
                    Assert.Equal(originalFrames.Array[i].MarkupContent, roundtripFrames.Array[i].MarkupContent);
                    break;
                case RenderTreeFrameType.Attribute:
                    Assert.Equal(originalFrames.Array[i].AttributeName, roundtripFrames.Array[i].AttributeName);
                    Assert.Equal(originalFrames.Array[i].AttributeValue, roundtripFrames.Array[i].AttributeValue);
                    break;
            }
        }
    }

    [Fact]
    public void Roundtrip_ElementWithKey_PreservesKey()
    {
        RenderFragment original = builder =>
        {
            builder.OpenElement(0, "div");
            builder.SetKey("my-key");
            builder.AddContent(1, "keyed");
            builder.CloseElement();
        };

        var serialized = RenderFragmentSerializer.Serialize(original);

        Assert.Equal("my-key", serialized[0].ElementKey);

        var deserialized = RenderFragmentSerializer.Deserialize(serialized);
        var builder2 = new RenderTreeBuilder();
        deserialized(builder2);
        var frames = builder2.GetFrames();

        Assert.Equal("my-key", frames.Array[0].ElementKey);
    }

    [Fact]
    public void Roundtrip_IntKey_PreservesType()
    {
        RenderFragment original = builder =>
        {
            builder.OpenElement(0, "div");
            builder.SetKey(42);
            builder.AddContent(1, "keyed");
            builder.CloseElement();
        };

        var serialized = RenderFragmentSerializer.Serialize(original);

        Assert.Equal(42, serialized[0].ElementKey);
        Assert.Equal("System.Int32", serialized[0].ElementKeyType);

        var json = JsonSerializer.Serialize(serialized);
        var deserialized = JsonSerializer.Deserialize<List<RenderTreeFrameDTO>>(json)!;
        var fragment = RenderFragmentSerializer.Deserialize(deserialized);

        var builder2 = new RenderTreeBuilder();
        fragment(builder2);
        var frames = builder2.GetFrames();

        Assert.IsType<int>(frames.Array[0].ElementKey);
        Assert.Equal(42, frames.Array[0].ElementKey);
    }

    [Fact]
    public void Roundtrip_GuidKey_PreservesType()
    {
        var guid = Guid.NewGuid();

        RenderFragment original = builder =>
        {
            builder.OpenElement(0, "div");
            builder.SetKey(guid);
            builder.AddContent(1, "keyed");
            builder.CloseElement();
        };

        var serialized = RenderFragmentSerializer.Serialize(original);
        var json = JsonSerializer.Serialize(serialized);
        var deserialized = JsonSerializer.Deserialize<List<RenderTreeFrameDTO>>(json)!;
        var fragment = RenderFragmentSerializer.Deserialize(deserialized);

        var builder2 = new RenderTreeBuilder();
        fragment(builder2);
        var frames = builder2.GetFrames();

        Assert.IsType<Guid>(frames.Array[0].ElementKey);
        Assert.Equal(guid, frames.Array[0].ElementKey);
    }

    [Fact]
    public void Roundtrip_ComponentWithIntKey_PreservesType()
    {
        RenderFragment original = builder =>
        {
            builder.OpenComponent<TestComponent>(0);
            builder.SetKey(99);
            builder.AddComponentParameter(1, "Title", "Hello");
            builder.CloseComponent();
        };

        var serialized = RenderFragmentSerializer.Serialize(original);

        Assert.Equal(99, serialized[0].ComponentKey);
        Assert.Equal("System.Int32", serialized[0].ComponentKeyType);

        var json = JsonSerializer.Serialize(serialized);
        var deserialized = JsonSerializer.Deserialize<List<RenderTreeFrameDTO>>(json)!;
        var fragment = RenderFragmentSerializer.Deserialize(deserialized);

        var builder2 = new RenderTreeBuilder();
        fragment(builder2);
        var frames = builder2.GetFrames();

        Assert.IsType<int>(frames.Array[0].ComponentKey);
        Assert.Equal(99, frames.Array[0].ComponentKey);
    }

    [Fact]
    public void Roundtrip_IntAttribute_PreservesTypeAfterJson()
    {
        RenderFragment original = builder =>
        {
            builder.OpenComponent<TypedComponent>(0);
            builder.AddComponentParameter(1, "Count", 42);
            builder.CloseComponent();
        };

        var serialized = RenderFragmentSerializer.Serialize(original);

        Assert.Equal(42, serialized[1].AttributeValue);
        Assert.Equal("System.Int32", serialized[1].AttributeValueType);

        var json = JsonSerializer.Serialize(serialized);
        var deserialized = JsonSerializer.Deserialize<List<RenderTreeFrameDTO>>(json)!;
        var fragment = RenderFragmentSerializer.Deserialize(deserialized);

        var builder2 = new RenderTreeBuilder();
        fragment(builder2);
        var frames = builder2.GetFrames();

        Assert.IsType<int>(frames.Array[1].AttributeValue);
        Assert.Equal(42, frames.Array[1].AttributeValue);
    }

    [Fact]
    public void Roundtrip_GuidAttribute_PreservesTypeAfterJson()
    {
        var guid = Guid.NewGuid();

        RenderFragment original = builder =>
        {
            builder.OpenComponent<TypedComponent>(0);
            builder.AddComponentParameter(1, "Id", guid);
            builder.CloseComponent();
        };

        var serialized = RenderFragmentSerializer.Serialize(original);
        var json = JsonSerializer.Serialize(serialized);
        var deserialized = JsonSerializer.Deserialize<List<RenderTreeFrameDTO>>(json)!;
        var fragment = RenderFragmentSerializer.Deserialize(deserialized);

        var builder2 = new RenderTreeBuilder();
        fragment(builder2);
        var frames = builder2.GetFrames();

        Assert.IsType<Guid>(frames.Array[1].AttributeValue);
        Assert.Equal(guid, frames.Array[1].AttributeValue);
    }

    [Fact]
    public void Roundtrip_DoubleAttribute_PreservesTypeAfterJson()
    {
        RenderFragment original = builder =>
        {
            builder.OpenComponent<TypedComponent>(0);
            builder.AddComponentParameter(1, "Score", 3.14);
            builder.CloseComponent();
        };

        var serialized = RenderFragmentSerializer.Serialize(original);
        var json = JsonSerializer.Serialize(serialized);
        var deserialized = JsonSerializer.Deserialize<List<RenderTreeFrameDTO>>(json)!;
        var fragment = RenderFragmentSerializer.Deserialize(deserialized);

        var builder2 = new RenderTreeBuilder();
        fragment(builder2);
        var frames = builder2.GetFrames();

        Assert.IsType<double>(frames.Array[1].AttributeValue);
        Assert.Equal(3.14, frames.Array[1].AttributeValue);
    }

    [Fact]
    public void Serialize_GenericRenderFragment_IsSkipped()
    {
        RenderFragment<string> typedFragment = value => builder =>
        {
            builder.AddContent(0, value);
        };

        RenderFragment fragment = builder =>
        {
            builder.OpenComponent<TestComponent>(0);
            builder.AddAttribute(1, "TypedContent", typedFragment);
            builder.AddComponentParameter(2, "Title", "Hello");
            builder.CloseComponent();
        };

        var result = RenderFragmentSerializer.Serialize(fragment);

        Assert.DoesNotContain(result, f => f.AttributeName == "TypedContent");
        Assert.Contains(result, f => f.AttributeName == "Title");
    }

    [Fact]
    public void Serialize_LargeFragment_HandlesHeapFallback()
    {
        RenderFragment fragment = builder =>
        {
            for (var i = 0; i < 200; i++)
            {
                builder.OpenElement(i * 2, "span");
                builder.CloseElement();
            }
        };

        var result = RenderFragmentSerializer.Serialize(fragment);

        Assert.Equal(400, result.Count);

        var deserialized = RenderFragmentSerializer.Deserialize(result);
        var builder2 = new RenderTreeBuilder();
        deserialized(builder2);
        var frames = builder2.GetFrames();
        Assert.Equal(200, frames.Count);
    }

    [Fact]
    public void Serialize_ExceedingMaxDepth_Throws()
    {
        static RenderFragment CreateDeep(int depth) => builder =>
        {
            builder.OpenComponent<TestComponent>(0);
            if (depth < 55)
            {
                builder.AddAttribute(1, "ChildContent", CreateDeep(depth + 1));
            }
            builder.CloseComponent();
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            RenderFragmentSerializer.Serialize(CreateDeep(0)));
        Assert.Contains("50", ex.Message);
    }

    private class TestComponent : IComponent
    {
        [Parameter]
        public string? Title { get; set; }

        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        public void Attach(RenderHandle renderHandle) { }

        public Task SetParametersAsync(ParameterView parameters) => Task.CompletedTask;
    }

    private class TypedComponent : IComponent
    {
        [Parameter]
        public int Count { get; set; }

        [Parameter]
        public Guid Id { get; set; }

        [Parameter]
        public double Score { get; set; }

        public void Attach(RenderHandle renderHandle) { }

        public Task SetParametersAsync(ParameterView parameters) => Task.CompletedTask;
    }
}
