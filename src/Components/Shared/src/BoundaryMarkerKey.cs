// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

internal readonly struct BoundaryMarkerKey(
    ReadOnlyMemory<char> componentTypeNameHash,
    ReadOnlyMemory<char> sequenceString,
    ReadOnlyMemory<char> formattedComponentKey)
{
    private const char Separator = ':';

    public bool HasComponentKey => !formattedComponentKey.IsEmpty;

    public static bool TryParse(ReadOnlyMemory<char> value, out BoundaryMarkerKey result)
    {
        Span<Range> ranges = stackalloc Range[4];
        if (value.Span.Split(ranges, Separator) != 3)
        {
            result = default;
            return false;
        }

        result = new(value[ranges[0]], value[ranges[1]], value[ranges[2]]);
        return true;
    }

    public override string ToString()
    {
        var separatorCount = 2;
        var length = componentTypeNameHash.Length + sequenceString.Length + formattedComponentKey.Length + separatorCount;
        return string.Create(length, (componentTypeNameHash, sequenceString, formattedComponentKey), static (destination, state) =>
        {
            WritePart(ref destination, state.componentTypeNameHash);
            WriteSeparator(ref destination);
            WritePart(ref destination, state.sequenceString);
            WriteSeparator(ref destination);
            WritePart(ref destination, state.formattedComponentKey);

            static void WritePart(ref Span<char> destination, ReadOnlyMemory<char> part)
            {
                part.Span.CopyTo(destination);
                destination = destination[part.Length..];
            }

            static void WriteSeparator(ref Span<char> destination)
            {
                destination[0] = Separator;
                destination = destination[1..];
            }
        });
    }
}
