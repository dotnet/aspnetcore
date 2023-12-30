// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Https;

namespace Microsoft.AspNetCore.Server.Kestrel.Core;

/// <summary>
/// Describes either an <see cref="System.Net.IPEndPoint"/>, Unix domain socket path, named pipe name, or a file descriptor for an already open
/// socket that Kestrel should bind to or open.
/// </summary>
public class ListenOptions : IConnectionBuilder, IMultiplexedConnectionBuilder
{
    internal const HttpProtocols DefaultHttpProtocols = HttpProtocols.Http1AndHttp2;

    private readonly List<Func<ConnectionDelegate, ConnectionDelegate>> _middleware = new List<Func<ConnectionDelegate, ConnectionDelegate>>();
    private readonly List<Func<MultiplexedConnectionDelegate, MultiplexedConnectionDelegate>> _multiplexedMiddleware = new List<Func<MultiplexedConnectionDelegate, MultiplexedConnectionDelegate>>();
    private HttpProtocols _protocols = DefaultHttpProtocols;

    internal ListenOptions(EndPoint endPoint)
    {
        EndPoint = endPoint;
    }

    internal ListenOptions(string socketPath)
    {
        EndPoint = new UnixDomainSocketEndPoint(socketPath);
    }

    internal ListenOptions(ulong fileHandle)
        : this(fileHandle, FileHandleType.Auto)
    {
    }

    internal ListenOptions(ulong fileHandle, FileHandleType handleType)
    {
        EndPoint = new FileHandleEndPoint(fileHandle, handleType);
    }

    /// <summary>
    /// Gets the <see cref="System.Net.EndPoint"/>.
    /// </summary>
    public EndPoint EndPoint { get; internal set; }

    // For comparing bound endpoints to changed config during endpoint config reload.
    internal EndpointConfig? EndpointConfig { get; set; }

    // IPEndPoint is mutable so port 0 can be updated to the bound port.
    /// <summary>
    /// Gets the bound <see cref="System.Net.IPEndPoint"/>.
    /// </summary>
    /// <remarks>
    /// Only set if the <see cref="ListenOptions"/> is bound to a <see cref="System.Net.IPEndPoint"/>.
    /// </remarks>
    public IPEndPoint? IPEndPoint => EndPoint as IPEndPoint;

    /// <summary>
    /// Gets the bound absolute path to a Unix domain socket.
    /// </summary>
    /// <remarks>
    /// Only set if the <see cref="ListenOptions"/> is bound to a <see cref="UnixDomainSocketEndPoint"/>.
    /// </remarks>
    public string? SocketPath => (EndPoint as UnixDomainSocketEndPoint)?.ToString();

    /// <summary>
    /// Gets the bound pipe name to a name pipe server.
    /// </summary>
    /// <remarks>
    /// Only set if the <see cref="ListenOptions"/> is bound to a <see cref="NamedPipeEndPoint"/>.
    /// </remarks>
    public string? PipeName => (EndPoint as NamedPipeEndPoint)?.PipeName.ToString();

    /// <summary>
    /// Gets the bound file descriptor to a socket.
    /// </summary>
    /// <remarks>
    /// Only set if the <see cref="ListenOptions"/> is bound to a <see cref="FileHandleEndPoint"/>.
    /// </remarks>
    public ulong FileHandle => (EndPoint as FileHandleEndPoint)?.FileHandle ?? 0;

    /// <summary>
    /// Gets the <see cref="Core.KestrelServerOptions"/> for the listener options.
    /// Enables connection middleware to resolve and use services registered by the application during startup.
    /// </summary>
    /// <remarks>
    /// Only set if accessed from the callback of a <see cref="Core.KestrelServerOptions"/> Listen* method.
    /// </remarks>
    public KestrelServerOptions KestrelServerOptions { get; internal set; } = default!; // Set via ConfigureKestrel callback

    /// <summary>
    /// The protocols enabled on this endpoint.
    /// </summary>
    /// <remarks>Defaults to HTTP/1.x, HTTP/2, and HTTP/3.</remarks>
    public HttpProtocols Protocols
    {
        get => _protocols;
        set
        {
            _protocols = value;
            ProtocolsSetExplicitly = true;
        }
    }

    /// <summary>
    /// Tracks whether <see cref="Protocols"/> has been set explicitly so that we can determine whether
    /// or not the value reflects the user's intention.
    /// </summary>
    internal bool ProtocolsSetExplicitly { get; private set; }

