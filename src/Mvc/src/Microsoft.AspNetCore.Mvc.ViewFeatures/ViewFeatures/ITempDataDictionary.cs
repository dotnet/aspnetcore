// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    /// <summary>
    /// Represents a set of data that persists only from one request to the next.
    /// </summary>
    public interface ITempDataDictionary : IDictionary<string, object>
    {
        /// <summary>
        /// Loads the dictionary by using the registered <see cref="ITempDataProvider"/>.
        /// </summary>
        void Load();

        /// <summary>
        /// Saves the dictionary by using the registered <see cref="ITempDataProvider"/>.
        /// </summary>
        void Save();

        /// <summary>
        /// Marks all keys in the dictionary for retention.
        /// </summary>
        void Keep();

        /// <summary>
        /// Marks the specified key in the dictionary for retention.
        /// </summary>
        /// <param name="key">The key to retain in the dictionary.</param>
        void Keep(string key);

        /// <summary>
        /// Returns an object that contains the element that is associated with the specified key,
        /// without marking the key for deletion.
        /// </summary>
        /// <param name="key">The key of the element to return.</param>
        /// <returns>An object that contains the element that is associated with the specified key.</returns>
        object Peek(string key);
    }
}