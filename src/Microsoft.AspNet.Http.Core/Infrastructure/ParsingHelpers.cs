// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Framework.Internal;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Http.Core.Infrastructure
{
    internal struct HeaderSegment : IEquatable<HeaderSegment>
    {
        private readonly StringSegment _formatting;
        private readonly StringSegment _data;

        // <summary>
        // Initializes a new instance of the <see cref="T:System.Object"/> class.
        // </summary>
        public HeaderSegment(StringSegment formatting, StringSegment data)
        {
            _formatting = formatting;
            _data = data;
        }

        public StringSegment Formatting
        {
            get { return _formatting; }
        }

        public StringSegment Data
        {
            get { return _data; }
        }

        #region Equality members

        public bool Equals(HeaderSegment other)
        {
            return _formatting.Equals(other._formatting) && _data.Equals(other._data);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            return obj is HeaderSegment && Equals((HeaderSegment)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_formatting.GetHashCode() * 397) ^ _data.GetHashCode();
            }
        }

        public static bool operator ==(HeaderSegment left, HeaderSegment right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(HeaderSegment left, HeaderSegment right)
        {
            return !left.Equals(right);
        }

        #endregion
    }

    [System.CodeDom.Compiler.GeneratedCode("App_Packages", "")]
    internal struct HeaderSegmentCollection : IEnumerable<HeaderSegment>, IEquatable<HeaderSegmentCollection>
    {
        private readonly string[] _headers;

        public HeaderSegmentCollection(string[] headers)
        {
            _headers = headers;
        }

        #region Equality members

        public bool Equals(HeaderSegmentCollection other)
        {
            return Equals(_headers, other._headers);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            return obj is HeaderSegmentCollection && Equals((HeaderSegmentCollection)obj);
        }

        public override int GetHashCode()
        {
            return (_headers != null ? _headers.GetHashCode() : 0);
        }

        public static bool operator ==(HeaderSegmentCollection left, HeaderSegmentCollection right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(HeaderSegmentCollection left, HeaderSegmentCollection right)
        {
            return !left.Equals(right);
        }

        #endregion

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_headers);
        }

        IEnumerator<HeaderSegment> IEnumerable<HeaderSegment>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal struct Enumerator : IEnumerator<HeaderSegment>
        {
            private readonly string[] _headers;
            private int _index;

            private string _header;
            private int _headerLength;
            private int _offset;

            private int _leadingStart;
            private int _leadingEnd;
            private int _valueStart;
            private int _valueEnd;
            private int _trailingStart;

            private Mode _mode;

            private static readonly string[] NoHeaders = new string[0];

            public Enumerator(string[] headers)
            {
                _headers = headers ?? NoHeaders;
                _header = string.Empty;
                _headerLength = -1;
                _index = -1;
                _offset = -1;
                _leadingStart = -1;
                _leadingEnd = -1;
                _valueStart = -1;
                _valueEnd = -1;
                _trailingStart = -1;
                _mode = Mode.Leading;
            }

            private enum Mode
            {
                Leading,
                Value,
                ValueQuoted,
                Trailing,
                Produce,
            }

            private enum Attr
            {
                Value,
                Quote,
                Delimiter,
                Whitespace
            }

            public HeaderSegment Current
            {
                get
                {
                    return new HeaderSegment(
                        new StringSegment(_header, _leadingStart, _leadingEnd - _leadingStart),
                        new StringSegment(_header, _valueStart, _valueEnd - _valueStart));
                }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                while (true)
                {
                    if (_mode == Mode.Produce)
                    {
                        _leadingStart = _trailingStart;
                        _leadingEnd = -1;
                        _valueStart = -1;
                        _valueEnd = -1;
                        _trailingStart = -1;

                        if (_offset == _headerLength &&
                            _leadingStart != -1 &&
                            _leadingStart != _offset)
                        {
                            // Also produce trailing whitespace
                            _leadingEnd = _offset;
                            return true;
                        }
                        _mode = Mode.Leading;
                    }

                    // if end of a string
                    if (_offset == _headerLength)
                    {
                        ++_index;
                        _offset = -1;
                        _leadingStart = 0;
                        _leadingEnd = -1;
                        _valueStart = -1;
                        _valueEnd = -1;
                        _trailingStart = -1;

                        // if that was the last string
                        if (_index == _headers.Length)
                        {
                            // no more move nexts
                            return false;
                        }

                        // grab the next string
                        _header = _headers[_index] ?? string.Empty;
                        _headerLength = _header.Length;
                    }
                    while (true)
                    {
                        ++_offset;
                        char ch = _offset == _headerLength ? (char)0 : _header[_offset];
                        // todo - array of attrs
                        Attr attr = char.IsWhiteSpace(ch) ? Attr.Whitespace : ch == '\"' ? Attr.Quote : (ch == ',' || ch == (char)0) ? Attr.Delimiter : Attr.Value;

                        switch (_mode)
                        {
                            case Mode.Leading:
                                switch (attr)
                                {
                                    case Attr.Delimiter:
                                        _leadingEnd = _offset;
                                        _mode = Mode.Produce;
                                        break;
                                    case Attr.Quote:
                                        _leadingEnd = _offset;
                                        _valueStart = _offset;
                                        _mode = Mode.ValueQuoted;
                                        break;
                                    case Attr.Value:
                                        _leadingEnd = _offset;
                                        _valueStart = _offset;
                                        _mode = Mode.Value;
                                        break;
                                    case Attr.Whitespace:
                                        // more
                                        break;
                                }
                                break;
                            case Mode.Value:
                                switch (attr)
                                {
                                    case Attr.Quote:
                                        _mode = Mode.ValueQuoted;
                                        break;
                                    case Attr.Delimiter:
                                        _valueEnd = _offset;
                                        _trailingStart = _offset;
                                        _mode = Mode.Produce;
                                        break;
                                    case Attr.Value:
                                        // more
                                        break;
                                    case Attr.Whitespace:
                                        _valueEnd = _offset;
                                        _trailingStart = _offset;
                                        _mode = Mode.Trailing;
                                        break;
                                }
                                break;
                            case Mode.ValueQuoted:
                                switch (attr)
                                {
                                    case Attr.Quote:
                                        _mode = Mode.Value;
                                        break;
                                    case Attr.Delimiter:
                                        if (ch == (char)0)
                                        {
                                            _valueEnd = _offset;
                                            _trailingStart = _offset;
                                            _mode = Mode.Produce;
                                        }
                                        break;
                                    case Attr.Value:
                                    case Attr.Whitespace:
                                        // more
                                        break;
                                }
                                break;
                            case Mode.Trailing:
                                switch (attr)
                                {
                                    case Attr.Delimiter:
                                        _mode = Mode.Produce;
                                        break;
                                    case Attr.Quote:
                                        // back into value
                                        _trailingStart = -1;
                                        _valueEnd = -1;
                                        _mode = Mode.ValueQuoted;
                                        break;
                                    case Attr.Value:
                                        // back into value
                                        _trailingStart = -1;
                                        _valueEnd = -1;
                                        _mode = Mode.Value;
                                        break;
                                    case Attr.Whitespace:
                                        // more
                                        break;
                                }
                                break;
                        }
                        if (_mode == Mode.Produce)
                        {
                            return true;
                        }
                    }
                }
            }

            public void Reset()
            {
                _index = 0;
                _offset = 0;
                _leadingStart = 0;
                _leadingEnd = 0;
                _valueStart = 0;
                _valueEnd = 0;
            }
        }
    }

    [System.CodeDom.Compiler.GeneratedCode("App_Packages", "")]
    internal struct StringSegment : IEquatable<StringSegment>
    {
        private readonly string _buffer;
        private readonly int _offset;
        private readonly int _count;

        // <summary>
        // Initializes a new instance of the <see cref="T:System.Object"/> class.
        // </summary>
        public StringSegment(string buffer, int offset, int count)
        {
            _buffer = buffer;
            _offset = offset;
            _count = count;
        }

        public string Buffer
        {
            get { return _buffer; }
        }

        public int Offset
        {
            get { return _offset; }
        }

        public int Count
        {
            get { return _count; }
        }

        public string Value
        {
            get { return _offset == -1 ? null : _buffer.Substring(_offset, _count); }
        }

        public bool HasValue
        {
            get { return _offset != -1 && _count != 0 && _buffer != null; }
        }

        #region Equality members

        public bool Equals(StringSegment other)
        {
            return string.Equals(_buffer, other._buffer) && _offset == other._offset && _count == other._count;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            return obj is StringSegment && Equals((StringSegment)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (_buffer != null ? _buffer.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ _offset;
                hashCode = (hashCode * 397) ^ _count;
                return hashCode;
            }
        }

        public static bool operator ==(StringSegment left, StringSegment right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StringSegment left, StringSegment right)
        {
            return !left.Equals(right);
        }

        #endregion

        public bool StartsWith([NotNull] string text, StringComparison comparisonType)
        {
            int textLength = text.Length;
            if (!HasValue || _count < textLength)
            {
                return false;
            }

            return string.Compare(_buffer, _offset, text, 0, textLength, comparisonType) == 0;
        }

        public bool EndsWith([NotNull] string text, StringComparison comparisonType)
        {
            int textLength = text.Length;
            if (!HasValue || _count < textLength)
            {
                return false;
            }

            return string.Compare(_buffer, _offset + _count - textLength, text, 0, textLength, comparisonType) == 0;
        }

        public bool Equals([NotNull] string text, StringComparison comparisonType)
        {
            int textLength = text.Length;
            if (!HasValue || _count != textLength)
            {
                return false;
            }

            return string.Compare(_buffer, _offset, text, 0, textLength, comparisonType) == 0;
        }

        public string Substring(int offset, int length)
        {
            return _buffer.Substring(_offset + offset, length);
        }

        public StringSegment Subsegment(int offset, int length)
        {
            return new StringSegment(_buffer, _offset + offset, length);
        }

        public override string ToString()
        {
            return Value ?? string.Empty;
        }
    }

    internal static class ParsingHelpers
    {
        public static string GetHeader(IDictionary<string, string[]> headers, string key)
        {
            string[] values = GetHeaderUnmodified(headers, key);
            return values == null ? null : string.Join(",", values);
        }

        public static IEnumerable<string> GetHeaderSplit(IDictionary<string, string[]> headers, string key)
        {
            string[] values = GetHeaderUnmodified(headers, key);
            return values == null ? null : GetHeaderSplitImplementation(values);
        }

        private static IEnumerable<string> GetHeaderSplitImplementation(string[] values)
        {
            foreach (var segment in new HeaderSegmentCollection(values))
            {
                if (segment.Data.HasValue)
                {
                    yield return DeQuote(segment.Data.Value);
                }
            }
        }

        public static string[] GetHeaderUnmodified([NotNull] IDictionary<string, string[]> headers, string key)
        {
            string[] values;
            return headers.TryGetValue(key, out values) ? values : null;
        }

        public static void SetHeader([NotNull] IDictionary<string, string[]> headers, [NotNull] string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key));
            }
            if (string.IsNullOrWhiteSpace(value))
            {
                headers.Remove(key);
            }
            else
            {
                headers[key] = new[] { value };
            }
        }

        public static void SetHeaderJoined([NotNull] IDictionary<string, string[]> headers, [NotNull] string key, params string[] values)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key));
            }
            if (values == null || values.Length == 0)
            {
                headers.Remove(key);
            }
            else
            {
                headers[key] = new[] { string.Join(",", values.Select(value => QuoteIfNeeded(value))) };
            }
        }

        // Quote items that contain comas and are not already quoted.
        private static string QuoteIfNeeded(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                // Ignore
            }
            else if (value.Contains(','))
            {
                if (value[0] != '"' || value[value.Length - 1] != '"')
                {
                    value = '"' + value + '"';
                }
            }

            return value;
        }

        private static string DeQuote(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                // Ignore
            }
            else if (value.Length > 1 && value[0] == '"' && value[value.Length - 1] == '"')
            {
                value = value.Substring(1, value.Length - 2);
            }

            return value;
        }

        public static void SetHeaderUnmodified([NotNull] IDictionary<string, string[]> headers, [NotNull] string key, params string[] values)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key));
            }
            if (values == null || values.Length == 0)
            {
                headers.Remove(key);
            }
            else
            {
                headers[key] = values;
            }
        }

        public static void SetHeaderUnmodified([NotNull] IDictionary<string, string[]> headers, [NotNull] string key, [NotNull] IEnumerable<string> values)
        {
            headers[key] = values.ToArray();
        }

        public static void AppendHeader([NotNull] IDictionary<string, string[]> headers, [NotNull] string key, string values)
        {
            if (string.IsNullOrWhiteSpace(values))
            {
                return;
            }

            string existing = GetHeader(headers, key);
            if (existing == null)
            {
                SetHeader(headers, key, values);
            }
            else
            {
                headers[key] = new[] { existing + "," + values };
            }
        }

        public static void AppendHeaderJoined([NotNull] IDictionary<string, string[]> headers, [NotNull] string key, params string[] values)
        {
            if (values == null || values.Length == 0)
            {
                return;
            }

            string existing = GetHeader(headers, key);
            if (existing == null)
            {
                SetHeaderJoined(headers, key, values);
            }
            else
            {
                headers[key] = new[] { existing + "," + string.Join(",", values.Select(value => QuoteIfNeeded(value))) };
            }
        }

        public static void AppendHeaderUnmodified([NotNull] IDictionary<string, string[]> headers, [NotNull] string key, params string[] values)
        {
            if (values == null || values.Length == 0)
            {
                return;
            }

            string[] existing = GetHeaderUnmodified(headers, key);
            if (existing == null)
            {
                SetHeaderUnmodified(headers, key, values);
            }
            else
            {
                SetHeaderUnmodified(headers, key, existing.Concat(values));
            }
        }

        public static long? GetContentLength([NotNull] IHeaderDictionary headers)
        {
            const NumberStyles styles = NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite;
            long value;
            string rawValue = headers.Get(HeaderNames.ContentLength);
            if (!string.IsNullOrWhiteSpace(rawValue) &&
                long.TryParse(rawValue, styles, CultureInfo.InvariantCulture, out value))
            {
                return value;
            }

            return null;
        }

        public static void SetContentLength([NotNull] IHeaderDictionary headers, long? value)
        {
            if (value.HasValue)
            {
                headers[HeaderNames.ContentLength] = value.Value.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                headers.Remove(HeaderNames.ContentLength);
            }
        }
    }
}
