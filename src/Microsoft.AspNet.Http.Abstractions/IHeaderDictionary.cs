// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Framework.Primitives;

namespace Microsoft.AspNet.Http
{
    /// <summary>
    /// Represents request and response headers
    /// </summary>
    public interface IHeaderDictionary : IReadableStringCollection, IDictionary<string, StringValues>
    {
        // This property is duplicated to resolve an ambiguity between IReadableStringCollection and IDictionary<string, StringValues>
        /// <summary>
        ///
        /// </summary>
        /// <param name="key"></param>
        /// <returns>The stored value, or StringValues.Empty if the key is not present.</returns>
        new StringValues this[string key] { get; set; }

        // This property is duplicated to resolve an ambiguity between IReadableStringCollection.Count and IDictionary<string, StringValues>.Count
        /// <summary>
        /// Gets the number of elements contained in the collection.
        /// </summary>
        new int Count { get; }

        // This property is duplicated to resolve an ambiguity between IReadableStringCollection.Keys and IDictionary<string, StringValues>.Keys
        /// <summary>
        /// Gets a collection containing the keys.
        /// </summary>
        new ICollection<string> Keys { get; }

        /// <summary>
        /// Get the associated values from the collection separated into individual values.
        /// Quoted values will not be split, and the quotes will be removed.
        /// </summary>
        /// <param name="key">The header name.</param>
        /// <returns>the associated values from the collection separated into individual values, or null if the key is not present.</returns>
        StringValues GetCommaSeparatedValues(string key);

        /// <summary>
        /// Add a new value. Appends to the header list if already present
        /// </summary>
        /// <param name="key">The header name.</param>
        /// <param name="value">The header value.</param>
        void Append(string key, StringValues value);

        /// <summary>
        /// Quotes any values containing comas, and then coma joins all of the values with any existing values.
        /// </summary>
        /// <param name="key">The header name.</param>
        /// <param name="values">The header values.</param>
        void AppendCommaSeparatedValues(string key, params string[] values);

        /// <summary>
        /// Quotes any values containing comas, and then coma joins all of the values.
        /// </summary>
        /// <param name="key">The header name.</param>
        /// <param name="values">The header values.</param>
        void SetCommaSeparatedValues(string key, params string[] values);
    }
}
