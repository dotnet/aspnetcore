// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.Tests;

/// <summary>
/// Unit tests for <see cref="ClientCertificateValidator"/>, the client-certificate validation that runs on
/// the pump thread after a handshake completes. These exercise the chain-policy hardening and every
/// accept/reject branch without epoll or a live TLS session.
/// </summary>
public class ClientCertificateValidatorTests
{
    // ── Chain policy ──────────────────────────────────────────────────────────

    [Fact]
    public void BuildChain_DisablesRevocationChecksAndAiaDownloads()
    {
        using var chain = ClientCertificateValidator.BuildChain(intermediates: null);

        Assert.Equal(X509RevocationMode.NoCheck, chain.ChainPolicy.RevocationMode);
        Assert.True(chain.ChainPolicy.DisableCertificateDownloads);
        Assert.Empty(chain.ChainPolicy.ExtraStore);
    }

    [Fact]
    public void BuildChain_AddsHandshakeIntermediatesToExtraStore()
    {
        using var intermediateA = CreateSelfSignedCertificate("CN=directtls-intermediate-a");
        using var intermediateB = CreateSelfSignedCertificate("CN=directtls-intermediate-b");
        var intermediates = new X509Certificate2Collection { intermediateA, intermediateB };

        using var chain = ClientCertificateValidator.BuildChain(intermediates);

        Assert.Equal(2, chain.ChainPolicy.ExtraStore.Count);
        Assert.Contains(intermediateA, chain.ChainPolicy.ExtraStore);
        Assert.Contains(intermediateB, chain.ChainPolicy.ExtraStore);
    }

    /// <summary>
    /// Proves why <see cref="ClientCertificateValidator.BuildChain"/> sets
    /// <see cref="X509ChainPolicy.DisableCertificateDownloads"/>: a leaf whose issuer is missing but named by
    /// an Authority Information Access (AIA) URL must not cause the chain engine to reach out over the
    /// network. Building on the pump thread with the flag set makes zero connections to the AIA URL; a
    /// control chain with downloads enabled is used to confirm the platform would otherwise fetch it.
    /// </summary>
    [Fact]
    public async Task BuildChain_DoesNotFetchMissingIntermediateOverAia()
    {
        // A loopback sink that records every inbound connection. If chain building tried to download the
        // missing issuer named by the leaf's AIA extension, it would connect here.
        var sink = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        sink.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        sink.Listen(16);
        var sinkPort = ((IPEndPoint)sink.LocalEndPoint!).Port;

        var connectionsSeen = 0;
        var acceptLoop = Task.Run(async () =>
        {
            while (true)
            {
                Socket accepted;
                try
                {
                    accepted = await sink.AcceptAsync();
                }
                catch
                {
                    break; // sink disposed - stop accepting
                }

                Interlocked.Increment(ref connectionsSeen);
                accepted.Dispose();
            }
        });

        int afterHardened;
        int afterControl;
        try
        {
            var now = DateTimeOffset.UtcNow;

            // Issuing CA - deliberately NOT supplied to the chain, so the only route to it is the AIA URL.
            using var caKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var caRequest = new CertificateRequest("CN=DirectTls Test Issuing CA", caKey, HashAlgorithmName.SHA256);
            caRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(certificateAuthority: true, hasPathLengthConstraint: false, pathLengthConstraint: 0, critical: true));
            using var ca = caRequest.CreateSelfSigned(now.AddDays(-1), now.AddDays(1));

            // Leaf signed by the CA, carrying a caIssuers AIA URL that points at the sink.
            using var leafKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var leafRequest = new CertificateRequest("CN=directtls-client", leafKey, HashAlgorithmName.SHA256);
            leafRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(certificateAuthority: false, hasPathLengthConstraint: false, pathLengthConstraint: 0, critical: true));
            leafRequest.CertificateExtensions.Add(new X509AuthorityInformationAccessExtension(
                ocspUris: null,
                caIssuersUris: new[] { $"http://127.0.0.1:{sinkPort}/issuer.cer" }));
            using var leaf = leafRequest.Create(ca, now.AddDays(-1), now.AddDays(1), new byte[] { 0x01, 0x02, 0x03, 0x04 });

            // Our hardened chain: DisableCertificateDownloads = true. No AIA fetch may occur.
            using (var chain = ClientCertificateValidator.BuildChain(intermediates: null))
            {
                Assert.False(chain.Build(leaf)); // issuer missing -> chain cannot complete
            }

