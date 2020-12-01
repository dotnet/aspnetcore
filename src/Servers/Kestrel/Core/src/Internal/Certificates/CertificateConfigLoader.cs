// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Certificates
{
    internal class CertificateConfigLoader : ICertificateConfigLoader
    {
        public CertificateConfigLoader(IHostEnvironment hostEnvironment, ILogger<KestrelServer> logger)
        {
            HostEnvironment = hostEnvironment;
            Logger = logger;
        }

        public IHostEnvironment HostEnvironment { get; }
        public ILogger<KestrelServer> Logger { get; }

        public bool IsTestMock => false;

        public X509Certificate2? LoadCertificate(CertificateConfig? certInfo, string endpointName)
        {
            if (certInfo is null)
            {
                return null;
            }

            if (certInfo.IsFileCert && certInfo.IsStoreCert)
            {
                throw new InvalidOperationException(CoreStrings.FormatMultipleCertificateSources(endpointName));
            }
            else if (certInfo.IsFileCert)
            {
                var certificatePath = Path.Combine(HostEnvironment.ContentRootPath, certInfo.Path!);
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
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            return PersistKey(certificate);
                        }

                        return certificate;
                    }
                    else
                    {
                        Logger.FailedToLoadCertificateKey(certificateKeyPath);
                    }

                    throw new InvalidOperationException(CoreStrings.InvalidPemKey);
                }

                return new X509Certificate2(Path.Combine(HostEnvironment.ContentRootPath, certInfo.Path!), certInfo.Password);
            }
            else if (certInfo.IsStoreCert)
            {
                return LoadFromStoreCert(certInfo);
            }

            return null;
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

            var keyText = File.ReadAllText(keyPath);
            return certificate.PublicKey.Oid.Value switch
            {
                RSAOid => AttachPemRSAKey(certificate, keyText, password),
                ECDsaOid => AttachPemECDSAKey(certificate, keyText, password),
                DSAOid => AttachPemDSAKey(certificate, keyText, password),
                _ => throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, CoreStrings.UnrecognizedCertificateKeyOid, certificate.PublicKey.Oid.Value))
            };
        }

        private static X509Certificate2? GetCertificate(string certificatePath)
        {
            if (X509Certificate2.GetCertContentType(certificatePath) == X509ContentType.Cert)
            {
                return new X509Certificate2(certificatePath);
            }

            return null;
        }

        private static X509Certificate2 AttachPemRSAKey(X509Certificate2 certificate, string keyText, string? password)
        {
            using var rsa = RSA.Create();
            if (password == null)
            {
                rsa.ImportFromPem(keyText);
            }
            else
            {
                rsa.ImportFromEncryptedPem(keyText, password);
            }

            return certificate.CopyWithPrivateKey(rsa);
        }

        private static X509Certificate2 AttachPemDSAKey(X509Certificate2 certificate, string keyText, string? password)
        {
            using var dsa = DSA.Create();
            if (password == null)
            {
                dsa.ImportFromPem(keyText);
            }
            else
            {
                dsa.ImportFromEncryptedPem(keyText, password);
            }

            return certificate.CopyWithPrivateKey(dsa);
        }

        private static X509Certificate2 AttachPemECDSAKey(X509Certificate2 certificate, string keyText, string? password)
        {
            using var ecdsa = ECDsa.Create();
            if (password == null)
            {
                ecdsa.ImportFromPem(keyText);
            }
            else
            {
                ecdsa.ImportFromEncryptedPem(keyText, password);
            }

            return certificate.CopyWithPrivateKey(ecdsa);
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
}
