// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Channels;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.SignalR.Client.InMemory;

/// <summary>
/// A channel for passing <see cref="ConnectionContext"/> instances between an in-memory
/// <see cref="IConnectionFactory"/> and the server-side connection handler.
/// </summary>
/// <remarks>
/// <para>
/// This type is keyed by the Hub type to allow multiple hubs to use in-memory connections independently.
/// Register it in DI using the <c>AddInMemoryHubConnection</c> extension method on <see cref="IServiceCollection"/>.
/// </para>
/// </remarks>
/// <typeparam name="THub">The Hub type this channel is associated with.</typeparam>
public sealed class InMemoryConnectionChannel<THub> where THub : class
{
    private readonly Channel<ConnectionContext> _channel;

    /// <summary>
    /// Initializes a new instance of <see cref="InMemoryConnectionChannel{THub}"/>.
    /// </summary>
    /// <param name="connectionHandlerType">The closed generic <c>HubConnectionHandler&lt;THub&gt;</c> type
    /// used to dispatch incoming in-memory connections.</param>
    public InMemoryConnectionChannel(Type connectionHandlerType)
    {
        ArgumentNullThrowHelper.ThrowIfNull(connectionHandlerType);

        if (!typeof(ConnectionHandler).IsAssignableFrom(connectionHandlerType))
        {
            throw new ArgumentException(
                $"The type '{connectionHandlerType.FullName}' must derive from '{nameof(ConnectionHandler)}'.",
                nameof(connectionHandlerType));
        }

        ConnectionHandlerType = connectionHandlerType;

        _channel = Channel.CreateUnbounded<ConnectionContext>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });
    }

    /// <summary>
    /// Gets the <see cref="ConnectionHandler"/> type used to dispatch incoming connections.
    /// </summary>
    internal Type ConnectionHandlerType { get; }

    /// <summary>
    /// Gets the writer side of the channel, used by the <see cref="InMemoryConnectionFactory{THub}"/> to submit new connections.
    /// </summary>
    internal ChannelWriter<ConnectionContext> Writer => _channel.Writer;

    /// <summary>
    /// Gets the reader side of the channel, used by the server-side hosted service to accept new connections.
    /// </summary>
    internal ChannelReader<ConnectionContext> Reader => _channel.Reader;
}
