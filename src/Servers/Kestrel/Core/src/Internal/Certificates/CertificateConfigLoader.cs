// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
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

        const string MLDsa44Oid = "2.16.840.1.101.3.4.3.17";
        const string MLDsa65Oid = "2.16.840.1.101.3.4.3.18";
        const string MLDsa87Oid = "2.16.840.1.101.3.4.3.19";

        const string SlhDsaSha2_128sOid = "2.16.840.1.101.3.4.3.20";
        const string SlhDsaSha2_128fOid = "2.16.840.1.101.3.4.3.21";
        const string SlhDsaSha2_192sOid = "2.16.840.1.101.3.4.3.22";
        const string SlhDsaSha2_192fOid = "2.16.840.1.101.3.4.3.23";
        const string SlhDsaSha2_256sOid = "2.16.840.1.101.3.4.3.24";
        const string SlhDsaSha2_256fOid = "2.16.840.1.101.3.4.3.25";
        const string SlhDsaShake_128sOid = "2.16.840.1.101.3.4.3.26";
        const string SlhDsaShake_128fOid = "2.16.840.1.101.3.4.3.27";
        const string SlhDsaShake_192sOid = "2.16.840.1.101.3.4.3.28";
        const string SlhDsaShake_192fOid = "2.16.840.1.101.3.4.3.29";
        const string SlhDsaShake_256sOid = "2.16.840.1.101.3.4.3.30";
        const string SlhDsaShake_256fOid = "2.16.840.1.101.3.4.3.31";

        const string MLDsa44WithRSA2048PssPreHashSha256Oid = "2.16.840.1.114027.80.9.1.0";
        const string MLDsa44WithRSA2048Pkcs15PreHashSha256Oid = "2.16.840.1.114027.80.9.1.1";
        const string MLDsa44WithEd25519PreHashSha512Oid = "2.16.840.1.114027.80.9.1.2";
        const string MLDsa44WithECDsaP256PreHashSha256Oid = "2.16.840.1.114027.80.9.1.3";
        const string MLDsa65WithRSA3072PssPreHashSha512Oid = "2.16.840.1.114027.80.9.1.4";
        const string MLDsa65WithRSA3072Pkcs15PreHashSha512Oid = "2.16.840.1.114027.80.9.1.5";
        const string MLDsa65WithRSA4096PssPreHashSha512Oid = "2.16.840.1.114027.80.9.1.6";
        const string MLDsa65WithRSA4096Pkcs15PreHashSha512Oid = "2.16.840.1.114027.80.9.1.7";
        const string MLDsa65WithECDsaP256PreHashSha512Oid = "2.16.840.1.114027.80.9.1.8";
        const string MLDsa65WithECDsaP384PreHashSha512Oid = "2.16.840.1.114027.80.9.1.9";
        const string MLDsa65WithECDsaBrainpoolP256r1PreHashSha512Oid = "2.16.840.1.114027.80.9.1.10";
        const string MLDsa65WithEd25519PreHashSha512Oid = "2.16.840.1.114027.80.9.1.11";
        const string MLDsa87WithECDsaP384PreHashSha512Oid = "2.16.840.1.114027.80.9.1.12";
        const string MLDsa87WithECDsaBrainpoolP384r1PreHashSha512Oid = "2.16.840.1.114027.80.9.1.13";
        const string MLDsa87WithEd448PreHashShake256_512Oid = "2.16.840.1.114027.80.9.1.14";
        const string MLDsa87WithRSA3072PssPreHashSha512Oid = "2.16.840.1.114027.80.9.1.15";
        const string MLDsa87WithRSA4096PssPreHashSha512Oid = "2.16.840.1.114027.80.9.1.16";
        const string MLDsa87WithECDsaP521PreHashSha512Oid = "2.16.840.1.114027.80.9.1.17";

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
            case MLDsa44Oid:
            case MLDsa65Oid:
            case MLDsa87Oid:
                {
#pragma warning disable SYSLIB5006 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                    using var mlDsa = ImportMLDsaKeyFromFile(keyText, password);

                    try
                    {
                        return certificate.CopyWithPrivateKey(mlDsa);
                    }
                    catch (Exception ex)
                    {
                        throw CreateErrorGettingPrivateKeyException(keyPath, ex);
                    }
                }
            case SlhDsaSha2_128sOid:
            case SlhDsaSha2_128fOid:
            case SlhDsaSha2_192sOid:
            case SlhDsaSha2_192fOid:
            case SlhDsaSha2_256sOid:
            case SlhDsaSha2_256fOid:
            case SlhDsaShake_128sOid:
            case SlhDsaShake_128fOid:
            case SlhDsaShake_192sOid:
            case SlhDsaShake_192fOid:
            case SlhDsaShake_256sOid:
            case SlhDsaShake_256fOid:
                {
                    using var slhDsa = ImportSlhDsaKeyFromFile(keyText, password);

                    try
                    {
                        return certificate.CopyWithPrivateKey(slhDsa);
                    }
                    catch (Exception ex)
                    {
                        throw CreateErrorGettingPrivateKeyException(keyPath, ex);
                    }
                }
            case MLDsa44WithRSA2048PssPreHashSha256Oid:
            case MLDsa44WithRSA2048Pkcs15PreHashSha256Oid:
            case MLDsa44WithEd25519PreHashSha512Oid:
            case MLDsa44WithECDsaP256PreHashSha256Oid:
            case MLDsa65WithRSA3072PssPreHashSha512Oid:
            case MLDsa65WithRSA3072Pkcs15PreHashSha512Oid:
            case MLDsa65WithRSA4096PssPreHashSha512Oid:
            case MLDsa65WithRSA4096Pkcs15PreHashSha512Oid:
            case MLDsa65WithECDsaP256PreHashSha512Oid:
            case MLDsa65WithECDsaP384PreHashSha512Oid:
            case MLDsa65WithECDsaBrainpoolP256r1PreHashSha512Oid:
            case MLDsa65WithEd25519PreHashSha512Oid:
            case MLDsa87WithECDsaP384PreHashSha512Oid:
            case MLDsa87WithECDsaBrainpoolP384r1PreHashSha512Oid:
            case MLDsa87WithEd448PreHashShake256_512Oid:
            case MLDsa87WithRSA3072PssPreHashSha512Oid:
            case MLDsa87WithRSA4096PssPreHashSha512Oid:
            case MLDsa87WithECDsaP521PreHashSha512Oid:
                {
                    using var compositeMLDsa = ImportCompositeMLDsaKeyFromFile(keyText, password);

                    try
                    {
                        return certificate.CopyWithPrivateKey(compositeMLDsa);
                    }
                    catch (Exception ex)
                    {
                        throw CreateErrorGettingPrivateKeyException(keyPath, ex);
                    }
                }
#pragma warning restore SYSLIB5006 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
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

    [Experimental("SYSLIB5006")]
    private static MLDsa ImportMLDsaKeyFromFile(string keyText, string? password)
    {
        if (password == null)
        {
            return MLDsa.ImportFromPem(keyText);
        }
        else
        {
            return MLDsa.ImportFromEncryptedPem(keyText, password);
        }
    }

    [Experimental("SYSLIB5006")]
    private static SlhDsa ImportSlhDsaKeyFromFile(string keyText, string? password)
    {
        if (password == null)
        {
            return SlhDsa.ImportFromPem(keyText);
        }
        else
        {
            return SlhDsa.ImportFromEncryptedPem(keyText, password);
        }
    }

    [Experimental("SYSLIB5006")]
    private static CompositeMLDsa ImportCompositeMLDsaKeyFromFile(string keyText, string? password)
    {
        if (password == null)
        {
            return CompositeMLDsa.ImportFromPem(keyText);
        }
        else
        {
            return CompositeMLDsa.ImportFromEncryptedPem(keyText, password);
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
