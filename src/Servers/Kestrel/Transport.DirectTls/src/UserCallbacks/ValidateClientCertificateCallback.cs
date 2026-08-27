// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.Connection;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.UserCallbacks;

/// <summary>
/// The client-certificate validation suspension: the chain build plus the endpoint's validation callback, run
/// once the handshake reports <c>Complete</c>.
/// </summary>
internal sealed class ValidateClientCertificateCallback : HandshakeUserCallback
{
    private readonly RemoteCertificateValidationCallback _validateClientCertificate;
    private readonly object _validationSender;
    private readonly X509Certificate2Collection? _intermediates;

    /// <summary>
    /// Creates the work item for the client-certificate validation suspension. The certificates were read from
    /// the session on the pump thread; the chain build and the endpoint's callback run here.
    /// </summary>
    public ValidateClientCertificateCallback(
        TlsEventPump pump,
        int fd,
        DirectTlsConnection? connection,
        object validationSender,
        X509Certificate2? presentedCertificate,
        X509Certificate2Collection? intermediates,
        RemoteCertificateValidationCallback validateClientCertificate)
        : base(pump, fd, connection)
    {
        _validationSender = validationSender;
        PresentedCertificate = presentedCertificate;
        _intermediates = intermediates;
        _validateClientCertificate = validateClientCertificate;
    }

    /// <summary>The peer's leaf certificate, or null when it presented none.</summary>
    public X509Certificate2? PresentedCertificate { get; }

    /// <summary>Whether the endpoint's validation callback accepted the peer's certificate.</summary>
    public bool CertificateAccepted { get; private set; }

    /// <inheritdoc />
    protected override void RunUserCode()
        => CertificateAccepted = ClientCertificateValidator.Validate(
            _validationSender,
            PresentedCertificate,
            _intermediates,
            _validateClientCertificate);
}
