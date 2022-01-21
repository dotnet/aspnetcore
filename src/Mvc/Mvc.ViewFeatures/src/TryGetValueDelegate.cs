// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

/// <summary>
/// A delegate for <see cref="System.Collections.Generic.IDictionary{TKey, TValue}.TryGetValue(TKey, out TValue)"/>.
/// </summary>
/// <param name="dictionary">The <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/>.</param>
/// <param name="key">The key.</param>
/// <param name="value">The return value.</param>
/// <returns>Whether the key was found.</returns>
public delegate bool TryGetValueDelegate(object dictionary, string key, out object value);
