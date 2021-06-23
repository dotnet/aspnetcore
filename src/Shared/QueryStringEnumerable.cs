// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

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

        public readonly ref struct EscapedNameValuePair
        {
            public readonly bool HasValue;
            public readonly ReadOnlySpan<char> NameEscaped;
            public readonly ReadOnlySpan<char> ValueEscaped;

            public EscapedNameValuePair(ReadOnlySpan<char> nameEscaped, ReadOnlySpan<char> valueEscaped)
            {
                HasValue = true;
                NameEscaped = nameEscaped;
                ValueEscaped = valueEscaped;
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
                    queryString = string.Empty;
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

            public EscapedNameValuePair Current { get; private set; }

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

                        Current = new EscapedNameValuePair(
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
                            Current = new EscapedNameValuePair(
                                queryString.Slice(scanIndex, delimiterIndex - scanIndex),
                                ReadOnlySpan<char>.Empty);
                        }
                    }

                    scanIndex = delimiterIndex + 1;
                }

                return Current.HasValue;
            }
        }
    }
}
