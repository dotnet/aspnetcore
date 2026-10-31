// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.AspNetCore.InternalTesting;

#nullable enable
// Copied from https://github.com/dotnet/runtime/main/src/libraries/System.Net.Security/tests/FunctionalTests/TestHelper.cs
public static class CertHelper
{
    private static readonly X509KeyUsageExtension s_eeKeyUsage =
        new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DataEncipherment,
            critical: false);

    private static readonly X509EnhancedKeyUsageExtension s_tlsServerEku =
        new X509EnhancedKeyUsageExtension(
            new OidCollection
            {
                new Oid("1.3.6.1.5.5.7.3.1", null)
            },
            false);

    private static readonly X509EnhancedKeyUsageExtension s_tlsClientEku =
        new X509EnhancedKeyUsageExtension(
            new OidCollection
            {
                new Oid("1.3.6.1.5.5.7.3.2", null)
            },
            false);

    private static readonly X509BasicConstraintsExtension s_eeConstraints =
        new X509BasicConstraintsExtension(false, false, 0, false);

    public static bool AllowAnyServerCertificate(object sender, X509Certificate certificate, X509Chain chain)
    {
        return true;
    }

    internal static (NetworkStream ClientStream, NetworkStream ServerStream) GetConnectedTcpStreams()
    {
        using (Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
        {
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(1);

            var clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clientSocket.Connect(listener.LocalEndPoint!);
            Socket serverSocket = listener.Accept();

            serverSocket.NoDelay = true;
            clientSocket.NoDelay = true;

            return (new NetworkStream(clientSocket, ownsSocket: true), new NetworkStream(serverSocket, ownsSocket: true));
        }
    }

    internal static void CleanupCertificates([CallerMemberName] string? testName = null)
    {
        string caName = $"O={testName}";
        try
        {
            using (X509Store store = new X509Store(StoreName.CertificateAuthority, StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadWrite);
                foreach (X509Certificate2 cert in store.Certificates)
                {
                    if (cert.Subject.Contains(caName))
                    {
                        store.Remove(cert);
                    }
                    cert.Dispose();
                }
            }
        }
        catch { };

        try
        {
            using (X509Store store = new X509Store(StoreName.CertificateAuthority, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadWrite);
                foreach (X509Certificate2 cert in store.Certificates)
                {
                    if (cert.Subject.Contains(caName))
                    {
                        store.Remove(cert);
                    }
                    cert.Dispose();
                }
            }
        }
        catch { };
    }

    internal static X509ExtensionCollection BuildTlsServerCertExtensions(string serverName)
    {
        return BuildTlsCertExtensions(serverName, true);
    }

    private static X509ExtensionCollection BuildTlsCertExtensions(string targetName, bool serverCertificate)
    {
        X509ExtensionCollection extensions = new X509ExtensionCollection();

        SubjectAlternativeNameBuilder builder = new SubjectAlternativeNameBuilder();
        builder.AddDnsName(targetName);
        extensions.Add(builder.Build());
        extensions.Add(s_eeConstraints);
        extensions.Add(s_eeKeyUsage);
        extensions.Add(serverCertificate ? s_tlsServerEku : s_tlsClientEku);

        return extensions;
    }

    internal static (X509Certificate2 certificate, X509Certificate2Collection) GenerateCertificates(string targetName, [CallerMemberName] string? testName = null, bool longChain = false, bool serverCertificate = true)
    {
        const int keySize = 2048;
        if (OperatingSystem.IsWindows() && testName != null)
        {
            CleanupCertificates(testName);
        }

        X509Certificate2Collection chain = new X509Certificate2Collection();
        X509ExtensionCollection extensions = BuildTlsCertExtensions(targetName, serverCertificate);

        CertificateAuthority.BuildPrivatePki(
            PkiOptions.IssuerRevocationViaCrl,
            out RevocationResponder responder,
            out CertificateAuthority root,
            out CertificateAuthority[] intermediates,
            out X509Certificate2 endEntity,
            intermediateAuthorityCount: longChain ? 3 : 1,
            subjectName: targetName,
            testName: testName,
            keySize: keySize,
            extensions: extensions);

        // Walk the intermediates backwards so we build the chain collection as
        // Issuer3
        // Issuer2
        // Issuer1
        // Root
        for (int i = intermediates.Length - 1; i >= 0; i--)
        {
            CertificateAuthority authority = intermediates[i];

            chain.Add(authority.CloneIssuerCert());
            authority.Dispose();
        }

        chain.Add(root.CloneIssuerCert());

        responder.Dispose();
        root.Dispose();

        if (OperatingSystem.IsWindows())
        {
            X509Certificate2 ephemeral = endEntity;
            endEntity = new X509Certificate2(endEntity.Export(X509ContentType.Pfx), (string?)null, X509KeyStorageFlags.Exportable);
            ephemeral.Dispose();
        }

        return (endEntity, chain);
    }

    internal static string GetTestSNIName(string testMethodName, params SslProtocols?[] protocols)
    {
        static string ProtocolToString(SslProtocols? protocol)
        {
            return (protocol?.ToString() ?? "null").Replace(", ", "-");
        }

        var args = string.Join(".", protocols.Select(p => ProtocolToString(p)));
        var name = testMethodName.Length > 63 ? testMethodName.Substring(0, 63) : testMethodName;

        name = $"{name}.{args}";
        if (OperatingSystem.IsAndroid())
        {
            // Android does not support underscores in host names
            name = name.Replace("_", string.Empty);
        }

        return name;
    }
}
