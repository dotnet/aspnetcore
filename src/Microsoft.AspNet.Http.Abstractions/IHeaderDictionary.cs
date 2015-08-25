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
    }
}
