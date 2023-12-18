// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.HttpSys.Internal;

[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(StringValuesDictionaryDebugView))]
internal sealed partial class RequestHeaders : IHeaderDictionary
{
    private IDictionary<string, StringValues>? _extra;
    private readonly NativeRequestContext _requestMemoryBlob;
    private long? _contentLength;
    private StringValues _contentLengthText;

    internal RequestHeaders(NativeRequestContext requestMemoryBlob)
    {
        _requestMemoryBlob = requestMemoryBlob;
    }

    public bool IsReadOnly { get; internal set; }

    private IDictionary<string, StringValues> Extra
    {
        get
        {
            if (_extra == null)
            {
                var newDict = new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase);
                GetUnknownHeaders(newDict);
                Interlocked.CompareExchange(ref _extra, newDict, null);
            }
            return _extra;
        }
    }

    StringValues IDictionary<string, StringValues>.this[string key]
    {
        get
        {
            StringValues value;
            return PropertiesTryGetValue(key, out value) ? value : Extra[key];
        }
        set
        {
            ThrowIfReadOnly();
            if (!PropertiesTrySetValue(key, value))
            {
                Extra[key] = value;
            }
        }
    }

    private string? GetKnownHeader(HttpSysRequestHeader header)
    {
        return _requestMemoryBlob.GetKnownHeader(header);
    }

    private void GetUnknownHeaders(IDictionary<string, StringValues> extra)
    {
        _requestMemoryBlob.GetUnknownHeaders(extra);
    }

    void IDictionary<string, StringValues>.Add(string key, StringValues value)
    {
        if (ContainsKey(key))
        {
            ThrowDuplicateKeyException();
        }

        if (!PropertiesTrySetValue(key, value))
        {
            Extra.Add(key, value);
        }
    }

    public bool ContainsKey(string key)
    {
        return PropertiesContainsKey(key) || Extra.ContainsKey(key);
    }

    public ICollection<string> Keys
    {
        get
        {
            var destination = new string[Count];
            int knownHeadersCount = GetKnownHeadersKeys(destination);
            if (_extra != null)
            {
                foreach (var item in _extra)
                {
                    destination[knownHeadersCount++] = item.Key;
                }
            }
            else
            {
                _requestMemoryBlob.GetUnknownKeys(destination.AsSpan(knownHeadersCount));
            }
            return destination;
        }
    }

    ICollection<StringValues> IDictionary<string, StringValues>.Values
    {
        get { return PropertiesValues().Concat(Extra.Values).ToArray(); }
    }

    public int Count
    {
        get
        {
            int count = GetKnownHeadersCount();
            count += _extra != null ? _extra.Count : _requestMemoryBlob.CountUnknownHeaders();
            return count;
        }
    }

    public bool Remove(string key)
    {
        // Although this is a mutating operation, Extra is used instead of StrongExtra,
        // because if a real dictionary has not been allocated the default behavior of the
        // nil dictionary is perfectly fine.
        return PropertiesTryRemove(key) || Extra.Remove(key);
    }

    public bool TryGetValue(string key, out StringValues value)
    {
        return PropertiesTryGetValue(key, out value) || Extra.TryGetValue(key, out value);
    }

    internal void ResetFlags()
    {
        _flag0 = 0;
        _flag1 = 0;
        _extra = null;
    }

    void ICollection<KeyValuePair<string, StringValues>>.Add(KeyValuePair<string, StringValues> item)
    {
        ((IDictionary<string, StringValues>)this).Add(item.Key, item.Value);
    }

    void ICollection<KeyValuePair<string, StringValues>>.Clear()
    {
        foreach (var key in PropertiesKeys())
        {
            PropertiesTryRemove(key);
        }
        Extra.Clear();
    }

    bool ICollection<KeyValuePair<string, StringValues>>.Contains(KeyValuePair<string, StringValues> item)
    {
        return ((IDictionary<string, StringValues>)this).TryGetValue(item.Key, out var value) && Equals(value, item.Value);
    }

    void ICollection<KeyValuePair<string, StringValues>>.CopyTo(KeyValuePair<string, StringValues>[] array, int arrayIndex)
    {
        PropertiesEnumerable().Concat(Extra).ToArray().CopyTo(array, arrayIndex);
    }

    bool ICollection<KeyValuePair<string, StringValues>>.IsReadOnly
    {
        get { return false; }
    }

    long? IHeaderDictionary.ContentLength
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

    public StringValues this[string key]
    {
        get
        {
            return TryGetValue(key, out var values) ? values : StringValues.Empty;
        }
        set
        {
            if (value.Count == 0)
            {
                Remove(key);
            }
            else if (!PropertiesTrySetValue(key, value))
            {
                Extra[key] = value;
            }
        }
    }

    bool ICollection<KeyValuePair<string, StringValues>>.Remove(KeyValuePair<string, StringValues> item)
    {
        return ((IDictionary<string, StringValues>)this).Contains(item) &&
            ((IDictionary<string, StringValues>)this).Remove(item.Key);
    }

    IEnumerator<KeyValuePair<string, StringValues>> IEnumerable<KeyValuePair<string, StringValues>>.GetEnumerator()
    {
        return PropertiesEnumerable().Concat(Extra).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IDictionary<string, StringValues>)this).GetEnumerator();
    }

    private void ThrowIfReadOnly()
    {
        if (IsReadOnly)
        {
            throw new InvalidOperationException("The response headers cannot be modified because the response has already started.");
        }
    }

    private static void ThrowDuplicateKeyException()
    {
        throw new ArgumentException("An item with the same key has already been added.");
    }

    public IEnumerable<string> GetValues(string key)
    {
        StringValues values;
        if (TryGetValue(key, out values))
        {
            return HeaderParser.SplitValues(values);
        }
        return HeaderParser.Empty;
    }

    private int GetKnownHeadersKeys(Span<string> observedHeaders)
    {
        int observedHeadersCount = 0;
        for (int i = 0; i < HeaderKeys.Length; i++)
        {
            var header = HeaderKeys[i];
            if (HasKnownHeader(header))
            {
                observedHeaders[observedHeadersCount++] = GetHeaderKeyName(header);
            }
        }
        return observedHeadersCount;
    }

    private int GetKnownHeadersCount()
    {
        int observedHeadersCount = 0;
        for (int i = 0; i < HeaderKeys.Length; i++)
        {
            var header = HeaderKeys[i];
            if (HasKnownHeader(header))
            {
                observedHeadersCount++;
            }
        }
        return observedHeadersCount;
    }
}
