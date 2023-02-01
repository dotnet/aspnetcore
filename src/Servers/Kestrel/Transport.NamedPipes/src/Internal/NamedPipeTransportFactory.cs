// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.NamedPipes.Internal;

internal sealed class NamedPipeTransportFactory : IConnectionListenerFactory, IConnectionListenerFactorySelector
{
    private const string LocalComputerServerName = ".";

    private readonly ILoggerFactory _loggerFactory;
    private readonly NamedPipeTransportOptions _options;

    public NamedPipeTransportFactory(
        ILoggerFactory loggerFactory,
        IOptions<NamedPipeTransportOptions> options)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        Debug.Assert(OperatingSystem.IsWindows(), "Named pipes transport requires a Windows operating system.");

        _loggerFactory = loggerFactory;
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

        // Creating a named pipe server with an name isn't exclusive. Create a mutex with the pipe name to prevent multiple endpoints
        // accidently sharing the same pipe name. Will detect across Kestrel processes.
        // Note that this doesn't prevent other applications from using the pipe name.
        var mutexName = "Kestrel-NamedPipe-" + namedPipeEndPoint.PipeName;
        var mutex = new Mutex(false, mutexName, out var createdNew);
        if (!createdNew)
        {
            mutex.Dispose();
            throw new AddressInUseException($"Named pipe '{namedPipeEndPoint.PipeName}' is already in use by Kestrel.");
        }

        var listener = new NamedPipeConnectionListener(namedPipeEndPoint, _options, _loggerFactory, mutex);
        listener.Start();

        return new ValueTask<IConnectionListener>(listener);
    }

    public bool CanBind(EndPoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        return endpoint is NamedPipeEndPoint;
    }
}
