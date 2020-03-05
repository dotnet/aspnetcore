// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
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

        public KnownHeaderType KnownHeaderType { get; private set; }
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
            KnownHeaderType = default;
        }

        public void Initialize(HttpResponseTrailers headers)
        {
            _headersEnumerator = default;
            _trailersEnumerator = headers.GetEnumerator();
            _genericEnumerator = null;
            _isTrailers = true;

            _stringValuesEnumerator = default;
            Current = default;
            KnownHeaderType = default;
        }

        public void Initialize(IDictionary<string, StringValues> headers)
        {
            _headersEnumerator = default;
            _trailersEnumerator = default;
            _genericEnumerator = headers.GetEnumerator();
            _isTrailers = false;

            _stringValuesEnumerator = default;
            Current = default;
            KnownHeaderType = default;
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
                    KnownHeaderType = default;
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
                    KnownHeaderType = _trailersEnumerator.CurrentKnownType;
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
                    KnownHeaderType = _headersEnumerator.CurrentKnownType;
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
            KnownHeaderType = default;
        }

        public void Dispose()
        {
        }
    }
}
