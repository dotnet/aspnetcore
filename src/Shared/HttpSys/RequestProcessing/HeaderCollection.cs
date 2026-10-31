// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.HttpSys.Internal;

[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(HeaderCollectionDebugView))]
internal sealed class HeaderCollection : IHeaderDictionary
{
    // https://tools.ietf.org/html/rfc7230#section-4.1.2
    internal static readonly HashSet<string> DisallowedTrailers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Message framing headers.
            HeaderNames.TransferEncoding, HeaderNames.ContentLength,

            // Routing headers.
            HeaderNames.Host,

            // Request modifiers: controls and conditionals.
            // rfc7231#section-5.1: Controls.
            HeaderNames.CacheControl, HeaderNames.Expect, HeaderNames.MaxForwards, HeaderNames.Pragma, HeaderNames.Range, HeaderNames.TE,

            // rfc7231#section-5.2: Conditionals.
            HeaderNames.IfMatch, HeaderNames.IfNoneMatch, HeaderNames.IfModifiedSince, HeaderNames.IfUnmodifiedSince, HeaderNames.IfRange,

            // Authentication headers.
            HeaderNames.WWWAuthenticate, HeaderNames.Authorization, HeaderNames.ProxyAuthenticate, HeaderNames.ProxyAuthorization, HeaderNames.SetCookie, HeaderNames.Cookie,

            // Response control data.
            // rfc7231#section-7.1: Control Data.
            HeaderNames.Age, HeaderNames.Expires, HeaderNames.Date, HeaderNames.Location, HeaderNames.RetryAfter, HeaderNames.Vary, HeaderNames.Warning,

