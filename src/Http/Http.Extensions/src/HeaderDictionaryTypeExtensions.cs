// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Extension methods for accessing strongly typed HTTP request and response
/// headers.
/// </summary>
public static class HeaderDictionaryTypeExtensions
{
    /// <summary>
    /// Gets strongly typed HTTP request headers.
    /// </summary>
    /// <param name="request">The <see cref="HttpRequest"/>.</param>
    /// <returns>The <see cref="RequestHeaders"/>.</returns>
    public static RequestHeaders GetTypedHeaders(this HttpRequest request)
    {
        return new RequestHeaders(request.Headers);
    }

    /// <summary>
    /// Gets strongly typed HTTP response headers.
    /// </summary>
    /// <param name="response">The <see cref="HttpResponse"/>.</param>
    /// <returns>The <see cref="ResponseHeaders"/>.</returns>
    public static ResponseHeaders GetTypedHeaders(this HttpResponse response)
    {
        return new ResponseHeaders(response.Headers);
    }

    // These are all shared helpers used by both RequestHeaders and ResponseHeaders

    internal static DateTimeOffset? GetDate(this IHeaderDictionary headers, string name)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(name);

        return headers.Get<DateTimeOffset?>(name);
    }

    internal static void Set(this IHeaderDictionary headers, string name, object? value)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(name);

        if (value == null)
        {
            headers.Remove(name);
        }
        else
        {
            headers[name] = value.ToString();
        }
    }

    internal static void SetList<T>(this IHeaderDictionary headers, string name, IList<T>? values)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(name);

        if (values == null || values.Count == 0)
        {
            headers.Remove(name);
        }
        else if (values.Count == 1)
        {
            headers[name] = new StringValues(values[0]!.ToString());
        }
        else
        {
            var newValues = new string[values.Count];
            for (var i = 0; i < values.Count; i++)
            {
                newValues[i] = values[i]!.ToString()!;
            }
            headers[name] = new StringValues(newValues);
        }
    }

    /// <summary>
    /// Appends a sequence of values to <see cref="IHeaderDictionary"/>.
    /// </summary>
    /// <typeparam name="T">The type of header value.</typeparam>
    /// <param name="Headers">The <see cref="IHeaderDictionary"/>.</param>
    /// <param name="name">The header name.</param>
    /// <param name="values">The values to append.</param>
    public static void AppendList<T>(this IHeaderDictionary Headers, string name, IList<T> values)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(values);

        switch (values.Count)
        {
            case 0:
                Headers.Append(name, StringValues.Empty);
                break;
            case 1:
                Headers.Append(name, new StringValues(values[0]!.ToString()));
                break;
            default:
                var newValues = new string[values.Count];
                for (var i = 0; i < values.Count; i++)
                {
                    newValues[i] = values[i]!.ToString()!;
                }
                Headers.Append(name, new StringValues(newValues));
                break;
        }
    }

    internal static void SetDate(this IHeaderDictionary headers, string name, DateTimeOffset? value)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(name);

        if (value.HasValue)
        {
            headers[name] = HeaderUtilities.FormatDate(value.GetValueOrDefault());
        }
        else
        {
            headers.Remove(name);
        }
    }

    private static CacheControlHeaderValue? ParseCacheControlHeaderValue(string value) => CacheControlHeaderValue.TryParse(value, out var result) ? result : null;

    private static ContentDispositionHeaderValue? ParseCacheContentDispositionHeaderValue(string value) => ContentDispositionHeaderValue.TryParse(value, out var result) ? result : null;

    private static ContentRangeHeaderValue? ParseCacheContentRangeHeaderValue(string value) => ContentRangeHeaderValue.TryParse(value, out var result) ? result : null;

    private static MediaTypeHeaderValue? ParseCacheMediaTypeHeaderValue(string value) => MediaTypeHeaderValue.TryParse(value, out var result) ? result : null;

    private static RangeConditionHeaderValue? ParseCacheRangeConditionHeaderValue(string value) => RangeConditionHeaderValue.TryParse(value, out var result) ? result : null;

    private static RangeHeaderValue? ParseCacheRangeHeaderValue(string value) => RangeHeaderValue.TryParse(value, out var result) ? result : null;

    private static EntityTagHeaderValue? ParseCacheEntityTagHeaderValue(string value) => EntityTagHeaderValue.TryParse(value, out var result) ? result : null;

    private static DateTimeOffset? ParseCacheDateTimeOffset(string value) => HeaderUtilities.TryParseDate(value, out var result) ? result : null;

    private static long? ParseCacheInt64(string value) => HeaderUtilities.TryParseNonNegativeInt64(value, out var result) ? result : null;

    private static IList<MediaTypeHeaderValue> ParseMediaTypeHeaderValue(IList<string> value) =>
        MediaTypeHeaderValue.TryParseList(value, out var result) ? result : Array.Empty<MediaTypeHeaderValue>();

    private static IList<StringWithQualityHeaderValue> ParseStringWithQualityHeaderValue(IList<string> value) =>
        StringWithQualityHeaderValue.TryParseList(value, out var result) ? result : Array.Empty<StringWithQualityHeaderValue>();

    private static IList<CookieHeaderValue> ParseCookieHeaderValue(IList<string> value) =>
        CookieHeaderValue.TryParseList(value, out var result) ? result : Array.Empty<CookieHeaderValue>();

    private static IList<EntityTagHeaderValue> ParseEntityTagHeaderValue(IList<string> value) =>
        EntityTagHeaderValue.TryParseList(value, out var result) ? result : Array.Empty<EntityTagHeaderValue>();

    private static IList<SetCookieHeaderValue> ParseSetCookieHeaderValue(IList<string> value) =>
        SetCookieHeaderValue.TryParseList(value, out var result) ? result : Array.Empty<SetCookieHeaderValue>();

    internal static T? Get<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(this IHeaderDictionary headers, string name)
    {
        ArgumentNullException.ThrowIfNull(headers);
        var headerValue = headers[name];

        if (StringValues.IsNullOrEmpty(headerValue))
        {
            return default(T);
        }
        var value = headerValue.ToString();

        // Comparison of typeof(T) is optimized by JIT.
        if (typeof(T) == typeof(CacheControlHeaderValue))
        {
            return (T?)(object?)ParseCacheControlHeaderValue(value);
        }
        else if (typeof(T) == typeof(ContentDispositionHeaderValue))
        {
            return (T?)(object?)ParseCacheContentDispositionHeaderValue(value);
        }
        else if (typeof(T) == typeof(ContentRangeHeaderValue))
        {
            return (T?)(object?)ParseCacheContentRangeHeaderValue(value);
        }
        else if (typeof(T) == typeof(MediaTypeHeaderValue))
        {
            return (T?)(object?)ParseCacheMediaTypeHeaderValue(value);
        }
        else if (typeof(T) == typeof(RangeConditionHeaderValue))
        {
            return (T?)(object?)ParseCacheRangeConditionHeaderValue(value);
        }
        else if (typeof(T) == typeof(RangeHeaderValue))
        {
            return (T?)(object?)ParseCacheRangeHeaderValue(value);
        }
        else if (typeof(T) == typeof(EntityTagHeaderValue))
        {
            return (T?)(object?)ParseCacheEntityTagHeaderValue(value);
        }
        else if (typeof(T) == typeof(DateTimeOffset?))
        {
            return (T?)(object?)ParseCacheDateTimeOffset(value);
        }
        else if (typeof(T) == typeof(long?))
        {
            return (T?)(object?)ParseCacheInt64(value);
        }

        return GetViaReflection<T>(value);
    }

    internal static IList<T> GetList<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(this IHeaderDictionary headers, string name)
    {
        ArgumentNullException.ThrowIfNull(headers);

        var values = headers[name];

        return GetList<T>(values);
    }

    internal static IList<T> GetList<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(this StringValues values)
    {
        if (StringValues.IsNullOrEmpty(values))
        {
            return Array.Empty<T>();
        }

        // Comparison of typeof(T) is optimized by JIT.
        if (typeof(T) == typeof(MediaTypeHeaderValue))
        {
            return (IList<T>)ParseMediaTypeHeaderValue(values);
        }
        else if (typeof(T) == typeof(StringWithQualityHeaderValue))
        {
            return (IList<T>)ParseStringWithQualityHeaderValue(values);
        }
        else if (typeof(T) == typeof(CookieHeaderValue))
        {
            return (IList<T>)ParseCookieHeaderValue(values);
        }
        else if (typeof(T) == typeof(EntityTagHeaderValue))
        {
            return (IList<T>)ParseEntityTagHeaderValue(values);
        }
        else if (typeof(T) == typeof(SetCookieHeaderValue))
        {
            return (IList<T>)ParseSetCookieHeaderValue(values);
        }

        return GetListViaReflection<T>(values);
    }

    private static T? GetViaReflection<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(string value)
    {
        // TODO: Cache the reflected type for later? Only if success?
        var type = typeof(T);
        MethodInfo? method = null;
        foreach (var methodInfo in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            if (string.Equals("TryParse", methodInfo.Name, StringComparison.Ordinal) &&
                methodInfo.ReturnParameter.ParameterType.Equals(typeof(bool)))
            {
                var methodParams = methodInfo.GetParameters();
                if (methodParams.Length == 2
                    && methodParams[0].ParameterType.Equals(typeof(string))
                    && methodParams[1].IsOut
                    && methodParams[1].ParameterType.Equals(type.MakeByRefType()))
                {
                    method = methodInfo;
                    break;
                }
            }
        }

        if (method is null)
        {
            throw new NotSupportedException(
                $"The given type '{typeof(T)}' does not have a TryParse method with the required signature 'public static bool TryParse(string, out {typeof(T)}).");
        }

        var parameters = new object?[] { value, null };
        var success = (bool)method.Invoke(null, parameters)!;
        if (success)
        {
            return (T?)parameters[1];
        }
        return default(T);
    }

    private static IList<T> GetListViaReflection<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(StringValues values)
    {
        // TODO: Cache the reflected type for later? Only if success?
        var type = typeof(T);
        var method = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(methodInfo =>
            {
                if (string.Equals("TryParseList", methodInfo.Name, StringComparison.Ordinal)
                    && methodInfo.ReturnParameter.ParameterType.Equals(typeof(Boolean)))
                {
                    var methodParams = methodInfo.GetParameters();
                    return methodParams.Length == 2
                        && methodParams[0].ParameterType.Equals(typeof(IList<string>))
                        && methodParams[1].IsOut
                        && methodParams[1].ParameterType.Equals(typeof(IList<T>).MakeByRefType());
                }
                return false;
            });

        if (method == null)
        {
            throw new NotSupportedException(string.Format(
                CultureInfo.CurrentCulture,
                "The given type '{0}' does not have a TryParseList method with the required signature 'public static bool TryParseList(IList<string>, out IList<{0}>).",
                nameof(T)));
        }

        var parameters = new object?[] { values, null };
        var success = (bool)method.Invoke(null, parameters)!;
        if (success)
        {
            return (IList<T>)parameters[1]!;
        }
        return Array.Empty<T>();
    }
}
