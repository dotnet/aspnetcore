// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;

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
    private HttpResponseHeaders? _responseHeaders;
    private bool _currentIsRawContentLength;
    private KeyValuePair<string, string> _current;

    public Func<string, Encoding?> EncodingSelector { get; set; } = KestrelServerOptions.DefaultHeaderEncodingSelector;

    public (int index, bool matchedValue) GetQPackStaticTableId() => HttpHeadersCompression.MatchKnownHeaderQPack(_knownHeaderType, Current.Value);
    public KeyValuePair<string, string> Current => _currentIsRawContentLength ? GetFormattedContentLength() : _current;
    object IEnumerator.Current => Current;

    public void Initialize(HttpResponseHeaders headers)
    {
        EncodingSelector = headers.EncodingSelector;
        _headersEnumerator = headers.GetEnumeratorWithRawContentLength();
        _responseHeaders = headers;
        _headersType = HeadersType.Headers;
        _hasMultipleValues = false;
        _currentIsRawContentLength = false;
    }

    public void Initialize(HttpResponseTrailers headers)
    {
        EncodingSelector = headers.EncodingSelector;
        _trailersEnumerator = headers.GetEnumerator();
        _responseHeaders = null;
        _headersType = HeadersType.Trailers;
        _hasMultipleValues = false;
        _currentIsRawContentLength = false;
    }

    public void Initialize(IDictionary<string, StringValues> headers)
    {
        switch (headers)
        {
            case HttpResponseHeaders responseHeaders:
                _headersType = HeadersType.Headers;
                _headersEnumerator = responseHeaders.GetEnumeratorWithRawContentLength();
                _responseHeaders = responseHeaders;
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
        _currentIsRawContentLength = false;
    }

    public bool MoveNext()
    {
        _currentIsRawContentLength = false;

        if (_hasMultipleValues && MoveNextOnStringEnumerator(_current.Key))
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

        // Current is null only when result is false.
        _current = result ? new KeyValuePair<string, string>(key, _stringValuesEnumerator.Current!) : default;
        return result;
    }

    private bool SetCurrent(string name, StringValues value, KnownHeaderType knownHeaderType)
    {
        _knownHeaderType = knownHeaderType;

        if (knownHeaderType == KnownHeaderType.ContentLength && _headersType == HeadersType.Headers)
        {
            // The value is not formatted as a string by the enumerator to avoid an allocation per response.
            // Header writers should use TryGetCurrentContentLength; Current formats it on demand and caches it.
            Debug.Assert(value.Count == 0);
            _current = new KeyValuePair<string, string>(name, null!);
            _currentIsRawContentLength = true;
            _hasMultipleValues = false;
            return true;
        }

        if (value.Count == 1)
        {
            _current = new KeyValuePair<string, string>(name, value.ToString());
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
        _currentIsRawContentLength = false;
    }

    /// <summary>
    /// Returns true if the current header is Content-Length, which is not formatted as a string by the enumerator.
    /// </summary>
    public bool TryGetCurrentContentLength(out long contentLength)
    {
        if (_currentIsRawContentLength)
        {
            contentLength = _responseHeaders!.ContentLength.GetValueOrDefault();
            return true;
        }

        contentLength = 0;
        return false;
    }

    private KeyValuePair<string, string> GetFormattedContentLength()
    {
        // Format on first read and cache so repeated Current reads (e.g. with a custom encoding selector) don't allocate again.
        if (_current.Value is null)
        {
            _current = new KeyValuePair<string, string>(_current.Key, HeaderUtilities.FormatNonNegativeInt64(_responseHeaders!.ContentLength.GetValueOrDefault()));
        }

        return _current;
    }

    public void Dispose()
    {
    }
}
