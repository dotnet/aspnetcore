// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Provides extension methods for the <see cref="NavigationManager"/> type.
    /// </summary>
    public static class NavigationManagerExtensions
    {
        private delegate string QueryParameterFormatter(object value);

        // We don't include mappings for Nullable types because we explicitly check for null values
        // to see if the parameter should be excluded from the querystring. Therefore, we will only
        // invoke these formatters for non-null values. We also get the underlying type of any Nullable
        // types before performing lookups in this dictionary.
        private static readonly Dictionary<Type, QueryParameterFormatter> _queryParameterFormatters = new()
        {
            [typeof(string)] = value => (string)value,
            [typeof(bool)] = value => Format((bool)value),
            [typeof(DateTime)] = value => Format((DateTime)value),
            [typeof(decimal)] = value => Format((decimal)value),
            [typeof(double)] = value => Format((double)value),
            [typeof(float)] = value => Format((float)value),
            [typeof(Guid)] = value => Format((Guid)value),
            [typeof(int)] = value => Format((int)value),
            [typeof(long)] = value => Format((long)value),
        };

        private static string Format(bool value)
            => value.ToString(CultureInfo.InvariantCulture);

        private static string? Format(bool? value)
            => value?.ToString(CultureInfo.InvariantCulture);

        private static string Format(DateTime value)
            => value.ToString(CultureInfo.InvariantCulture);

        private static string? Format(DateTime? value)
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

        private struct QueryStringBuilder
        {
            private readonly StringBuilder _builder;

            private bool _hasNewParameters;

            public string UriWithQueryString => _builder.ToString();

            public QueryStringBuilder(ReadOnlySpan<char> uriWithoutQueryString)
            {
                _builder = new();
                _builder.Append(uriWithoutQueryString);

                _hasNewParameters = false;
            }

            public void AppendParameter(ReadOnlySpan<char> encodedName, ReadOnlySpan<char> encodedValue)
            {
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
        }

        private class ParameterData
        {
            public string? EncodedValue { get; private set; }
            public bool DidReplace { get; set; }

            public ParameterData(string? encodedValue)
            {
                EncodedValue = encodedValue;
                DidReplace = false;
            }
        }

        /// <summary>
        /// Returns a <see cref="string"/> equal to <see cref="NavigationManager.Uri"/> except with a single parameter
        /// added or updated.
        /// </summary>
        /// <param name="navigationManager">The <see cref="NavigationManager"/>.</param>
        /// <param name="name">The name of the paramter to add or update.</param>
        /// <param name="value">The value of the parameter to add or update.</param>
        public static string UriWithQueryParameter(this NavigationManager navigationManager, string name, bool value)
            => UriWithQueryParameter(navigationManager, name, Format(value));

        /// <summary>
        /// Returns a <see cref="string"/> equal to <see cref="NavigationManager.Uri"/> except with a single parameter
        /// added, updated, or removed.
        /// </summary>
        /// <param name="navigationManager">The <see cref="NavigationManager"/>.</param>
        /// <param name="name">The name of the paramter to add or update.</param>
        /// <param name="value">The value of the parameter to add or update.</param>
        /// <remarks>
        /// If <paramref name="value"/> is <c>null</c>, the parameter will be removed if it exists in the URI.
        /// Otherwise, it will be added or updated.
        /// </remarks>
        public static string UriWithQueryParameter(this NavigationManager navigationManager, string name, bool? value)
            => UriWithQueryParameter(navigationManager, name, Format(value));

        /// <summary>
        /// Returns a <see cref="string"/> equal to <see cref="NavigationManager.Uri"/> except with a single parameter
        /// added or updated.
        /// </summary>
        /// <param name="navigationManager">The <see cref="NavigationManager"/>.</param>
        /// <param name="name">The name of the paramter to add or update.</param>
        /// <param name="value">The value of the parameter to add or update.</param>
        public static string UriWithQueryParameter(this NavigationManager navigationManager, string name, DateTime value)
            => UriWithQueryParameter(navigationManager, name, Format(value));

        /// <summary>
        /// Returns a <see cref="string"/> equal to <see cref="NavigationManager.Uri"/> except with a single parameter
        /// added, updated, or removed.
        /// </summary>
        /// <param name="navigationManager">The <see cref="NavigationManager"/>.</param>
        /// <param name="name">The name of the paramter to add or update.</param>
        /// <param name="value">The value of the parameter to add or update.</param>
        /// <remarks>
        /// If <paramref name="value"/> is <c>null</c>, the parameter will be removed if it exists in the URI.
        /// Otherwise, it will be added or updated.
        /// </remarks>
        public static string UriWithQueryParameter(this NavigationManager navigationManager, string name, DateTime? value)
            => UriWithQueryParameter(navigationManager, name, Format(value));

        /// <summary>
        /// Returns a <see cref="string"/> equal to <see cref="NavigationManager.Uri"/> except with a single parameter
        /// added or updated.
        /// </summary>
        /// <param name="navigationManager">The <see cref="NavigationManager"/>.</param>
        /// <param name="name">The name of the paramter to add or update.</param>
        /// <param name="value">The value of the parameter to add or update.</param>
        public static string UriWithQueryParameter(this NavigationManager navigationManager, string name, decimal value)
            => UriWithQueryParameter(navigationManager, name, Format(value));

        /// <summary>
        /// Returns a <see cref="string"/> equal to <see cref="NavigationManager.Uri"/> except with a single parameter
        /// added, updated, or removed.
        /// </summary>
        /// <param name="navigationManager">The <see cref="NavigationManager"/>.</param>
        /// <param name="name">The name of the paramter to add or update.</param>
        /// <param name="value">The value of the parameter to add or update.</param>
        /// <remarks>
        /// If <paramref name="value"/> is <c>null</c>, the parameter will be removed if it exists in the URI.
        /// Otherwise, it will be added or updated.
        /// </remarks>
        public static string UriWithQueryParameter(this NavigationManager navigationManager, string name, decimal? value)
            => UriWithQueryParameter(navigationManager, name, Format(value));

        /// <summary>
        /// Returns a <see cref="string"/> equal to <see cref="NavigationManager.Uri"/> except with a single parameter
        /// added or updated.
        /// </summary>
        /// <param name="navigationManager">The <see cref="NavigationManager"/>.</param>
        /// <param name="name">The name of the paramter to add or update.</param>
        /// <param name="value">The value of the parameter to add or update.</param>
        public static string UriWithQueryParameter(this NavigationManager navigationManager, string name, double value)
            => UriWithQueryParameter(navigationManager, name, Format(value));

        /// <summary>
        /// Returns a <see cref="string"/> equal to <see cref="NavigationManager.Uri"/> except with a single parameter
        /// added, updated, or removed.
        /// </summary>
        /// <param name="navigationManager">The <see cref="NavigationManager"/>.</param>
        /// <param name="name">The name of the paramter to add or update.</param>
        /// <param name="value">The value of the parameter to add or update.</param>
        /// <remarks>
        /// If <paramref name="value"/> is <c>null</c>, the parameter will be removed if it exists in the URI.
        /// Otherwise, it will be added or updated.
        /// </remarks>
        public static string UriWithQueryParameter(this NavigationManager navigationManager, string name, double? value)
            => UriWithQueryParameter(navigationManager, name, Format(value));

        /// <summary>
        /// Returns a <see cref="string"/> equal to <see cref="NavigationManager.Uri"/> except with a single parameter
        /// added or updated.
        /// </summary>
        /// <param name="navigationManager">The <see cref="NavigationManager"/>.</param>
        /// <param name="name">The name of the paramter to add or update.</param>
        /// <param name="value">The value of the parameter to add or update.</param>
        public static string UriWithQueryParameter(this NavigationManager navigationManager, string name, float value)
            => UriWithQueryParameter(navigationManager, name, Format(value));

        /// <summary>
        /// Returns a <see cref="string"/> equal to <see cref="NavigationManager.Uri"/> except with a single parameter
        /// added, updated, or removed.
        /// </summary>
        /// <param name="navigationManager">The <see cref="NavigationManager"/>.</param>
        /// <param name="name">The name of the paramter to add or update.</param>
        /// <param name="value">The value of the parameter to add or update.</param>
        /// <remarks>
        /// If <paramref name="value"/> is <c>null</c>, the parameter will be removed if it exists in the URI.
        /// Otherwise, it will be added or updated.
        /// </remarks>
        public static string UriWithQueryParameter(this NavigationManager navigationManager, string name, float? value)
            => UriWithQueryParameter(navigationManager, name, Format(value));

        /// <summary>
        /// Returns a <see cref="string"/> equal to <see cref="NavigationManager.Uri"/> except with a single parameter
        /// added or updated.
        /// </summary>
        /// <param name="navigationManager">The <see cref="NavigationManager"/>.</param>
        /// <param name="name">The name of the paramter to add or update.</param>
        /// <param name="value">The value of the parameter to add or update.</param>
        public static string UriWithQueryParameter(this NavigationManager navigationManager, string name, Guid value)
            => UriWithQueryParameter(navigationManager, name, Format(value));

        /// <summary>
        /// Returns a <see cref="string"/> equal to <see cref="NavigationManager.Uri"/> except with a single parameter
        /// added, updated, or removed.
        /// </summary>
        /// <param name="navigationManager">The <see cref="NavigationManager"/>.</param>
        /// <param name="name">The name of the paramter to add or update.</param>
        /// <param name="value">The value of the parameter to add or update.</param>
        /// <remarks>
        /// If <paramref name="value"/> is <c>null</c>, the parameter will be removed if it exists in the URI.
        /// Otherwise, it will be added or updated.
        /// </remarks>
        public static string UriWithQueryParameter(this NavigationManager navigationManager, string name, Guid? value)
            => UriWithQueryParameter(navigationManager, name, Format(value));

        /// <summary>
        /// Returns a <see cref="string"/> equal to <see cref="NavigationManager.Uri"/> except with a single parameter
        /// added or updated.
        /// </summary>
        /// <param name="navigationManager">The <see cref="NavigationManager"/>.</param>
        /// <param name="name">The name of the paramter to add or update.</param>
        /// <param name="value">The value of the parameter to add or update.</param>
        public static string UriWithQueryParameter(this NavigationManager navigationManager, string name, int value)
            => UriWithQueryParameter(navigationManager, name, Format(value));

        /// <summary>
        /// Returns a <see cref="string"/> equal to <see cref="NavigationManager.Uri"/> except with a single parameter
        /// added, updated, or removed.
        /// </summary>
        /// <param name="navigationManager">The <see cref="NavigationManager"/>.</param>
        /// <param name="name">The name of the paramter to add or update.</param>
        /// <param name="value">The value of the parameter to add or update.</param>
        /// <remarks>
        /// If <paramref name="value"/> is <c>null</c>, the parameter will be removed if it exists in the URI.
        /// Otherwise, it will be added or updated.
        /// </remarks>
        public static string UriWithQueryParameter(this NavigationManager navigationManager, string name, int? value)
            => UriWithQueryParameter(navigationManager, name, Format(value));

        /// <summary>
        /// Returns a <see cref="string"/> equal to <see cref="NavigationManager.Uri"/> except with a single parameter
        /// added or updated.
        /// </summary>
        /// <param name="navigationManager">The <see cref="NavigationManager"/>.</param>
        /// <param name="name">The name of the paramter to add or update.</param>
        /// <param name="value">The value of the parameter to add or update.</param>
        public static string UriWithQueryParameter(this NavigationManager navigationManager, string name, long value)
            => UriWithQueryParameter(navigationManager, name, Format(value));

        /// <summary>
        /// Returns a <see cref="string"/> equal to <see cref="NavigationManager.Uri"/> except with a single parameter
        /// added, updated, or removed.
        /// </summary>
        /// <param name="navigationManager">The <see cref="NavigationManager"/>.</param>
        /// <param name="name">The name of the paramter to add or update.</param>
        /// <param name="value">The value of the parameter to add or update.</param>
        /// <remarks>
        /// If <paramref name="value"/> is <c>null</c>, the parameter will be removed if it exists in the URI.
        /// Otherwise, it will be added or updated.
        /// </remarks>
        public static string UriWithQueryParameter(this NavigationManager navigationManager, string name, long? value)
            => UriWithQueryParameter(navigationManager, name, Format(value));

        /// <summary>
        /// Returns a <see cref="string"/> equal to <see cref="NavigationManager.Uri"/> except with a single parameter
        /// added, updated, or removed.
        /// </summary>
        /// <param name="navigationManager">The <see cref="NavigationManager"/>.</param>
        /// <param name="name">The name of the paramter to add or update.</param>
        /// <param name="value">The value of the parameter to add or update.</param>
        /// <remarks>
        /// If <paramref name="value"/> is <c>null</c>, the parameter will be removed if it exists in the URI.
        /// Otherwise, it will be added or updated.
        /// </remarks>
        public static string UriWithQueryParameter(this NavigationManager navigationManager, string name, string? value)
        {
            if (navigationManager is null)
            {
                throw new ArgumentNullException(nameof(navigationManager));
            }

            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var uri = navigationManager.Uri;

            return value is null
                ? UriWithoutQueryParameter(uri, name)
                : UriWithQueryParameterCore(uri, name, value);
        }

        /// <summary>
        /// Returns a <see cref="string"/> equal to <see cref="NavigationManager.Uri"/> except with a single parameter
        /// updated with the provided <paramref name="values"/>.
        /// </summary>
        /// <param name="navigationManager">The <see cref="NavigationManager"/>.</param>
        /// <param name="name">The name of the paramter to add or update.</param>
        /// <param name="values">The value of the parameter to add or update.</param>
        /// <remarks>
        /// Any <c>null</c> entries in <paramref name="values"/> will be skipped. Existing querystring parameters not in
        /// <paramref name="values"/> will be removed from the querystring in the returned URI.
        /// </remarks>
        public static string UriWithQueryParameter<TValue>(this NavigationManager navigationManager, string name, IEnumerable<TValue> values)
        {
            if (navigationManager is null)
            {
                throw new ArgumentNullException(nameof(navigationManager));
            }

            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var uri = navigationManager.Uri;

            if (!TryRebuildExistingQueryFromUri(uri, out var existingQueryStringEnumerable, out var newQueryStringBuilder))
            {
                return UriWithAppendedQueryParameters(uri, name, values);
            }

            var formatter = GetFormatterFromParameterValueType(typeof(TValue));
            var encodedName = Uri.EscapeDataString(name).AsSpan();
            var valueEnumerator = values.GetEnumerator();
            var hasNextValue = valueEnumerator.MoveNext();

            foreach (var pair in existingQueryStringEnumerable)
            {
                if (pair.EncodedName.Span.SequenceEqual(encodedName))
                {
                    if (!hasNextValue)
                    {
                        // We've added all provided values, so we'll skip any parameters with the provided name from
                        // now on.
                        continue;
                    }

                    hasNextValue = AppendNextValue(pair.EncodedName.Span, valueEnumerator, formatter, ref newQueryStringBuilder);
                }
                else
                {
                    newQueryStringBuilder.AppendParameter(pair.EncodedName.Span, pair.EncodedValue.Span);
                }
            }

            while (hasNextValue)
            {
                hasNextValue = AppendNextValue(encodedName, valueEnumerator, formatter, ref newQueryStringBuilder);
            }

            return newQueryStringBuilder.UriWithQueryString;

            static bool AppendNextValue(
                ReadOnlySpan<char> name,
                IEnumerator<TValue> valueEnumerator,
                QueryParameterFormatter formatter,
                ref QueryStringBuilder queryStringBuilder)
            {
                var currentValue = valueEnumerator.Current;
                var hasNext = valueEnumerator.MoveNext();

                if (currentValue is null)
                {
                    // If we encounter a null value, we exclude it from the querystring, so we skip whatever
                    // the old value was.
                    return hasNext;
                }

                var formattedValue = formatter(currentValue);
                var encodedValue = Uri.EscapeDataString(formattedValue);
                queryStringBuilder.AppendParameter(name, encodedValue);

                return hasNext;
            }
        }

        private static string UriWithQueryParameterCore(string uri, string name, string value)
        {
            var encodedName = Uri.EscapeDataString(name);
            var encodedValue = Uri.EscapeDataString(value);

            if (!TryRebuildExistingQueryFromUri(uri, out var existingQueryStringEnumerable, out var newQueryStringBuilder))
            {
                // There was no existing query, so we can generate the new URI immediately.
                return $"{uri}?{encodedName}={encodedValue}";
            }

            var didReplace = false;
            foreach (var pair in existingQueryStringEnumerable)
            {
                if (pair.EncodedName.Span.SequenceEqual(encodedName))
                {
                    didReplace = true;
                    newQueryStringBuilder.AppendParameter(pair.EncodedName.Span, encodedValue);
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

            return newQueryStringBuilder.UriWithQueryString;
        }

        private static string UriWithoutQueryParameter(string uri, string name)
        {
            if (!TryRebuildExistingQueryFromUri(uri, out var existingQueryStringEnumerable, out var newQueryStringBuilder))
            {
                // There was no existing query, so the URI remains unchanged.
                return uri;
            }

            var encodedName = Uri.EscapeDataString(name);

            // Rebuild the query omitting parameters with a matching name.
            foreach (var pair in existingQueryStringEnumerable)
            {
                if (!pair.EncodedName.Span.SequenceEqual(encodedName))
                {
                    newQueryStringBuilder.AppendParameter(pair.EncodedName.Span, pair.EncodedValue.Span);
                }
            }

            return newQueryStringBuilder.UriWithQueryString;
        }

        /// <summary>
        /// Returns a <see cref="string"/> equal to <see cref="NavigationManager.Uri"/> except with multiple parameters
        /// added, updated, or removed.
        /// </summary>
        /// <param name="navigationManager">The <see cref="NavigationManager"/>.</param>
        /// <param name="parameters">The values to add, update, or remove.</param>
        public static string UriWithQueryParameters(
            this NavigationManager navigationManager,
            IReadOnlyDictionary<string, object?> parameters)
            => UriWithQueryParameters(navigationManager, navigationManager.Uri, parameters);

        /// <summary>
        /// Returns a <see cref="string"/> equal to <paramref name="uri"/> except with multiple parameters
        /// added, updated, or removed.
        /// </summary>
        /// <param name="navigationManager">The <see cref="NavigationManager"/>.</param>
        /// <param name="uri">The URI with the query to modify.</param>
        /// <param name="parameters">The values to add, update, or remove.</param>
        public static string UriWithQueryParameters(
            this NavigationManager navigationManager,
            string uri,
            IReadOnlyDictionary<string, object?> parameters)
        {
            if (navigationManager is null)
            {
                throw new ArgumentNullException(nameof(navigationManager));
            }

            if (uri is null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (!TryRebuildExistingQueryFromUri(uri, out var existingQueryStringEnumerable, out var newQueryStringBuilder))
            {
                // There was no existing query, so there is no need to allocate a new dictionary to cache
                // encoded parameter values and track which parameters have been added.
                return UriWithAppendedQueryParameters(uri, parameters);
            }

            // Build a dictionary mapping encoded parameter names to an object containing their encoded values
            // and whether they've replaced an existing parameter.
            var parameterDataByEncodedName = new Dictionary<ReadOnlyMemory<char>, ParameterData>(
                QueryParameterNameComparer.Instance);
            foreach (var (name, value) in parameters)
            {
                var encodedName = Uri.EscapeDataString(name).AsMemory();
                var encodedValue = GetEncodedParameterValue(value);

                parameterDataByEncodedName.Add(encodedName, new ParameterData(encodedValue));
            }

            // Rebuild the query, updating or removing parameters.
            foreach (var pair in existingQueryStringEnumerable)
            {
                if (parameterDataByEncodedName.TryGetValue(pair.EncodedName, out var parameterData))
                {
                    parameterData.DidReplace = true;

                    if (parameterData.EncodedValue is not null)
                    {
                        newQueryStringBuilder.AppendParameter(pair.EncodedName.Span, parameterData.EncodedValue);
                    }
                }
                else
                {
                    newQueryStringBuilder.AppendParameter(pair.EncodedName.Span, pair.EncodedValue.Span);
                }
            }

            // Append any parameters with non-null values that did not replace existing parameters.
            foreach (var (encodedName, data) in parameterDataByEncodedName)
            {
                if (!data.DidReplace && data.EncodedValue is not null)
                {
                    newQueryStringBuilder.AppendParameter(encodedName.Span, data.EncodedValue);
                }
            }

            return newQueryStringBuilder.UriWithQueryString;
        }

        private static string UriWithAppendedQueryParameters<TValue>(
            string uriWithoutQueryString,
            string name,
            IEnumerable<TValue> values)
        {
            var formatter = GetFormatterFromParameterValueType(typeof(TValue));
            var encodedName = Uri.EscapeDataString(name);
            var builder = new QueryStringBuilder(uriWithoutQueryString);

            // Build a new query from the existing URI, appending all values.
            foreach (var value in values)
            {
                if (value is null)
                {
                    continue;
                }

                var formattedValue = formatter(value);
                var encodedValue = Uri.EscapeDataString(formattedValue);

                builder.AppendParameter(encodedName, encodedValue);
            }

            return builder.UriWithQueryString;
        }

        private static string UriWithAppendedQueryParameters(
            string uriWithoutQueryString,
            IReadOnlyDictionary<string, object?> parameters)
        {
            var builder = new QueryStringBuilder(uriWithoutQueryString);

            // Build a new query from the existing URI, appending all parameters with non-null values.
            foreach (var (name, value) in parameters)
            {
                var encodedName = Uri.EscapeDataString(name);
                var encodedValue = GetEncodedParameterValue(value);

                if (encodedValue is not null)
                {
                    builder.AppendParameter(encodedName, encodedValue);
                }
            }

            return builder.UriWithQueryString;
        }

        private static string? GetEncodedParameterValue(object? value)
        {
            if (value is null)
            {
                return null;
            }

            var formatter = GetFormatterFromParameterValueType(value.GetType());
            var formattedValue = formatter(value);
            return formattedValue is null ? null : Uri.EscapeDataString(formattedValue);
        }

        private static QueryParameterFormatter GetFormatterFromParameterValueType(Type parameterValueType)
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
            out QueryStringBuilder newQueryStringBuilder)
        {
            var queryStartIndex = uri.IndexOf('?');

            if (queryStartIndex < 0)
            {
                existingQueryStringEnumerable = default;
                newQueryStringBuilder = default;
                return false;
            }

            var query = uri.AsMemory(queryStartIndex);
            existingQueryStringEnumerable = new(query);

            var uriWithoutQueryString = uri.AsSpan(0, queryStartIndex);
            newQueryStringBuilder = new(uriWithoutQueryString);

            return true;
        }
    }
}
