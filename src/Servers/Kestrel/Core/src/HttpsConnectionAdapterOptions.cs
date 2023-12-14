// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Microsoft.AspNetCore.Server.Kestrel.Https;

/// <summary>
/// Settings for how Kestrel should handle HTTPS connections.
/// </summary>
public class HttpsConnectionAdapterOptions
{
    internal static TimeSpan DefaultHandshakeTimeout = TimeSpan.FromSeconds(10);

    private TimeSpan _handshakeTimeout;

    /// <summary>
    /// Initializes a new instance of <see cref="HttpsConnectionAdapterOptions"/>.
    /// </summary>
    public HttpsConnectionAdapterOptions()
    {
        ClientCertificateMode = ClientCertificateMode.NoCertificate;
        HandshakeTimeout = DefaultHandshakeTimeout;
    }

    /// <summary>
    /// <para>
    /// Specifies the server certificate information presented when an https connection is initiated. This is ignored if ServerCertificateSelector is set.
    /// </para>
    /// <para>
    /// If the server certificate has an Extended Key Usage extension, the usages must include Server Authentication (OID 1.3.6.1.5.5.7.3.1).
    /// </para>
    /// </summary>
    public X509Certificate2? ServerCertificate { get; set; }

    /// <summary>
    /// <para>
    /// Specifies the full server certificate chain presented when an https connection is initiated
    /// </para>
    /// </summary>
    public X509Certificate2Collection? ServerCertificateChain { get; set; }

    /// <summary>
    /// <para>
    /// A callback that will be invoked to dynamically select a server certificate. This is higher priority than ServerCertificate.
    /// If SNI is not available then the name parameter will be null. The <see cref="ConnectionContext"/> will be null for HTTP/3 connections.
    /// </para>
    /// <para>
    /// If the server certificate has an Extended Key Usage extension, the usages must include Server Authentication (OID 1.3.6.1.5.5.7.3.1).
    /// </para>
    /// </summary>
    public Func<ConnectionContext?, string?, X509Certificate2?>? ServerCertificateSelector { get; set; }

    /// <summary>
    /// Convenient shorthand for a common check.
    /// </summary>
    internal bool HasServerCertificateOrSelector => ServerCertificate is not null || ServerCertificateSelector is not null;

    /// <summary>
    /// Specifies the client certificate requirements for a HTTPS connection. Defaults to <see cref="ClientCertificateMode.NoCertificate"/>.
    /// </summary>
    public ClientCertificateMode ClientCertificateMode { get; set; }

    /// <summary>
    /// Specifies a callback for additional client certificate validation that will be invoked during authentication. This will be ignored
    /// if <see cref="AllowAnyClientCertificate"/> is called after this callback is set.
    /// </summary>
    public Func<X509Certificate2, X509Chain?, SslPolicyErrors, bool>? ClientCertificateValidation { get; set; }

    /// <summary>
    /// Specifies allowable SSL protocols. Defaults to <see cref="SslProtocols.None" /> which allows the operating system to choose the best protocol to use,
    /// and to block protocols that are not secure. Unless your app has a specific reason not to, you should use this default.
    /// </summary>
    public SslProtocols SslProtocols { get; set; }

    /// <summary>
    /// Specifies whether the certificate revocation list is checked during authentication.
    /// </summary>
    public bool CheckCertificateRevocation { get; set; }

    /// <summary>
    /// Overrides the current <see cref="ClientCertificateValidation"/> callback and allows any client certificate.
    /// </summary>
    public void AllowAnyClientCertificate()
    {
        ClientCertificateValidation = (_, __, ___) => true;
    }

    /// <summary>
    /// Provides direct configuration of the <see cref="SslServerAuthenticationOptions"/> on a per-connection basis.
    /// This is called after all of the other settings have already been applied.
    /// </summary>
    public Action<ConnectionContext, SslServerAuthenticationOptions>? OnAuthenticate { get; set; }

    /// <summary>
    /// Specifies the maximum amount of time allowed for the TLS/SSL handshake. This must be positive
    /// or <see cref="Timeout.InfiniteTimeSpan"/>. Defaults to 10 seconds.
    /// </summary>
    public TimeSpan HandshakeTimeout
    {
        get => _handshakeTimeout;
        set
        {
            if (value <= TimeSpan.Zero && value != Timeout.InfiniteTimeSpan)
            {
                throw new ArgumentOutOfRangeException(nameof(value), CoreStrings.PositiveTimeSpanRequired);
            }
            _handshakeTimeout = value != Timeout.InfiniteTimeSpan ? value : TimeSpan.MaxValue;
        }
    }
}
