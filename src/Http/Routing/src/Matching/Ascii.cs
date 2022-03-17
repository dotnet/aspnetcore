// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Routing.Matching;

internal static class Ascii
{
    // case-sensitive equality comparison when we KNOW that 'a' is in the ASCII range
    // and we know that the spans are the same length.
    //
    // Similar to https://github.com/dotnet/coreclr/blob/master/src/System.Private.CoreLib/shared/System/Globalization/CompareInfo.cs#L549
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool AsciiIgnoreCaseEquals(ReadOnlySpan<char> a, ReadOnlySpan<char> b, int length)
    {
        // The caller should have checked the length. We enforce that here by THROWING if the
        // lengths are unequal.
        if (a.Length < length || b.Length < length)
        {
            // This should never happen, but we don't want to have undefined
            // behavior if it does.
            ThrowArgumentExceptionForLength();
        }

        ref var charA = ref MemoryMarshal.GetReference(a);
        ref var charB = ref MemoryMarshal.GetReference(b);

        // Iterates each span for the provided length and compares each character
        // case-insensitively. This looks funky because we're using unsafe operations
        // to elide bounds-checks.
        while (length > 0 && AsciiIgnoreCaseEquals(charA, charB))
        {
            charA = ref Unsafe.Add(ref charA, 1);
            charB = ref Unsafe.Add(ref charB, 1);
            length--;
        }

        return length == 0;
    }

    // case-insensitive equality comparison for characters in the ASCII range
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool AsciiIgnoreCaseEquals(char charA, char charB)
    {
        const uint AsciiToLower = 0x20;
        return
            // Equal when chars are exactly equal
            charA == charB ||

            // Equal when converted to-lower AND they are letters
            ((charA | AsciiToLower) == (charB | AsciiToLower) && (uint)((charA | AsciiToLower) - 'a') <= (uint)('z' - 'a'));
    }

    public static bool IsAscii(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] > (char)0x7F)
            {
                return false;
            }
        }

        return true;
    }

    private static void ThrowArgumentExceptionForLength()
    {
        throw new ArgumentException("length");
    }
}