    /// <summary>
    /// Gets or sets a value that controls whether the "Alt-Svc" header is included with response headers.
    /// The "Alt-Svc" header is used by clients to upgrade HTTP/1.1 and HTTP/2 connections to HTTP/3.
    /// <para>
    /// The "Alt-Svc" header is automatically included with a response if <see cref="Protocols"/> has either
    /// HTTP/1.1 or HTTP/2 enabled, and HTTP/3 is enabled. If an "Alt-Svc" header value has already been set
    /// by the app then it isn't changed.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Defaults to false.
    /// </remarks>
    public bool DisableAltSvcHeader { get; set; }

    /// <summary>
    /// Gets the application <see cref="IServiceProvider"/>.
    /// </summary>
    public IServiceProvider ApplicationServices => KestrelServerOptions?.ApplicationServices!; // TODO - Always available?

    internal string Scheme
    {
        get
        {
            return IsTls ? HttpProtocol.SchemeHttps : HttpProtocol.SchemeHttp;
        }
    }

    internal bool IsTls { get; set; }
    internal HttpsConnectionAdapterOptions? HttpsOptions { get; set; }
    internal TlsHandshakeCallbackOptions? HttpsCallbackOptions { get; set; }

    /// <summary>
    /// Gets the name of this endpoint to display on command-line when the web server starts.
    /// </summary>
    internal virtual string GetDisplayName()
    {
        switch (EndPoint)
        {
            case UnixDomainSocketEndPoint _:
                return $"{Scheme}://unix:{EndPoint}";
            case NamedPipeEndPoint namedPipeEndPoint:
                return $"{Scheme}://pipe:/{namedPipeEndPoint.PipeName}";
            case FileHandleEndPoint _:
                return $"{Scheme}://<file handle>";
            default:
                return $"{Scheme}://{EndPoint}";
        }
    }

    /// <inheritdoc />
    public override string? ToString() => GetDisplayName();

    /// <summary>
    /// Adds a middleware delegate to the connection pipeline.
    /// Configured by the <c>UseHttps()</c> and <see cref="Hosting.ListenOptionsConnectionLoggingExtensions.UseConnectionLogging(ListenOptions)"/>
    /// extension methods.
    /// </summary>
    /// <param name="middleware">The middleware delegate.</param>
    /// <returns>The <see cref="IConnectionBuilder"/>.</returns>
    public IConnectionBuilder Use(Func<ConnectionDelegate, ConnectionDelegate> middleware)
    {
        _middleware.Add(middleware);
        return this;
    }

    IMultiplexedConnectionBuilder IMultiplexedConnectionBuilder.Use(Func<MultiplexedConnectionDelegate, MultiplexedConnectionDelegate> middleware)
    {
        _multiplexedMiddleware.Add(middleware);
        return this;
    }

    /// <summary>
    /// Builds the <see cref="ConnectionDelegate"/>.
    /// </summary>
    /// <returns>The <see cref="ConnectionDelegate"/>.</returns>
    public ConnectionDelegate Build()
    {
        ConnectionDelegate app = context =>
        {
            return Task.CompletedTask;
        };

        for (var i = _middleware.Count - 1; i >= 0; i--)
        {
            var component = _middleware[i];
            app = component(app);
        }

        return app;
    }

    MultiplexedConnectionDelegate IMultiplexedConnectionBuilder.Build()
    {
        MultiplexedConnectionDelegate app = context =>
        {
            return Task.CompletedTask;
        };

        for (int i = _multiplexedMiddleware.Count - 1; i >= 0; i--)
        {
            var component = _multiplexedMiddleware[i];
            app = component(app);
        }

        return app;
    }

    internal virtual async Task BindAsync(AddressBindContext context, CancellationToken cancellationToken)
    {
        await AddressBinder.BindEndpointAsync(this, context, cancellationToken).ConfigureAwait(false);
        context.Addresses.Add(GetDisplayName());
    }

    /// <summary>
    /// used for cloning to two IPEndpoints
    /// </summary>
    /// <remarks>
    /// Internal for testing
    /// </remarks>
    protected internal ListenOptions Clone(IPAddress address)
    {
        var options = new ListenOptions(new IPEndPoint(address, IPEndPoint!.Port))
        {
            KestrelServerOptions = KestrelServerOptions,
            _protocols = _protocols, // Avoid side-effects from setting Protocols
            ProtocolsSetExplicitly = ProtocolsSetExplicitly,
            DisableAltSvcHeader = DisableAltSvcHeader,
            IsTls = IsTls,
            HttpsOptions = HttpsOptions,
            HttpsCallbackOptions = HttpsCallbackOptions,
            EndpointConfig = EndpointConfig
        };

        options._middleware.AddRange(_middleware);
        options._multiplexedMiddleware.AddRange(_multiplexedMiddleware);
        return options;
    }
}
