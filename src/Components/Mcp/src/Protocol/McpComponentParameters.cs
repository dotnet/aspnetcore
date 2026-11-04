// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.AspNetCore.Components.Mcp.Protocol;

internal sealed class McpComponentParameters
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly Entry[] _entries;

    private McpComponentParameters(Entry[] entries) => _entries = entries;

    public static McpComponentParameters Empty { get; } = new([]);

    public static McpComponentParameters Capture(IReadOnlyDictionary<string, object?> parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        var entries = new List<Entry>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, value) in parameters)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            if (!names.Add(name))
            {
                throw new ArgumentException($"The component parameter '{name}' is supplied more than once.", nameof(parameters));
            }

            if (value is IComponent or Delegate or IServiceProvider)
            {
                throw new ArgumentException($"The component parameter '{name}' must contain serializable data, not a component, callback, or service provider.", nameof(parameters));
            }

            var type = value?.GetType();
            entries.Add(new Entry(name, type, type is null ? default : JsonSerializer.SerializeToElement(value, type, JsonOptions)));
        }

        return new(entries.ToArray());
    }

    public Dictionary<string, object?> Restore()
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in _entries)
        {
            result.Add(entry.Name, entry.Type is null ? null : entry.Value.Deserialize(entry.Type, JsonOptions));
        }

        return result;
    }

    private sealed record Entry(string Name, Type? Type, JsonElement Value);
}
