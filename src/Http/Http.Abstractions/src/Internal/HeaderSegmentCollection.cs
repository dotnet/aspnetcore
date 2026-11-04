// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Http;

internal readonly struct HeaderSegmentCollection : IEnumerable<HeaderSegment>, IEquatable<HeaderSegmentCollection>
{
    private readonly StringValues _headers;

    public HeaderSegmentCollection(StringValues headers)
    {
        _headers = headers;
    }

    public bool Equals(HeaderSegmentCollection other)
    {
        return StringValues.Equals(_headers, other._headers);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        return obj is HeaderSegmentCollection collection && Equals(collection);
    }

    public override int GetHashCode()
    {
        return (!StringValues.IsNullOrEmpty(_headers) ? _headers.GetHashCode() : 0);
    }

    public static bool operator ==(HeaderSegmentCollection left, HeaderSegmentCollection right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(HeaderSegmentCollection left, HeaderSegmentCollection right)
    {
        return !left.Equals(right);
    }

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

    public struct Enumerator : IEnumerator<HeaderSegment>
    {
        private readonly StringValues _headers;
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

        public Enumerator(StringValues headers)
        {
            _headers = headers;
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
                    if (_index == _headers.Count)
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
                                    _valueStart = _valueStart == -1 ? _offset : _valueStart;
                                    _valueEnd = _valueEnd == -1 ? _offset : _valueEnd;
                                    _trailingStart = _trailingStart == -1 ? _offset : _trailingStart;
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
                                    if (ch == (char)0)
                                    {
                                        _valueEnd = _offset;
                                        _trailingStart = _offset;
                                    }
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
