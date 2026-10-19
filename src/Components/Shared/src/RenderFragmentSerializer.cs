// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components;

internal static partial class RenderFragmentSerializer
{
    internal const string SerializedRenderFragmentValueType = "#RenderFragment";

    /// Converts the captured render tree frames from a <see cref="RenderFragmentCapture"/> into a
    /// JSON-serializable tree of <see cref="RenderTreeNode"/> objects. This is the entry point for
    /// serializing a RenderFragment that was captured in renderer.
    internal static List<RenderTreeNode> SerializeFrames(
        RenderFragmentCapture capture,
        ILogger logger,
        string? ownerComponentType = null)
    {
        var result = new List<RenderTreeNode>();
        var frames = capture.GetCapturedFrames();
        var position = 0;
        SerializeChildren(ref position, frames, frames.Length, result, capture, logger, ownerComponentType);
        return result;
    }

    /// Walks a contiguous range of render tree frames and converts each into a <see cref="RenderTreeNode"/>,
    /// recursively processing element children, component parameters, and nested RenderFragment captures.
    /// Non-serializable frames (event handlers, @ref captures, @rendermode, @formname) are skipped with warnings.
    private static void SerializeChildren(
        ref int position,
        ReadOnlySpan<RenderTreeFrame> frames,
        int endExcl,
        List<RenderTreeNode> target,
        RenderFragmentCapture currentCapture,
        ILogger logger,
        string? ownerComponentType)
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
                        node.KeyTypeName = frame.ElementKey.GetType().FullName;
                        node.KeyTypeAssembly = frame.ElementKey.GetType().Assembly.GetName().Name;
                    }

                    var subtreeEnd = position + frame.ElementSubtreeLength;
                    position++;

                    // Collect attributes
                    while (position < subtreeEnd && frames[position].FrameType is RenderTreeFrameType.Attribute)
                    {
                        ref readonly var attrFrame = ref frames[position];
                        if (TrySerializeAttribute(attrFrame, position, currentCapture, logger, ownerComponentType, out var attr))
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
                        SerializeChildren(ref position, frames, subtreeEnd, node.Children, currentCapture, logger, ownerComponentType);
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
                        TypeName = frame.ComponentType?.FullName,
                        TypeAssembly = frame.ComponentType?.Assembly.GetName().Name,
                        Sequence = frame.Sequence,
                    };
                    if (frame.ComponentKey is not null)
                    {
                        node.Key = frame.ComponentKey;
                        node.KeyTypeName = frame.ComponentKey.GetType().FullName;
                        node.KeyTypeAssembly = frame.ComponentKey.GetType().Assembly.GetName().Name;
                    }

                    var subtreeEnd = position + frame.ComponentSubtreeLength;
                    position++;

                    // Collect component's attribute frames
                    while (position < subtreeEnd && frames[position].FrameType is RenderTreeFrameType.Attribute)
                    {
                        ref readonly var attrFrame = ref frames[position];
                        if (TrySerializeAttribute(attrFrame, position, currentCapture, logger, ownerComponentType, out var attr))
                        {
                            node.ComponentParameters ??= new();
                            node.ComponentParameters.Add(attr);
                        }
                        position++;
                    }

                    while (position < subtreeEnd)
                    {
                        ref readonly var inner = ref frames[position];
                        if (inner.FrameType is RenderTreeFrameType.ComponentRenderMode)
                        {
                            node.RenderModeName = GetRenderModeName(inner.ComponentRenderMode);
                            node.Prerender = GetRenderModePrerender(inner.ComponentRenderMode);
                            break;
                        }
                        position++;
                    }

                    position = subtreeEnd;
                    target.Add(node);
                    break;
                }
                case RenderTreeFrameType.Region:
                {
                    var subtreeEnd = position + frame.RegionSubtreeLength;
                    position++;
                    // Inline region's children into the parent
                    SerializeChildren(ref position, frames, subtreeEnd, target, currentCapture, logger, ownerComponentType);
                    break;
                }
                case RenderTreeFrameType.ElementReferenceCapture:
                    Log.ElementReferenceCaptureSkipped(logger, ownerComponentType);
                    position++;
                    break;
                case RenderTreeFrameType.ComponentReferenceCapture:
                    Log.ComponentReferenceCaptureSkipped(logger, ownerComponentType);
                    position++;
                    break;
                case RenderTreeFrameType.NamedEvent:
                    Log.NamedEventSkipped(logger, ownerComponentType);
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
        string? ownerComponentType,
        [NotNullWhen(true)] out RenderTreeAttribute? result)
    {
        result = null;
        if (frame.AttributeValue is RenderFragment)
        {
            if (!currentCapture.ChildCaptures.TryGetValue(frameIndex, out var nestedCapture))
            {
                // If we get here, then it means that wrapping didn't happen, or it was not executed (e.g. SSRRenderBoundary in the case of disabled prerendering).
                // TODO: https://github.com/dotnet/aspnetcore/issues/66739 - Add an opt-in annotation that tells the
                // serializer to execute the RenderFragment directly when it wasn't invoked during prerendering.
                throw new InvalidOperationException(
                    $"Cannot serialize RenderFragment parameter '{frame.AttributeName}' because it was not captured during rendering.");
            }

            result = new RenderTreeAttribute
            {
                Name = frame.AttributeName!,
                Value = new SerializedRenderFragment { Nodes = SerializeFrames(nestedCapture, logger, ownerComponentType) },
                TypeName = SerializedRenderFragmentValueType,
            };
            return true;
        }

        if (frame.AttributeValue is Delegate d)
        {
            if (d.GetType().IsGenericType && d.GetType().GetGenericTypeDefinition() == typeof(RenderFragment<>))
            {
                Log.GenericRenderFragmentSkipped(logger, frame.AttributeName, ownerComponentType);
                return false;
            }

            Log.EventHandlerSkipped(logger, frame.AttributeName, ownerComponentType);
            return false;
        }

        if (IsEventCallback(frame.AttributeValue))
        {
            Log.EventHandlerSkipped(logger, frame.AttributeName, ownerComponentType);
            return false;
        }

        result = new RenderTreeAttribute
        {
            Name = frame.AttributeName!,
            Value = frame.AttributeValue,
            TypeName = frame.AttributeValue?.GetType().FullName,
            TypeAssembly = frame.AttributeValue?.GetType().Assembly.GetName().Name,
        };
        return true;
    }

    internal static RenderFragment Deserialize(List<RenderTreeNode> nodes, JsonSerializerOptions jsonOptions, ComponentParametersTypeCache typeCache)
    {
        return builder => DeserializeNodes(builder, nodes, jsonOptions, typeCache);
    }

    internal static string? GetRenderModeName(IComponentRenderMode? renderMode)
    {
        return renderMode switch
        {
            null => null,
            InteractiveServerRenderMode => "InteractiveServer",
            InteractiveWebAssemblyRenderMode => "InteractiveWebAssembly",
            InteractiveAutoRenderMode => "InteractiveAuto",
            _ => throw new InvalidOperationException($"Unsupported render mode type: '{renderMode.GetType().Name}'."),
        };
    }

    // Captures the prerender flag so a custom render-mode instantiation
    // (e.g. new InteractiveWebAssemblyRenderMode(prerender: false)) round-trips correctly.
    internal static bool GetRenderModePrerender(IComponentRenderMode? renderMode)
    {
        return renderMode switch
        {
            InteractiveServerRenderMode m => m.Prerender,
            InteractiveWebAssemblyRenderMode m => m.Prerender,
            InteractiveAutoRenderMode m => m.Prerender,
            _ => true,
        };
    }

    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Component types referenced in serialized RenderFragments are expected to be preserved by the application.")]
    private static void DeserializeNodes(RenderTreeBuilder builder, List<RenderTreeNode> nodes, JsonSerializerOptions? jsonOptions, ComponentParametersTypeCache typeCache)
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
                        builder.SetKey(node.Key is JsonElement je ? ConvertTypedValue(je, node.KeyTypeAssembly!, node.KeyTypeName!, jsonOptions, typeCache) : node.Key);
                    }
                    DeserializeAttributes(builder, node.Attributes, jsonOptions, typeCache);
                    if (node.Children is not null)
                    {
                        DeserializeNodes(builder, node.Children, jsonOptions, typeCache);
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
                    var componentType = typeCache.GetParameterType(node.TypeAssembly!, node.TypeName!);
                    if (componentType is null)
                    {
                        throw new InvalidOperationException($"Cannot resolve component type '{node.TypeName}' from assembly '{node.TypeAssembly}'.");
                    }
                    builder.OpenComponent(node.Sequence ?? 0, componentType);
                    if (node.Key is not null)
                    {
                        builder.SetKey(node.Key is JsonElement je ? ConvertTypedValue(je, node.KeyTypeAssembly!, node.KeyTypeName!, jsonOptions, typeCache) : node.Key);
                    }
                    DeserializeComponentParameters(builder, node.ComponentParameters, jsonOptions, typeCache);
                    if (node.RenderModeName is { } renderModeName)
                    {
                        var prerender = node.Prerender ?? true;
                        builder.AddComponentRenderMode(renderModeName switch
                        {
                            "InteractiveServer" => prerender ? Web.RenderMode.InteractiveServer : new InteractiveServerRenderMode(prerender: false),
                            "InteractiveWebAssembly" => prerender ? Web.RenderMode.InteractiveWebAssembly : new InteractiveWebAssemblyRenderMode(prerender: false),
                            "InteractiveAuto" => prerender ? Web.RenderMode.InteractiveAuto : new InteractiveAutoRenderMode(prerender: false),
                            _ => throw new InvalidOperationException($"Unknown render mode name '{renderModeName}'."),
                        });
                    }
                    builder.CloseComponent();
                    break;

                default:
                    throw new NotImplementedException($"Deserialization for node type '{node.Type}' is not implemented.");
            }
        }
    }

    private static void DeserializeAttributes(RenderTreeBuilder builder, List<RenderTreeAttribute>? attributes, JsonSerializerOptions? jsonOptions, ComponentParametersTypeCache typeCache)
    {
        if (attributes is null)
        {
            return;
        }

        foreach (var attr in attributes)
        {
            var value = attr.Value is JsonElement je
                ? ConvertTypedValue(je, attr.TypeAssembly!, attr.TypeName!, jsonOptions, typeCache)
                : attr.Value;
            builder.AddAttribute(0, attr.Name, value);
        }
    }

    private static void DeserializeComponentParameters(RenderTreeBuilder builder, List<RenderTreeAttribute>? parameters, JsonSerializerOptions? jsonOptions, ComponentParametersTypeCache typeCache)
    {
        if (parameters is null)
        {
            return;
        }

        foreach (var param in parameters)
        {
            var value = DeserializeAttributeValue(param, jsonOptions, typeCache);
            builder.AddComponentParameter(0, param.Name, value);
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "SerializedRenderFragment and its members are preserved by existing DynamicDependency attributes.")]
    private static object? DeserializeAttributeValue(RenderTreeAttribute attr, JsonSerializerOptions? jsonOptions, ComponentParametersTypeCache typeCache)
    {
        if (attr.TypeName == SerializedRenderFragmentValueType)
        {
            var serialized = attr.Value switch
            {
                JsonElement je => JsonSerializer.Deserialize<SerializedRenderFragment>(je.GetRawText(), jsonOptions),
                SerializedRenderFragment sf => sf,
                _ => throw new InvalidOperationException($"Unexpected value type '{attr.Value?.GetType()}' for serialized RenderFragment attribute '{attr.Name}'.")
            };
            return Deserialize(serialized!.Nodes, jsonOptions!, typeCache);
        }

        return attr.Value is JsonElement json ? ConvertTypedValue(json, attr.TypeAssembly!, attr.TypeName!, jsonOptions, typeCache) : attr.Value;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Attribute and key types are primitive or well-known types preserved by the application.")]
    private static object? ConvertTypedValue(JsonElement json, string assemblyName, string typeName, JsonSerializerOptions? jsonOptions, ComponentParametersTypeCache typeCache)
    {
        var type = typeCache.GetParameterType(assemblyName, typeName) ?? throw new InvalidOperationException($"Could not resolve serialized type '{typeName}' from assembly '{assemblyName}'.");
        return json.Deserialize(type, jsonOptions);
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
        [LoggerMessage(1, LogLevel.Warning, "Event handler '{AttributeName}' inside a RenderFragment on component '{OwnerComponentType}' was skipped during serialization. Delegates cannot cross render mode boundaries.", EventName = "EventHandlerSkipped")]
        public static partial void EventHandlerSkipped(ILogger logger, string? attributeName, string? ownerComponentType);

        [LoggerMessage(2, LogLevel.Warning, "An element @ref capture inside a RenderFragment on component '{OwnerComponentType}' was skipped during serialization. Element references cannot cross render mode boundaries.", EventName = "ElementReferenceCaptureSkipped")]
        public static partial void ElementReferenceCaptureSkipped(ILogger logger, string? ownerComponentType);

        [LoggerMessage(3, LogLevel.Warning, "A component @ref capture inside a RenderFragment on component '{OwnerComponentType}' was skipped during serialization. Component references cannot cross render mode boundaries.", EventName = "ComponentReferenceCaptureSkipped")]
        public static partial void ComponentReferenceCaptureSkipped(ILogger logger, string? ownerComponentType);

        [LoggerMessage(5, LogLevel.Warning, "A @formname directive inside a RenderFragment on component '{OwnerComponentType}' was skipped during serialization. Named events are an SSR-only mechanism and cannot cross render mode boundaries.", EventName = "NamedEventSkipped")]
        public static partial void NamedEventSkipped(ILogger logger, string? ownerComponentType);

        [LoggerMessage(6, LogLevel.Warning, "A generic RenderFragment<T> parameter '{AttributeName}' inside a RenderFragment on component '{OwnerComponentType}' was skipped during serialization. Only non-generic RenderFragment is supported across render mode boundaries.", EventName = "GenericRenderFragmentSkipped")]
        public static partial void GenericRenderFragmentSkipped(ILogger logger, string? attributeName, string? ownerComponentType);
    }
}

internal sealed class SerializedRenderFragment
{
    public List<RenderTreeNode> Nodes { get; init; } = [];

    // Total captured markup length, set when the fragment is built so consumers (e.g. the in-memory
    // cache store) can size the entry without re-serializing. Not part of the wire format.
    [JsonIgnore]
    public long ContentSize { get; set; }
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
    public string? KeyTypeName { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? KeyTypeAssembly { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Content { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TypeName { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TypeAssembly { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<RenderTreeAttribute>? ComponentParameters { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<RenderTreeNode>? Children { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Sequence { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RenderModeName { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Prerender { get; set; }
}

internal sealed class RenderTreeAttribute
{
    public string Name { get; set; } = "";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Value { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TypeName { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TypeAssembly { get; set; }
}
