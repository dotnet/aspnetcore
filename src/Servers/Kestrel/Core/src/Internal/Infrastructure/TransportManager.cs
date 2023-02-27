// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

internal sealed class TransportManager : TransportManagerBase, ITransportManager
{
    private readonly List<IConnectionListenerFactory> _factories;

    public TransportManager(
        ServiceContext serviceContext,
        IEnumerable<IConnectionListenerFactory> factories)
        : base(serviceContext)
    {
        _factories = factories.Reverse().ToList();
    }

    public override bool HasFactories => _factories.Count > 0;

    public async Task<EndPoint> BindAsync(EndPoint endPoint, ConnectionDelegate connectionDelegate, EndpointConfig? endpointConfig, CancellationToken cancellationToken)
    {
        if (!HasFactories)
        {
            throw new InvalidOperationException($"Cannot bind with {nameof(ConnectionDelegate)} no {nameof(IConnectionListenerFactory)} is registered.");
        }

        foreach (var factory in _factories)
        {
            var selector = factory as IConnectionListenerFactorySelector;
            if (CanBindFactory(endPoint, selector))
            {
                var transport = await factory.BindAsync(endPoint, cancellationToken).ConfigureAwait(false);
                StartAcceptLoop(new GenericConnectionListener(transport), c => connectionDelegate(c), endpointConfig);
                return transport.EndPoint;
            }
        }

        // Special case situation where a named pipe endpoint is specified and there is no matching transport.
        // The named pipe transport is only registered on Windows. The test is done at this point so there is
        // the opportunity for the app to register their own transport to handle named pipe endpoints.
        if (endPoint is NamedPipeEndPoint && !OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Named pipes transport requires a Windows operating system.");
        }

        throw new InvalidOperationException($"No registered {nameof(IConnectionListenerFactory)} supports endpoint {endPoint.GetType().Name}: {endPoint}");
    }

    private sealed class GenericConnectionListener : IConnectionListener<ConnectionContext>
    {
        private readonly IConnectionListener _connectionListener;

        public GenericConnectionListener(IConnectionListener connectionListener)
        {
            _connectionListener = connectionListener;
        }

        public EndPoint EndPoint => _connectionListener.EndPoint;

        public ValueTask<ConnectionContext?> AcceptAsync(CancellationToken cancellationToken = default)
             => _connectionListener.AcceptAsync(cancellationToken);

        public ValueTask UnbindAsync(CancellationToken cancellationToken = default)
            => _connectionListener.UnbindAsync(cancellationToken);

        public ValueTask DisposeAsync()
            => _connectionListener.DisposeAsync();
    }
}
