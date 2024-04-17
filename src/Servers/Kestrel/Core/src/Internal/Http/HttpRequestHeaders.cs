// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Text;
using System.Collections;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

internal sealed partial class HttpRequestHeaders : HttpHeaders
{
    private EnumeratorCache? _enumeratorCache;
    private long _previousBits;
    private long _pseudoBits;

    public bool ReuseHeaderValues { get; set; }
    public Func<string, Encoding?> EncodingSelector { get; set; }

    public HttpRequestHeaders(bool reuseHeaderValues = true, Func<string, Encoding?>? encodingSelector = null)
    {
        ReuseHeaderValues = reuseHeaderValues;
        EncodingSelector = encodingSelector ?? KestrelServerOptions.DefaultHeaderEncodingSelector;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MergeCookies()
    {
        if (HasCookie && _headers._Cookie.Count > 1)
        {
            _headers._Cookie = string.Join("; ", _headers._Cookie.ToArray());
        }
    }

    protected override void ClearFast()
    {
        if (!ReuseHeaderValues)
        {
            // If we aren't reusing headers clear them all
            Clear(_bits | _pseudoBits);
        }
        else
        {
            // If we are reusing headers, store the currently set headers for comparison later
            // Pseudo header bits were cleared at the start of a request to hide from the user.
            // Keep those values for reuse.
            _previousBits = _bits | _pseudoBits;
        }

        // Mark no headers as currently in use
        _bits = 0;
        _pseudoBits = 0;
        // Clear ContentLength and any unknown headers as we will never reuse them
        _contentLength = null;
        MaybeUnknown?.Clear();
        _enumeratorCache?.Reset();
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
            KestrelBadHttpRequestException.Throw(RequestRejectionReason.InvalidContentLength, value.GetRequestHeaderString(HeaderNames.ContentLength, EncodingSelector, checkForNewlineChars: false));
        }

        _contentLength = parsed;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [SkipLocalsInit]
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
            KestrelBadHttpRequestException.Throw(RequestRejectionReason.InvalidContentLength, value.GetRequestHeaderString(HeaderNames.ContentLength, EncodingSelector, checkForNewlineChars: false));
        }

        _contentLength = parsed;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void SetValueUnknown(string key, StringValues value)
    {
        Unknown[GetInternedHeaderName(key)] = value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool AddValueUnknown(string key, StringValues value)
    {
        Unknown.Add(GetInternedHeaderName(key), value);
        // Return true, above will throw and exit for false
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private unsafe void AppendUnknownHeaders(string name, string valueString)
    {
        name = GetInternedHeaderName(name);
        Unknown.TryGetValue(name, out var existing);
        Unknown[name] = AppendValue(existing, valueString);
    }

    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    protected override IEnumerator<KeyValuePair<string, StringValues>> GetEnumeratorFast()
    {
        // Get or create the cache.
        var cache = _enumeratorCache ??= new();

        EnumeratorBox enumerator;
        if (cache.CachedEnumerator is not null)
        {
            // Previous enumerator, reuse that one.
            enumerator = cache.InUseEnumerator = cache.CachedEnumerator;
            // Set previous to null so if there is a second enumerator call
            // during the same request it doesn't get the same one.
            cache.CachedEnumerator = null;
        }
        else
        {
            // Create new enumerator box and store as in use.
            enumerator = cache.InUseEnumerator = new();
        }

        // Set the underlying struct enumerator to a new one.
        enumerator.Enumerator = new Enumerator(this);
        return enumerator;
    }

    private sealed class EnumeratorCache
    {
        /// <summary>
        /// Enumerator created from previous request
        /// </summary>
        public EnumeratorBox? CachedEnumerator { get; set; }
        /// <summary>
        /// Enumerator used on this request
        /// </summary>
        public EnumeratorBox? InUseEnumerator { get; set; }

        /// <summary>
        /// Moves InUseEnumerator to CachedEnumerator
        /// </summary>
        public void Reset()
        {
            var enumerator = InUseEnumerator;
            if (enumerator is not null)
            {
                InUseEnumerator = null;
                enumerator.Enumerator = default;
                CachedEnumerator = enumerator;
            }
        }
    }

    /// <summary>
    /// Strong box enumerator for the IEnumerator interface to cache and amortizate the
    /// IEnumerator allocations across requests if the header collection is commonly
    /// enumerated for forwarding in a reverse-proxy type situation.
    /// </summary>
    private sealed class EnumeratorBox : IEnumerator<KeyValuePair<string, StringValues>>
    {
        public Enumerator Enumerator;

        public KeyValuePair<string, StringValues> Current => Enumerator.Current;

        public bool MoveNext() => Enumerator.MoveNext();

        object IEnumerator.Current => Current;

        public void Dispose() { }

        public void Reset() => throw new NotSupportedException();
    }

    public partial struct Enumerator : IEnumerator<KeyValuePair<string, StringValues>>
    {
        private readonly HttpRequestHeaders _collection;
        private long _currentBits;
        private int _next;
        private KeyValuePair<string, StringValues> _current;
        private readonly bool _hasUnknown;
        private Dictionary<string, StringValues>.Enumerator _unknownEnumerator;

        internal Enumerator(HttpRequestHeaders collection)
        {
            _collection = collection;
            _currentBits = collection._bits;
            _next = GetNext(_currentBits, collection.ContentLength.HasValue);
            _current = default;
            _hasUnknown = collection.MaybeUnknown != null;
            _unknownEnumerator = _hasUnknown
                ? collection.MaybeUnknown!.GetEnumerator()
                : default;
        }

        public readonly KeyValuePair<string, StringValues> Current => _current;

        readonly object IEnumerator.Current => _current;

        public readonly void Dispose()
        {
        }

        public void Reset()
        {
            _currentBits = _collection._bits;
            _next = GetNext(_currentBits, _collection.ContentLength.HasValue);
        }
    }
}
