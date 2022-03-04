// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing
{
    [DebuggerDisplay("{DebuggerToString(),nq}")]
    internal class UriBuildingContext
    {
        // Holds the 'accepted' parts of the path.
        private readonly StringBuilder _path;
        private StringBuilder _query;

        // Holds the 'optional' parts of the path. We need a secondary buffer to handle cases where an optional
        // segment is in the middle of the uri. We don't know if we need to write it out - if it's
        // followed by other optional segments than we will just throw it away.
        private readonly List<BufferValue> _buffer;
        private readonly UrlEncoder _urlEncoder;

        private bool _hasEmptySegment;
        private int _lastValueOffset;

        public UriBuildingContext(UrlEncoder urlEncoder)
        {
            _urlEncoder = urlEncoder;
            _path = new StringBuilder();
            _query = new StringBuilder();
            _buffer = new List<BufferValue>();
            PathWriter = new StringWriter(_path);
            QueryWriter = new StringWriter(_query);
            _lastValueOffset = -1;

            BufferState = SegmentState.Beginning;
            UriState = SegmentState.Beginning;
        }
        
        public bool LowercaseUrls { get; set; }

        public bool LowercaseQueryStrings { get; set; }

        public bool AppendTrailingSlash { get; set; }

        public SegmentState BufferState { get; private set; }

        public SegmentState UriState { get; private set; }

        public TextWriter PathWriter { get; }

        public TextWriter QueryWriter { get; }

        public bool Accept(string value)
        {
            return Accept(value, encodeSlashes: true);
        }

        public bool Accept(string value, bool encodeSlashes)
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

            // NOTE: this needs to be above all 'EncodeValue' and _path.Append calls
            if (LowercaseUrls)
            {
                value = value.ToLowerInvariant();
            }

            var buffer = _buffer;
            for (var i = 0; i < buffer.Count; i++)
            {
                var bufferValue = buffer[i].Value;
                if (LowercaseUrls)
                {
                    bufferValue = bufferValue.ToLowerInvariant();
                }

                if (buffer[i].RequiresEncoding)
                {
                    EncodeValue(bufferValue);
                }
                else
                {
                    _path.Append(bufferValue);
                }
            }
            buffer.Clear();

            if (UriState == SegmentState.Beginning && BufferState == SegmentState.Beginning)
            {
                if (_path.Length != 0)
                {
                    _path.Append("/");
                }
            }

            BufferState = SegmentState.Inside;
            UriState = SegmentState.Inside;

            _lastValueOffset = _path.Length;

            // Allow the first segment to have a leading slash.
            // This prevents the leading slash from PathString segments from being encoded.
            if (_path.Length == 0 && value.Length > 0 && value[0] == '/')
            {
                _path.Append("/");
                EncodeValue(value, 1, value.Length - 1, encodeSlashes);
            }
            else
            {
                EncodeValue(value, encodeSlashes);
            }

            return true;
        }

        public void Remove(string literal)
        {
            Debug.Assert(_lastValueOffset != -1, "Cannot invoke Remove more than once.");
            _path.Length = _lastValueOffset;
            _lastValueOffset = -1;
        }

        public bool Buffer(string value)
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
                var result = Accept(value);

                // We've already checked the conditions that could result in a rejected part, so this should
                // always be true.
                Debug.Assert(result);

                return result;
            }

            if (UriState == SegmentState.Beginning && BufferState == SegmentState.Beginning)
            {
                if (_path.Length != 0 || _buffer.Count != 0)
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
            _path.Clear();
            if (_path.Capacity > 128)
            {
                // We don't want to retain too much memory if this is getting pooled.
                _path.Capacity = 128;
            }

            _query.Clear();
            if (_query.Capacity > 128)
            {
                _query.Capacity = 128;
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

            AppendTrailingSlash = false;
            LowercaseQueryStrings = false;
            LowercaseUrls = false;
        }

        // Used by TemplateBinder.BindValues - the legacy code path of IRouter
        public override string ToString()
        {
            // We can ignore any currently buffered segments - they are are guaranteed to be 'defaults'.
            if (_path.Length > 0 && _path[0] != '/')
            {
                // Normalize generated paths so that they always contain a leading slash.
                _path.Insert(0, '/');
            }

            return _path.ToString() + _query.ToString();
        }

        // Used by TemplateBinder.TryBindValues - the new code path of LinkGenerator
        public PathString ToPathString()
        {
            PathString pathString;

            if (_path.Length > 0)
            {
                if (_path[0] != '/')
                {
                    // Normalize generated paths so that they always contain a leading slash.
                    _path.Insert(0, '/');
                }

                if (AppendTrailingSlash && _path[_path.Length - 1] != '/')
                {
                    _path.Append('/');
                }

                pathString = new PathString(_path.ToString());
            }
            else
            {
                pathString = PathString.Empty;
            }

            return pathString;
        }

        // Used by TemplateBinder.TryBindValues - the new code path of LinkGenerator
        public QueryString ToQueryString()
        {
            if (_query.Length > 0 && _query[0] != '?')
            {
                // Normalize generated query so that they always contain a leading ?.
                _query.Insert(0, '?');
            }

            return new QueryString(_query.ToString());
        }

        private void EncodeValue(string value)
        {
            EncodeValue(value, encodeSlashes: true);
        }

        private void EncodeValue(string value, bool encodeSlashes)
        {
            EncodeValue(value, start: 0, characterCount: value.Length, encodeSlashes);
        }

        // For testing
        internal void EncodeValue(string value, int start, int characterCount, bool encodeSlashes)
        {
            // Just encode everything if its ok to encode slashes
            if (encodeSlashes)
            {
                _urlEncoder.Encode(PathWriter, value, start, characterCount);
            }
            else
            {
                int end;
                int length = start + characterCount;
                while ((end = value.IndexOf('/', start, characterCount)) >= 0)
                {
                    _urlEncoder.Encode(PathWriter, value, start, end - start);
                    _path.Append("/");

                    start = end + 1;
                    characterCount = length - start;
                }

                if (end < 0 && characterCount >= 0)
                {
                    _urlEncoder.Encode(PathWriter, value, start, length - start);
                }
            }
        }

        private string DebuggerToString()
        {
            return string.Format("{{Accepted: '{0}' Buffered: '{1}'}}", _path, string.Join("", _buffer));
        }

        private readonly struct BufferValue
        {
            public BufferValue(string value, bool requiresEncoding)
            {
                Value = value;
                RequiresEncoding = requiresEncoding;
            }

            public bool RequiresEncoding { get; }

            public string Value { get; }
        }
    }
}
