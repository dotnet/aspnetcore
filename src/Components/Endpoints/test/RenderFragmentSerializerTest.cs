// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text.Json;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class RenderFragmentSerializerTest
{
    private static readonly NullLogger _logger = NullLogger.Instance;

    private static List<RenderTreeNode> SerializeFragment(RenderFragment fragment)
    {
        var capture = new RenderFragmentCapture(fragment);
        using var builder = new RenderTreeBuilder();
        capture.Invoke(builder);
        return RenderFragmentSerializer.SerializeFrames(capture, _logger);
    }

    [Fact]
    public void Serialize_EmptyFragment_ReturnsEmptyList()
    {
        RenderFragment fragment = builder => { };

        var result = SerializeFragment(fragment);

        Assert.Empty(result);
    }

    [Fact]
    public void Serialize_TextContent_ProducesTextNode()
    {
        RenderFragment fragment = builder =>
        {
            builder.AddContent(0, "Hello world");
        };

        var result = SerializeFragment(fragment);

        var node = Assert.Single(result);
        Assert.Equal("text", node.Type);
        Assert.Equal("Hello world", node.Content);
    }

    [Fact]
    public void Serialize_MarkupContent_ProducesMarkupNode()
    {
        RenderFragment fragment = builder =>
        {
            builder.AddMarkupContent(0, "<b>bold</b>");
        };

        var result = SerializeFragment(fragment);

        var node = Assert.Single(result);
        Assert.Equal("markup", node.Type);
        Assert.Equal("<b>bold</b>", node.Content);
    }

    [Fact]
    public void Serialize_ElementWithAttributes_ProducesTreeNode()
    {
        RenderFragment fragment = builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "container");
            builder.AddAttribute(2, "id", "test");
            builder.AddContent(3, "content");
            builder.CloseElement();
        };

        var result = SerializeFragment(fragment);

        var node = Assert.Single(result);
        Assert.Equal("element", node.Type);
        Assert.Equal("div", node.Tag);
        Assert.NotNull(node.Attributes);
        Assert.Equal(2, node.Attributes!.Count);
        Assert.Equal("class", node.Attributes[0].Name);
        Assert.Equal("container", node.Attributes[0].Value);
        Assert.Equal("id", node.Attributes[1].Name);
        Assert.Equal("test", node.Attributes[1].Value);
        Assert.NotNull(node.Children);
        var child = Assert.Single(node.Children!);
        Assert.Equal("text", child.Type);
        Assert.Equal("content", child.Content);
    }

    [Fact]
    public void Serialize_NestedElements_ProducesTreeStructure()
    {
        RenderFragment fragment = builder =>
        {
            builder.OpenElement(0, "div");
            builder.OpenElement(1, "p");
            builder.AddContent(2, "text");
            builder.CloseElement();
            builder.CloseElement();
        };

        var result = SerializeFragment(fragment);

        var div = Assert.Single(result);
        Assert.Equal("element", div.Type);
        Assert.Equal("div", div.Tag);
        var p = Assert.Single(div.Children!);
        Assert.Equal("element", p.Type);
        Assert.Equal("p", p.Tag);
        var text = Assert.Single(p.Children!);
        Assert.Equal("text", text.Type);
        Assert.Equal("text", text.Content);
    }

    [Fact]
    public void Serialize_Component_ProducesComponentDescriptor()
    {
        RenderFragment fragment = builder =>
        {
            builder.OpenComponent<TestComponent>(0);
            builder.AddComponentParameter(1, "Title", "Hello");
            builder.CloseComponent();
        };

        var result = SerializeFragment(fragment);

        var node = Assert.Single(result);
        Assert.Equal("component", node.Type);
        Assert.Contains(nameof(TestComponent), node.ComponentType);
        Assert.NotNull(node.ComponentParameters);
        var param = Assert.Single(node.ComponentParameters!);
        Assert.Equal("Title", param.Name);
        Assert.Equal("Hello", param.Value);
    }

    [Fact]
    public void Serialize_Region_IsTransparent()
    {
        RenderFragment fragment = builder =>
        {
            builder.OpenRegion(0);
            builder.AddContent(1, "inside region");
            builder.CloseRegion();
        };

        var result = SerializeFragment(fragment);

        // Region is transparent — its children are inlined
        var node = Assert.Single(result);
        Assert.Equal("text", node.Type);
        Assert.Equal("inside region", node.Content);
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

        var result = SerializeFragment(fragment);

        var node = Assert.Single(result);
        Assert.Equal("element", node.Type);
        Assert.Null(node.Attributes); // onclick was skipped, no attributes remain
        var child = Assert.Single(node.Children!);
        Assert.Equal("text", child.Type);
        Assert.Equal("Click me", child.Content);
    }

    [Fact]
    public void Serialize_EventCallbackAttribute_IsSkipped()
    {
        RenderFragment fragment = builder =>
        {
            builder.OpenComponent<TestComponent>(0);
            builder.AddAttribute(1, "OnClick", EventCallback.Factory.Create(new object(), () => { }));
            builder.AddComponentParameter(2, "Title", "Hello");
            builder.CloseComponent();
        };

        var result = SerializeFragment(fragment);

        var node = Assert.Single(result);
        Assert.DoesNotContain(node.ComponentParameters!, p => p.Name == "OnClick");
        Assert.Contains(node.ComponentParameters!, p => p.Name == "Title");
    }

    [Fact]
    public void Serialize_EventCallbackOfT_Attribute_IsSkipped()
    {
        RenderFragment fragment = builder =>
        {
            builder.OpenComponent<TestComponent>(0);
            builder.AddAttribute(1, "OnChange", EventCallback.Factory.Create<string>(new object(), _ => { }));
            builder.AddComponentParameter(2, "Title", "Hello");
            builder.CloseComponent();
        };

        var result = SerializeFragment(fragment);

        var node = Assert.Single(result);
        Assert.DoesNotContain(node.ComponentParameters!, p => p.Name == "OnChange");
        Assert.Contains(node.ComponentParameters!, p => p.Name == "Title");
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

        var result = SerializeFragment(fragment);

        var div = Assert.Single(result);
        Assert.Equal("element", div.Type);
        Assert.Equal("div", div.Tag);
        Assert.Equal(2, div.Children!.Count);

        var button = div.Children[0];
        Assert.Equal("element", button.Type);
        Assert.Equal("button", button.Tag);
        Assert.Null(button.Attributes); // onclick skipped
        var buttonText = Assert.Single(button.Children!);
        Assert.Equal("Click", buttonText.Content);

        var span = div.Children[1];
        Assert.Equal("element", span.Type);
        Assert.Equal("span", span.Tag);
        var spanText = Assert.Single(span.Children!);
        Assert.Equal("sibling", spanText.Content);
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

        var result = SerializeFragment(fragment);

        var node = Assert.Single(result);
        var attr = Assert.Single(node.Attributes!);
        Assert.Equal("disabled", attr.Name);
        Assert.Equal(true, attr.Value);
    }

    [Fact]
    public void Deserialize_EmptyList_ProducesEmptyFragment()
    {
        var nodes = new List<RenderTreeNode>();

        var fragment = RenderFragmentSerializer.Deserialize(nodes);

        var builder = new RenderTreeBuilder();
        fragment(builder);
        var frames = builder.GetFrames();
        Assert.Equal(0, frames.Count);
    }

    [Fact]
    public void Deserialize_TextNode_ProducesTextContent()
    {
        var nodes = new List<RenderTreeNode>
        {
            new() { Type = "text", Content = "Hello" }
        };

        var fragment = RenderFragmentSerializer.Deserialize(nodes);

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
        var nodes = new List<RenderTreeNode>
        {
            new()
            {
                Type = "element",
                Tag = "div",
                Attributes = new()
                {
                    new() { Name = "class", Value = "test" }
                },
                Children = new()
                {
                    new() { Type = "text", Content = "content" }
                }
            }
        };

        var fragment = RenderFragmentSerializer.Deserialize(nodes);

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
    public void Deserialize_NestedRenderFragmentFromModel_ProducesRenderFragmentParameter()
    {
        var nodes = new List<RenderTreeNode>
        {
            new()
            {
                Type = "component",
                ComponentType = typeof(TestComponent).AssemblyQualifiedName,
                ComponentParameters = new()
                {
                    new()
                    {
                        Name = "ChildContent",
                        Value = new SerializedRenderFragment
                        {
                            Nodes = new()
                            {
                                new() { Type = "text", Content = "hello from nested" }
                            }
                        },
                        ValueType = RenderFragmentSerializer.SerializedRenderFragmentValueType,
                    }
                }
            }
        };

        var fragment = RenderFragmentSerializer.Deserialize(nodes);

        var builder = new RenderTreeBuilder();
        fragment(builder);
        var frames = builder.GetFrames();

        Assert.Equal(RenderTreeFrameType.Component, frames.Array[0].FrameType);
        Assert.Equal(RenderTreeFrameType.Attribute, frames.Array[1].FrameType);
        Assert.Equal("ChildContent", frames.Array[1].AttributeName);
        var childContent = Assert.IsType<RenderFragment>(frames.Array[1].AttributeValue);

        var innerBuilder = new RenderTreeBuilder();
        childContent(innerBuilder);
        var innerFrames = innerBuilder.GetFrames();
        Assert.Equal(RenderTreeFrameType.Text, innerFrames.Array[0].FrameType);
        Assert.Equal("hello from nested", innerFrames.Array[0].TextContent);
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

        var serialized = SerializeFragment(original);
        var deserialized = RenderFragmentSerializer.Deserialize(serialized);

        var roundtripBuilder = new RenderTreeBuilder();
        deserialized(roundtripBuilder);
        var roundtripFrames = roundtripBuilder.GetFrames();

        // Verify the tree structure is preserved
        // div > (p > ("Hello " + strong > "world"), <hr />)
        Assert.Equal(RenderTreeFrameType.Element, roundtripFrames.Array[0].FrameType);
        Assert.Equal("div", roundtripFrames.Array[0].ElementName);
        Assert.Equal(RenderTreeFrameType.Attribute, roundtripFrames.Array[1].FrameType);
        Assert.Equal("class", roundtripFrames.Array[1].AttributeName);
        Assert.Equal("wrapper", roundtripFrames.Array[1].AttributeValue);
        Assert.Equal(RenderTreeFrameType.Element, roundtripFrames.Array[2].FrameType);
        Assert.Equal("p", roundtripFrames.Array[2].ElementName);
        Assert.Equal(RenderTreeFrameType.Text, roundtripFrames.Array[3].FrameType);
        Assert.Equal("Hello ", roundtripFrames.Array[3].TextContent);
        Assert.Equal(RenderTreeFrameType.Element, roundtripFrames.Array[4].FrameType);
        Assert.Equal("strong", roundtripFrames.Array[4].ElementName);
        Assert.Equal(RenderTreeFrameType.Text, roundtripFrames.Array[5].FrameType);
        Assert.Equal("world", roundtripFrames.Array[5].TextContent);
        Assert.Equal(RenderTreeFrameType.Markup, roundtripFrames.Array[6].FrameType);
        Assert.Equal("<hr />", roundtripFrames.Array[6].MarkupContent);
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

        var serialized = SerializeFragment(original);

        Assert.Equal("my-key", serialized[0].Key);

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

        var serialized = SerializeFragment(original);

        Assert.Equal(42, serialized[0].Key);
        Assert.Contains("System.Int32", serialized[0].KeyType);

        var json = JsonSerializer.Serialize(serialized);
        var deserialized = JsonSerializer.Deserialize<List<RenderTreeNode>>(json)!;
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

        var serialized = SerializeFragment(original);
        var json = JsonSerializer.Serialize(serialized);
        var deserialized = JsonSerializer.Deserialize<List<RenderTreeNode>>(json)!;
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

        var serialized = SerializeFragment(original);

        Assert.Equal(99, serialized[0].Key);
        Assert.Contains("System.Int32", serialized[0].KeyType);

        var json = JsonSerializer.Serialize(serialized);
        var deserialized = JsonSerializer.Deserialize<List<RenderTreeNode>>(json)!;
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

        var serialized = SerializeFragment(original);

        var param = Assert.Single(serialized[0].ComponentParameters!);
        Assert.Equal("Count", param.Name);
        Assert.Equal(42, param.Value);
        Assert.Contains("System.Int32", param.ValueType);

        var json = JsonSerializer.Serialize(serialized);
        var deserialized = JsonSerializer.Deserialize<List<RenderTreeNode>>(json)!;
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

        var serialized = SerializeFragment(original);
        var json = JsonSerializer.Serialize(serialized);
        var deserialized = JsonSerializer.Deserialize<List<RenderTreeNode>>(json)!;
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

        var serialized = SerializeFragment(original);
        var json = JsonSerializer.Serialize(serialized);
        var deserialized = JsonSerializer.Deserialize<List<RenderTreeNode>>(json)!;
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

        var result = SerializeFragment(fragment);

        var node = Assert.Single(result);
        Assert.DoesNotContain(node.ComponentParameters!, p => p.Name == "TypedContent");
        Assert.Contains(node.ComponentParameters!, p => p.Name == "Title");
    }

    [Fact]
    public void Serialize_LargeFragment_HandlesCorrectly()
    {
        RenderFragment fragment = builder =>
        {
            for (var i = 0; i < 200; i++)
            {
                builder.OpenElement(i * 2, "span");
                builder.CloseElement();
            }
        };

        var result = SerializeFragment(fragment);

        Assert.Equal(200, result.Count);
        Assert.All(result, n => Assert.Equal("element", n.Type));

        var deserialized = RenderFragmentSerializer.Deserialize(result);
        var builder2 = new RenderTreeBuilder();
        deserialized(builder2);
        var frames = builder2.GetFrames();
        Assert.Equal(200, frames.Count);
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
