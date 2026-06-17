// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Contains extension methods for modifying an <see cref="IHeaderDictionary"/> instance.
/// </summary>
public static class HeaderDictionaryExtensions
{
    /// <summary>
    /// Add new values. Each item remains a separate array entry.
    /// </summary>
    /// <param name="headers">The <see cref="IHeaderDictionary"/> to use.</param>
    /// <param name="key">The header name.</param>
    /// <param name="value">The header value.</param>
    public static void Append(this IHeaderDictionary headers, string key, StringValues value)
    {
        ParsingHelpers.AppendHeaderUnmodified(headers, key, value);
    }

    /// <summary>
    /// Quotes any values containing commas, and then comma joins all of the values with any existing values.
    /// </summary>
    /// <param name="headers">The <see cref="IHeaderDictionary"/> to use.</param>
    /// <param name="key">The header name.</param>
    /// <param name="values">The header values.</param>
    public static void AppendCommaSeparatedValues(this IHeaderDictionary headers, string key, params string[] values)
    {
        ParsingHelpers.AppendHeaderJoined(headers, key, values);
    }

    /// <summary>
    /// Get the associated values from the collection separated into individual values.
    /// Quoted values will not be split, and the quotes will be removed.
    /// </summary>
    /// <param name="headers">The <see cref="IHeaderDictionary"/> to use.</param>
    /// <param name="key">The header name.</param>
    /// <returns>the associated values from the collection separated into individual values, or StringValues.Empty if the key is not present.</returns>
    public static string[] GetCommaSeparatedValues(this IHeaderDictionary headers, string key)
    {
        // GetHeaderSplit will return only non-null elements of the given IHeaderDictionary.
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
        return ParsingHelpers.GetHeaderSplit(headers, key);
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.
    }

    /// <summary>
    /// Quotes any values containing commas, and then comma joins all of the values.
    /// </summary>
    /// <param name="headers">The <see cref="IHeaderDictionary"/> to use.</param>
    /// <param name="key">The header name.</param>
    /// <param name="values">The header values.</param>
    public static void SetCommaSeparatedValues(this IHeaderDictionary headers, string key, params string[] values)
    {
        ParsingHelpers.SetHeaderJoined(headers, key, values);
    }
}
