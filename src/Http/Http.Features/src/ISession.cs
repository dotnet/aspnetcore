// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Http
{
    public interface ISession
    {
        /// <summary>
        /// Indicate whether the current session has loaded.
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// A unique identifier for the current session. This is not the same as the session cookie
        /// since the cookie lifetime may not be the same as the session entry lifetime in the data store.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Enumerates all the keys, if any.
        /// </summary>
        IEnumerable<string> Keys { get; }

        /// <summary>
        /// Load the session from the data store. This may throw if the data store is unavailable.
        /// </summary>
        /// <returns></returns>
        Task LoadAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Store the session in the data store. This may throw if the data store is unavailable.
        /// </summary>
        /// <returns></returns>
        Task CommitAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Retrieve the value of the given key, if present.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        bool TryGetValue(string key, out byte[] value);

        /// <summary>
        /// Set the given key and value in the current session. This will throw if the session
        /// was not established prior to sending the response.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        void Set(string key, byte[] value);

        /// <summary>
        /// Remove the given key from the session if present.
        /// </summary>
        /// <param name="key"></param>
        void Remove(string key);

        /// <summary>
        /// Remove all entries from the current session, if any.
        /// The session cookie is not removed.
        /// </summary>
        void Clear();
    }
}