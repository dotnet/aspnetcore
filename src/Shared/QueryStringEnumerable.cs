// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if NET
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif

#if QueryStringEnumerable_In_WebUtilities
namespace Microsoft.AspNetCore.WebUtilities;
#else
namespace Microsoft.AspNetCore.Internal;
#endif

/// <summary>
/// An enumerable that can supply the name/value pairs from a URI query string.
/// </summary>
#if QueryStringEnumerable_In_WebUtilities
public
#else
internal
#endif
    readonly struct QueryStringEnumerable
{
    private readonly ReadOnlyMemory<char> _queryString;

    /// <summary>
    /// Constructs an instance of <see cref="QueryStringEnumerable"/>.
    /// </summary>
    /// <param name="queryString">The query string.</param>
    public QueryStringEnumerable(string? queryString)
        : this(queryString.AsMemory())
    {
    }

    /// <summary>
    /// Constructs an instance of <see cref="QueryStringEnumerable"/>.
    /// </summary>
    /// <param name="queryString">The query string.</param>
    public QueryStringEnumerable(ReadOnlyMemory<char> queryString)
    {
        _queryString = queryString;
    }

    /// <summary>
    /// Retrieves an object that can iterate through the name/value pairs in the query string.
    /// </summary>
    /// <returns>An object that can iterate through the name/value pairs in the query string.</returns>
    public Enumerator GetEnumerator()
        => new Enumerator(_queryString);

    /// <summary>
    /// Represents a single name/value pair extracted from a query string during enumeration.
    /// </summary>
    public readonly struct EncodedNameValuePair
    {
        /// <summary>
        /// Gets the name from this name/value pair in its original encoded form.
        /// To get the decoded string, call <see cref="DecodeName"/>.
        /// </summary>
        public readonly ReadOnlyMemory<char> EncodedName { get; }

        /// <summary>
        /// Gets the value from this name/value pair in its original encoded form.
        /// To get the decoded string, call <see cref="DecodeValue"/>.
        /// </summary>
        public readonly ReadOnlyMemory<char> EncodedValue { get; }

        internal EncodedNameValuePair(ReadOnlyMemory<char> encodedName, ReadOnlyMemory<char> encodedValue)
        {
            EncodedName = encodedName;
            EncodedValue = encodedValue;
        }

        /// <summary>
        /// Decodes the name from this name/value pair.
        /// </summary>
        /// <returns>Characters representing the decoded name.</returns>
        public ReadOnlyMemory<char> DecodeName()
            => Decode(EncodedName);

        /// <summary>
        /// Decodes the value from this name/value pair.
        /// </summary>
        /// <returns>Characters representing the decoded value.</returns>
        public ReadOnlyMemory<char> DecodeValue()
            => Decode(EncodedValue);

        /// <summary>
        /// Decodes the name from this name/value pair directly into a string.
        /// </summary>
        internal string DecodeNameToString()
            => DecodeToString(EncodedName);

        /// <summary>
        /// Decodes the value from this name/value pair directly into a string.
        /// </summary>
        internal string DecodeValueToString()
            => DecodeToString(EncodedValue);

        private static string DecodeToString(ReadOnlyMemory<char> chars)
        {
#if NET
            ReadOnlySpan<char> source = chars.Span;
            if (!source.ContainsAny('%', '+'))
            {
                return chars.ToString();
            }

            return DecodeToStringSlow(source);
#else
            return Decode(chars).ToString();
#endif
        }

#if NET
        // Kept separate so the stack buffer doesn't penalize the common no-decoding path.
        private static string DecodeToStringSlow(ReadOnlySpan<char> source)
        {
            // Decode into a temporary buffer to avoid allocating an intermediate char[] in addition to the string.
            const int StackAllocThreshold = 256;
            char[]? rented = null;
            Span<char> buffer = source.Length <= StackAllocThreshold
                ? stackalloc char[StackAllocThreshold]
                : (rented = ArrayPool<char>.Shared.Rent(source.Length));
            buffer = buffer.Slice(0, source.Length);

            source.Replace(buffer, '+', ' ');
            var success = Uri.TryUnescapeDataString(buffer, buffer, out var unescapedLength);
            Debug.Assert(success);
            var result = new string(buffer.Slice(0, unescapedLength));

            if (rented is not null)
            {
                ArrayPool<char>.Shared.Return(rented);
            }

            return result;
        }
#endif

        private static ReadOnlyMemory<char> Decode(ReadOnlyMemory<char> chars)
        {
            ReadOnlySpan<char> source = chars.Span;
#if NET
            if (!source.ContainsAny('%', '+'))
            {
                return chars;
            }
            var buffer = new char[source.Length];
            source.Replace(buffer, '+', ' ');
            var success = Uri.TryUnescapeDataString(buffer, buffer, out var unescapedLength);
            Debug.Assert(success);
            return buffer.AsMemory(0, unescapedLength);
#else
            if (source.IndexOf('%') < 0 && source.IndexOf('+') < 0)
            {
                return chars;
            }
            return Uri.UnescapeDataString(chars.ToString().Replace('+', ' ')).AsMemory();
#endif
        }
    }

    /// <summary>
    /// An enumerator that supplies the name/value pairs from a URI query string.
    /// </summary>
    public struct Enumerator
    {
        private ReadOnlyMemory<char> _query;

        internal Enumerator(ReadOnlyMemory<char> query)
        {
            Current = default;
            _query = query.IsEmpty || query.Span[0] != '?'
                ? query
                : query.Slice(1);
        }

        /// <summary>
        /// Gets the currently referenced key/value pair in the query string being enumerated.
        /// </summary>
        public EncodedNameValuePair Current { get; private set; }

        /// <summary>
        /// Moves to the next key/value pair in the query string being enumerated.
        /// </summary>
        /// <returns>True if there is another key/value pair, otherwise false.</returns>
        public bool MoveNext()
        {
            while (!_query.IsEmpty)
            {
                // Chomp off the next segment
                ReadOnlyMemory<char> segment;
                var delimiterIndex = _query.Span.IndexOf('&');
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
                var equalIndex = segment.Span.IndexOf('=');
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
}
