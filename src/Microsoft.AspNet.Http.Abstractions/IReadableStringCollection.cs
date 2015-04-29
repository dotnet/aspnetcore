// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNet.Http
{
    /// <summary>
    /// Accessors for headers, query, forms, etc.
    /// </summary>
    public interface IReadableStringCollection : IEnumerable<KeyValuePair<string, string[]>>
    {
        /// <summary>
        /// Get the associated value from the collection.  Multiple values will be merged.
        /// Returns null if the key is not present.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        string this[string key] { get; }

        /// <summary>
        /// Gets the number of elements contained in the collection.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets a collection containing the keys.
        /// </summary>
        ICollection<string> Keys { get; }

        /// <summary>
        /// Determines whether the collection contains an element with the specified key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        bool ContainsKey(string key);

        /// <summary>
        /// Get the associated value from the collection.  Multiple values will be merged.
        /// Returns null if the key is not present.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Get", Justification = "Re-evaluate later.")]
        string Get(string key);

        /// <summary>
        /// Get the associated values from the collection in their original format.
        /// Returns null if the key is not present.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        IList<string> GetValues(string key);
    }
}
