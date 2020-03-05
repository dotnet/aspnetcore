// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    internal sealed partial class HttpRequestHeaders : HttpHeaders
    {
        private long _previousBits = 0;

        public bool ReuseHeaderValues { get; set; }
        public bool UseLatin1 { get; set; }

        public HttpRequestHeaders(bool reuseHeaderValues = true, bool useLatin1 = false)
        {
            ReuseHeaderValues = reuseHeaderValues;
            UseLatin1 = useLatin1;
        }

        public void OnHeadersComplete()
        {
            var bitsToClear = _previousBits & ~_bits;
            _previousBits = 0;

            if (bitsToClear != 0)
            {
                // Some previous headers were not reused or overwritten.

                // While they cannot be accessed by the current request (as they were not supplied by it)
                // there is no point in holding on to them, so clear them now,
                // to allow them to get collected by the GC.
                Clear(bitsToClear);
            }
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
                BadHttpRequestException.Throw(RequestRejectionReason.InvalidContentLength, value);
            }

            return parsed;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void AppendContentLength(ReadOnlySpan<byte> value)
        {
            if (_contentLength.HasValue)
            {
                BadHttpRequestException.Throw(RequestRejectionReason.MultipleContentLengths);
            }

            if (!Utf8Parser.TryParse(value, out long parsed, out var consumed) ||
                parsed < 0 ||
                consumed != value.Length)
            {
                BadHttpRequestException.Throw(RequestRejectionReason.InvalidContentLength, value.GetRequestHeaderStringNonNullCharacters(UseLatin1));
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
        private unsafe void AppendUnknownHeaders(ReadOnlySpan<byte> name, string valueString)
        {
            string key = name.GetHeaderName();
            Unknown.TryGetValue(key, out var existing);
            Unknown[key] = AppendValue(existing, valueString);
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
