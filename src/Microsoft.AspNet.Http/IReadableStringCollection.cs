// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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

        // Joined

        /// <summary>
        /// Get the associated value from the collection.  Multiple values will be merged.
        /// Returns null if the key is not present.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Get", Justification = "Re-evaluate later.")]
        string Get(string key);

        // Joined

        /// <summary>
        /// Get the associated values from the collection in their original format.
        /// Returns null if the key is not present.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        IList<string> GetValues(string key);

        // Raw
    }
}
