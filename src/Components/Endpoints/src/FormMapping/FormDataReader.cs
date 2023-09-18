// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

[DebuggerDisplay($"{{{nameof(DebuggerDisplay)}(),nq}}")]
internal struct FormDataReader : IDisposable
{
    private readonly IReadOnlyDictionary<FormKey, StringValues> _readOnlyMemoryKeys;
    private readonly Memory<char> _prefixBuffer;
    private Memory<char> _currentPrefixBuffer;
    private int _currentDepth;
    private int _errorCount;

    // As an implementation detail, reuse FormKey for the values.
    // It's just a thin wrapper over ReadOnlyMemory<char> that caches
    // the computed hash code.
    private IReadOnlyDictionary<FormKey, HashSet<FormKey>>? _formDictionaryKeysByPrefix;

    private PrefixResolver _prefixResolver;

    public FormDataReader(IReadOnlyDictionary<FormKey, StringValues> formCollection, CultureInfo culture, Memory<char> buffer)
    {
        _readOnlyMemoryKeys = formCollection;
        Culture = culture;
        _prefixBuffer = buffer;
    }

    public FormDataReader(IReadOnlyDictionary<FormKey, StringValues> formCollection, CultureInfo culture, Memory<char> buffer, IFormFileCollection formFileCollection)
        : this(formCollection, culture, buffer)
    {
        FormFileCollection = formFileCollection;
    }

    internal ReadOnlyMemory<char> CurrentPrefix => _currentPrefixBuffer;

    public IFormatProvider Culture { get; }

    public IFormFileCollection? FormFileCollection { get; internal set; }

    public int MaxRecursionDepth { get; set; } = 64;

    public Action<string, FormattableString, string?>? ErrorHandler { get; set; }

    public Action<string, object>? AttachInstanceToErrorsHandler { get; set; }

    public int MaxErrorCount { get; set; } = 200;

    public void AddMappingError(FormattableString errorMessage, string? attemptedValue)
    {
        ArgumentNullException.ThrowIfNull(errorMessage);

        if (ErrorHandler == null)
        {
            throw new FormDataMappingException(new FormDataMappingError(_currentPrefixBuffer.ToString(), errorMessage, attemptedValue));
        }

        _errorCount++;
        if (_errorCount == MaxErrorCount)
        {
            ErrorHandler.Invoke(
                _currentPrefixBuffer.ToString(),
                FormattableStringFactory.Create($"Maximum number of errors ({MaxErrorCount}) reached. Further errors will be suppressed."),
                null);
        }

        if (_errorCount >= MaxErrorCount)
        {
            return;
        }

        ErrorHandler.Invoke(_currentPrefixBuffer.ToString(), errorMessage, attemptedValue);
    }

    public void AddMappingError(Exception exception, string? attemptedValue)
    {
        ArgumentNullException.ThrowIfNull(exception);

        // Avoid re-wrapping the exception if it is already a FormDataMappingException
        // and we don't have an ErrorHandler configured.
        if (exception is FormDataMappingException && ErrorHandler == null)
        {
            throw exception;
        }

        var errorMessage = FormattableStringFactory.Create(exception.Message);
        AddMappingError(errorMessage, attemptedValue);
    }

    public void AttachInstanceToErrors(object value)
    {
        if (AttachInstanceToErrorsHandler == null)
        {
            return;
        }

        AttachInstanceToErrorsHandler(_currentPrefixBuffer.ToString(), value);
    }

    internal FormKeyCollection GetKeys()
    {
        if (_formDictionaryKeysByPrefix == null)
        {
            _formDictionaryKeysByPrefix = ProcessFormKeys();
        }

        if (_formDictionaryKeysByPrefix.TryGetValue(new FormKey(_currentPrefixBuffer), out var foundKeys))
        {
            return new FormKeyCollection(foundKeys);
        }

        return FormKeyCollection.Empty;
    }

    // Internal for testing purposes
    internal IReadOnlyDictionary<FormKey, HashSet<FormKey>> ProcessFormKeys()
    {
        var keys = _readOnlyMemoryKeys.Keys;
        var result = new Dictionary<FormKey, HashSet<FormKey>>();
        // We need to iterate over all the keys in the dictionary and process each key to split it into segments where
        // the prefixes are string separated by . and the keys are enclosed in []. For example if the key is
        // Customer.Orders[<<OrderId>>]BillingInfo.FirstName, then we need to split it into Customer.Orders,
        // [<<OrderId>>] and BillingInfo.FirstName. We then, need to group all the keys by the prefix. So, for the
        // above example, we will have an entry for the prefix Customer.Orders that will include [<<OrderId>>] as the
        // key.

        foreach (var key in keys)
        {
            var startIndex = key.Value.Span.IndexOf('[');
            while (startIndex >= 0)
            {
                var endIndex = key.Value.Span[startIndex..].IndexOf(']') + startIndex;
                if (endIndex == -1)
                {
                    // Ignore malformed keys
                    break;
                }

                var prefix = key.Value[..startIndex];
                var keyValue = key.Value[startIndex..(endIndex + 1)];
                if (result.TryGetValue(new FormKey(prefix), out var foundKeys))
                {
                    foundKeys.Add(new FormKey(keyValue));
                }
                else
                {
                    result.Add(new FormKey(prefix), new HashSet<FormKey> { new FormKey(keyValue) });
                }

                var nextOpenBracket = key.Value.Span[(endIndex + 1)..].IndexOf('[');

                startIndex = nextOpenBracket != -1 ? endIndex + 1 + nextOpenBracket : -1;
            }
        }

        return result;
    }

