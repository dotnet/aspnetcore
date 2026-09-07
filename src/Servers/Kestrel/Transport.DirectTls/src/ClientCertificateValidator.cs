// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls;

/// <summary>
/// Runs the endpoint's client-certificate validation for a completed handshake on the Linux fd fast path.
/// The runtime cannot enforce the accept/reject decision on that path (OpenSSL only applies SSL_VERIFY_PEER,
/// not FAIL_IF_NO_PEER_CERT, and the fd read/write fast paths bypass the runtime's pending-validation fault),
/// so the transport builds the peer's chain and invokes the endpoint's callback itself, exactly as
/// <see cref="System.Net.Security.SslStream"/> does for server-side client-certificate validation.
/// This logic is factored out of the native pump loop so it can be unit tested without epoll or a live TLS session.
/// </summary>
internal static class ClientCertificateValidator
{
    /// <summary>
    /// Builds the <see cref="X509Chain"/> used to validate a peer's client certificate, mirroring
    /// <see cref="SslStream"/>'s default server-side client-certificate validation policy.
    /// </summary>
    /// <remarks>
    /// The chain build and the endpoint callback run on the thread pool, never on the pump thread.
    /// <list type="bullet">
    /// <item>
    /// <see cref="X509RevocationMode.NoCheck"/> avoids blocking on CRL/OCSP network I/O and matches the
    /// transport default (<c>CheckCertificateRevocation == false</c>).
    /// </item>
    /// <item>
    /// <see cref="X509ChainPolicy.DisableCertificateDownloads"/> is <see langword="true"/> so the chain
    /// engine never makes synchronous AIA fetches for missing intermediates. Otherwise a supplied
    /// leaf whose Authority Information Access extension points at an unreachable or slow URL would occupy a
    /// thread pool thread for the lifetime of that fetch, one per connection.
    /// <see cref="SslStream"/> sets the same flag on the server side for this reason.
    /// Legitimate clients send their intermediates in the handshake, which are supplied here via <paramref name="intermediates"/>.
    /// </item>
    /// </list>
    /// The caller owns the returned chain and must dispose it.
    /// </remarks>
    /// <param name="intermediates">
    /// Intermediate certificates the peer sent during the handshake, added to the chain's extra store so a
    /// valid chain can be built without any network fetch. May be <see langword="null"/>.
    /// </param>
    internal static X509Chain BuildChain(X509Certificate2Collection? intermediates)
    {
        var chain = new X509Chain();
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        chain.ChainPolicy.DisableCertificateDownloads = true;
        if (intermediates is not null)
        {
            chain.ChainPolicy.ExtraStore.AddRange(intermediates);
        }

        return chain;
    }

    /// <summary>
    /// Runs the endpoint's client-certificate validation callback and returns whether the connection is
    /// accepted.
    /// </summary>
    /// <param name="sender">The sender passed to the callback (the peer's TLS session).</param>
    /// <param name="presentedCertificate">
    /// The peer's leaf certificate, or <see langword="null"/> when the client presented none.
    /// </param>
    /// <param name="intermediates">
    /// Intermediate certificates the peer sent, used to build the chain. Ignored when
    /// <paramref name="presentedCertificate"/> is <see langword="null"/>.
    /// </param>
    /// <param name="validateClientCertificate">The endpoint's validation callback. Must not be null.</param>
    /// <returns><see langword="true"/> to accept the connection; <see langword="false"/> to reject it.</returns>
    internal static bool Validate(
        object sender,
        X509Certificate2? presentedCertificate,
        X509Certificate2Collection? intermediates,
        RemoteCertificateValidationCallback validateClientCertificate)
    {
        if (presentedCertificate is null)
        {
            // No client certificate presented. The endpoint's callback encodes the mode:
            // AllowCertificate accepts (returns true); RequireCertificate rejects (returns false).
            return validateClientCertificate(sender, null, null, SslPolicyErrors.RemoteCertificateNotAvailable);
        }

        // Build the peer's chain so the endpoint's callback and any operator ClientCertificateValidation
        // delegate observe a real chain and errors (mirrors SslStream's default client-certificate
        // validation). The chain is untrusted unless the issuing CA is in the machine store; the callback
        // decides whether to accept despite RemoteCertificateChainErrors.
        using var chain = BuildChain(intermediates);
        var sslPolicyErrors = chain.Build(presentedCertificate)
            ? SslPolicyErrors.None
            : SslPolicyErrors.RemoteCertificateChainErrors;

        return validateClientCertificate(sender, presentedCertificate, chain, sslPolicyErrors);
    }
}
