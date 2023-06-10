// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Provides extension methods for the <see cref="NavigationManager"/> type.
/// </summary>
public static class NavigationManagerExtensions
{
    private const string EmptyQueryParameterNameExceptionMessage = "Cannot have empty query parameter names.";

    private delegate string? QueryParameterFormatter<TValue>(TValue value);

    // We don't include mappings for Nullable types because we explicitly check for null values
    // to see if the parameter should be excluded from the querystring. Therefore, we will only
    // invoke these formatters for non-null values. We also get the underlying type of any Nullable
    // types before performing lookups in this dictionary.
    private static readonly Dictionary<Type, QueryParameterFormatter<object>> _queryParameterFormatters = new()
    {
        [typeof(string)] = value => Format((string)value)!,
        [typeof(bool)] = value => Format((bool)value),
        [typeof(DateTime)] = value => Format((DateTime)value),
        [typeof(DateOnly)] = value => Format((DateOnly)value),
        [typeof(TimeOnly)] = value => Format((TimeOnly)value),
        [typeof(decimal)] = value => Format((decimal)value),
        [typeof(double)] = value => Format((double)value),
        [typeof(float)] = value => Format((float)value),
        [typeof(Guid)] = value => Format((Guid)value),
        [typeof(int)] = value => Format((int)value),
        [typeof(long)] = value => Format((long)value),
    };

    private static string? Format(string? value)
        => value;

    private static string Format(bool value)
        => value.ToString(CultureInfo.InvariantCulture);

    private static string? Format(bool? value)
        => value?.ToString(CultureInfo.InvariantCulture);

    private static string Format(DateTime value)
        => value.ToString(CultureInfo.InvariantCulture);

    private static string? Format(DateTime? value)
        => value?.ToString(CultureInfo.InvariantCulture);

    private static string Format(DateOnly value)
        => value.ToString(CultureInfo.InvariantCulture);

    private static string? Format(DateOnly? value)
        => value?.ToString(CultureInfo.InvariantCulture);

    private static string Format(TimeOnly value)
        => value.ToString(CultureInfo.InvariantCulture);

    private static string? Format(TimeOnly? value)
        => value?.ToString(CultureInfo.InvariantCulture);

    private static string Format(decimal value)
        => value.ToString(CultureInfo.InvariantCulture);

    private static string? Format(decimal? value)
        => value?.ToString(CultureInfo.InvariantCulture);

    private static string Format(double value)
        => value.ToString(CultureInfo.InvariantCulture);

    private static string? Format(double? value)
        => value?.ToString(CultureInfo.InvariantCulture);

    private static string Format(float value)
        => value.ToString(CultureInfo.InvariantCulture);

    private static string? Format(float? value)
        => value?.ToString(CultureInfo.InvariantCulture);

    private static string Format(Guid value)
        => value.ToString(null, CultureInfo.InvariantCulture);

    private static string? Format(Guid? value)
        => value?.ToString(null, CultureInfo.InvariantCulture);

    private static string Format(int value)
        => value.ToString(CultureInfo.InvariantCulture);

    private static string? Format(int? value)
        => value?.ToString(CultureInfo.InvariantCulture);

    private static string Format(long value)
        => value.ToString(CultureInfo.InvariantCulture);

    private static string? Format(long? value)
        => value?.ToString(CultureInfo.InvariantCulture);

    // Used for constructing a URI with a new querystring from an existing URI.
    private struct QueryStringBuilder
    {
        private readonly StringBuilder _builder;

        private bool _hasNewParameters;
        private bool _hasHash;

        public string UriWithQueryString => _builder.ToString();

        public QueryStringBuilder(ReadOnlySpan<char> uriWithoutQueryStringAndHash, int additionalCapacity = 0)
        {
            _builder = new(uriWithoutQueryStringAndHash.Length + additionalCapacity);
            _builder.Append(uriWithoutQueryStringAndHash);

            _hasNewParameters = false;
            _hasHash = false;
        }

        public void AppendParameter(ReadOnlySpan<char> encodedName, ReadOnlySpan<char> encodedValue)
        {
            if (_hasHash)
            {
                throw new InvalidOperationException("Cannot append parameter after hash was added.");
            }

            if (!_hasNewParameters)
            {
                _hasNewParameters = true;
                _builder.Append('?');
            }
            else
            {
                _builder.Append('&');
            }

            _builder.Append(encodedName);
            _builder.Append('=');
            _builder.Append(encodedValue);
        }

