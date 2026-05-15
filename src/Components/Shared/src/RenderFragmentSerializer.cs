// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components;

internal static partial class RenderFragmentSerializer
{
    internal const string SerializedRenderFragmentValueType = "#RenderFragment";

    internal static List<RenderTreeNode> SerializeFrames(
        RenderFragmentCapture capture,
        ILogger logger)
    {
        var result = new List<RenderTreeNode>();
        var frames = capture.GetCapturedFrames();
        var position = 0;
        SerializeChildren(ref position, frames, frames.Length, result, capture, logger);
        return result;
    }

    private static void SerializeChildren(
        ref int position,
        ReadOnlySpan<RenderTreeFrame> frames,
        int endExcl,
        List<RenderTreeNode> target,
        RenderFragmentCapture currentCapture,
        ILogger logger)
    {
        while (position < endExcl)
        {
            ref readonly var frame = ref frames[position];
            switch (frame.FrameType)
            {
                case RenderTreeFrameType.Element:
                {
                    var node = new RenderTreeNode
                    {
                        Type = "element",
                        Tag = frame.ElementName,
                    };
                    if (frame.ElementKey is not null)
                    {
                        node.Key = frame.ElementKey;
                        node.KeyType = frame.ElementKey.GetType().AssemblyQualifiedName;
                    }

                    var subtreeEnd = position + frame.ElementSubtreeLength;
                    position++;

                    // Collect attributes
                    while (position < subtreeEnd && frames[position].FrameType is RenderTreeFrameType.Attribute)
                    {
                        ref readonly var attrFrame = ref frames[position];
                        if (TrySerializeAttribute(attrFrame, position, currentCapture, logger, out var attr))
                        {
                            node.Attributes ??= new();
                            node.Attributes.Add(attr);
                        }
                        position++;
                    }

                    // Collect children
                    if (position < subtreeEnd)
                    {
                        node.Children = new();
                        SerializeChildren(ref position, frames, subtreeEnd, node.Children, currentCapture, logger);
                    }

                    target.Add(node);
                    break;
                }
                case RenderTreeFrameType.Text:
                    target.Add(new RenderTreeNode { Type = "text", Content = frame.TextContent });
                    position++;
                    break;
                case RenderTreeFrameType.Markup:
                    target.Add(new RenderTreeNode { Type = "markup", Content = frame.MarkupContent });
                    position++;
                    break;
                case RenderTreeFrameType.Component:
                {
                    var node = new RenderTreeNode
                    {
                        Type = "component",
                        ComponentType = frame.ComponentType?.AssemblyQualifiedName,
                    };
                    if (frame.ComponentKey is not null)
                    {
                        node.Key = frame.ComponentKey;
                        node.KeyType = frame.ComponentKey.GetType().AssemblyQualifiedName;
                    }

                    var subtreeEnd = position + frame.ComponentSubtreeLength;
                    position++;

                    // Collect component parameters (attribute frames)
                    while (position < subtreeEnd && frames[position].FrameType is RenderTreeFrameType.Attribute)
                    {
                        ref readonly var attrFrame = ref frames[position];
                        if (TrySerializeAttribute(attrFrame, position, currentCapture, logger, out var attr))
                        {
                            node.ComponentParameters ??= new();
                            node.ComponentParameters.Add(attr);
                        }
                        position++;
                    }

                    // Skip remaining child frames within the component subtree.
                    // Components inside fragments are descriptors; their children are rendered on the client.
                    position = subtreeEnd;
                    target.Add(node);
                    break;
                }
                case RenderTreeFrameType.Region:
                {
                    var subtreeEnd = position + frame.RegionSubtreeLength;
                    position++;
                    // Regions are transparent — inline their children into the parent
                    SerializeChildren(ref position, frames, subtreeEnd, target, currentCapture, logger);
                    break;
                }
                case RenderTreeFrameType.ElementReferenceCapture:
                    Log.ElementReferenceCaptureSkipped(logger);
                    position++;
                    break;
                case RenderTreeFrameType.ComponentReferenceCapture:
                    Log.ComponentReferenceCaptureSkipped(logger);
                    position++;
                    break;
                case RenderTreeFrameType.ComponentRenderMode:
                    Log.ComponentRenderModeSkipped(logger);
                    position++;
                    break;
                case RenderTreeFrameType.NamedEvent:
                    Log.NamedEventSkipped(logger);
                    position++;
                    break;
                case RenderTreeFrameType.Attribute:
                    // Stray attribute outside an element/component — skip
                    position++;
                    break;

                default:
                    throw new NotImplementedException($"Serialization for frame type '{frame.FrameType}' is not implemented.");
            }
        }
    }

