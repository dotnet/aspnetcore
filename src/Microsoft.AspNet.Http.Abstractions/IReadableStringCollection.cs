// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Framework.Primitives;

namespace Microsoft.AspNet.Http
{
    /// <summary>
    /// Accessors for headers, query, forms, etc.
    /// </summary>
    public interface IReadableStringCollection : IEnumerable<KeyValuePair<string, StringValues>>
    {
        /// <summary>
        /// Get the associated value from the collection.
        /// Returns StringValues.Empty if the key is not present.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        StringValues this[string key] { get; }

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
    }
}
