// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Microsoft.AspNetCore.Http;

[CompilerGenerated]
internal static partial class HttpCharacters
{
    private static class Constants
    {
        public static ReadOnlySpan<bool> LookupAuthority => new bool[] { false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, true, false, true, true, true, true, true, true, true, true, true, true, true, false, false, false, false, false, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, true, false, false, false, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false, false, false, false };
        public static ReadOnlySpan<bool> LookupToken => new bool[] { false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, false, true, true, true, true, true, false, false, true, true, false, true, true, false, true, true, true, true, true, true, true, true, true, true, false, false, false, false, false, false, false, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false, false, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, true, false, true, false };
        public static ReadOnlySpan<bool> LookupHost => new bool[] { false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, false, false, true, false, true, true, true, true, false, false, false, true, true, false, true, true, true, true, true, true, true, true, true, true, false, false, false, false, false, false, false, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false, false, false, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false, false, true, false };
        public static ReadOnlySpan<bool> LookupFieldValue => new bool[] { false, false, false, false, false, false, false, false, false, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false };
    }

    // Due to a JIT limitation we need to slice these constants (see comments above) in order
    // to "unlink" the span, and allow proper hoisting out of the loop.
    // This is tracked in https://github.com/dotnet/runtime/issues/12241

    private static partial ReadOnlySpan<bool> LookupAuthority() => Constants.LookupAuthority.Slice(0);
    private static partial ReadOnlySpan<bool> LookupToken() => Constants.LookupToken.Slice(0);
    private static partial ReadOnlySpan<bool> LookupHost() => Constants.LookupHost.Slice(0);
    private static partial ReadOnlySpan<bool> LookupFieldValue() => Constants.LookupFieldValue.Slice(0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)] private static partial Vector128<sbyte> BitMaskLookupAuthority() => Vector128.Create(0x47, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x8F, 0xAF, 0x8B, 0xAB, 0xAF).AsSByte();
    [MethodImpl(MethodImplOptions.AggressiveInlining)] private static partial Vector128<sbyte> BitMaskLookupToken() => Vector128.Create(0x17, 0x03, 0x07, 0x03, 0x03, 0x03, 0x03, 0x03, 0x07, 0x07, 0x0B, 0xAB, 0x2F, 0xAB, 0x0B, 0x8F).AsSByte();
    [MethodImpl(MethodImplOptions.AggressiveInlining)] private static partial Vector128<sbyte> BitMaskLookupHost() => Vector128.Create(0x57, 0x03, 0x07, 0x07, 0x03, 0x07, 0x03, 0x03, 0x03, 0x03, 0x0F, 0xAF, 0xAF, 0xAB, 0x2B, 0x8F).AsSByte();
    [MethodImpl(MethodImplOptions.AggressiveInlining)] private static partial Vector128<sbyte> BitMaskLookupFieldValue() => Vector128.Create(0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x02, 0x03, 0x03, 0x03, 0x03, 0x03, 0x83).AsSByte();
}
