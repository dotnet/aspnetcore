// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Internal;

internal sealed partial class QuicConnectionContext : IProtocolErrorCodeFeature, ITlsConnectionFeature
{
    private X509Certificate2? _clientCert;
    private Task<X509Certificate2?>? _clientCertTask;
    private long? _error;

    public long Error
    {
        get => _error ?? -1;
        set
        {
            QuicTransportOptions.ValidateErrorCode(value);
            _error = value;
        }
    }

    public X509Certificate2? ClientCertificate
    {
        get { return _clientCert ??= ConvertToX509Certificate2(_connection.RemoteCertificate); }
        set
        {
            _clientCert = value;
            _clientCertTask = Task.FromResult(value);
        }
    }

    public Task<X509Certificate2?> GetClientCertificateAsync(CancellationToken cancellationToken)
    {
        return _clientCertTask ??= Task.FromResult(ClientCertificate);
    }

    private void InitializeFeatures()
    {
        _currentIProtocolErrorCodeFeature = this;
        _currentITlsConnectionFeature = this;
    }

    private static X509Certificate2? ConvertToX509Certificate2(X509Certificate? certificate)
    {
        return certificate switch
        {
            null => null,
            X509Certificate2 cert2 => cert2,
            _ => new X509Certificate2(certificate),
        };
    }
}
