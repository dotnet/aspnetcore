// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// Defines the methods that are required for a value provider.
    /// </summary>
    public interface IValueProvider
    {
        /// <summary>
        /// Determines whether the collection contains the specified prefix.
        /// </summary>
        /// <param name="prefix">The prefix to search for.</param>
        /// <returns>true if the collection contains the specified prefix; otherwise, false.</returns>
        bool ContainsPrefix(string prefix);

        /// <summary>
        /// Retrieves a value object using the specified key.
        /// </summary>
        /// <param name="key">The key of the value object to retrieve.</param>
        /// <returns>The value object for the specified key. If the exact key is not found, <see cref="ValueProviderResult.None" />.</returns>
        ValueProviderResult GetValue(string key);
    }
}
