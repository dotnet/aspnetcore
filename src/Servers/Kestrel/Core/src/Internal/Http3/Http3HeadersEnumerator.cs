// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http.QPack;
using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Extensions.Primitives;

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

    public Func<string, Encoding?> EncodingSelector { get; set; } = KestrelServerOptions.DefaultHeaderEncodingSelector;

    public int QPackStaticTableId => GetResponseHeaderStaticTableId(_knownHeaderType);
    public KeyValuePair<string, string> Current { get; private set; }
    object IEnumerator.Current => Current;

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

        // Current is null only when result is false.
        Current = result ? new KeyValuePair<string, string>(key, _stringValuesEnumerator.Current!) : default;
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
        // Removed from this test are request-only headers, e.g. cookie.
        //
        // Not every header in the QPACK static table is known.
        // These are missing from this test and the full header name is written.
        // Missing:
        // - link
        // - location
        // - strict-transport-security
        // - x-content-type-options
        // - x-xss-protection
        // - content-security-policy
        // - early-data
        // - expect-ct
        // - purpose
        // - timing-allow-origin
        // - x-forwarded-for
        // - x-frame-options
        switch (responseHeaderType)
        {
            case KnownHeaderType.Age:
                return H3StaticTable.Age0;
            case KnownHeaderType.ContentLength:
                return H3StaticTable.ContentLength0;
            case KnownHeaderType.Date:
                return H3StaticTable.Date;
            case KnownHeaderType.ETag:
                return H3StaticTable.ETag;
            case KnownHeaderType.LastModified:
                return H3StaticTable.LastModified;
            case KnownHeaderType.Location:
                return H3StaticTable.Location;
            case KnownHeaderType.SetCookie:
                return H3StaticTable.SetCookie;
            case KnownHeaderType.AcceptRanges:
                return H3StaticTable.AcceptRangesBytes;
            case KnownHeaderType.AccessControlAllowHeaders:
                return H3StaticTable.AccessControlAllowHeadersCacheControl;
            case KnownHeaderType.AccessControlAllowOrigin:
                return H3StaticTable.AccessControlAllowOriginAny;
            case KnownHeaderType.CacheControl:
                return H3StaticTable.CacheControlMaxAge0;
            case KnownHeaderType.ContentEncoding:
                return H3StaticTable.ContentEncodingBr;
            case KnownHeaderType.ContentType:
                return H3StaticTable.ContentTypeApplicationDnsMessage;
            case KnownHeaderType.Vary:
                return H3StaticTable.VaryAcceptEncoding;
            case KnownHeaderType.AccessControlAllowCredentials:
                return H3StaticTable.AccessControlAllowCredentials;
            case KnownHeaderType.AccessControlAllowMethods:
                return H3StaticTable.AccessControlAllowMethodsGet;
            case KnownHeaderType.AltSvc:
                return H3StaticTable.AltSvcClear;
            case KnownHeaderType.Server:
                return H3StaticTable.Server;
            default:
                return -1;
        }
    }
}
