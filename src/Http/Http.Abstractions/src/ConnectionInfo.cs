// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Represents the underlying connection for a request.
/// </summary>
[DebuggerDisplay("{DebuggerToString(),nq}")]
[DebuggerTypeProxy(typeof(ConnectionInfoDebugView))]
public abstract class ConnectionInfo
{
    /// <summary>
    /// Gets or sets a unique identifier to represent this connection.
    /// </summary>
    public abstract string Id { get; set; }

    /// <summary>
    /// Gets or sets the IP address of the remote target. Can be null.
    /// </summary>
    /// <remarks>
    /// The result is <c>null</c> if the connection isn't a TCP connection, e.g., a Unix Domain Socket or a transport that isn't TCP based.
    /// </remarks>
    public abstract IPAddress? RemoteIpAddress { get; set; }

    /// <summary>
    /// Gets or sets the port of the remote target.
    /// </summary>
    public abstract int RemotePort { get; set; }

    /// <summary>
    /// Gets or sets the IP address of the local host.
    /// </summary>
    public abstract IPAddress? LocalIpAddress { get; set; }

    /// <summary>
    /// Gets or sets the port of the local host.
    /// </summary>
    public abstract int LocalPort { get; set; }

    /// <summary>
    /// Gets or sets the client certificate.
    /// </summary>
    public abstract X509Certificate2? ClientCertificate { get; set; }

    /// <summary>
    /// Retrieves the client certificate.
    /// </summary>
    /// <returns>Asynchronously returns an <see cref="X509Certificate2" />. Can be null.</returns>
    public abstract Task<X509Certificate2?> GetClientCertificateAsync(CancellationToken cancellationToken = new CancellationToken());

    /// <summary>
    /// Close connection gracefully.
    /// </summary>
    public virtual void RequestClose()
    {
    }

    private string DebuggerToString()
    {
        var remoteEndpoint = RemoteIpAddress == null ? "(null)" : new IPEndPoint(RemoteIpAddress, RemotePort).ToString();
        var localEndpoint = LocalIpAddress == null ? "(null)" : new IPEndPoint(LocalIpAddress, LocalPort).ToString();

        var s = $"Id = {Id ?? "(null)"}, Remote = {remoteEndpoint}, Local = {localEndpoint}";
        if (ClientCertificate != null)
        {
            s += $", ClientCertificate = {ClientCertificate.Subject}";
        }
        return s;
    }

    private sealed class ConnectionInfoDebugView(ConnectionInfo info)
    {
        private readonly ConnectionInfo _info = info;

        public string Id => _info.Id;
        public IPAddress? RemoteIpAddress => _info.RemoteIpAddress;
        public int RemotePort => _info.RemotePort;
        public IPAddress? LocalIpAddress => _info.LocalIpAddress;
        public int LocalPort => _info.LocalPort;
        public X509Certificate2? ClientCertificate => _info.ClientCertificate;
    }
}
