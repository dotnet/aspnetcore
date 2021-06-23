// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3
{
    internal sealed class Http3HeadersEnumerator : IEnumerator<KeyValuePair<string, string>>
    {
        private enum HeadersType : byte
        {
            Headers,
            Trailers,
            Untyped
        }
        private HeadersType _headersType;
        private HttpResponseHeaders.Enumerator _headersEnumerator;
        private HttpResponseTrailers.Enumerator _trailersEnumerator;
        private IEnumerator<KeyValuePair<string, StringValues>>? _genericEnumerator;
        private StringValues.Enumerator _stringValuesEnumerator;
        private bool _hasMultipleValues;
        private KnownHeaderType _knownHeaderType;

        public Func<string, Encoding?> EncodingSelector { get; set; } = KestrelServerOptions.DefaultHeaderEncodingSelector;

        public int QPackStaticTableId => GetResponseHeaderStaticTableId(_knownHeaderType);
        public KeyValuePair<string, string> Current { get; private set; }
        object IEnumerator.Current => Current;

        public Http3HeadersEnumerator()
        {
        }

        public void Initialize(HttpResponseHeaders headers)
        {
            EncodingSelector = headers.EncodingSelector;
            _headersEnumerator = headers.GetEnumerator();
            _headersType = HeadersType.Headers;
            _hasMultipleValues = false;
        }

        public void Initialize(HttpResponseTrailers headers)
        {
            EncodingSelector = headers.EncodingSelector;
            _trailersEnumerator = headers.GetEnumerator();
            _headersType = HeadersType.Trailers;
            _hasMultipleValues = false;
        }

        public void Initialize(IDictionary<string, StringValues> headers)
        {
            switch (headers)
            {
                case HttpResponseHeaders responseHeaders:
                    _headersType = HeadersType.Headers;
                    _headersEnumerator = responseHeaders.GetEnumerator();
                    break;
                case HttpResponseTrailers responseTrailers:
                    _headersType = HeadersType.Trailers;
                    _trailersEnumerator = responseTrailers.GetEnumerator();
                    break;
                default:
                    _headersType = HeadersType.Untyped;
                    _genericEnumerator = headers.GetEnumerator();
                    break;
            }

            _hasMultipleValues = false;
        }

        public bool MoveNext()
        {
            if (_hasMultipleValues && MoveNextOnStringEnumerator(Current.Key))
            {
                return true;
            }

            if (_headersType == HeadersType.Headers)
            {
                return _headersEnumerator.MoveNext()
                    ? SetCurrent(_headersEnumerator.Current.Key, _headersEnumerator.Current.Value, _headersEnumerator.CurrentKnownType)
                    : false;
            }
            else if (_headersType == HeadersType.Trailers)
            {
                return _trailersEnumerator.MoveNext()
                    ? SetCurrent(_trailersEnumerator.Current.Key, _trailersEnumerator.Current.Value, _trailersEnumerator.CurrentKnownType)
                    : false;
            }
            else
            {
                return _genericEnumerator!.MoveNext()
                    ? SetCurrent(_genericEnumerator.Current.Key, _genericEnumerator.Current.Value, default)
                    : false;
            }
        }

        private bool MoveNextOnStringEnumerator(string key)
        {
            var result = _stringValuesEnumerator.MoveNext();
            Current = result ? new KeyValuePair<string, string>(key, _stringValuesEnumerator.Current) : default;
            return result;
        }

        private bool SetCurrent(string name, StringValues value, KnownHeaderType knownHeaderType)
        {
            _knownHeaderType = knownHeaderType;

            if (value.Count == 1)
            {
                Current = new KeyValuePair<string, string>(name, value.ToString());
                _hasMultipleValues = false;
                return true;
            }
            else
            {
                _stringValuesEnumerator = value.GetEnumerator();
                _hasMultipleValues = true;
                return MoveNextOnStringEnumerator(name);
            }
        }

        public void Reset()
        {
            if (_headersType == HeadersType.Headers)
            {
                _headersEnumerator.Reset();
            }
            else if (_headersType == HeadersType.Trailers)
            {
                _trailersEnumerator.Reset();
            }
            else
            {
                _genericEnumerator!.Reset();
            }
            _stringValuesEnumerator = default;
            _knownHeaderType = default;
        }

        public void Dispose()
        {
        }

        internal static int GetResponseHeaderStaticTableId(KnownHeaderType responseHeaderType)
        {
            // Not Implemented
            return -1;
        }
    }
}
