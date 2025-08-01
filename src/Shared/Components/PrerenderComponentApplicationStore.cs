// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.Web;

namespace Microsoft.AspNetCore.Components;

#pragma warning disable CA1852 // Seal internal types
internal class PrerenderComponentApplicationStore : IPersistentComponentStateStore
#pragma warning restore CA1852 // Seal internal types
{
    private bool _stateIsPersisted;

    public PrerenderComponentApplicationStore()
    {
        ExistingState = new Dictionary<string, byte[]>();
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Simple deserialize of primitive types.")]
    public PrerenderComponentApplicationStore(string existingState)
    {
        ArgumentNullException.ThrowIfNull(existingState);

        DeserializeState(Convert.FromBase64String(existingState));
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Simple deserialize of primitive types.")]
    protected void DeserializeState(byte[] existingState)
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new ComponentsBase64ByteArrayConverter());
        var state = JsonSerializer.Deserialize<Dictionary<string, byte[]>>(existingState, options)
            ?? throw new ArgumentException("Could not deserialize state correctly", nameof(existingState));
        ExistingState = state;
    }

#nullable enable
    public string? PersistedState { get; private set; }
#nullable disable

    public Dictionary<string, byte[]> ExistingState { get; protected set; }

    public Task<IDictionary<string, byte[]>> GetPersistedStateAsync()
    {
        return Task.FromResult((IDictionary<string, byte[]>)ExistingState);
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Simple serialize of primitive types.")]
    protected virtual byte[] SerializeState(IReadOnlyDictionary<string, byte[]> state)
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new ComponentsBase64ByteArrayConverter());
        return JsonSerializer.SerializeToUtf8Bytes(state, options);
    }

    public Task PersistStateAsync(IReadOnlyDictionary<string, byte[]> state)
    {
        if (_stateIsPersisted)
        {
            throw new InvalidOperationException("State already persisted.");
        }

        _stateIsPersisted = true;

        if (state is not null && state.Count > 0)
        {
            PersistedState = ComponentsBase64Helper.ToBase64(SerializeState(state));
        }

        return Task.CompletedTask;
    }

    public virtual bool SupportsRenderMode(IComponentRenderMode renderMode) =>
        renderMode is null || renderMode is InteractiveWebAssemblyRenderMode || renderMode is InteractiveAutoRenderMode;
}

internal sealed class ComponentsBase64ByteArrayConverter : JsonConverter<byte[]>
{
    public override byte[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var base64String = reader.GetString();
        if (string.IsNullOrEmpty(base64String))
        {
            return [];
        }
        return Convert.FromBase64String(base64String);
    }

    public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options)
    {
        if (value is null || value.Length == 0)
        {
            writer.WriteStringValue(string.Empty);
        }
        else
        {
            writer.WriteStringValue(ComponentsBase64Helper.ToBase64(value));
        }
    }
}

[JsonSerializable(typeof(Dictionary<string, byte[]>), GenerationMode = JsonSourceGenerationMode.Serialization)]
internal sealed partial class PrerenderComponentApplicationStoreSerializerContext : JsonSerializerContext;
