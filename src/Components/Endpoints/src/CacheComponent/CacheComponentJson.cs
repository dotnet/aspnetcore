// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.Web;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed partial class CacheComponentJson
{
    private readonly List<CacheSegment> _segments = [];

    public int Count => _segments.Count;

    public void AddHtml(string html)
    {
        ArgumentNullException.ThrowIfNull(html);
        _segments.Add(CacheSegment.CreateHtml(html));
    }

    public void AddHole(Type componentType, string? renderModeName = null, string? componentKey = null)
    {
        ArgumentNullException.ThrowIfNull(componentType);
        _segments.Add(CacheSegment.CreateHole(componentType, renderModeName, componentKey));
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
                    Key = segment.ComponentKey,
                },
                _ => throw new InvalidOperationException($"Unknown segment kind: {segment.Kind}"),
            };
        }

        return JsonSerializer.Serialize(entries, CacheJsonContext.Default.JsonCacheSegmentArray);
    }

    public static CacheComponentJson Deserialize(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        var entries = JsonSerializer.Deserialize(json, CacheJsonContext.Default.JsonCacheSegmentArray)
            ?? throw new InvalidOperationException("Failed to deserialize cache entry.");

        var result = new CacheComponentJson();
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
                    result.AddHole(type, entry.RenderMode, entry.Key);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown cache segment type: '{entry.Type}'.");
            }
        }

        return result;
    }

    internal sealed class JsonCacheSegment
    {
        public string Type { get; set; } = "html";

        public string? Content { get; set; }

        public string? RenderMode { get; set; }

        public string? Key { get; set; }
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
    public string? RenderModeName { get; }
    public string? ComponentKey { get; }

    private CacheSegment(CacheSegmentKind kind, string? html, Type? componentType, string? renderModeName = null, string? componentKey = null)
    {
        Kind = kind;
        Html = html;
        ComponentType = componentType;
        RenderModeName = renderModeName;
        ComponentKey = componentKey;
    }

    public static CacheSegment CreateHtml(string html) => new(CacheSegmentKind.Html, html, componentType: null);
    public static CacheSegment CreateHole(Type componentType, string? renderModeName = null, string? componentKey = null)
        => new(CacheSegmentKind.Hole, html: null, componentType, renderModeName, componentKey);

    /// <summary>
    /// Reconstructs the <see cref="IComponentRenderMode"/> from the serialized name, or returns <c>null</c> if no render mode was cached.
    /// </summary>
    public IComponentRenderMode? ReconstructRenderMode()
    {
        return RenderModeName switch
        {
            null => null,
            "InteractiveServer" => RenderMode.InteractiveServer,
            "InteractiveWebAssembly" => RenderMode.InteractiveWebAssembly,
            "InteractiveAuto" => RenderMode.InteractiveAuto,
            _ => throw new InvalidOperationException($"Unknown cached render mode: '{RenderModeName}'."),
        };
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
}

internal enum CacheSegmentKind
{
    Html,
    Hole,
}
