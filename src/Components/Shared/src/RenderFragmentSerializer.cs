// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Components;

internal static partial class RenderFragmentSerializer
{
    private static ILogger _logger = NullLogger.Instance;

    internal static void SetLogger(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Components.RenderFragmentSerializer");
    }

    private const int MaxSerializationDepth = 50;

    internal static List<RenderTreeFrameDTO> Serialize(RenderFragment renderFragment, int depth = 0)
    {
        if (depth > MaxSerializationDepth)
        {
            throw new InvalidOperationException(
                $"RenderFragment serialization exceeded the maximum nesting depth of {MaxSerializationDepth}.");
        }

        using var builder = new RenderTreeBuilder();
        renderFragment(builder);
        var frames = builder.GetFrames();
        var framesSpan = frames.Array.AsSpan(0, frames.Count);
        var result = new List<RenderTreeFrameDTO>();

        const int stackAllocThreshold = 128;
        Span<(int OriginalEndIndex, RenderTreeFrameType Type)> containerStack = framesSpan.Length <= stackAllocThreshold
            ? stackalloc (int, RenderTreeFrameType)[framesSpan.Length]
            : new (int, RenderTreeFrameType)[framesSpan.Length];
        var containerCount = 0;

        for (var i = 0; i < framesSpan.Length; i++)
        {
            while (containerCount > 0 && containerStack[containerCount - 1].OriginalEndIndex <= i)
            {
                result.Add(new RenderTreeFrameDTO { Type = containerStack[--containerCount].Type, IsClosingFrame = true });
            }

            ref var frame = ref framesSpan[i];
            var dto = new RenderTreeFrameDTO
            {
                Type = frame.FrameType,
                Sequence = frame.Sequence,
            };

            switch (frame.FrameType)
            {
                case RenderTreeFrameType.Element:
                    dto.ElementName = frame.ElementName;
                    dto.ElementKey = frame.ElementKey;
                    dto.ElementKeyType = frame.ElementKey?.GetType().FullName;
                    containerStack[containerCount++] = (i + frame.ElementSubtreeLength, RenderTreeFrameType.Element);
                    break;
                case RenderTreeFrameType.Text:
                    dto.TextContent = frame.TextContent;
                    break;
                case RenderTreeFrameType.Markup:
                    dto.MarkupContent = frame.MarkupContent;
                    break;
                case RenderTreeFrameType.Attribute:
                    dto.AttributeName = frame.AttributeName;
                    if (frame.AttributeValue is RenderFragment nestedFragment)
                    {
                        dto.NestedRenderFragment = Serialize(nestedFragment, depth + 1);
                    }
                    else if (frame.AttributeValue is Delegate d && d.GetType().IsGenericType && d.GetType().GetGenericTypeDefinition() == typeof(RenderFragment<>))
                    {
                        Log.GenericRenderFragmentSkipped(_logger, frame.AttributeName);
                        continue;
                    }
                    else if (frame.AttributeValue is Delegate)
                    {
                        Log.EventHandlerSkipped(_logger, frame.AttributeName);
                        continue;
                    }
                    else
                    {
                        dto.AttributeValue = frame.AttributeValue;
                        dto.AttributeValueType = frame.AttributeValue?.GetType().FullName;
                    }
                    break;
                case RenderTreeFrameType.Component:
                    dto.ComponentType = frame.ComponentType is not null
                        ? $"{frame.ComponentType.FullName}, {frame.ComponentType.Assembly.GetName().Name}"
                        : null;
                    if (frame.ComponentKey is not null)
                    {
                        dto.ComponentKey = frame.ComponentKey;
                        dto.ComponentKeyType = frame.ComponentKey.GetType().FullName;
                    }
                    containerStack[containerCount++] = (i + frame.ComponentSubtreeLength, RenderTreeFrameType.Component);
                    break;
                case RenderTreeFrameType.Region:
                    containerStack[containerCount++] = (i + frame.RegionSubtreeLength, RenderTreeFrameType.Region);
                    break;
                case RenderTreeFrameType.ElementReferenceCapture:
                    Log.ElementReferenceCaptureSkipped(_logger);
                    continue;
                case RenderTreeFrameType.ComponentReferenceCapture:
                    Log.ComponentReferenceCaptureSkipped(_logger);
                    continue;
                case RenderTreeFrameType.ComponentRenderMode:
                    Log.ComponentRenderModeSkipped(_logger);
                    continue;
                case RenderTreeFrameType.NamedEvent:
                    Log.NamedEventSkipped(_logger);
                    continue;
                default:
                    throw new NotImplementedException($"Serialization for frame type '{frame.FrameType}' is not implemented.");
            }
            result.Add(dto);
        }

        while (containerCount > 0)
        {
            result.Add(new RenderTreeFrameDTO { Type = containerStack[--containerCount].Type, IsClosingFrame = true });
        }

        return result;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "Component types referenced in serialized RenderFragments are expected to be preserved by the application.")]
    internal static RenderFragment Deserialize(List<RenderTreeFrameDTO> frameDTOs)
    {
        return builder =>
        {
            for (var i = 0; i < frameDTOs.Count; i++)
            {
                var dto = frameDTOs[i];

                if (dto.IsClosingFrame)
                {
                    CloseContainer(builder, dto.Type);
                    continue;
                }

                switch (dto.Type)
                {
                    case RenderTreeFrameType.Element:
                        builder.OpenElement(dto.Sequence, dto.ElementName!);
                        if (dto.ElementKey is not null)
                        {
                            builder.SetKey(dto.ElementKey is JsonElement jsonElement
                                ? ConvertTypedValue(jsonElement, dto.ElementKeyType!)!
                                : dto.ElementKey);
                        }
                        break;
                    case RenderTreeFrameType.Text:
                        builder.AddContent(dto.Sequence, dto.TextContent);
                        break;
                    case RenderTreeFrameType.Markup:
                        builder.AddMarkupContent(dto.Sequence, dto.MarkupContent);
                        break;
                    case RenderTreeFrameType.Attribute:
                        if (dto.NestedRenderFragment is not null)
                        {
                            var nestedFragment = Deserialize(dto.NestedRenderFragment);
                            builder.AddAttribute(dto.Sequence, dto.AttributeName!, nestedFragment);
                        }
                        else
                        {
                            var value = dto.AttributeValue is JsonElement je
                                ? ConvertTypedValue(je, dto.AttributeValueType!)
                                : dto.AttributeValue;
                            builder.AddAttribute(dto.Sequence, dto.AttributeName!, value);
                        }
                        break;
                    case RenderTreeFrameType.Component:
                        var componentType = Type.GetType(dto.ComponentType!);
                        if (componentType is null)
                        {
                            throw new InvalidOperationException($"Cannot resolve component type '{dto.ComponentType}'.");
                        }
                        builder.OpenComponent(dto.Sequence, componentType);
                        if (dto.ComponentKey is not null)
                        {
                            builder.SetKey(dto.ComponentKey is JsonElement jsonElement
                                ? ConvertTypedValue(jsonElement, dto.ComponentKeyType!)!
                                : dto.ComponentKey);
                        }
                        break;
                    case RenderTreeFrameType.Region:
                        builder.OpenRegion(dto.Sequence);
                        break;
                    default:
                        throw new NotImplementedException($"Deserialization for frame type '{dto.Type}' is not implemented.");
                }
            }
        };
    }

    private static void CloseContainer(RenderTreeBuilder builder, RenderTreeFrameType type)
    {
        switch (type)
        {
            case RenderTreeFrameType.Element:
                builder.CloseElement();
                break;
            case RenderTreeFrameType.Component:
                builder.CloseComponent();
                break;
            case RenderTreeFrameType.Region:
                builder.CloseRegion();
                break;
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
    public List<RenderTreeFrameDTO> Frames { get; init; } = [];
}

internal sealed class RenderTreeFrameDTO
{
    public RenderTreeFrameType Type { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Sequence { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsClosingFrame { get; set; }

    // Element
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ElementName { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? ElementKey { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ElementKeyType { get; set; }

    // Text
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TextContent { get; set; }

    // Markup
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MarkupContent { get; set; }

    // Attribute
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AttributeName { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? AttributeValue { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AttributeValueType { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<RenderTreeFrameDTO>? NestedRenderFragment { get; set; }

    // Component
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ComponentType { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? ComponentKey { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ComponentKeyType { get; set; }
}