    private static bool TrySerializeAttribute(
        in RenderTreeFrame frame,
        int frameIndex,
        RenderFragmentCapture currentCapture,
        ILogger logger,
        [NotNullWhen(true)] out RenderTreeAttribute? result)
    {
        result = null;
        if (frame.AttributeValue is RenderFragment)
        {
            if (!currentCapture.ChildCaptures.TryGetValue(frameIndex, out var nestedCapture))
            {
                // If we get here, then it means that wrapping didn't happen, or it was not executed (e.g. SSRRenderBoundary in the case of disabled prerendering).
                // TODO: throw for now, then add an anotation that will allow for component frames to be executed through builder if this happens.
                throw new InvalidOperationException(
                    $"Cannot serialize RenderFragment parameter '{frame.AttributeName}' because it was not captured during rendering.");
            }

            result = new RenderTreeAttribute
            {
                Name = frame.AttributeName!,
                Value = new SerializedRenderFragment { Nodes = SerializeFrames(nestedCapture, logger) },
                ValueType = SerializedRenderFragmentValueType,
            };
            return true;
        }

        if (frame.AttributeValue is Delegate d)
        {
            if (d.GetType().IsGenericType && d.GetType().GetGenericTypeDefinition() == typeof(RenderFragment<>))
            {
                Log.GenericRenderFragmentSkipped(logger, frame.AttributeName);
                return false;
            }

            Log.EventHandlerSkipped(logger, frame.AttributeName);
            return false;
        }

        if (IsEventCallback(frame.AttributeValue))
        {
            Log.EventHandlerSkipped(logger, frame.AttributeName);
            return false;
        }

        result = new RenderTreeAttribute
        {
            Name = frame.AttributeName!,
            Value = frame.AttributeValue,
            ValueType = frame.AttributeValue?.GetType().AssemblyQualifiedName,
        };
        return true;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "Component types referenced in serialized RenderFragments are expected to be preserved by the application.")]
    internal static RenderFragment Deserialize(List<RenderTreeNode> nodes)
    {
        return builder => DeserializeNodes(builder, nodes);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "Component types referenced in serialized RenderFragments are expected to be preserved by the application.")]
    private static void DeserializeNodes(RenderTreeBuilder builder, List<RenderTreeNode> nodes)
    {
        for (var i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            switch (node.Type)
            {
                case "element":
                    builder.OpenElement(0, node.Tag!);
                    if (node.Key is not null)
                    {
                        builder.SetKey(node.Key is JsonElement je ? ConvertTypedValue(je, node.KeyType!) : node.Key);
                    }
                    DeserializeAttributes(builder, node.Attributes);
                    if (node.Children is not null)
                    {
                        DeserializeNodes(builder, node.Children);
                    }
                    builder.CloseElement();
                    break;

                case "text":
                    builder.AddContent(0, node.Content);
                    break;

                case "markup":
                    builder.AddMarkupContent(0, node.Content);
                    break;

                case "component":
                    var componentType = Type.GetType(node.ComponentType!);
                    if (componentType is null)
                    {
                        throw new InvalidOperationException($"Cannot resolve component type '{node.ComponentType}'.");
                    }
                    builder.OpenComponent(0, componentType);
                    if (node.Key is not null)
                    {
                        builder.SetKey(node.Key is JsonElement je ? ConvertTypedValue(je, node.KeyType!) : node.Key);
                    }
                    DeserializeComponentParameters(builder, node.ComponentParameters);
                    builder.CloseComponent();
                    break;

                default:
                    throw new NotImplementedException($"Deserialization for node type '{node.Type}' is not implemented.");
            }
        }
    }

    private static void DeserializeAttributes(RenderTreeBuilder builder, List<RenderTreeAttribute>? attributes)
    {
        if (attributes is null)
        {
            return;
        }

        foreach (var attr in attributes)
        {
            var value = attr.Value is JsonElement je
                ? ConvertTypedValue(je, attr.ValueType!)
                : attr.Value;
            builder.AddAttribute(0, attr.Name, value);
        }
    }

    private static void DeserializeComponentParameters(RenderTreeBuilder builder, List<RenderTreeAttribute>? parameters)
    {
        if (parameters is null)
        {
            return;
        }

        foreach (var param in parameters)
        {
            var value = DeserializeAttributeValue(param);
            builder.AddComponentParameter(0, param.Name, value);
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "SerializedRenderFragment and its members are preserved by existing DynamicDependency attributes.")]
    private static object? DeserializeAttributeValue(RenderTreeAttribute attr)
    {
        if (attr.ValueType == SerializedRenderFragmentValueType)
        {
            var serialized = attr.Value switch
            {
                JsonElement je => JsonSerializer.Deserialize<SerializedRenderFragment>(je.GetRawText()),
                SerializedRenderFragment sf => sf,
                _ => throw new InvalidOperationException(
                    $"Unexpected value type '{attr.Value?.GetType()}' for serialized RenderFragment attribute '{attr.Name}'.")
            };
            return Deserialize(serialized!.Nodes);
        }

        return attr.Value is JsonElement json
            ? ConvertTypedValue(json, attr.ValueType!)
            : attr.Value;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "Type names are serialized from known component parameter types.")]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Attribute and key types are primitive or well-known types preserved by the application.")]
    private static object? ConvertTypedValue(JsonElement json, string typeName)
    {
        var type = Type.GetType(typeName) ?? throw new InvalidOperationException($"Could not resolve serialized attribute type '{typeName}'.");
        return json.Deserialize(type);
    }

    private static bool IsEventCallback(object? value)
    {
        if (value is null)
        {
            return false;
        }

        if (value is EventCallback)
        {
            return true;
        }

        var type = value.GetType();
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EventCallback<>);
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Warning, "Event handler '{AttributeName}' inside a RenderFragment was skipped during serialization. Delegates cannot cross render mode boundaries.", EventName = "EventHandlerSkipped")]
        public static partial void EventHandlerSkipped(ILogger logger, string? attributeName);

        [LoggerMessage(2, LogLevel.Warning, "An element @ref capture inside a RenderFragment was skipped during serialization. Element references cannot cross render mode boundaries.", EventName = "ElementReferenceCaptureSkipped")]
        public static partial void ElementReferenceCaptureSkipped(ILogger logger);

        [LoggerMessage(3, LogLevel.Warning, "A component @ref capture inside a RenderFragment was skipped during serialization. Component references cannot cross render mode boundaries.", EventName = "ComponentReferenceCaptureSkipped")]
        public static partial void ComponentReferenceCaptureSkipped(ILogger logger);

        [LoggerMessage(4, LogLevel.Warning, "A @rendermode directive inside a RenderFragment was skipped during serialization. The render mode is already determined by the boundary the RenderFragment is crossing.", EventName = "ComponentRenderModeSkipped")]
        public static partial void ComponentRenderModeSkipped(ILogger logger);

        [LoggerMessage(5, LogLevel.Warning, "A @formname directive inside a RenderFragment was skipped during serialization. Named events are an SSR-only mechanism and cannot cross render mode boundaries.", EventName = "NamedEventSkipped")]
        public static partial void NamedEventSkipped(ILogger logger);

        [LoggerMessage(6, LogLevel.Warning, "A generic RenderFragment<T> parameter '{AttributeName}' inside a RenderFragment was skipped during serialization. Only non-generic RenderFragment is supported across render mode boundaries.", EventName = "GenericRenderFragmentSkipped")]
        public static partial void GenericRenderFragmentSkipped(ILogger logger, string? attributeName);
    }

    internal static Dictionary<string, RenderFragmentCapture>? WrapRenderFragments(Dictionary<string, object?> parameters)
    {
        List<string>? fragmentNames = null;

        foreach (var (name, value) in parameters)
        {
            if (value is RenderFragment)
            {
                fragmentNames ??= new();
                fragmentNames.Add(name);
            }
        }

        if (fragmentNames is null)
        {
            return null;
        }

        var topLevelCaptures = new Dictionary<string, RenderFragmentCapture>(fragmentNames.Count);

        foreach (var name in fragmentNames)
        {
            var original = (RenderFragment)parameters[name]!;
            var capture = RenderFragmentCapture.Wrap(original);
            topLevelCaptures[name] = capture;
            parameters[name] = (RenderFragment)capture.Invoke;
        }

        return topLevelCaptures;
    }
}

