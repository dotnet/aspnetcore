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
    internal static List<RenderTreeNode> SerializeFrames(ReadOnlySpan<RenderTreeFrame> framesSpan, ILogger logger)
    {
        var result = new List<RenderTreeNode>();
        var position = 0;
        SerializeChildren(framesSpan, ref position, framesSpan.Length, result, logger);
        return result;
    }

    private static void SerializeChildren(
        ReadOnlySpan<RenderTreeFrame> frames,
        ref int position,
        int endExcl,
        List<RenderTreeNode> target,
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
                        if (TrySerializeAttribute(attrFrame, logger, out var attr))
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
                        SerializeChildren(frames, ref position, subtreeEnd, node.Children, logger);
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
                        if (TrySerializeAttribute(attrFrame, logger, out var attr))
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
                    SerializeChildren(frames, ref position, subtreeEnd, target, logger);
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
        ILogger logger,
        [NotNullWhen(true)] out RenderTreeAttribute? result)
    {
        result = null;

        if (frame.AttributeValue is RenderFragment)
        {
            throw new NotSupportedException(
                $"Serializing a RenderFragment that contains another RenderFragment attribute " +
                $"('{frame.AttributeName}') across a rendermode boundary is not yet supported.");
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
            var value = param.Value is JsonElement je
                ? ConvertTypedValue(je, param.ValueType!)
                : param.Value;
            builder.AddComponentParameter(0, param.Name, value);
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "Type names are serialized from known component parameter types.")]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Attribute and key types are primitive or well-known types preserved by the application.")]
    private static object? ConvertTypedValue(JsonElement json, string typeName)
    {
        var type = Type.GetType(typeName);
        if (type is not null)
        {
            return json.Deserialize(type);
        }
        return json.ToString();
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
