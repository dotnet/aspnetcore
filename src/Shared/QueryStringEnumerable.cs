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
            public readonly ReadOnlySpan<char> EncodedName;
            public readonly ReadOnlySpan<char> EncodedValue;

            public EncodedNameValuePair(ReadOnlySpan<char> encodedName, ReadOnlySpan<char> encodedValue)
            {
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
            private ReadOnlySpan<char> _query;

            public Enumerator(ReadOnlySpan<char> query)
            {
                Current = default;
                _query = query.IsEmpty || query[0] != '?'
                    ? query
                    : query.Slice(1);
            }

            public EncodedNameValuePair Current { get; private set; }

            public bool MoveNext()
            {
                while (!_query.IsEmpty)
                {
                    // Chomp off the next segment
                    ReadOnlySpan<char> segment;
                    var delimiterIndex = _query.IndexOf('&');
                    if (delimiterIndex >= 0)
                    {
                        segment = _query.Slice(0, delimiterIndex);
                        _query = _query.Slice(delimiterIndex + 1);
                    }
                    else
                    {
                        segment = _query;
                        _query = default;
                    }

                    // If it's nonempty, emit it
                    var equalIndex = segment.IndexOf('=');
                    if (equalIndex >= 0)
                    {
                        Current = new EncodedNameValuePair(
                            segment.Slice(0, equalIndex),
                            segment.Slice(equalIndex + 1));
                        return true;
                    }
                    else if (!segment.IsEmpty)
                    {
                        Current = new EncodedNameValuePair(segment, default);
                        return true;
                    }
                }

                Current = default;
                return false;
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
                        var vecPlus = Vector128.Create('+');
                        var vecSpace = Vector128.Create(' ');

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
