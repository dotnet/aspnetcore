// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components;

namespace NativeAotTestApp.Models;

public sealed record InteropRequest(string Name, int Count);

public sealed record InteropResponse(string Message, int Doubled);

public sealed class ResolverOrderPayload
{
    public string SomeValue { get; set; } = "";
}

public sealed class SplitEventArgs : EventArgs
{
    public string Orientation { get; set; } = "";

    public double Ratio { get; set; }
}

[JsonConverter(typeof(StorageProfileConverter))]
public sealed record StorageProfile(string Name, int Age);

public sealed class StorageProfileConverter : JsonConverter<StorageProfile>
{
    public static int ReadCount { get; private set; }

    public static int WriteCount { get; private set; }

    public override StorageProfile Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        ReadCount++;
        var parts = reader.GetString()!.Split('|');

        return new StorageProfile(parts[0], int.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture));
    }

    public override void Write(Utf8JsonWriter writer, StorageProfile value, JsonSerializerOptions options)
    {
        WriteCount++;
        writer.WriteStringValue($"{value.Name}|{value.Age}");
    }
}

public sealed record PersistenceSnapshot(string Token);

public sealed class PersistenceSnapshotSerializer : PersistentComponentStateSerializer<PersistenceSnapshot>
{
    public static int PersistCount { get; private set; }

    public static int RestoreCount { get; private set; }

    public override void Persist(PersistenceSnapshot value, IBufferWriter<byte> writer)
    {
        PersistCount++;
        writer.Write(Encoding.UTF8.GetBytes(value.Token));
    }

    public override PersistenceSnapshot Restore(ReadOnlySequence<byte> data)
    {
        RestoreCount++;

        return new PersistenceSnapshot(Encoding.UTF8.GetString(data.ToArray()));
    }
}

public sealed class DashboardInput
{
    [Required]
    public string Name { get; set; } = "";

    [Range(1, 100)]
    public int Count { get; set; }

    public bool Enabled { get; set; }

    public InputMode Mode { get; set; }

    public InputMode? OptionalMode { get; set; }

    public NestedInput Nested { get; } = new();

    public List<ListInput> Items { get; } = [new()];
}

public sealed class NestedInput
{
    public string Label { get; set; } = "";
}

public sealed class ListInput
{
    public string Value { get; set; } = "";
}

public enum InputMode
{
    Compact,
    Detailed,
}

public sealed class UploadState
{
    public string Name { get; set; } = "";

    public string Content { get; set; } = "";

    public long Length { get; set; }
}
