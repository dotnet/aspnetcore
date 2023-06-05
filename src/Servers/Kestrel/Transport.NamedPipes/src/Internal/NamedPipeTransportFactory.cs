// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.NamedPipes.Internal;

internal sealed class NamedPipeTransportFactory : IConnectionListenerFactory, IConnectionListenerFactorySelector
{
    private const string LocalComputerServerName = ".";

    private readonly ILoggerFactory _loggerFactory;
    private readonly ObjectPoolProvider _objectPoolProvider;
    private readonly NamedPipeTransportOptions _options;

    public NamedPipeTransportFactory(
        ILoggerFactory loggerFactory,
        IOptions<NamedPipeTransportOptions> options,
        ObjectPoolProvider objectPoolProvider)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        Debug.Assert(OperatingSystem.IsWindows(), "Named pipes transport requires a Windows operating system.");

        _loggerFactory = loggerFactory;
        _objectPoolProvider = objectPoolProvider;
        _options = options.Value;
    }

    public ValueTask<IConnectionListener> BindAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        if (endpoint is not NamedPipeEndPoint namedPipeEndPoint)
        {
            throw new NotSupportedException($"{endpoint.GetType()} is not supported.");
        }
        if (namedPipeEndPoint.ServerName != LocalComputerServerName)
        {
            throw new NotSupportedException($@"Server name '{namedPipeEndPoint.ServerName}' is invalid. The server name must be ""{LocalComputerServerName}"".");
        }

        var listener = new NamedPipeConnectionListener(namedPipeEndPoint, _options, _loggerFactory, _objectPoolProvider);
        try
        {
            listener.Start();
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new AddressInUseException($"Named pipe '{namedPipeEndPoint.PipeName}' is already in use by Kestrel.", ex);
        }

        return new ValueTask<IConnectionListener>(listener);
    }

    public bool CanBind(EndPoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        return endpoint is NamedPipeEndPoint;
    }
}
