// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
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

    public int MaxCollectionSize { get; set; } = FormReader.DefaultValueCountLimit;

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
        // Scan the input dictionary for keys matching the current prefix followed by a bracket segment.
        // This avoids building a large upfront dictionary of all prefix→key mappings.
        var prefix = _currentPrefixBuffer;
        var result = new HashSet<FormKey>();

        foreach (var kvp in _readOnlyMemoryKeys)
        {
            var key = kvp.Key.Value;

            // The key must start with the current prefix (case-insensitive).
            if (key.Length <= prefix.Length ||
                !key.Span[..prefix.Length].Equals(prefix.Span, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Immediately after the prefix, there must be a '['.
            if (key.Span[prefix.Length] != '[')
            {
                continue;
            }

            // Find the closing ']' after the '['.
            var remaining = key[prefix.Length..];
            var closeIndex = remaining.Span[1..].IndexOf(']');
            if (closeIndex == -1)
            {
                // Malformed key — no closing bracket. Skip it.
                continue;
            }

            // Extract "[value]" (closeIndex is relative to position 1, so add 2 for the full segment).
            var segment = remaining[..(closeIndex + 2)];
            result.Add(new FormKey(segment));

            // Allow one extra item through so collection/dictionary binding can still detect overflow
            // and report the existing max-size error instead of silently truncating the input.
            if (result.Count > MaxCollectionSize)
            {
                break;
            }
        }

        return result.Count > 0
            ? new FormKeyCollection(result)
            : FormKeyCollection.Empty;
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