// Wrapper for one RenderFragment instance. Captures the frames produced by the original
// fragment, and tracks any nested RenderFragment parameters discovered inside those frames
// as child captures keyed by their attribute frame index inside the snapshot.
internal sealed class RenderFragmentCapture
{
    private readonly RenderFragment _original;
    private readonly Dictionary<int, RenderFragmentCapture> _childCaptures = new();
    private RenderTreeFrame[]? _capturedFrames;

    private RenderFragmentCapture(RenderFragment original)
    {
        _original = original;
    }

    public static RenderFragmentCapture Wrap(RenderFragment original)
        => new(original);

    public IReadOnlyDictionary<int, RenderFragmentCapture> ChildCaptures => _childCaptures;

    public void Invoke(RenderTreeBuilder builder)
    {
        var start = builder.GetFrames().Count;
        _original(builder);
        var end = builder.GetFrames().Count;

        // Walk the produced frames and wrap any nested RenderFragment component parameters.
        // Child captures are keyed by index relative to the snapshot start, so the keys
        // line up with the positions seen by the serializer when it walks _capturedFrames.
        WrapNestedFragments(builder, start, end);

        var frames = builder.GetFrames();
        var count = end - start;
        _capturedFrames = new RenderTreeFrame[count];
        Array.Copy(frames.Array, start, _capturedFrames, 0, count);
    }

