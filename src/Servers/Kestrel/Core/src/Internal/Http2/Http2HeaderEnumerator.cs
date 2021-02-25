// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Net.Http.HPack;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    internal sealed class Http2HeadersEnumerator : IEnumerator<KeyValuePair<string, string>>
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

        public int HPackStaticTableId => GetResponseHeaderStaticTableId(_knownHeaderType);
        public KeyValuePair<string, string> Current { get; private set; }
        object IEnumerator.Current => Current;

        public Http2HeadersEnumerator()
        {
        }

        public void Initialize(HttpResponseHeaders headers)
        {
            _headersEnumerator = headers.GetEnumerator();
            _headersType = HeadersType.Headers;
            _hasMultipleValues = false;
        }

        public void Initialize(HttpResponseTrailers headers)
        {
            _trailersEnumerator = headers.GetEnumerator();
            _headersType = HeadersType.Trailers;
            _hasMultipleValues = false;
        }

        public void Initialize(IDictionary<string, StringValues> headers)
        {
            _genericEnumerator = headers.GetEnumerator();
            _headersType = HeadersType.Untyped;
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
            switch (responseHeaderType)
            {
                case KnownHeaderType.CacheControl:
                    return H2StaticTable.CacheControl;
                case KnownHeaderType.Date:
                    return H2StaticTable.Date;
                case KnownHeaderType.TransferEncoding:
                    return H2StaticTable.TransferEncoding;
                case KnownHeaderType.Via:
                    return H2StaticTable.Via;
                case KnownHeaderType.Allow:
                    return H2StaticTable.Allow;
                case KnownHeaderType.ContentType:
                    return H2StaticTable.ContentType;
                case KnownHeaderType.ContentEncoding:
                    return H2StaticTable.ContentEncoding;
                case KnownHeaderType.ContentLanguage:
                    return H2StaticTable.ContentLanguage;
                case KnownHeaderType.ContentLocation:
                    return H2StaticTable.ContentLocation;
                case KnownHeaderType.ContentRange:
                    return H2StaticTable.ContentRange;
                case KnownHeaderType.Expires:
                    return H2StaticTable.Expires;
                case KnownHeaderType.LastModified:
                    return H2StaticTable.LastModified;
                case KnownHeaderType.AcceptRanges:
                    return H2StaticTable.AcceptRanges;
                case KnownHeaderType.Age:
                    return H2StaticTable.Age;
                case KnownHeaderType.ETag:
                    return H2StaticTable.ETag;
                case KnownHeaderType.Location:
                    return H2StaticTable.Location;
                case KnownHeaderType.ProxyAuthenticate:
                    return H2StaticTable.ProxyAuthenticate;
                case KnownHeaderType.RetryAfter:
                    return H2StaticTable.RetryAfter;
                case KnownHeaderType.Server:
                    return H2StaticTable.Server;
                case KnownHeaderType.SetCookie:
                    return H2StaticTable.SetCookie;
                case KnownHeaderType.Vary:
                    return H2StaticTable.Vary;
                case KnownHeaderType.WWWAuthenticate:
                    return H2StaticTable.WwwAuthenticate;
                case KnownHeaderType.AccessControlAllowOrigin:
                    return H2StaticTable.AccessControlAllowOrigin;
                case KnownHeaderType.ContentLength:
                    return H2StaticTable.ContentLength;
                default:
                    return -1;
            }
        }
    }
}
