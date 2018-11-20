// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Represents the HttpRequest cookie collection
    /// </summary>
    public interface IRequestCookieCollection : IEnumerable<KeyValuePair<string, string>>
    {
        /// <summary>
        ///     Gets the number of elements contained in the <see cref="IRequestCookieCollection" />.
        /// </summary>
        /// <returns>
        ///     The number of elements contained in the <see cref="IRequestCookieCollection" />.
        /// </returns>
        int Count { get; }

        /// <summary>
        ///     Gets an <see cref="ICollection{T}" /> containing the keys of the
        ///     <see cref="IRequestCookieCollection" />.
        /// </summary>
        /// <returns>
        ///     An <see cref="ICollection{T}" /> containing the keys of the object
        ///     that implements <see cref="IRequestCookieCollection" />.
        /// </returns>
        ICollection<string> Keys { get; }

        /// <summary>
        ///     Determines whether the <see cref="IRequestCookieCollection" /> contains an element
        ///     with the specified key.
        /// </summary>
        /// <param name="key">
        /// The key to locate in the <see cref="IRequestCookieCollection" />.
        /// </param>
        /// <returns>
        ///     true if the <see cref="IRequestCookieCollection" /> contains an element with
        ///     the key; otherwise, false.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        ///     key is null.
        /// </exception>
        bool ContainsKey(string key);

        /// <summary>
        ///    Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">
        ///     The key of the value to get.
        /// </param>
        /// <param name="value">
        ///     The key of the value to get.
        ///     When this method returns, the value associated with the specified key, if the
        ///     key is found; otherwise, the default value for the type of the value parameter.
        ///     This parameter is passed uninitialized.
        /// </param>
        /// <returns>
        ///    true if the object that implements <see cref="IRequestCookieCollection" /> contains
        ///     an element with the specified key; otherwise, false.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        ///     key is null.
        /// </exception>
        bool TryGetValue(string key, out string value);

        /// <summary>
        ///     Gets the value with the specified key.
        /// </summary>
        /// <param name="key">
        ///     The key of the value to get.
        /// </param>
        /// <returns>
        ///     The element with the specified key, or <c>string.Empty</c> if the key is not present.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        ///     key is null.
        /// </exception>
        /// <remarks>
        ///     <see cref="IRequestCookieCollection" /> has a different indexer contract than
        ///     <see cref="IDictionary{TKey, TValue}" />, as it will return <c>string.Empty</c> for missing entries
        ///     rather than throwing an Exception.
        /// </remarks>
        string this[string key] { get; }
    }
}
