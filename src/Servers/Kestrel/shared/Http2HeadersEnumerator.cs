// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Net.Http.HPack;
using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Extensions.Primitives;

#if !(IS_TESTS || IS_BENCHMARKS)
namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
#else
namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;
#endif

#nullable enable

// This file is used by Kestrel to write response headers and tests to write request headers.
// To avoid adding test code to Kestrel this file is shared. Test specifc code is excluded from Kestrel by ifdefs.
internal sealed class Http2HeadersEnumerator : IEnumerator<KeyValuePair<string, string>>
{
    private enum HeadersType : byte
    {
        Headers,
        Trailers,
#if IS_TESTS || IS_BENCHMARKS
        Untyped,
#endif
    }
    private HeadersType _headersType;
    private HttpResponseHeaders.Enumerator _headersEnumerator;
    private HttpResponseTrailers.Enumerator _trailersEnumerator;
#if IS_TESTS || IS_BENCHMARKS
    private IEnumerator<KeyValuePair<string, StringValues>>? _genericEnumerator;
#endif
    private StringValues.Enumerator _stringValuesEnumerator;
    private bool _hasMultipleValues;
    private KnownHeaderType _knownHeaderType;

    public Func<string, Encoding?> EncodingSelector { get; set; } = KestrelServerOptions.DefaultHeaderEncodingSelector;

    public int HPackStaticTableId => GetResponseHeaderStaticTableId(_knownHeaderType);
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

#if IS_TESTS || IS_BENCHMARKS
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
#endif

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
#if IS_TESTS || IS_BENCHMARKS
            return _genericEnumerator!.MoveNext()
                ? SetCurrent(_genericEnumerator.Current.Key, _genericEnumerator.Current.Value, GetKnownRequestHeaderType(_genericEnumerator.Current.Key))
                : false;
#else
            ThrowUnexpectedHeadersType();
            return false;
#endif
        }
    }

#if IS_TESTS || IS_BENCHMARKS
    private static KnownHeaderType GetKnownRequestHeaderType(string headerName)
    {
        switch (headerName)
        {
            case ":method":
                return KnownHeaderType.Method;
            default:
                return default;
        }
    }
#else
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static void ThrowUnexpectedHeadersType()
    {
        throw new InvalidOperationException("Unexpected headers collection type.");
    }
#endif

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
#if IS_TESTS || IS_BENCHMARKS
            _genericEnumerator!.Reset();
#else
            ThrowUnexpectedHeadersType();
#endif
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
#if IS_TESTS || IS_BENCHMARKS
            // Include request headers for tests.
            case KnownHeaderType.Method:
                return H2StaticTable.MethodGet;
#endif
        }
    }
}