    // This only ever gets invoked if we have a recursive type.
    // Recursive types make an existential check for the current prefix to determine if they
    // need to continue mapping data.
    // At that point, we are placing the keys into a sorted array, and we use binary search to
    // determine if the prefix exists.
    // We only make this search on recursive paths, as opposed to in every new scope that we push.
    internal bool CurrentPrefixExists()
    {
        if (CurrentPrefix.Span.Length == 0)
        {
            // Avoid creating the prefix array at the root level.
            return _readOnlyMemoryKeys.Count > 0;
        }

        if (!_prefixResolver.HasValues)
        {
            _prefixResolver = new PrefixResolver(_readOnlyMemoryKeys.Keys, _readOnlyMemoryKeys.Count);
        }

        return _prefixResolver.HasPrefix(_currentPrefixBuffer);
    }

    internal void PopPrefix(string key)
    {
        PopPrefix(key.AsSpan());
    }

    internal void PopPrefix(ReadOnlySpan<char> key)
    {
        if (_currentDepth > MaxRecursionDepth)
        {
            return;
        }

        _currentDepth--;
        Debug.Assert(_currentDepth >= 0);
        var keyLength = key.Length;
        // If keyLength is bigger than the current scope keyLength typically means there is a
        // bug where some part of the code has not popped the scope appropriately.
        Debug.Assert(_currentPrefixBuffer.Length >= keyLength);
        if (_currentPrefixBuffer.Length == keyLength || _currentPrefixBuffer.Span[^(keyLength + 1)] != '.')
        {
            _currentPrefixBuffer = _currentPrefixBuffer[..^keyLength];
        }
        else
        {
            _currentPrefixBuffer = _currentPrefixBuffer[..^(keyLength + 1)];
        }
    }

    internal void PushPrefix(string key)
    {
        PushPrefix(key.AsSpan());
    }

    internal void PushPrefix(scoped ReadOnlySpan<char> key)
    {
        _currentDepth++;
        // We automatically append a "." before adding the suffix, except when its the first element pushed to the
        // scope, or when we are accessing a property after a collection or an indexer like items[1].
        var separator = _currentPrefixBuffer.Length > 0 && key[0] != '['
            ? ".".AsSpan()
            : "".AsSpan();
        if (_currentDepth > MaxRecursionDepth)
        {
            throw new InvalidOperationException($"The maximum recursion depth of '{MaxRecursionDepth}' was exceeded for '{_currentPrefixBuffer}{separator}{key}'.");
        }

        Debug.Assert(_prefixBuffer.Length >= (_currentPrefixBuffer.Length + separator.Length));

        separator.CopyTo(_prefixBuffer.Span[_currentPrefixBuffer.Length..]);

        var startingPoint = _currentPrefixBuffer.Length + separator.Length;
        _currentPrefixBuffer = _prefixBuffer.Slice(0, startingPoint + key.Length);
        key.CopyTo(_prefixBuffer[startingPoint..].Span);
    }

    internal readonly bool TryGetValue([NotNullWhen(true)] out string? value)
    {
        var foundSingleValue = _readOnlyMemoryKeys.TryGetValue(new FormKey(_currentPrefixBuffer), out var result) || result.Count == 1;
        if (foundSingleValue)
        {
            value = result[0];
        }
        else
        {
            value = null;
        }

        return foundSingleValue;
    }

    internal readonly bool TryGetValues(out StringValues values) =>
        _readOnlyMemoryKeys.TryGetValue(new FormKey(_currentPrefixBuffer), out values);

    internal string GetPrefix() => _currentPrefixBuffer.ToString();

    internal string GetLastPrefixSegment()
    {
        var index = _currentPrefixBuffer.Span.LastIndexOfAny(".[");
        if (index == -1)
        {
            return _currentPrefixBuffer.ToString();
        }
        if (_currentPrefixBuffer.Span[index] == '.')
        {
            return _currentPrefixBuffer.Span[(index + 1)..].ToString();
        }
        else
        {
            // Return the value without the closing bracket ]
            return _currentPrefixBuffer.Span[(index + 1)..^1].ToString();
        }
    }

    public void Dispose()
    {
        _prefixResolver.Dispose();
    }

    internal readonly struct FormKeyCollection : IEnumerable<ReadOnlyMemory<char>>
    {
        private readonly HashSet<FormKey> _values;
        internal static readonly FormKeyCollection Empty;

        public bool HasValues() => _values != null;

        public FormKeyCollection(HashSet<FormKey> values) => _values = values;

        public Enumerator GetEnumerator() => new Enumerator(_values.GetEnumerator());

        IEnumerator<ReadOnlyMemory<char>> IEnumerable<ReadOnlyMemory<char>>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        internal struct Enumerator : IEnumerator<ReadOnlyMemory<char>>
        {
            private HashSet<FormKey>.Enumerator _enumerator;

            public Enumerator(HashSet<FormKey>.Enumerator enumerator)
            {
                _enumerator = enumerator;
            }

            public ReadOnlyMemory<char> Current => _enumerator.Current.Value;

            object IEnumerator.Current => _enumerator.Current;

            void IDisposable.Dispose() => _enumerator.Dispose();

            public bool MoveNext() => _enumerator.MoveNext();

            void IEnumerator.Reset() { }
        }
    }

    private readonly string DebuggerDisplay =>
        $"Key count = {_readOnlyMemoryKeys.Count}, Prefix = {_currentPrefixBuffer}, Error count = {_errorCount}, Current depth = {_currentDepth}";
}