    public RenderTreeFrame[] GetCapturedFrames()
    {
        if (_capturedFrames is null)
        {
            throw new InvalidOperationException("Cannot retrieve captured frames because the RenderFragment was not invoked during rendering.");
        }
        return _capturedFrames;
    }

    private void WrapNestedFragments(RenderTreeBuilder builder, int start, int end)
    {
        var frames = builder.GetFrames();
        for (var i = start; i < end; i++)
        {
            ref readonly var frame = ref frames.Array[i];
            if (frame.FrameType is not RenderTreeFrameType.Component)
            {
                continue;
            }

            var componentSubtreeEnd = i + frame.ComponentSubtreeLength;
            // Attribute frames are always contiguous immediately after their owning Component
            // frame (enforced by RenderTreeBuilder.AssertCanAddAttribute), so the first
            // non-attribute frame ends the parameter block.
            for (var j = i + 1; j < componentSubtreeEnd && frames.Array[j].FrameType is RenderTreeFrameType.Attribute; j++)
            {
                ref readonly var attrFrame = ref frames.Array[j];
                if (attrFrame.AttributeValue is RenderFragment innerRf)
                {
                    var innerCapture = Wrap(innerRf);

                    // Replace the original delegate in the live render buffer with the wrapper.
                    // This is required so that when the nested component later invokes its
                    // RenderFragment parameter, control flows through innerCapture.Invoke and
                    // populates innerCapture._capturedFrames. Looking up the capture by frame
                    // index in _childCaptures would otherwise find an entry whose frames were
                    // never recorded.
                    // We can't mutate frames.Array[j].AttributeValueField directly here because
                    // that field is internal to Microsoft.AspNetCore.Components and this file is
                    // linked into Endpoints/Server/WebAssembly, which are outside its
                    // InternalsVisibleTo set. SetAttributeValue is the public bridge.
                    builder.SetAttributeValue(j, (RenderFragment)innerCapture.Invoke);
                    _childCaptures[j - start] = innerCapture;
                }
            }
        }
    }
}

internal sealed class SerializedRenderFragment
{
    public List<RenderTreeNode> Nodes { get; init; } = [];
}

internal sealed class RenderTreeNode
{
    public string Type { get; set; } = "";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Tag { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<RenderTreeAttribute>? Attributes { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Key { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? KeyType { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Content { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ComponentType { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<RenderTreeAttribute>? ComponentParameters { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<RenderTreeNode>? Children { get; set; }
}

internal sealed class RenderTreeAttribute
{
    public string Name { get; set; } = "";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Value { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ValueType { get; set; }
}
