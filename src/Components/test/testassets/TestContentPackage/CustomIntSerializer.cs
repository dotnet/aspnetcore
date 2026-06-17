// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text;
using Microsoft.AspNetCore.Components;

namespace TestContentPackage;

/// <summary>
/// A custom serializer for int values that uses a custom format to test serialization extensibility.
/// This serializer prefixes integer values with "CUSTOM:" to clearly distinguish them from JSON serialization.
/// </summary>
public class CustomIntSerializer : PersistentComponentStateSerializer<int>
{
    public override void Persist(int value, IBufferWriter<byte> writer)
    {
        var customFormat = $"CUSTOM:{value}";
        var bytes = Encoding.UTF8.GetBytes(customFormat);
        writer.Write(bytes);
    }

    public override int Restore(ReadOnlySequence<byte> data)
    {
        var bytes = data.ToArray();
        var text = Encoding.UTF8.GetString(bytes);
        
        if (text.StartsWith("CUSTOM:", StringComparison.Ordinal) && int.TryParse(text.Substring(7), out var value))
        {
            return value;
        }
        
        // Fallback to direct parsing if format is unexpected
        return int.TryParse(text, out var fallbackValue) ? fallbackValue : 0;
    }
}