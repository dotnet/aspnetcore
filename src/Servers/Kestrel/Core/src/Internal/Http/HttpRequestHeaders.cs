// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    internal sealed partial class HttpRequestHeaders : HttpHeaders
    {
        private long _previousBits = 0;

        public bool ReuseHeaderValues { get; set; }
        public Func<string, Encoding> EncodingSelector { get; set; }

        public HttpRequestHeaders(bool reuseHeaderValues = true, Func<string, Encoding> encodingSelector = null)
        {
            ReuseHeaderValues = reuseHeaderValues;
            EncodingSelector = encodingSelector ?? KestrelServerOptions.DefaultRequestHeaderEncodingSelector;
        }

        public void OnHeadersComplete()
        {
            var newHeaderFlags = _bits;
            var previousHeaderFlags = _previousBits;
            _previousBits = 0;

            var headersToClear = (~newHeaderFlags) & previousHeaderFlags;
            if (headersToClear == 0)
            {
                // All headers were resued.
                return;
            }

            // Some previous headers were not reused or overwritten.
            // While they cannot be accessed by the current request (as they were not supplied by it)
            // there is no point in holding on to them, so clear them now,
            // to allow them to get collected by the GC.
            Clear(headersToClear);
        }

        protected override void ClearFast()
        {
            if (!ReuseHeaderValues)
            {
                // If we aren't reusing headers clear them all
                Clear(_bits);
            }
            else
            {
                // If we are reusing headers, store the currently set headers for comparison later
                _previousBits = _bits;
            }

            // Mark no headers as currently in use
            _bits = 0;
            // Clear ContentLength and any unknown headers as we will never reuse them 
            _contentLength = null;
            MaybeUnknown?.Clear();
        }

        private static long ParseContentLength(string value)
        {
            if (!HeaderUtilities.TryParseNonNegativeInt64(value, out var parsed))
            {
                KestrelBadHttpRequestException.Throw(RequestRejectionReason.InvalidContentLength, value);
            }

            return parsed;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void AppendContentLength(ReadOnlySpan<byte> value)
        {
            if (_contentLength.HasValue)
            {
                KestrelBadHttpRequestException.Throw(RequestRejectionReason.MultipleContentLengths);
            }

            if (!Utf8Parser.TryParse(value, out long parsed, out var consumed) ||
                parsed < 0 ||
                consumed != value.Length)
            {
                KestrelBadHttpRequestException.Throw(RequestRejectionReason.InvalidContentLength, value.GetRequestHeaderString(HeaderNames.ContentLength, EncodingSelector));
            }

            _contentLength = parsed;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void AppendContentLengthCustomEncoding(ReadOnlySpan<byte> value, Encoding customEncoding)
        {
            if (_contentLength.HasValue)
            {
                KestrelBadHttpRequestException.Throw(RequestRejectionReason.MultipleContentLengths);
            }

            // long.MaxValue = 9223372036854775807 (19 chars)
            Span<char> decodedChars = stackalloc char[20];
            var numChars = customEncoding.GetChars(value, decodedChars);
            long parsed = -1;

            if (numChars > 19 ||
                !long.TryParse(decodedChars.Slice(0, numChars), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed) ||
                parsed < 0)
            {
                KestrelBadHttpRequestException.Throw(RequestRejectionReason.InvalidContentLength, value.GetRequestHeaderString(HeaderNames.ContentLength, EncodingSelector));
            }

            _contentLength = parsed;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void SetValueUnknown(string key, StringValues value)
        {
            Unknown[key] = value;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool AddValueUnknown(string key, StringValues value)
        {
            Unknown.Add(key, value);
            // Return true, above will throw and exit for false
            return true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private unsafe void AppendUnknownHeaders(string name, string valueString)
        {
            Unknown.TryGetValue(name, out var existing);
            Unknown[name] = AppendValue(existing, valueString);
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        protected override IEnumerator<KeyValuePair<string, StringValues>> GetEnumeratorFast()
        {
            return GetEnumerator();
        }

        public partial struct Enumerator : IEnumerator<KeyValuePair<string, StringValues>>
        {
            private readonly HttpRequestHeaders _collection;
            private readonly long _bits;
            private int _next;
            private KeyValuePair<string, StringValues> _current;
            private KnownHeaderType _currentKnownType;
            private readonly bool _hasUnknown;
            private Dictionary<string, StringValues>.Enumerator _unknownEnumerator;

            internal Enumerator(HttpRequestHeaders collection)
            {
                _collection = collection;
                _bits = collection._bits;
                _next = 0;
                _current = default;
                _currentKnownType = default;
                _hasUnknown = collection.MaybeUnknown != null;
                _unknownEnumerator = _hasUnknown
                    ? collection.MaybeUnknown.GetEnumerator()
                    : default;
            }

            public KeyValuePair<string, StringValues> Current => _current;

            internal KnownHeaderType CurrentKnownType => _currentKnownType;

            object IEnumerator.Current => _current;

            public void Dispose()
            {
            }

            public void Reset()
            {
                _next = 0;
            }
        }
    }
}
