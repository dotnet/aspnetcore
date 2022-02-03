// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Concurrent;

namespace Microsoft.AspNetCore.SignalR;

/// <summary>
/// Stores <see cref="HubConnectionContext"/>s by ID.
/// </summary>
/// <remarks>
/// This API is meant for internal usage.
/// </remarks>
public class HubConnectionStore
{
    private readonly ConcurrentDictionary<string, HubConnectionContext> _connections =
        new ConcurrentDictionary<string, HubConnectionContext>(StringComparer.Ordinal);

    /// <summary>
    /// Get the <see cref="HubConnectionContext"/> by connection ID.
    /// </summary>
    /// <param name="connectionId">The ID of the connection.</param>
    /// <returns>The connection for the <paramref name="connectionId"/>, null if there is no connection.</returns>
    public HubConnectionContext? this[string connectionId]
    {
        get
        {
            _connections.TryGetValue(connectionId, out var connection);
            return connection;
        }
    }

    /// <summary>
    /// The number of connections in the store.
    /// </summary>
    public int Count => _connections.Count;

    /// <summary>
    /// Add a <see cref="HubConnectionContext"/> to the store.
    /// </summary>
    /// <param name="connection">The connection to add.</param>
    public void Add(HubConnectionContext connection)
    {
        _connections.TryAdd(connection.ConnectionId, connection);
    }

    /// <summary>
    /// Removes a <see cref="HubConnectionContext"/> from the store.
    /// </summary>
    /// <param name="connection">The connection to remove.</param>
    public void Remove(HubConnectionContext connection)
    {
        _connections.TryRemove(connection.ConnectionId, out _);
    }

    /// <summary>
    /// Gets an enumerator over the connection store.
    /// </summary>
    /// <returns>The <see cref="Enumerator"/> over the connections.</returns>
    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    /// <summary>
    /// An <see cref="IEnumerator"/> over the <see cref="HubConnectionStore"/>
    /// </summary>
    public readonly struct Enumerator : IEnumerator<HubConnectionContext>
    {
        private readonly IEnumerator<KeyValuePair<string, HubConnectionContext>> _enumerator;

        /// <summary>
        /// Constructs the <see cref="Enumerator"/> over the <see cref="HubConnectionStore"/>.
        /// </summary>
        /// <param name="hubConnectionList">The store of connections to enumerate over.</param>
        public Enumerator(HubConnectionStore hubConnectionList)
        {
            _enumerator = hubConnectionList._connections.GetEnumerator();
        }

        /// <summary>
        /// The current connection the enumerator is on.
        /// </summary>
        public HubConnectionContext Current => _enumerator.Current.Value;

        object IEnumerator.Current => Current;

        /// <summary>
        /// Disposes the enumerator.
        /// </summary>
        public void Dispose() => _enumerator.Dispose();

        /// <summary>
        /// Moves the enumerator to the next value.
        /// </summary>
        /// <returns>True if there is another connection. False if there are no more connections.</returns>
        public bool MoveNext() => _enumerator.MoveNext();

        /// <summary>
        /// Resets the enumerator to the beginning.
        /// </summary>
        public void Reset() => _enumerator.Reset();
    }
}
