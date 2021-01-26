// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    /// <summary>
    /// A delegate for <see cref="System.Collections.Generic.IDictionary{TKey, TValue}.TryGetValue(TKey, out TValue)"/>.
    /// </summary>
    /// <param name="dictionary">The <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/>.</param>
    /// <param name="key">The key.</param>
    /// <param name="value">The return value.</param>
    /// <returns>Whether the key was found.</returns>
    public delegate bool TryGetValueDelegate(object dictionary, string key, out object value);
}
