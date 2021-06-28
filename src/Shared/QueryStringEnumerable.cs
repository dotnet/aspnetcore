// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Microsoft.AspNetCore.Internal
{
    // A mechanism for reading key/value pairs from a querystring without having to allocate.
    // It doesn't perform escaping because:
    // [1] Uri.UnescapeDataString can only operate on string, not on ReadOnlySpan<char>
    // [2] Maybe the caller doesn't even want to pay the cost of unescaping values they don't care about
    // So, it's up to the caller to unescape the results if they want.
    internal readonly ref struct QueryStringEnumerable
    {
        private readonly ReadOnlySpan<char> _queryString;

        public QueryStringEnumerable(ReadOnlySpan<char> queryString)
        {
            _queryString = queryString;
        }

        public Enumerator GetEnumerator()
            => new Enumerator(_queryString);

        public readonly ref struct EncodedNameValuePair
        {
            public readonly bool HasValue;
            public readonly ReadOnlySpan<char> EncodedName;
            public readonly ReadOnlySpan<char> EncodedValue;

            public EncodedNameValuePair(ReadOnlySpan<char> encodedName, ReadOnlySpan<char> encodedValue)
            {
                HasValue = true;
                EncodedName = encodedName;
                EncodedValue = encodedValue;
            }

            public ReadOnlySpan<char> DecodeName()
            {
                return EncodedName.IsEmpty
                    ? default
                    : Uri.UnescapeDataString(SpanHelper.ReplacePlusWithSpace(EncodedName));
            }

            public ReadOnlySpan<char> DecodeValue()
            {
                return EncodedValue.IsEmpty
                    ? default
                    : Uri.UnescapeDataString(SpanHelper.ReplacePlusWithSpace(EncodedValue));
            }
        }

        public ref struct Enumerator
        {
            private readonly ReadOnlySpan<char> queryString;
            private readonly int textLength;
            private int scanIndex;
            private int equalIndex;

            public Enumerator(ReadOnlySpan<char> query)
            {
                if (query.IsEmpty)
                {
                    this = default;
                }
                else
                {
                    Current = default;
                    queryString = query;
                    scanIndex = queryString[0] == '?' ? 1 : 0;
                    textLength = queryString.Length;
                    equalIndex = queryString.IndexOf('=');
                    if (equalIndex == -1)
                    {
                        equalIndex = textLength;
                    }
                }
            }

            public EncodedNameValuePair Current { get; private set; }

            public bool MoveNext()
            {
                Current = default;

                if (scanIndex < textLength)
                {
                    var delimiterIndex = queryString.Slice(scanIndex).IndexOf('&') + scanIndex;
                    if (delimiterIndex < scanIndex)
                    {
                        delimiterIndex = textLength;
                    }

                    if (equalIndex < delimiterIndex)
                    {
                        while (scanIndex != equalIndex && char.IsWhiteSpace(queryString[scanIndex]))
                        {
                            ++scanIndex;
                        }

                        Current = new EncodedNameValuePair(
                            queryString.Slice(scanIndex, equalIndex - scanIndex),
                            queryString.Slice(equalIndex + 1, delimiterIndex - equalIndex - 1));

                        equalIndex = queryString.Slice(delimiterIndex).IndexOf('=') + delimiterIndex;
                        if (equalIndex < delimiterIndex)
                        {
                            equalIndex = textLength;
                        }
                    }
                    else
                    {
                        if (delimiterIndex > scanIndex)
                        {
                            Current = new EncodedNameValuePair(
                                queryString.Slice(scanIndex, delimiterIndex - scanIndex),
                                ReadOnlySpan<char>.Empty);
                        }
                    }

                    scanIndex = delimiterIndex + 1;
                }

                return Current.HasValue;
            }
        }

        private static class SpanHelper
        {
            private static readonly SpanAction<char, IntPtr> s_replacePlusWithSpace = ReplacePlusWithSpaceCore;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static unsafe string ReplacePlusWithSpace(ReadOnlySpan<char> span)
            {
                fixed (char* ptr = &MemoryMarshal.GetReference(span))
                {
                    return string.Create(span.Length, (IntPtr)ptr, s_replacePlusWithSpace);
                }
            }

            private static unsafe void ReplacePlusWithSpaceCore(Span<char> buffer, IntPtr state)
            {
                fixed (char* ptr = &MemoryMarshal.GetReference(buffer))
                {
                    var input = (ushort*)state.ToPointer();
                    var output = (ushort*)ptr;

                    var i = (nint)0;
                    var n = (nint)(uint)buffer.Length;

                    if (Sse41.IsSupported && n >= Vector128<ushort>.Count)
                    {
                        var vecPlus = Vector128.Create((ushort)'+');
                        var vecSpace = Vector128.Create((ushort)' ');

                        do
                        {
                            var vec = Sse2.LoadVector128(input + i);
                            var mask = Sse2.CompareEqual(vec, vecPlus);
                            var res = Sse41.BlendVariable(vec, vecSpace, mask);
                            Sse2.Store(output + i, res);
                            i += Vector128<ushort>.Count;
                        } while (i <= n - Vector128<ushort>.Count);
                    }

                    for (; i < n; ++i)
                    {
                        if (input[i] != '+')
                        {
                            output[i] = input[i];
                        }
                        else
                        {
                            output[i] = ' ';
                        }
                    }
                }
            }
        }
    }
}
