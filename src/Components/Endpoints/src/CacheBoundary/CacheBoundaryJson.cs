// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.Web;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed partial class CacheBoundaryJson
{
    private readonly List<CacheSegment> _segments = [];

    public int Count => _segments.Count;

    public void AddHtml(string html)
    {
        ArgumentNullException.ThrowIfNull(html);
        _segments.Add(CacheSegment.CreateHtml(html));
    }

    public void AddHole(Type componentType, int sequence = 0, string? renderModeName = null, object? componentKey = null)
    {
        ArgumentNullException.ThrowIfNull(componentType);
        _segments.Add(CacheSegment.CreateHole(componentType, sequence, renderModeName, componentKey));
    }

    public List<CacheSegment>.Enumerator GetEnumerator() => _segments.GetEnumerator();

    public string Serialize()
    {
        var entries = new JsonCacheSegment[_segments.Count];
        for (var i = 0; i < _segments.Count; i++)
        {
            var segment = _segments[i];
            entries[i] = segment.Kind switch
            {
                CacheSegmentKind.Html => new JsonCacheSegment { Type = "html", Content = segment.Html },
                CacheSegmentKind.Hole => new JsonCacheSegment
                {
                    Type = "hole",
                    Content = segment.ComponentType!.AssemblyQualifiedName,
                    RenderMode = segment.RenderModeName,
                    Sequence = segment.Sequence,
                    Key = SerializeKey(segment.ComponentKey),
                    KeyType = segment.ComponentKey?.GetType().FullName,
                },
                _ => throw new InvalidOperationException($"Unknown segment kind: {segment.Kind}"),
            };
        }
        return JsonSerializer.Serialize(entries, CacheJsonContext.Default.JsonCacheSegmentArray);
    }

    public static CacheBoundaryJson Deserialize(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        var entries = JsonSerializer.Deserialize(json, CacheJsonContext.Default.JsonCacheSegmentArray)
            ?? throw new InvalidOperationException("Failed to deserialize cache entry.");

        var result = new CacheBoundaryJson();
        foreach (var entry in entries)
        {
            switch (entry.Type)
            {
                case "html":
                    result.AddHtml(entry.Content ?? string.Empty);
                    break;
                case "hole":
                    var type = Type.GetType(entry.Content ?? throw new InvalidOperationException("Hole segment missing component type."))
                        ?? throw new InvalidOperationException($"Could not resolve hole component type: '{entry.Content}'.");
                    if (!typeof(IComponent).IsAssignableFrom(type))
                    {
                        throw new InvalidOperationException($"Resolved type '{type.FullName}' is not a valid component type.");
                    }
                    result.AddHole(type, entry.Sequence, entry.RenderMode, DeserializeKey(entry.Key, entry.KeyType));
                    break;
                default:
                    throw new InvalidOperationException($"Unknown cache segment type: '{entry.Type}'.");
            }
        }

        return result;
    }

    private static string? SerializeKey(object? key)
    {
        if (key is null)
        {
            return null;
        }
        return JsonSerializer.Serialize(key);
    }

    private static object? DeserializeKey(string? keyValue, string? keyType)
    {
        if (keyValue is null || keyType is null)
        {
            return null;
        }
        var type = Type.GetType(keyType) ?? throw new InvalidOperationException($"Could not resolve key type: '{keyType}'.");
        return JsonSerializer.Deserialize(keyValue, type);
    }

    internal sealed class JsonCacheSegment
    {
        public string Type { get; set; } = "html";

        public string? Content { get; set; }

        public string? RenderMode { get; set; }

        public int Sequence { get; set; }

        public string? Key { get; set; }

        public string? KeyType { get; set; }
    }

    [JsonSerializable(typeof(JsonCacheSegment[]))]
    internal sealed partial class CacheJsonContext : JsonSerializerContext
    {
    }
}

internal readonly struct CacheSegment
{
    public CacheSegmentKind Kind { get; }
    public string? Html { get; }
    public Type? ComponentType { get; }
    public int Sequence { get; }
    public string? RenderModeName { get; }
    public object? ComponentKey { get; }

    private CacheSegment(CacheSegmentKind kind, string? html, Type? componentType, int sequence = 0, string? renderModeName = null, object? componentKey = null)
    {
        Kind = kind;
        Html = html;
        ComponentType = componentType;
        Sequence = sequence;
        RenderModeName = renderModeName;
        ComponentKey = componentKey;
    }

    public static CacheSegment CreateHtml(string html) => new(CacheSegmentKind.Html, html, componentType: null);
    public static CacheSegment CreateHole(Type componentType, int sequence = 0, string? renderModeName = null, object? componentKey = null)
        => new(CacheSegmentKind.Hole, html: null, componentType, sequence, renderModeName, componentKey);

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
}

internal enum CacheSegmentKind
{
    Html,
    Hole,
}
