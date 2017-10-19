// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;

namespace Microsoft.AspNetCore.Dispatcher
{
    [DebuggerDisplay("{DebuggerToString(),nq}")]
    internal class UriBuildingContext
    {
        // Holds the 'accepted' parts of the uri.
        private readonly StringBuilder _uri;

        // Holds the 'optional' parts of the uri. We need a secondary buffer to handle cases where an optional
        // segment is in the middle of the uri. We don't know if we need to write it out - if it's
        // followed by other optional segments than we will just throw it away.
        private readonly List<BufferValue> _buffer;

        private bool _hasEmptySegment;
        private int _lastValueOffset;

        public UriBuildingContext()
        {
            _uri = new StringBuilder();
            _buffer = new List<BufferValue>();
            Writer = new StringWriter(_uri);
            _lastValueOffset = -1;

            BufferState = SegmentState.Beginning;
            UriState = SegmentState.Beginning;
        }

        public SegmentState BufferState { get; private set; }

        public SegmentState UriState { get; private set; }

        public TextWriter Writer { get; }

        public bool Accept(UrlEncoder encoder, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                if (UriState == SegmentState.Inside || BufferState == SegmentState.Inside)
                {
                    // We can't write an 'empty' part inside a segment
                    return false;
                }
                else
                {
                    _hasEmptySegment = true;
                    return true;
                }
            }
            else if (_hasEmptySegment)
            {
                // We're trying to write text after an empty segment - this is not allowed.
                return false;
            }

            for (var i = 0; i < _buffer.Count; i++)
            {
                if (_buffer[i].RequiresEncoding)
                {
                    encoder.Encode(Writer, _buffer[i].Value);
                }
                else
                {
                    _uri.Append(_buffer[i].Value);
                }
            }
            _buffer.Clear();

            if (UriState == SegmentState.Beginning && BufferState == SegmentState.Beginning)
            {
                if (_uri.Length != 0)
                {
                    _uri.Append("/");
                }
            }

            BufferState = SegmentState.Inside;
            UriState = SegmentState.Inside;

            _lastValueOffset = _uri.Length;
            // Allow the first segment to have a leading slash.
            // This prevents the leading slash from PathString segments from being encoded.
            if (_uri.Length == 0 && value.Length > 0 && value[0] == '/')
            {
                _uri.Append("/");
                encoder.Encode(Writer, value, 1, value.Length - 1);
            }
            else
            {
                encoder.Encode(Writer, value);
            }

            return true;
        }

        public void Remove(string literal)
        {
            Debug.Assert(_lastValueOffset != -1, "Cannot invoke Remove more than once.");
            _uri.Length = _lastValueOffset;
            _lastValueOffset = -1;
        }

        public bool Buffer(UrlEncoder encoder, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                if (BufferState == SegmentState.Inside)
                {
                    // We can't write an 'empty' part inside a segment
                    return false;
                }
                else
                {
                    _hasEmptySegment = true;
                    return true;
                }
            }
            else if (_hasEmptySegment)
            {
                // We're trying to write text after an empty segment - this is not allowed.
                return false;
            }

            if (UriState == SegmentState.Inside)
            {
                // We've already written part of this segment so there's no point in buffering, we need to
                // write out the rest or give up.
                var result = Accept(encoder, value);

                // We've already checked the conditions that could result in a rejected part, so this should
                // always be true.
                Debug.Assert(result);

                return result;
            }

            if (UriState == SegmentState.Beginning && BufferState == SegmentState.Beginning)
            {
                if (_uri.Length != 0 || _buffer.Count != 0)
                {
                    _buffer.Add(new BufferValue("/", requiresEncoding: false));
                }

                BufferState = SegmentState.Inside;
            }

            _buffer.Add(new BufferValue(value, requiresEncoding: true));
            return true;
        }

        public void EndSegment()
        {
            BufferState = SegmentState.Beginning;
            UriState = SegmentState.Beginning;
        }

        public void Clear()
        {
            _uri.Clear();
            if (_uri.Capacity > 128)
            {
                // We don't want to retain too much memory if this is getting pooled.
                _uri.Capacity = 128;
            }

            _buffer.Clear();
            if (_buffer.Capacity > 8)
            {
                _buffer.Capacity = 8;
            }

            _hasEmptySegment = false;
            _lastValueOffset = -1;
            BufferState = SegmentState.Beginning;
            UriState = SegmentState.Beginning;
        }

        public override string ToString()
        {
            // We can ignore any currently buffered segments - they are are guaranteed to be 'defaults'.
            if (_uri.Length > 0 && _uri[0] != '/')
            {
                // Normalize generated paths so that they always contain a leading slash.
                _uri.Insert(0, '/');
            }

            return _uri.ToString();
        }

        private string DebuggerToString()
        {
            return string.Format("{{Accepted: '{0}' Buffered: '{1}'}}", _uri, string.Join("", _buffer));
        }
    }
}
