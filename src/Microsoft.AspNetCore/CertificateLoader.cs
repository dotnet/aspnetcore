// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNetCore
{
    /// <summary>
    /// A helper class to load certificates from files and certificate stores based on <seealso cref="IConfiguration"/> data.
    /// </summary>
    public static class CertificateLoader
    {
        /// <summary>
        /// Loads one or more certificates from a single source.
        /// </summary>
        /// <param name="certificateConfiguration">An <seealso cref="IConfiguration"/> with information about a certificate source.</param>
        /// <param name="password">The certificate password, in case it's being loaded from a file.</param>
        /// <returns>The loaded certificates.</returns>
        public static X509Certificate2 Load(IConfiguration certificateConfiguration, string password)
        {
            var sourceKind = certificateConfiguration.GetValue<string>("Source");

            CertificateSource certificateSource;
            switch (sourceKind.ToLowerInvariant())
            {
                case "file":
                    certificateSource = new CertificateFileSource(password);
                    break;
                case "store":
                    certificateSource = new CertificateStoreSource();
                    break;
                default:
                    throw new InvalidOperationException($"Invalid certificate source kind: {sourceKind}");
            }

            certificateConfiguration.Bind(certificateSource);
            return certificateSource.Load();
        }

        /// <summary>
        /// Loads all certificates specified in an <seealso cref="IConfiguration"/>.
        /// </summary>
        /// <param name="configurationRoot">The root <seealso cref="IConfiguration"/>.</param>
        /// <returns>
        /// A dictionary mapping certificate names to loaded certificates.
        /// </returns>
        public static Dictionary<string, X509Certificate2> LoadAll(IConfiguration configurationRoot)
        {
            return configurationRoot.GetSection("Certificates").GetChildren()
                .ToDictionary(
                    certificateSource => certificateSource.Key,
                    certificateSource => Load(certificateSource, certificateSource["Password"]));
        }

        private abstract class CertificateSource
        {
            public string Source { get; set; }

            public abstract X509Certificate2 Load();
        }

        private class CertificateFileSource : CertificateSource
        {
            private readonly string _password;

            public CertificateFileSource(string password)
            {
                _password = password;
            }

            public string Path { get; set; }

            public override X509Certificate2 Load()
            {
                var certificate = TryLoad(X509KeyStorageFlags.DefaultKeySet, out var error)
                    ?? TryLoad(X509KeyStorageFlags.UserKeySet, out error)
    #if NETCOREAPP2_0
                    ?? TryLoad(X509KeyStorageFlags.EphemeralKeySet, out error)
    #endif
                    ;

                if (error != null)
                {
                    throw error;
                }

                return certificate;
            }

            private X509Certificate2 TryLoad(X509KeyStorageFlags flags, out Exception exception)
            {
                try
                {
                    var loadedCertificate = new X509Certificate2(Path, _password, flags);
                    exception = null;
                    return loadedCertificate;
                }
                catch (Exception e)
                {
                    exception = e;
                    return null;
                }
            }
        }

        private class CertificateStoreSource : CertificateSource
        {
            public string Subject { get; set; }
            public string StoreName { get; set; }
            public string StoreLocation { get; set; }
            public bool AllowInvalid { get; set; }

            public override X509Certificate2 Load()
            {
                if (!Enum.TryParse(StoreLocation, ignoreCase: true, result: out StoreLocation storeLocation))
                {
                    throw new InvalidOperationException($"Invalid store location: {StoreLocation}");
                }

                using (var store = new X509Store(StoreName, storeLocation))
                {
                    X509Certificate2Collection storeCertificates = null;
                    X509Certificate2Collection foundCertificates = null;
                    X509Certificate2 foundCertificate = null;

                    try
                    {
                        store.Open(OpenFlags.ReadOnly);
                        storeCertificates = store.Certificates;
                        foundCertificates = storeCertificates.Find(X509FindType.FindBySubjectDistinguishedName, Subject, validOnly: !AllowInvalid);
                        foundCertificate = foundCertificates
                            .OfType<X509Certificate2>()
                            .OrderByDescending(certificate => certificate.NotAfter)
                            .FirstOrDefault();

                        if (foundCertificate == null)
                        {
                            throw new InvalidOperationException($"No certificate found for {Subject} in store {StoreName} in {StoreLocation}");
                        }

                        return foundCertificate;
                    }
                    finally
                    {
                        if (foundCertificate != null)
                        {
                            storeCertificates.Remove(foundCertificate);
                            foundCertificates.Remove(foundCertificate);
                        }

                        DisposeCertificates(storeCertificates);
                        DisposeCertificates(foundCertificates);
                    }
                }
            }

            private void DisposeCertificates(X509Certificate2Collection certificates)
            {
                if (certificates != null)
                {
                    foreach (var certificate in certificates)
                    {
                        certificate.Dispose();
                    }
                }
            }
        }
    }
}
