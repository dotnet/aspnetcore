// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Http;

/// <summary>
///     Represents the HttpRequest query string collection
/// </summary>
public interface IQueryCollection : IEnumerable<KeyValuePair<string, StringValues>>
{
    /// <summary>
    ///     Gets the number of elements contained in the <see cref="IQueryCollection" />.
    /// </summary>
    /// <returns>
    ///     The number of elements contained in the <see cref="IQueryCollection" />.
    /// </returns>
    int Count { get; }

    /// <summary>
    ///     Gets an <see cref="ICollection{T}" /> containing the keys of the
    ///     <see cref="IQueryCollection" />.
    /// </summary>
    /// <returns>
    ///     An <see cref="ICollection{T}" /> containing the keys of the object
    ///     that implements <see cref="IQueryCollection" />.
    /// </returns>
    ICollection<string> Keys { get; }

    /// <summary>
    ///     Determines whether the <see cref="IQueryCollection" /> contains an element
    ///     with the specified key.
    /// </summary>
    /// <param name="key">
    /// The key to locate in the <see cref="IQueryCollection" />.
    /// </param>
    /// <returns>
    ///     true if the <see cref="IQueryCollection" /> contains an element with
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
    ///    true if the object that implements <see cref="IQueryCollection" /> contains
    ///     an element with the specified key; otherwise, false.
    /// </returns>
    /// <exception cref="System.ArgumentNullException">
    ///     key is null.
    /// </exception>
    bool TryGetValue(string key, out StringValues value);

    /// <summary>
    ///     Gets the value with the specified key.
    /// </summary>
    /// <param name="key">
    ///     The key of the value to get.
    /// </param>
    /// <returns>
    ///     The element with the specified key, or <c>StringValues.Empty</c> if the key is not present.
    /// </returns>
    /// <exception cref="System.ArgumentNullException">
    ///     key is null.
    /// </exception>
    /// <remarks>
    ///     <see cref="IQueryCollection" /> has a different indexer contract than
    ///     <see cref="IDictionary{TKey, TValue}" />, as it will return <c>StringValues.Empty</c> for missing entries
    ///     rather than throwing an Exception.
    /// </remarks>
    StringValues this[string key] { get; }
}
