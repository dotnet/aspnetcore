// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    internal struct Http2HeadersEnumerator
    {
        private bool _isTrailers;
        private HttpResponseHeaders.Enumerator _headersEnumerator;
        private HttpResponseTrailers.Enumerator _trailersEnumerator;
        private IEnumerator<KeyValuePair<string, StringValues>> _genericEnumerator;
        private StringValues.Enumerator _stringValuesEnumerator;

        public KeyValuePair<string, string> Current { get; private set; }

        public Http2HeadersEnumerator(HttpResponseHeaders headers)
        {
            _headersEnumerator = headers.GetEnumerator();
            _trailersEnumerator = default;
            _genericEnumerator = null;
            _isTrailers = false;

            _stringValuesEnumerator = default;
            Current = default;
        }

        public Http2HeadersEnumerator(HttpResponseTrailers trailers)
        {
            _headersEnumerator = default;
            _trailersEnumerator = trailers.GetEnumerator();
            _genericEnumerator = null;
            _isTrailers = true;

            _stringValuesEnumerator = default;
            Current = default;
        }

        public Http2HeadersEnumerator(IDictionary<string, StringValues> headers)
        {
            _headersEnumerator = default;
            _trailersEnumerator = default;
            _genericEnumerator = headers.GetEnumerator();
            _isTrailers = true;

            _stringValuesEnumerator = default;
            Current = default;
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
            var e = _stringValuesEnumerator;
            var result = e.MoveNext();
            if (result)
            {
                Current = new KeyValuePair<string, string>(GetCurrentKey(), e.Current);
            }
            else
            {
                Current = default;
            }
            _stringValuesEnumerator = e;
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
                    return true;
                }
            }
        }
    }
}
