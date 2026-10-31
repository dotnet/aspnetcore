// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Text;
using System.Collections;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
    private Dictionary<string, HeaderValue>? _unknown;
    private Dictionary<string, HeaderValue> RequestUnknown => _unknown ??= new(StringComparer.OrdinalIgnoreCase);
    public bool LazyRequestHeaders { get; }

    public HttpRequestHeaders(bool reuseHeaderValues = true, Func<string, Encoding?>? encodingSelector = null, bool? lazyRequestHeaders = null)
    {
        ReuseHeaderValues = reuseHeaderValues;
        EncodingSelector = encodingSelector ?? KestrelServerOptions.DefaultHeaderEncodingSelector;
        LazyRequestHeaders = lazyRequestHeaders ??
            (AppContext.TryGetSwitch("Microsoft.AspNetCore.Server.Kestrel.Experimental.LazyRequestHeaders", out var enabled) && enabled);
    }

    private bool CanStoreLazy(ReadOnlySpan<byte> value, bool checkForNewlineChars)
    {
        return LazyRequestHeaders &&
            ReferenceEquals(EncodingSelector, KestrelServerOptions.DefaultHeaderEncodingSelector) &&
            value.Length <= HeaderReferences.BufferBudget &&
            Ascii.IsValid(value) && !value.Contains((byte)0) &&
            (!checkForNewlineChars || value.IndexOfAny((byte)'\r', (byte)'\n') < 0);
    }

    private bool TryAppendLazy(ReadOnlySpan<byte> value, ref HeaderValue destination, long flag, bool checkForNewlineChars)
    {
        if ((_bits & flag) != 0 || !CanStoreLazy(value, checkForNewlineChars) ||
            !_headers.TryStore(value, ref destination))
        {
            return false;
        }

        if (destination.Length != 0)
        {
            _headers.RawBits |= flag;
        }
        _previousBits &= ~flag;
        _bits |= flag;
        return true;
    }

    internal int LazyValueCount
    {
        get
        {
            var count = BitOperations.PopCount((ulong)_headers.RawBits);
            if (_unknown is not null)
            {
                foreach (var pair in _unknown)
                {
                    if (pair.Value.Length != 0)
                    {
                        count++;
                    }
                }
            }
            return count;
        }
    }

    private struct HeaderValue
    {
        public StringValues Value;
        public int Offset;
        public int Length;
    }

    private partial struct HeaderReferences
    {
        public const int BufferBudget = 4096;

        public bool TryStore(ReadOnlySpan<byte> value, ref HeaderValue destination)
        {
            if (destination.Value.Count == 1 &&
                StringUtilities.BytesOrdinalEqualsStringAndAscii(destination.Value.ToString(), value))
            {
                return true;
            }
            if (value.IsEmpty)
            {
                destination = new HeaderValue { Value = string.Empty };
                return true;
            }
            if (value.Length > BufferBudget - _used)
            {
                return false;
            }

            var required = _used + value.Length;
            if (_bytes is null || _bytes.Length < required)
            {
                var bytes = new byte[Math.Min(BufferBudget, Math.Max(required, (_bytes?.Length ?? 128) * 2))];
                if (_bytes is not null)
                {
                    _bytes.AsSpan(0, _used).CopyTo(bytes);
                    _bytes.AsSpan(0, _used).Clear();
                }
                _bytes = bytes;
            }

            value.CopyTo(_bytes.AsSpan(_used));
            destination = new HeaderValue { Offset = _used, Length = value.Length };
            _used = required;
            return true;
        }

        public void Discard(ref HeaderValue value)
        {
            if (value.Length != 0)
            {
                _bytes.AsSpan(value.Offset, value.Length).Clear();
            }
            value = default;
        }

        public void Set(ref HeaderValue destination, StringValues value, long flag)
        {
            Discard(ref destination);
            RawBits &= ~flag;
            destination.Value = value;
        }

        public ref StringValues Get(ref HeaderValue value, long flag)
        {
            if (value.Length != 0)
            {
                var decoded = Encoding.ASCII.GetString(_bytes!, value.Offset, value.Length);
                Discard(ref value);
                value.Value = decoded;
                RawBits &= ~flag;
            }
            return ref value.Value;
        }

        public void ResetBuffer()
        {
            _bytes.AsSpan(0, _used).Clear();
            _used = 0;
            RawBits = 0;
        }
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
        var rawBits = _headers.RawBits;
        Clear(rawBits);
        _headers.ResetBuffer();
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
            _previousBits = (_bits | _pseudoBits) & ~rawBits;
        }

        // Mark no headers as currently in use
        _bits = 0;
        _pseudoBits = 0;
        // Clear ContentLength and any unknown headers as we will never reuse them
        _contentLength = null;
        _unknown?.Clear();
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

        // Per RFC 9110 §8.6, Content-Length is 1*DIGIT: only ASCII digits are allowed.
        // https://www.rfc-editor.org/rfc/rfc9110.html#section-8.6
        // Utf8Parser.TryParse accepts a leading '+' or '-' sign, so ensure the first byte is a digit.
        long parsed = 0;
        if (value.IsEmpty ||
            value[0] is < (byte)'0' or > (byte)'9' ||
            !Utf8Parser.TryParse(value, out parsed, out var consumed) ||
            parsed < 0 ||
            consumed != value.Length)
        {
            KestrelBadHttpRequestException.Throw(RequestRejectionReason.InvalidContentLength, value.GetRequestHeaderString(HeaderNames.ContentLength, EncodingSelector, checkForNewlineChars: false));
        }

        _contentLength = parsed;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [SkipLocalsInit]
    private unsafe void AppendContentLengthCustomEncoding(ReadOnlySpan<byte> value, Encoding customEncoding)
    {
        if (_contentLength.HasValue)
        {
            KestrelBadHttpRequestException.Throw(RequestRejectionReason.MultipleContentLengths);
        }

        // long.MaxValue = 9223372036854775807 (19 chars)
        Span<char> decodedChars = stackalloc char[20];
        var numChars = customEncoding.GetChars(value, decodedChars);
        long parsed = -1;

        // Per RFC 9110 §8.6, Content-Length is 1*DIGIT. NumberStyles.None
        // https://www.rfc-editor.org/rfc/rfc9110.html#section-8.6
        // disallows leading signs and whitespace that NumberStyles.Integer would accept.
        if (numChars > 19 ||
            !long.TryParse(decodedChars.Slice(0, numChars), NumberStyles.None, CultureInfo.InvariantCulture, out parsed) ||
            parsed < 0)
        {
            KestrelBadHttpRequestException.Throw(RequestRejectionReason.InvalidContentLength, value.GetRequestHeaderString(HeaderNames.ContentLength, EncodingSelector, checkForNewlineChars: false));
        }

        _contentLength = parsed;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void SetValueUnknown(string key, StringValues value)
    {
        ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(RequestUnknown, GetInternedHeaderName(key), out _);
        _headers.Discard(ref entry);
        entry.Value = value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool AddValueUnknown(string key, StringValues value)
    {
        RequestUnknown.Add(GetInternedHeaderName(key), new HeaderValue { Value = value });
        // Return true, above will throw and exit for false
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void AppendUnknownHeaders(string name, string valueString)
    {
        name = GetInternedHeaderName(name);
        ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(RequestUnknown, name, out _);
        var existing = _headers.Get(ref entry, 0);
        entry.Value = AppendValue(existing, valueString);
    }

    private void AppendUnknown(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value, bool checkForNewlineChars, bool allowLazy)
    {
        var nameString = name.GetHeaderName();
        if (allowLazy && CanStoreLazy(value, checkForNewlineChars) && !RequestUnknown.ContainsKey(nameString))
        {
            var entry = default(HeaderValue);
            if (_headers.TryStore(value, ref entry))
            {
                RequestUnknown.Add(GetInternedHeaderName(nameString), entry);
                return;
            }
        }
        AppendUnknownHeaders(nameString, value.GetRequestHeaderString(nameString, EncodingSelector, checkForNewlineChars));
    }

    protected override bool TryGetUnknown(string key, ref StringValues value)
    {
        if (_unknown is null)
        {
            return false;
        }
        ref var entry = ref CollectionsMarshal.GetValueRefOrNullRef(_unknown, key);
        if (Unsafe.IsNullRef(ref entry))
        {
            return false;
        }
        value = _headers.Get(ref entry, 0);
        return true;
    }

    protected override bool RemoveUnknown(string key)
    {
        if (_unknown is null || !_unknown.Remove(key, out var entry))
        {
            return false;
        }
        _headers.Discard(ref entry);
        return true;
    }

    private void CopyUnknownTo(KeyValuePair<string, StringValues>[] array, int index)
    {
        if (_unknown is null)
        {
            return;
        }
        ArgumentNullException.ThrowIfNull(array);
        if ((uint)index > (uint)array.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
        if (array.Length - index < _unknown.Count)
        {
            ThrowArgumentException();
        }
        foreach (var pair in _unknown)
        {
            StringValues value = default;
            TryGetUnknown(pair.Key, ref value);
            array[index++] = new(pair.Key, value);
        }
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
        private bool _hasUnknown;
        private Dictionary<string, HeaderValue>.Enumerator _unknownEnumerator;

        internal Enumerator(HttpRequestHeaders collection)
        {
            _collection = collection;
            _currentBits = collection._bits;
            _next = GetNext(_currentBits, collection.ContentLength.HasValue);
            _current = default;
            _hasUnknown = collection._unknown != null;
            _unknownEnumerator = _hasUnknown
                ? collection._unknown!.GetEnumerator()
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
            _current = default;
            _hasUnknown = _collection._unknown is not null;
            _unknownEnumerator = _hasUnknown ? _collection._unknown!.GetEnumerator() : default;
        }
    }
}
