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
        private bool _isTrailers;
        private HttpResponseHeaders.Enumerator _headersEnumerator;
        private HttpResponseTrailers.Enumerator _trailersEnumerator;
        private IEnumerator<KeyValuePair<string, StringValues>> _genericEnumerator;
        private StringValues.Enumerator _stringValuesEnumerator;
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
            _trailersEnumerator = default;
            _genericEnumerator = null;
            _isTrailers = false;

            _stringValuesEnumerator = default;
            Current = default;
            _knownHeaderType = default;
        }

        public void Initialize(HttpResponseTrailers headers)
        {
            _headersEnumerator = default;
            _trailersEnumerator = headers.GetEnumerator();
            _genericEnumerator = null;
            _isTrailers = true;

            _stringValuesEnumerator = default;
            Current = default;
            _knownHeaderType = default;
        }

        public void Initialize(IDictionary<string, StringValues> headers)
        {
            _headersEnumerator = default;
            _trailersEnumerator = default;
            _genericEnumerator = headers.GetEnumerator();
            _isTrailers = false;

            _stringValuesEnumerator = default;
            Current = default;
            _knownHeaderType = default;
        }

        public bool MoveNext()
        {
            if (MoveNextOnStringEnumerator())
            {
                return true;
            }

            if (!TryGetNextStringEnumerator(out _stringValuesEnumerator))
            {
                return false;
            }

            return MoveNextOnStringEnumerator();
        }

        private string GetCurrentKey()
        {
            if (_genericEnumerator != null)
            {
                return _genericEnumerator.Current.Key;
            }
            else if (_isTrailers)
            {
                return _trailersEnumerator.Current.Key;
            }
            else
            {
                return _headersEnumerator.Current.Key;
            }
        }

        private bool MoveNextOnStringEnumerator()
        {
            var result = _stringValuesEnumerator.MoveNext();
            Current = result ? new KeyValuePair<string, string>(GetCurrentKey(), _stringValuesEnumerator.Current) : default;
            return result;
        }

        private bool TryGetNextStringEnumerator(out StringValues.Enumerator enumerator)
        {
            if (_genericEnumerator != null)
            {
                if (!_genericEnumerator.MoveNext())
                {
                    enumerator = default;
                    return false;
                }
                else
                {
                    enumerator = _genericEnumerator.Current.Value.GetEnumerator();
                    _knownHeaderType = default;
                    return true;
                }
            }
            else if (_isTrailers)
            {
                if (!_trailersEnumerator.MoveNext())
                {
                    enumerator = default;
                    return false;
                }
                else
                {
                    enumerator = _trailersEnumerator.Current.Value.GetEnumerator();
                    _knownHeaderType = _trailersEnumerator.CurrentKnownType;
                    return true;
                }
            }
            else
            {
                if (!_headersEnumerator.MoveNext())
                {
                    enumerator = default;
                    return false;
                }
                else
                {
                    enumerator = _headersEnumerator.Current.Value.GetEnumerator();
                    _knownHeaderType = _headersEnumerator.CurrentKnownType;
                    return true;
                }
            }
        }

        public void Reset()
        {
            if (_genericEnumerator != null)
            {
                _genericEnumerator.Reset();
            }
            else if (_isTrailers)
            {
                _trailersEnumerator.Reset();
            }
            else
            {
                _headersEnumerator.Reset();
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