            // Content-Encoding, Content-Type, Content-Range, and Trailer itself.
            HeaderNames.ContentEncoding, HeaderNames.ContentType, HeaderNames.ContentRange, HeaderNames.Trailer
        };

    // Should this instance check for prohibited trailers?
    private readonly bool _checkTrailers;
    private long? _contentLength;
    private StringValues _contentLengthText;

    public HeaderCollection(bool checkTrailers = false)
        : this(new Dictionary<string, StringValues>(4, StringComparer.OrdinalIgnoreCase))
    {
        _checkTrailers = checkTrailers;
    }

    public HeaderCollection(IDictionary<string, StringValues> store)
    {
        Store = store;
    }

    private IDictionary<string, StringValues> Store { get; set; }

    // Readonly after the response has been started.
    public bool IsReadOnly { get; internal set; }

    public StringValues this[string key]
    {
        get
        {
            StringValues values;
            return TryGetValue(key, out values) ? values : StringValues.Empty;
        }
        set
        {
            ValidateRestrictedTrailers(key);
            ThrowIfReadOnly();
            if (StringValues.IsNullOrEmpty(value))
            {
                Remove(key);
            }
            else
            {
                ValidateHeaderCharacters(key);
                ValidateHeaderCharacters(value);
                Store[key] = value;
            }
        }
    }

    StringValues IDictionary<string, StringValues>.this[string key]
    {
        get { return Store[key]; }
        set
        {
            ValidateRestrictedTrailers(key);
            ThrowIfReadOnly();
            ValidateHeaderCharacters(key);
            ValidateHeaderCharacters(value);
            Store[key] = value;
        }
    }

    public int Count
    {
        get { return Store.Count; }
    }

    public ICollection<string> Keys
    {
        get { return Store.Keys; }
    }

    public ICollection<StringValues> Values
    {
        get { return Store.Values; }
    }

    public long? ContentLength
    {
        get
        {
            long value;
            var rawValue = this[HeaderNames.ContentLength];

            if (_contentLengthText.Equals(rawValue))
            {
                return _contentLength;
            }

            if (rawValue.Count == 1 &&
                !string.IsNullOrWhiteSpace(rawValue[0]) &&
                HeaderUtilities.TryParseNonNegativeInt64(new StringSegment(rawValue[0]).Trim(), out value))
            {
                _contentLengthText = rawValue;
                _contentLength = value;
                return value;
            }

            return null;
        }
        set
        {
            ValidateRestrictedTrailers(HeaderNames.ContentLength);
            ThrowIfReadOnly();

            if (value.HasValue)
            {
                if (value.Value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value.Value, "Cannot be negative.");
                }
                _contentLengthText = HeaderUtilities.FormatNonNegativeInt64(value.Value);
                this[HeaderNames.ContentLength] = _contentLengthText;
                _contentLength = value;
            }
            else
            {
                Remove(HeaderNames.ContentLength);
                _contentLengthText = StringValues.Empty;
                _contentLength = null;
            }
        }
    }

    public void Add(KeyValuePair<string, StringValues> item)
    {
        ValidateRestrictedTrailers(item.Key);
        ThrowIfReadOnly();
        ValidateHeaderCharacters(item.Key);
        ValidateHeaderCharacters(item.Value);
        Store.Add(item);
    }

    public void Add(string key, StringValues value)
    {
        ValidateRestrictedTrailers(key);
        ThrowIfReadOnly();
        ValidateHeaderCharacters(key);
        ValidateHeaderCharacters(value);
        Store.Add(key, value);
    }

    public void Append(string key, string value)
    {
        ValidateRestrictedTrailers(key);
        ThrowIfReadOnly();
        ValidateHeaderCharacters(key);
        ValidateHeaderCharacters(value);
        StringValues values;
        Store.TryGetValue(key, out values);
        Store[key] = StringValues.Concat(values, value);
    }

    public void Clear()
    {
        ThrowIfReadOnly();
        Store.Clear();
    }

    public bool Contains(KeyValuePair<string, StringValues> item)
    {
        return Store.Contains(item);
    }

    public bool ContainsKey(string key)
    {
        return Store.ContainsKey(key);
    }

    public void CopyTo(KeyValuePair<string, StringValues>[] array, int arrayIndex)
    {
        Store.CopyTo(array, arrayIndex);
    }

    public IEnumerator<KeyValuePair<string, StringValues>> GetEnumerator()
    {
        return Store.GetEnumerator();
    }

    public IEnumerable<string> GetValues(string key)
    {
        StringValues values;
        if (Store.TryGetValue(key, out values))
        {
            return HeaderParser.SplitValues(values);
        }
        return HeaderParser.Empty;
    }

    public bool Remove(KeyValuePair<string, StringValues> item)
    {
        ThrowIfReadOnly();
        return Store.Remove(item);
    }

    public bool Remove(string key)
    {
        ThrowIfReadOnly();
        return Store.Remove(key);
    }

    public bool TryGetValue(string key, out StringValues value)
    {
        return Store.TryGetValue(key, out value);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private void ThrowIfReadOnly()
    {
        if (IsReadOnly)
        {
            if (_checkTrailers)
            {
                throw new InvalidOperationException("The response trailers cannot be modified because the response has already completed. "
                    + "If this is a Content-Length response then you need to call HttpResponse.DeclareTrailer before starting the body.");
            }
            throw new InvalidOperationException("The response headers cannot be modified because the response has already started.");
        }
    }

    public static void ValidateHeaderCharacters(StringValues headerValues)
    {
        foreach (var value in headerValues)
        {
            ValidateHeaderCharacters(value!);
        }
    }

    public static void ValidateHeaderCharacters(string headerCharacters)
    {
        if (headerCharacters != null)
        {
            var invalidIndex = HttpCharacters.IndexOfInvalidFieldValueCharExtended(headerCharacters);
            if (invalidIndex >= 0)
            {
                Throw(headerCharacters, invalidIndex);
                static void Throw(string headerCharacters, int invalidIndex)
                    => throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "Invalid control character in header: 0x{0:X2}", headerCharacters[invalidIndex]));
            }
        }
    }

    private void ValidateRestrictedTrailers(string key)
    {
        if (_checkTrailers && DisallowedTrailers.Contains(key))
        {
            throw new InvalidOperationException($"The '{key}' header is not allowed in HTTP trailers.");
        }
    }

    private sealed class HeaderCollectionDebugView(HeaderCollection collection)
    {
        private readonly HeaderCollection _collection = collection;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public KeyValuePair<string, string>[] Items => _collection.Select(pair => new KeyValuePair<string, string>(pair.Key, pair.Value.ToString())).ToArray();
    }
}