            await Task.Delay(500); // allow any stray fetch to arrive
            afterHardened = Volatile.Read(ref connectionsSeen);

            // Positive control: an otherwise-identical chain with downloads ENABLED. If this platform performs
            // AIA downloads at all, it connects to the sink here - proving the flag above is what suppresses it.
            using (var control = new X509Chain())
            {
                control.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                control.ChainPolicy.DisableCertificateDownloads = false;
                control.Build(leaf);
            }

            await Task.Delay(1000);
            afterControl = Volatile.Read(ref connectionsSeen);
        }
        finally
        {
            sink.Dispose();
        }

        await acceptLoop;

        // The hardened chain (DisableCertificateDownloads = true) must never reach out to the AIA URL.
        Assert.Equal(0, afterHardened);

        // Positive proof of the flag's purpose: with downloads enabled the platform connects to the AIA URL,
        // so the hardened count is strictly lower. On a platform that performs no AIA download at all the
        // control also sees zero connections; the negative assertion above and the BuildChain_* policy tests
        // still guard the flag in that case.
        if (afterControl > 0)
        {
            Assert.True(afterControl > afterHardened);
        }
    }

    // ── Validate: no certificate presented ────────────────────────────────────

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Validate_NoCertificatePresented_InvokesCallbackWithNotAvailable_AndReturnsDecision(bool callbackDecision)
    {
        var callback = new CapturingCallback { ReturnValue = callbackDecision };
        var sender = new object();

        var accepted = ClientCertificateValidator.Validate(sender, presentedCertificate: null, intermediates: null, callback.Invoke);

        Assert.Equal(callbackDecision, accepted);
        Assert.True(callback.WasInvoked);
        Assert.Same(sender, callback.Sender);
        Assert.Null(callback.Certificate);
        Assert.Null(callback.Chain);
        Assert.Equal(SslPolicyErrors.RemoteCertificateNotAvailable, callback.Errors);
    }

    // ── Validate: certificate presented ───────────────────────────────────────

    [Fact]
    public void Validate_UntrustedCertificatePresented_ReportsChainErrors_AndSurfacesChain()
    {
        using var leaf = CreateSelfSignedCertificate();
        var callback = new CapturingCallback { ReturnValue = true };
        var sender = new object();

        var accepted = ClientCertificateValidator.Validate(sender, leaf, intermediates: null, callback.Invoke);

        Assert.True(accepted);
        Assert.True(callback.WasInvoked);
        Assert.Same(sender, callback.Sender);
        Assert.Same(leaf, callback.Certificate);   // the presented leaf is handed to the callback
        Assert.NotNull(callback.Chain);            // a real chain was built and passed through
        // A self-signed leaf is not in any trust store, so the chain must not validate cleanly.
        Assert.Equal(SslPolicyErrors.RemoteCertificateChainErrors, callback.Errors);
    }

    [Fact]
    public void Validate_CertificatePresented_CallbackRejects_ReturnsFalse()
    {
        using var leaf = CreateSelfSignedCertificate();
        var callback = new CapturingCallback { ReturnValue = false };

        var accepted = ClientCertificateValidator.Validate(new object(), leaf, intermediates: null, callback.Invoke);

        Assert.False(accepted);
        Assert.True(callback.WasInvoked);
    }

    private static X509Certificate2 CreateSelfSignedCertificate(string subjectName = "CN=directtls-test-client")
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest(subjectName, key, HashAlgorithmName.SHA256);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(certificateAuthority: false, hasPathLengthConstraint: false, pathLengthConstraint: 0, critical: false));
        var now = DateTimeOffset.UtcNow;
        return request.CreateSelfSigned(now.AddDays(-1), now.AddDays(1));
    }

    /// <summary>Captures the arguments passed to a <see cref="RemoteCertificateValidationCallback"/>.</summary>
    private sealed class CapturingCallback
    {
        public bool WasInvoked { get; private set; }
        public object? Sender { get; private set; }
        public X509Certificate? Certificate { get; private set; }
        public X509Chain? Chain { get; private set; }
        public SslPolicyErrors Errors { get; private set; }
        public bool ReturnValue { get; set; }

        public bool Invoke(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
        {
            WasInvoked = true;
            Sender = sender;
            Certificate = certificate;
            Chain = chain;
            Errors = sslPolicyErrors;
            return ReturnValue;
        }
    }
}
