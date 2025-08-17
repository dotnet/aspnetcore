// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Certificates;

internal sealed class CertificateConfigLoader : ICertificateConfigLoader
{
    public CertificateConfigLoader(IHostEnvironment hostEnvironment, ILogger<KestrelServer> logger)
    {
        HostEnvironment = hostEnvironment;
        Logger = logger;
    }

    public IHostEnvironment HostEnvironment { get; }
    public ILogger<KestrelServer> Logger { get; }

    public bool IsTestMock => false;

    public (X509Certificate2?, X509Certificate2Collection?) LoadCertificate(CertificateConfig? certInfo, string endpointName)
    {
        if (certInfo is null)
        {
            return (null, null);
        }

        if (certInfo.IsFileCert && certInfo.IsStoreCert)
        {
            throw new InvalidOperationException(CoreStrings.FormatMultipleCertificateSources(endpointName));
        }
        else if (certInfo.IsFileCert)
        {
            var certificatePath = Path.Combine(HostEnvironment.ContentRootPath, certInfo.Path!);
            var fullChain = new X509Certificate2Collection();
            fullChain.ImportFromPemFile(certificatePath);

            if (certInfo.KeyPath != null)
            {
                var certificateKeyPath = Path.Combine(HostEnvironment.ContentRootPath, certInfo.KeyPath);
                var certificate = GetCertificate(certificatePath);

                if (certificate != null)
                {
                    certificate = LoadCertificateKey(certificate, certificateKeyPath, certInfo.Password);
                }
                else
                {
                    Logger.FailedToLoadCertificate(certificateKeyPath);
                }

                if (certificate != null)
                {
                    if (OperatingSystem.IsWindows())
                    {
                        return (PersistKey(certificate), fullChain);
                    }

                    return (certificate, fullChain);
                }
                else
                {
                    Logger.FailedToLoadCertificateKey(certificateKeyPath);
                }

                throw new InvalidOperationException(CoreStrings.InvalidPemKey);
            }

            return (new X509Certificate2(Path.Combine(HostEnvironment.ContentRootPath, certInfo.Path!), certInfo.Password), fullChain);
        }
        else if (certInfo.IsStoreCert)
        {
            return (LoadFromStoreCert(certInfo), null);
        }

        return (null, null);
    }

    private static X509Certificate2 PersistKey(X509Certificate2 fullCertificate)
    {
        // We need to force the key to be persisted.
        // See https://github.com/dotnet/runtime/issues/23749
        var certificateBytes = fullCertificate.Export(X509ContentType.Pkcs12, "");
        return new X509Certificate2(certificateBytes, "", X509KeyStorageFlags.DefaultKeySet);
    }

    private static X509Certificate2 LoadCertificateKey(X509Certificate2 certificate, string keyPath, string? password)
    {
        // OIDs for the certificate key types.
        const string RSAOid = "1.2.840.113549.1.1.1";
        const string DSAOid = "1.2.840.10040.4.1";
        const string ECDsaOid = "1.2.840.10045.2.1";

        // Duplication is required here because there are separate CopyWithPrivateKey methods for each algorithm.
        var keyText = File.ReadAllText(keyPath);
        switch (certificate.PublicKey.Oid.Value)
        {
            case RSAOid:
                {
                    using var rsa = RSA.Create();
                    ImportKeyFromFile(rsa, keyText, password);

                    try
                    {
                        return certificate.CopyWithPrivateKey(rsa);
                    }
                    catch (Exception ex)
                    {
                        throw CreateErrorGettingPrivateKeyException(keyPath, ex);
                    }
                }
            case ECDsaOid:
                {
                    using var ecdsa = ECDsa.Create();
                    ImportKeyFromFile(ecdsa, keyText, password);

                    try
                    {
                        return certificate.CopyWithPrivateKey(ecdsa);
                    }
                    catch (Exception ex)
                    {
                        throw CreateErrorGettingPrivateKeyException(keyPath, ex);
                    }
                }
            case DSAOid:
                {
                    using var dsa = DSA.Create();
                    ImportKeyFromFile(dsa, keyText, password);

                    try
                    {
                        return certificate.CopyWithPrivateKey(dsa);
                    }
                    catch (Exception ex)
                    {
                        throw CreateErrorGettingPrivateKeyException(keyPath, ex);
                    }
                }
            default:
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, CoreStrings.UnrecognizedCertificateKeyOid, certificate.PublicKey.Oid.Value));
        }
    }

    private static InvalidOperationException CreateErrorGettingPrivateKeyException(string keyPath, Exception ex)
    {
        return new InvalidOperationException($"Error getting private key from '{keyPath}'.", ex);
    }

    private static X509Certificate2? GetCertificate(string certificatePath)
    {
        if (X509Certificate2.GetCertContentType(certificatePath) == X509ContentType.Cert)
        {
            return new X509Certificate2(certificatePath);
        }

        return null;
    }

    private static void ImportKeyFromFile(AsymmetricAlgorithm asymmetricAlgorithm, string keyText, string? password)
    {
        if (password == null)
        {
            asymmetricAlgorithm.ImportFromPem(keyText);
        }
        else
        {
            asymmetricAlgorithm.ImportFromEncryptedPem(keyText, password);
        }
    }

    private static X509Certificate2 LoadFromStoreCert(CertificateConfig certInfo)
    {
        var subject = certInfo.Subject!;
        var storeName = string.IsNullOrEmpty(certInfo.Store) ? StoreName.My.ToString() : certInfo.Store;
        var location = certInfo.Location;
        var storeLocation = StoreLocation.CurrentUser;
        if (!string.IsNullOrEmpty(location))
        {
            storeLocation = (StoreLocation)Enum.Parse(typeof(StoreLocation), location, ignoreCase: true);
        }
        var allowInvalid = certInfo.AllowInvalid ?? false;

        return CertificateLoader.LoadFromStoreCert(subject, storeName, storeLocation, allowInvalid);
    }
}