        public void AppendHash(ReadOnlySpan<char> hash)
        {
            _hasHash = true;
            _builder.Append(hash);
        }
    }

    // A utility for feeding a collection of parameter values into a QueryStringBuilder.
    // This is used when generating a querystring with a query parameter that has multiple values.
    private readonly struct QueryParameterSource<TValue>
    {
        private readonly IEnumerator<TValue?>? _enumerator;
        private readonly QueryParameterFormatter<TValue>? _formatter;

        public string EncodedName { get; }

        // Creates an empty instance to simulate a source without any elements.
        public QueryParameterSource(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new InvalidOperationException(EmptyQueryParameterNameExceptionMessage);
            }

            EncodedName = Uri.EscapeDataString(name);

            _enumerator = default;
            _formatter = default;
        }

        public QueryParameterSource(string name, IEnumerable<TValue?> values, QueryParameterFormatter<TValue> formatter)
            : this(name)
        {
            _enumerator = values.GetEnumerator();
            _formatter = formatter;
        }

        public bool TryAppendNextParameter(ref QueryStringBuilder builder)
        {
            if (_enumerator is null || !_enumerator.MoveNext())
            {
                return false;
            }

            var currentValue = _enumerator.Current;

            if (currentValue is null)
            {
                // No-op to simulate appending a null parameter.
                return true;
            }

            var formattedValue = _formatter!(currentValue);
            var encodedValue = Uri.EscapeDataString(formattedValue!);
            builder.AppendParameter(EncodedName, encodedValue);
            return true;
        }
    }

    // A utility for feeding an object of unknown type as one or more parameter values into
    // a QueryStringBuilder.
    private struct QueryParameterSource
    {
        private readonly QueryParameterSource<object> _source;
        private string? _encodedValue;

        public string EncodedName => _source.EncodedName;

        public QueryParameterSource(string name, object? value)
        {
            if (value is null)
            {
                _source = new(name);
                _encodedValue = default;
                return;
            }

            var valueType = value.GetType();

            if (valueType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(valueType))
            {
                // The provided value was of enumerable type, so we populate the underlying source.
                var elementType = valueType.GetElementType()!;
                var formatter = GetFormatterFromParameterValueType(elementType);

                // This cast is inevitable; the values have to be boxed anyway to be formatted.
                var values = ((IEnumerable)value).Cast<object>();

                _source = new(name, values, formatter);
                _encodedValue = default;
            }
            else
            {
                // The provided value was not of enumerable type, so we leave the underlying source
                // empty and instead cache the encoded value to be appended later.
                var formatter = GetFormatterFromParameterValueType(valueType);
                var formattedValue = formatter(value);
                _source = new(name);
                _encodedValue = Uri.EscapeDataString(formattedValue!);
            }
        }

        public bool TryAppendNextParameter(ref QueryStringBuilder builder)
        {
            if (_source.TryAppendNextParameter(ref builder))
            {
                // The underlying source of values had elements, so there is no more work to do here.
                return true;
            }

            // Either we've run out of elements to append or the given value was not of enumerable
            // type in the first place.

            // If the value was not of enumerable type and has not been appended, append it
            // and set it to null so we don't provide the value more than once.
            if (_encodedValue is not null)
            {
                builder.AppendParameter(_source.EncodedName, _encodedValue);
                _encodedValue = null;
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Returns a URI that is constructed by updating <see cref="NavigationManager.Uri"/> with a single parameter
    /// added or updated.
    /// </summary>
    /// <param name="navigationManager">The <see cref="NavigationManager"/>.</param>
    /// <param name="name">The name of the parameter to add or update.</param>
    /// <param name="value">The value of the parameter to add or update.</param>
    public static string GetUriWithQueryParameter(this NavigationManager navigationManager, string name, bool value)
        => GetUriWithQueryParameter(navigationManager, name, Format(value));

    /// <summary>
    /// Returns a URI that is constructed by updating <see cref="NavigationManager.Uri"/> with a single parameter
    /// added, updated, or removed.
    /// </summary>
    /// <param name="navigationManager">The <see cref="NavigationManager"/>.</param>
    /// <param name="name">The name of the parameter to add or update.</param>
    /// <param name="value">The value of the parameter to add or update.</param>
    /// <remarks>
    /// If <paramref name="value"/> is <c>null</c>, the parameter will be removed if it exists in the URI.
    /// Otherwise, it will be added or updated.
    /// </remarks>
    public static string GetUriWithQueryParameter(this NavigationManager navigationManager, string name, bool? value)
        => GetUriWithQueryParameter(navigationManager, name, Format(value));

    /// <summary>
    /// Returns a URI that is constructed by updating <see cref="NavigationManager.Uri"/> with a single parameter
    /// added or updated.
    /// </summary>
    /// <param name="navigationManager">The <see cref="NavigationManager"/>.</param>
    /// <param name="name">The name of the parameter to add or update.</param>
    /// <param name="value">The value of the parameter to add or update.</param>
    public static string GetUriWithQueryParameter(this NavigationManager navigationManager, string name, DateTime value)
        => GetUriWithQueryParameter(navigationManager, name, Format(value));

    /// <summary>
    /// Returns a URI that is constructed by updating <see cref="NavigationManager.Uri"/> with a single parameter
    /// added, updated, or removed.
    /// </summary>
    /// <param name="navigationManager">The <see cref="NavigationManager"/>.</param>
    /// <param name="name">The name of the parameter to add or update.</param>
    /// <param name="value">The value of the parameter to add or update.</param>
    /// <remarks>
    /// If <paramref name="value"/> is <c>null</c>, the parameter will be removed if it exists in the URI.
    /// Otherwise, it will be added or updated.
    /// </remarks>
    public static string GetUriWithQueryParameter(this NavigationManager navigationManager, string name, DateTime? value)
        => GetUriWithQueryParameter(navigationManager, name, Format(value));

    /// <summary>
    /// Returns a URI that is constructed by updating <see cref="NavigationManager.Uri"/> with a single parameter
    /// added or updated.
    /// </summary>
    /// <param name="navigationManager">The <see cref="NavigationManager"/>.</param>
    /// <param name="name">The name of the parameter to add or update.</param>
    /// <param name="value">The value of the parameter to add or update.</param>
    public static string GetUriWithQueryParameter(this NavigationManager navigationManager, string name, DateOnly value)
        => GetUriWithQueryParameter(navigationManager, name, Format(value));

    /// <summary>
    /// Returns a URI that is constructed by updating <see cref="NavigationManager.Uri"/> with a single parameter
    /// added, updated, or removed.
    /// </summary>
    /// <param name="navigationManager">The <see cref="NavigationManager"/>.</param>
    /// <param name="name">The name of the parameter to add or update.</param>
    /// <param name="value">The value of the parameter to add or update.</param>
    /// <remarks>
    /// If <paramref name="value"/> is <c>null</c>, the parameter will be removed if it exists in the URI.
    /// Otherwise, it will be added or updated.
    /// </remarks>
    public static string GetUriWithQueryParameter(this NavigationManager navigationManager, string name, DateOnly? value)
        => GetUriWithQueryParameter(navigationManager, name, Format(value));

    /// <summary>
    /// Returns a URI that is constructed by updating <see cref="NavigationManager.Uri"/> with a single parameter
    /// added or updated.
    /// </summary>
    /// <param name="navigationManager">The <see cref="NavigationManager"/>.</param>
    /// <param name="name">The name of the parameter to add or update.</param>
    /// <param name="value">The value of the parameter to add or update.</param>
    public static string GetUriWithQueryParameter(this NavigationManager navigationManager, string name, TimeOnly value)
        => GetUriWithQueryParameter(navigationManager, name, Format(value));

    /// <summary>
    /// Returns a URI that is constructed by updating <see cref="NavigationManager.Uri"/> with a single parameter
    /// added, updated, or removed.
    /// </summary>
    /// <param name="navigationManager">The <see cref="NavigationManager"/>.</param>
    /// <param name="name">The name of the parameter to add or update.</param>
    /// <param name="value">The value of the parameter to add or update.</param>
    /// <remarks>
    /// If <paramref name="value"/> is <c>null</c>, the parameter will be removed if it exists in the URI.
    /// Otherwise, it will be added or updated.
    /// </remarks>
    public static string GetUriWithQueryParameter(this NavigationManager navigationManager, string name, TimeOnly? value)
        => GetUriWithQueryParameter(navigationManager, name, Format(value));

    /// <summary>
    /// Returns a URI that is constructed by updating <see cref="NavigationManager.Uri"/> with a single parameter
    /// added or updated.
    /// </summary>
    /// <param name="navigationManager">The <see cref="NavigationManager"/>.</param>
    /// <param name="name">The name of the parameter to add or update.</param>
    /// <param name="value">The value of the parameter to add or update.</param>
    public static string GetUriWithQueryParameter(this NavigationManager navigationManager, string name, decimal value)
        => GetUriWithQueryParameter(navigationManager, name, Format(value));

    /// <summary>
    /// Returns a URI that is constructed by updating <see cref="NavigationManager.Uri"/> with a single parameter
    /// added, updated, or removed.
    /// </summary>
    /// <param name="navigationManager">The <see cref="NavigationManager"/>.</param>
    /// <param name="name">The name of the parameter to add or update.</param>
    /// <param name="value">The value of the parameter to add or update.</param>
    /// <remarks>
    /// If <paramref name="value"/> is <c>null</c>, the parameter will be removed if it exists in the URI.
    /// Otherwise, it will be added or updated.
    /// </remarks>
    public static string GetUriWithQueryParameter(this NavigationManager navigationManager, string name, decimal? value)
        => GetUriWithQueryParameter(navigationManager, name, Format(value));

    /// <summary>
    /// Returns a URI that is constructed by updating <see cref="NavigationManager.Uri"/> with a single parameter
    /// added or updated.
    /// </summary>
    /// <param name="navigationManager">The <see cref="NavigationManager"/>.</param>
    /// <param name="name">The name of the parameter to add or update.</param>
    /// <param name="value">The value of the parameter to add or update.</param>
    public static string GetUriWithQueryParameter(this NavigationManager navigationManager, string name, double value)
        => GetUriWithQueryParameter(navigationManager, name, Format(value));

    /// <summary>
    /// Returns a URI that is constructed by updating <see cref="NavigationManager.Uri"/> with a single parameter
    /// added, updated, or removed.
    /// </summary>
    /// <param name="navigationManager">The <see cref="NavigationManager"/>.</param>
    /// <param name="name">The name of the parameter to add or update.</param>
    /// <param name="value">The value of the parameter to add or update.</param>
    /// <remarks>
    /// If <paramref name="value"/> is <c>null</c>, the parameter will be removed if it exists in the URI.
    /// Otherwise, it will be added or updated.
    /// </remarks>
    public static string GetUriWithQueryParameter(this NavigationManager navigationManager, string name, double? value)
        => GetUriWithQueryParameter(navigationManager, name, Format(value));

    /// <summary>
    /// Returns a URI that is constructed by updating <see cref="NavigationManager.Uri"/> with a single parameter
    /// added or updated.
    /// </summary>
    /// <param name="navigationManager">The <see cref="NavigationManager"/>.</param>
    /// <param name="name">The name of the parameter to add or update.</param>
    /// <param name="value">The value of the parameter to add or update.</param>
    public static string GetUriWithQueryParameter(this NavigationManager navigationManager, string name, float value)
        => GetUriWithQueryParameter(navigationManager, name, Format(value));

    /// <summary>
    /// Returns a URI that is constructed by updating <see cref="NavigationManager.Uri"/> with a single parameter
    /// added, updated, or removed.
    /// </summary>
    /// <param name="navigationManager">The <see cref="NavigationManager"/>.</param>
    /// <param name="name">The name of the parameter to add or update.</param>
    /// <param name="value">The value of the parameter to add or update.</param>
    /// <remarks>
    /// If <paramref name="value"/> is <c>null</c>, the parameter will be removed if it exists in the URI.
    /// Otherwise, it will be added or updated.
    /// </remarks>
    public static string GetUriWithQueryParameter(this NavigationManager navigationManager, string name, float? value)
        => GetUriWithQueryParameter(navigationManager, name, Format(value));

    /// <summary>
    /// Returns a URI that is constructed by updating <see cref="NavigationManager.Uri"/> with a single parameter
    /// added or updated.
    /// </summary>
    /// <param name="navigationManager">The <see cref="NavigationManager"/>.</param>
    /// <param name="name">The name of the parameter to add or update.</param>
    /// <param name="value">The value of the parameter to add or update.</param>
    public static string GetUriWithQueryParameter(this NavigationManager navigationManager, string name, Guid value)
        => GetUriWithQueryParameter(navigationManager, name, Format(value));

    /// <summary>
    /// Returns a URI that is constructed by updating <see cref="NavigationManager.Uri"/> with a single parameter
    /// added, updated, or removed.
    /// </summary>
    /// <param name="navigationManager">The <see cref="NavigationManager"/>.</param>
    /// <param name="name">The name of the parameter to add or update.</param>
    /// <param name="value">The value of the parameter to add or update.</param>
    /// <remarks>
    /// If <paramref name="value"/> is <c>null</c>, the parameter will be removed if it exists in the URI.
    /// Otherwise, it will be added or updated.
    /// </remarks>
    public static string GetUriWithQueryParameter(this NavigationManager navigationManager, string name, Guid? value)
        => GetUriWithQueryParameter(navigationManager, name, Format(value));

    /// <summary>
    /// Returns a URI that is constructed by updating <see cref="NavigationManager.Uri"/> with a single parameter
    /// added or updated.
    /// </summary>
    /// <param name="navigationManager">The <see cref="NavigationManager"/>.</param>
    /// <param name="name">The name of the parameter to add or update.</param>
    /// <param name="value">The value of the parameter to add or update.</param>
    public static string GetUriWithQueryParameter(this NavigationManager navigationManager, string name, int value)
        => GetUriWithQueryParameter(navigationManager, name, Format(value));

    /// <summary>
    /// Returns a URI that is constructed by updating <see cref="NavigationManager.Uri"/> with a single parameter
    /// added, updated, or removed.
    /// </summary>
    /// <param name="navigationManager">The <see cref="NavigationManager"/>.</param>
    /// <param name="name">The name of the parameter to add or update.</param>
    /// <param name="value">The value of the parameter to add or update.</param>
    /// <remarks>
    /// If <paramref name="value"/> is <c>null</c>, the parameter will be removed if it exists in the URI.
    /// Otherwise, it will be added or updated.
    /// </remarks>
    public static string GetUriWithQueryParameter(this NavigationManager navigationManager, string name, int? value)
        => GetUriWithQueryParameter(navigationManager, name, Format(value));

    /// <summary>
    /// Returns a URI that is constructed by updating <see cref="NavigationManager.Uri"/> with a single parameter
    /// added or updated.
    /// </summary>
    /// <param name="navigationManager">The <see cref="NavigationManager"/>.</param>
    /// <param name="name">The name of the parameter to add or update.</param>
    /// <param name="value">The value of the parameter to add or update.</param>
    public static string GetUriWithQueryParameter(this NavigationManager navigationManager, string name, long value)
        => GetUriWithQueryParameter(navigationManager, name, Format(value));

    /// <summary>
    /// Returns a URI that is constructed by updating <see cref="NavigationManager.Uri"/> with a single parameter
    /// added, updated, or removed.
    /// </summary>
    /// <param name="navigationManager">The <see cref="NavigationManager"/>.</param>
    /// <param name="name">The name of the parameter to add or update.</param>
    /// <param name="value">The value of the parameter to add or update.</param>
    /// <remarks>
    /// If <paramref name="value"/> is <c>null</c>, the parameter will be removed if it exists in the URI.
    /// Otherwise, it will be added or updated.
    /// </remarks>
    public static string GetUriWithQueryParameter(this NavigationManager navigationManager, string name, long? value)
        => GetUriWithQueryParameter(navigationManager, name, Format(value));

    /// <summary>
    /// Returns a URI that is constructed by updating <see cref="NavigationManager.Uri"/> with a single parameter
    /// added, updated, or removed.
    /// </summary>
    /// <param name="navigationManager">The <see cref="NavigationManager"/>.</param>
    /// <param name="name">The name of the parameter to add or update.</param>
    /// <param name="value">The value of the parameter to add or update.</param>
    /// <remarks>
    /// If <paramref name="value"/> is <c>null</c>, the parameter will be removed if it exists in the URI.
    /// Otherwise, it will be added or updated.
    /// </remarks>
    public static string GetUriWithQueryParameter(this NavigationManager navigationManager, string name, string? value)
    {
        ArgumentNullException.ThrowIfNull(navigationManager);

        if (string.IsNullOrEmpty(name))
        {
            throw new InvalidOperationException(EmptyQueryParameterNameExceptionMessage);
        }

        var uri = navigationManager.Uri;

        return value is null
            ? GetUriWithRemovedQueryParameter(uri, name)
            : GetUriWithUpdatedQueryParameter(uri, name, value);
    }

    /// <summary>
    /// Returns a URI constructed from <see cref="NavigationManager.Uri"/> with multiple parameters
    /// added, updated, or removed.
    /// </summary>
    /// <param name="navigationManager">The <see cref="NavigationManager"/>.</param>
    /// <param name="parameters">The values to add, update, or remove.</param>
    public static string GetUriWithQueryParameters(
        this NavigationManager navigationManager,
        IReadOnlyDictionary<string, object?> parameters)
        => GetUriWithQueryParameters(navigationManager, navigationManager.Uri, parameters);

    /// <summary>
    /// Returns a URI constructed from <paramref name="uri"/> except with multiple parameters
    /// added, updated, or removed.
    /// </summary>
    /// <param name="navigationManager">The <see cref="NavigationManager"/>.</param>
    /// <param name="uri">The URI with the query to modify.</param>
    /// <param name="parameters">The values to add, update, or remove.</param>
    public static string GetUriWithQueryParameters(
        this NavigationManager navigationManager,
        string uri,
        IReadOnlyDictionary<string, object?> parameters)
    {
        ArgumentNullException.ThrowIfNull(navigationManager);
        ArgumentNullException.ThrowIfNull(uri);

        if (!TryRebuildExistingQueryFromUri(
            uri,
            out var existingQueryStringEnumerable,
            out var hash,
            out var newQueryStringBuilder))
        {
            // There was no existing query, so there is no need to allocate a new dictionary to cache
            // encoded parameter values and track which parameters have been added.
            return GetUriWithAppendedQueryParameters(uri, parameters, hash);
        }

        var parameterSources = CreateParameterSourceDictionary(parameters);

        // Rebuild the query, updating or removing parameters.
        foreach (var pair in existingQueryStringEnumerable)
        {
            if (parameterSources.TryGetValue(pair.EncodedName, out var source))
            {
                if (source.TryAppendNextParameter(ref newQueryStringBuilder))
                {
                    // We have just mutated the struct value so we need to overwrite the copy in the dictionary.
                    parameterSources[pair.EncodedName] = source;
                }
            }
            else
            {
                newQueryStringBuilder.AppendParameter(pair.EncodedName.Span, pair.EncodedValue.Span);
            }
        }

        // Append any parameters with non-null values that did not replace existing parameters.
        foreach (var source in parameterSources.Values)
        {
            while (source.TryAppendNextParameter(ref newQueryStringBuilder))
            {
                // Read all parameters.
            }
        }

        newQueryStringBuilder.AppendHash(hash);

        return newQueryStringBuilder.UriWithQueryString;
    }

    private static string GetUriWithUpdatedQueryParameter(string uri, string name, string value)
    {
        var encodedName = Uri.EscapeDataString(name);
        var encodedValue = Uri.EscapeDataString(value);

        if (!TryRebuildExistingQueryFromUri(
            uri,
            out var existingQueryStringEnumerable,
            out var hash,
            out var newQueryStringBuilder))
        {
            // There was no existing query, so we can generate the new URI.
            newQueryStringBuilder.AppendParameter(encodedName, encodedValue);
            newQueryStringBuilder.AppendHash(hash);
            return newQueryStringBuilder.UriWithQueryString;
        }

        var didReplace = false;
        foreach (var pair in existingQueryStringEnumerable)
        {
            if (pair.EncodedName.Span.Equals(encodedName, StringComparison.OrdinalIgnoreCase))
            {
                didReplace = true;
                newQueryStringBuilder.AppendParameter(encodedName, encodedValue);
            }
            else
            {
                newQueryStringBuilder.AppendParameter(pair.EncodedName.Span, pair.EncodedValue.Span);
            }
        }

        // If there was no matching parameter, add it to the end of the query.
        if (!didReplace)
        {
            newQueryStringBuilder.AppendParameter(encodedName, encodedValue);
        }

        newQueryStringBuilder.AppendHash(hash);

        return newQueryStringBuilder.UriWithQueryString;
    }

    private static string GetUriWithRemovedQueryParameter(string uri, string name)
    {
        if (!TryRebuildExistingQueryFromUri(
            uri,
            out var existingQueryStringEnumerable,
            out var hash,
            out var newQueryStringBuilder))
        {
            // There was no existing query, so the URI remains unchanged.
            return uri;
        }

        var encodedName = Uri.EscapeDataString(name);

        // Rebuild the query omitting parameters with a matching name.
        foreach (var pair in existingQueryStringEnumerable)
        {
            if (!pair.EncodedName.Span.Equals(encodedName, StringComparison.OrdinalIgnoreCase))
            {
                newQueryStringBuilder.AppendParameter(pair.EncodedName.Span, pair.EncodedValue.Span);
            }
        }

        newQueryStringBuilder.AppendHash(hash);

        return newQueryStringBuilder.UriWithQueryString;
    }

    private static string GetUriWithAppendedQueryParameters(
        string uriWithoutQueryString,
        IReadOnlyDictionary<string, object?> parameters,
        ReadOnlySpan<char> hash)
    {
        var hashStartIndex = uriWithoutQueryString.IndexOf('#');

        var uriWithoutQueryStringAndHash = hashStartIndex < 0 ? uriWithoutQueryString : uriWithoutQueryString.AsSpan(0, hashStartIndex);

        var builder = new QueryStringBuilder(uriWithoutQueryStringAndHash);

        foreach (var (name, value) in parameters)
        {
            var source = new QueryParameterSource(name, value);
            while (source.TryAppendNextParameter(ref builder))
            {
                // Read all parameters.
            }
        }

        builder.AppendHash(hash);

        return builder.UriWithQueryString;
    }

    private static Dictionary<ReadOnlyMemory<char>, QueryParameterSource> CreateParameterSourceDictionary(
        IReadOnlyDictionary<string, object?> parameters)
    {
        var parameterSources = new Dictionary<ReadOnlyMemory<char>, QueryParameterSource>(QueryParameterNameComparer.Instance);

        foreach (var (name, value) in parameters)
        {
            var parameterSource = new QueryParameterSource(name, value);
            parameterSources.Add(parameterSource.EncodedName.AsMemory(), parameterSource);
        }

        return parameterSources;
    }

    private static QueryParameterFormatter<object> GetFormatterFromParameterValueType(Type parameterValueType)
    {
        var underlyingParameterValueType = Nullable.GetUnderlyingType(parameterValueType) ?? parameterValueType;

        if (!_queryParameterFormatters.TryGetValue(underlyingParameterValueType, out var formatter))
        {
            throw new InvalidOperationException(
                $"Cannot format query parameters with values of type '{underlyingParameterValueType}'.");
        }

        return formatter;
    }

    private static bool TryRebuildExistingQueryFromUri(
        string uri,
        out QueryStringEnumerable existingQueryStringEnumerable,
        out ReadOnlySpan<char> hash,
        out QueryStringBuilder newQueryStringBuilder)
    {
        ReadOnlySpan<char> uriWithoutQueryStringAndHash;

        var hashStartIndex = uri.IndexOf('#');
        hash = hashStartIndex < 0 ? "" : uri.AsSpan(hashStartIndex);

        var queryStartIndex = (hashStartIndex > 0 ? uri.AsSpan(0, hashStartIndex) : uri).IndexOf('?');

        if (queryStartIndex < 0)
        {

            existingQueryStringEnumerable = default;
            uriWithoutQueryStringAndHash = hashStartIndex < 0 ? uri : uri.AsSpan(0, hashStartIndex);
            newQueryStringBuilder = new(uriWithoutQueryStringAndHash);
            return false;
        }

        var queryLength = hashStartIndex < 0 ?
            uri.Length - queryStartIndex :
            hashStartIndex - queryStartIndex;

        var query = uri.AsMemory(queryStartIndex, queryLength);

        existingQueryStringEnumerable = new(query);

        uriWithoutQueryStringAndHash = uri.AsSpan(0, queryStartIndex);
        newQueryStringBuilder = new(uriWithoutQueryStringAndHash, query.Length + hash.Length);

        return true;
    }
}
