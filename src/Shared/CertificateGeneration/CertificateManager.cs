// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

#nullable enable

namespace Microsoft.AspNetCore.Certificates.Generation
{
    internal abstract class CertificateManager
    {
        internal const int CurrentAspNetCoreCertificateVersion = 2;
        internal const string AspNetHttpsOid = "1.3.6.1.4.1.311.84.1.1";
        internal const string AspNetHttpsOidFriendlyName = "ASP.NET Core HTTPS development certificate";

        private const string ServerAuthenticationEnhancedKeyUsageOid = "1.3.6.1.5.5.7.3.1";
        private const string ServerAuthenticationEnhancedKeyUsageOidFriendlyName = "Server Authentication";

        private const string LocalhostHttpsDnsName = "localhost";
        private const string LocalhostHttpsDistinguishedName = "CN=" + LocalhostHttpsDnsName;

        public const int RSAMinimumKeySizeInBits = 2048;

        public static CertificateManager Instance { get; } = OperatingSystem.IsWindows() ?
#pragma warning disable CA1416 // Validate platform compatibility
            new WindowsCertificateManager() :
#pragma warning restore CA1416 // Validate platform compatibility
            OperatingSystem.IsMacOS() ?
                new MacOSCertificateManager() as CertificateManager :
                new UnixCertificateManager();

        public static CertificateManagerEventSource Log { get; set; } = new CertificateManagerEventSource();

        // Setting to 0 means we don't append the version byte,
        // which is what all machines currently have.
        public int AspNetHttpsCertificateVersion
        {
            get;
            // For testing purposes only
            internal set;
        }

        public string Subject { get; }

        public CertificateManager() : this(LocalhostHttpsDistinguishedName, CurrentAspNetCoreCertificateVersion)
        {
        }

        // For testing purposes only
        internal CertificateManager(string subject, int version)
        {
            Subject = subject;
            AspNetHttpsCertificateVersion = version;
        }

        public bool IsHttpsDevelopmentCertificate(X509Certificate2 certificate) =>
            certificate.Extensions.OfType<X509Extension>()
            .Any(e => string.Equals(AspNetHttpsOid, e.Oid?.Value, StringComparison.Ordinal));

        public IList<X509Certificate2> ListCertificates(
            StoreName storeName,
            StoreLocation location,
            bool isValid,
            bool requireExportable = true)
        {
            Log.ListCertificatesStart(location, storeName);
            var certificates = new List<X509Certificate2>();
            try
            {
                using var store = new X509Store(storeName, location);
                store.Open(OpenFlags.ReadOnly);
                certificates.AddRange(store.Certificates.OfType<X509Certificate2>());
                IEnumerable<X509Certificate2> matchingCertificates = certificates;
                matchingCertificates = matchingCertificates
                    .Where(c => HasOid(c, AspNetHttpsOid));

                if (Log.IsEnabled())
                {
                    Log.DescribeFoundCertificates(ToCertificateDescription(matchingCertificates));
                }

                if (isValid)
                {
                    // Ensure the certificate hasn't expired, has a private key and its exportable
                    // (for container/unix scenarios).
                    Log.CheckCertificatesValidity();
                    var now = DateTimeOffset.Now;
                    var validCertificates = matchingCertificates
                        .Where(c => IsValidCertificate(c, now, requireExportable))
                        .OrderByDescending(c => GetCertificateVersion(c))
                        .ToArray();

                    if (Log.IsEnabled())
                    {
                        var invalidCertificates = matchingCertificates.Except(validCertificates);
                        Log.DescribeValidCertificates(ToCertificateDescription(validCertificates));
                        Log.DescribeInvalidValidCertificates(ToCertificateDescription(invalidCertificates));
                    }

                    matchingCertificates = validCertificates;
                }

                // We need to enumerate the certificates early to prevent disposing issues.
                matchingCertificates = matchingCertificates.ToList();

                var certificatesToDispose = certificates.Except(matchingCertificates);
                DisposeCertificates(certificatesToDispose);

                store.Close();

                Log.ListCertificatesEnd();
                return (IList<X509Certificate2>)matchingCertificates;
            }
            catch (Exception e)
            {
                if (Log.IsEnabled())
                {
                    Log.ListCertificatesError(e.ToString());
                }
                DisposeCertificates(certificates);
                certificates.Clear();
                return certificates;
            }

            bool HasOid(X509Certificate2 certificate, string oid) =>
                certificate.Extensions.OfType<X509Extension>()
                    .Any(e => string.Equals(oid, e.Oid?.Value, StringComparison.Ordinal));

            static byte GetCertificateVersion(X509Certificate2 c)
            {
                var byteArray = c.Extensions.OfType<X509Extension>()
                    .Where(e => string.Equals(AspNetHttpsOid, e.Oid?.Value, StringComparison.Ordinal))
                    .Single()
                    .RawData;

                if ((byteArray.Length == AspNetHttpsOidFriendlyName.Length && byteArray[0] == (byte)'A') || byteArray.Length == 0)
                {
                    // No Version set, default to 0
                    return 0b0;
                }
                else
                {
                    // Version is in the only byte of the byte array.
                    return byteArray[0];
                }
            }

            bool IsValidCertificate(X509Certificate2 certificate, DateTimeOffset currentDate, bool requireExportable) =>
                certificate.NotBefore <= currentDate &&
                currentDate <= certificate.NotAfter &&
                (!requireExportable || IsExportable(certificate)) &&
                GetCertificateVersion(certificate) >= AspNetHttpsCertificateVersion;
        }

        public IList<X509Certificate2> GetHttpsCertificates() =>
            ListCertificates(StoreName.My, StoreLocation.CurrentUser, isValid: true, requireExportable: true);

        public EnsureCertificateResult EnsureAspNetCoreHttpsDevelopmentCertificate(
            DateTimeOffset notBefore,
            DateTimeOffset notAfter,
            string? path = null,
            bool trust = false,
            bool includePrivateKey = false,
            string? password = null,
            CertificateKeyExportFormat keyExportFormat = CertificateKeyExportFormat.Pfx,
            bool isInteractive = true)
        {
            var result = EnsureCertificateResult.Succeeded;

            var currentUserCertificates = ListCertificates(StoreName.My, StoreLocation.CurrentUser, isValid: true, requireExportable: true);
            var trustedCertificates = ListCertificates(StoreName.My, StoreLocation.LocalMachine, isValid: true, requireExportable: true);
            var certificates = currentUserCertificates.Concat(trustedCertificates);

            var filteredCertificates = certificates.Where(c => c.Subject == Subject);

            if (Log.IsEnabled())
            {
                var excludedCertificates = certificates.Except(filteredCertificates);
                Log.FilteredCertificates(ToCertificateDescription(filteredCertificates));
                Log.ExcludedCertificates(ToCertificateDescription(excludedCertificates));
            }

            certificates = filteredCertificates;

            X509Certificate2? certificate = null;
            var isNewCertificate = false;
            if (certificates.Any())
            {
                certificate = certificates.First();
                var failedToFixCertificateState = false;
                if (isInteractive)
                {
                    // Skip this step if the command is not interactive,
                    // as we don't want to prompt on first run experience.
                    foreach (var candidate in currentUserCertificates)
                    {
                        var status = CheckCertificateState(candidate, true);
                        if (!status.Success)
                        {
                            try
                            {
                                if (Log.IsEnabled())
                                {
                                    Log.CorrectCertificateStateStart(GetDescription(candidate));
                                }
                                CorrectCertificateState(candidate);
                                Log.CorrectCertificateStateEnd();
                            }
                            catch (Exception e)
                            {
                                if (Log.IsEnabled())
                                {
                                    Log.CorrectCertificateStateError(e.ToString());
                                }
                                result = EnsureCertificateResult.FailedToMakeKeyAccessible;
                                // We don't return early on this type of failure to allow for tooling to
                                // export or trust the certificate even in this situation, as that enables
                                // exporting the certificate to perform any necessary fix with native tooling.
                                failedToFixCertificateState = true;
                            }
                        }
                    }
                }

                if (!failedToFixCertificateState)
                {
                    if (Log.IsEnabled())
                    {
                        Log.ValidCertificatesFound(ToCertificateDescription(certificates));
                    }
                    certificate = certificates.First();
                    if (Log.IsEnabled())
                    {
                        Log.SelectedCertificate(GetDescription(certificate));
                    }
                    result = EnsureCertificateResult.ValidCertificatePresent;
                }
            }
            else
            {
                Log.NoValidCertificatesFound();
                try
                {
                    Log.CreateDevelopmentCertificateStart();
                    isNewCertificate = true;
                    certificate = CreateAspNetCoreHttpsDevelopmentCertificate(notBefore, notAfter);
                }
                catch (Exception e)
                {
                    if (Log.IsEnabled())
                    {
                        Log.CreateDevelopmentCertificateError(e.ToString());
                    }
                    result = EnsureCertificateResult.ErrorCreatingTheCertificate;
                    return result;
                }
                Log.CreateDevelopmentCertificateEnd();

                try
                {
                    certificate = SaveCertificate(certificate);
                }
                catch (Exception e)
                {
                    Log.SaveCertificateInStoreError(e.ToString());
                    result = EnsureCertificateResult.ErrorSavingTheCertificateIntoTheCurrentUserPersonalStore;
                    return result;
                }

                if (isInteractive)
                {
                    try
                    {
                        if (Log.IsEnabled())
                        {
                            Log.CorrectCertificateStateStart(GetDescription(certificate));
                        }
                        CorrectCertificateState(certificate);
                        Log.CorrectCertificateStateEnd();
                    }
                    catch (Exception e)
                    {
                        if (Log.IsEnabled())
                        {
                            Log.CorrectCertificateStateError(e.ToString());
                        }

                        // We don't return early on this type of failure to allow for tooling to
                        // export or trust the certificate even in this situation, as that enables
                        // exporting the certificate to perform any necessary fix with native tooling.
                        result = EnsureCertificateResult.FailedToMakeKeyAccessible;
                    }
                }
            }

            if (path != null)
            {
                try
                {
                    ExportCertificate(certificate, path, includePrivateKey, password, keyExportFormat);
                }
                catch (Exception e)
                {
                    if (Log.IsEnabled())
                    {
                        Log.ExportCertificateError(e.ToString());
                    }

                    // We don't want to mask the original source of the error here.
                    result = result != EnsureCertificateResult.Succeeded && result != EnsureCertificateResult.ValidCertificatePresent ?
                        result :
                        EnsureCertificateResult.ErrorExportingTheCertificate;

                    return result;
                }
            }

            if (trust)
            {
                try
                {
                    TrustCertificate(certificate);
                }
                catch (UserCancelledTrustException)
                {
                    result = EnsureCertificateResult.UserCancelledTrustStep;
                    return result;
                }
                catch
                {
                    result = EnsureCertificateResult.FailedToTrustTheCertificate;
                    return result;
                }
            }

            DisposeCertificates(!isNewCertificate ? certificates : certificates.Append(certificate));

            return result;
        }

        internal ImportCertificateResult ImportCertificate(string certificatePath, string password)
        {
            if (!File.Exists(certificatePath))
            {
                Log.ImportCertificateMissingFile(certificatePath);
                return ImportCertificateResult.CertificateFileMissing;
            }

            var certificates = ListCertificates(StoreName.My, StoreLocation.CurrentUser, isValid: false, requireExportable: false);
            if (certificates.Any())
            {
                if (Log.IsEnabled())
                {
                    Log.ImportCertificateExistingCertificates(ToCertificateDescription(certificates));
                }
                return ImportCertificateResult.ExistingCertificatesPresent;
            }

            X509Certificate2 certificate;
            try
            {
                Log.LoadCertificateStart(certificatePath);
                certificate = new X509Certificate2(certificatePath, password, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet);
                if (Log.IsEnabled())
                {
                    Log.LoadCertificateEnd(GetDescription(certificate));
                }
            }
            catch (Exception e)
            {
                if (Log.IsEnabled())
                {
                    Log.LoadCertificateError(e.ToString());
                }
                return ImportCertificateResult.InvalidCertificate;
            }

            if (!IsHttpsDevelopmentCertificate(certificate))
            {
                if (Log.IsEnabled())
                {
                    Log.NoHttpsDevelopmentCertificate(GetDescription(certificate));
                }
                return ImportCertificateResult.NoDevelopmentHttpsCertificate;
            }

            try
            {
                SaveCertificate(certificate);
            }
            catch (Exception e)
            {
                if (Log.IsEnabled())
                {
                    Log.SaveCertificateInStoreError(e.ToString());
                }
                return ImportCertificateResult.ErrorSavingTheCertificateIntoTheCurrentUserPersonalStore;
            }

            return ImportCertificateResult.Succeeded;
        }

        public void CleanupHttpsCertificates()
        {
            // On OS X we don't have a good way to manage trusted certificates in the system keychain
            // so we do everything by invoking the native toolchain.
            // This has some limitations, like for example not being able to identify our custom OID extension. For that
            // matter, when we are cleaning up certificates on the machine, we start by removing the trusted certificates.
            // To do this, we list the certificates that we can identify on the current user personal store and we invoke
            // the native toolchain to remove them from the sytem keychain. Once we have removed the trusted certificates,
            // we remove the certificates from the local user store to finish up the cleanup.
            var certificates = ListCertificates(StoreName.My, StoreLocation.CurrentUser, isValid: false);
            var filteredCertificates = certificates.Where(c => c.Subject == Subject);

            if (Log.IsEnabled())
            {
                var excludedCertificates = certificates.Except(filteredCertificates);
                Log.FilteredCertificates(ToCertificateDescription(filteredCertificates));
                Log.ExcludedCertificates(ToCertificateDescription(excludedCertificates));
            }

            foreach (var certificate in filteredCertificates)
            {
                RemoveCertificate(certificate, RemoveLocations.All);
            }
        }

        public abstract bool IsTrusted(X509Certificate2 certificate);

        protected abstract X509Certificate2 SaveCertificateCore(X509Certificate2 certificate);

        protected abstract void TrustCertificateCore(X509Certificate2 certificate);

        protected abstract bool IsExportable(X509Certificate2 c);

        protected abstract void RemoveCertificateFromTrustedRoots(X509Certificate2 certificate);

        protected abstract IList<X509Certificate2> GetCertificatesToRemove(StoreName storeName, StoreLocation storeLocation);

        internal void ExportCertificate(X509Certificate2 certificate, string path, bool includePrivateKey, string? password, CertificateKeyExportFormat format)
        {
            if (Log.IsEnabled())
            {
                Log.ExportCertificateStart(GetDescription(certificate), path, includePrivateKey);
            }

            if (includePrivateKey && password == null)
            {
                Log.NoPasswordForCertificate();
            }

            var targetDirectoryPath = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(targetDirectoryPath))
            {
                Log.CreateExportCertificateDirectory(targetDirectoryPath);
                Directory.CreateDirectory(targetDirectoryPath);
            }

            byte[] bytes;
            byte[] keyBytes;
            byte[]? pemEnvelope = null;
            RSA? key = null;

            try
            {
                if (includePrivateKey)
                {
                    switch (format)
                    {
                        case CertificateKeyExportFormat.Pfx:
                            bytes = certificate.Export(X509ContentType.Pkcs12, password);
                            break;
                        case CertificateKeyExportFormat.Pem:
                            key = certificate.GetRSAPrivateKey()!;

                            char[] pem;
                            if (password != null)
                            {
                                keyBytes = key.ExportEncryptedPkcs8PrivateKey(password, new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 100000));
                                pem = PemEncoding.Write("ENCRYPTED PRIVATE KEY", keyBytes);
                                pemEnvelope = Encoding.ASCII.GetBytes(pem);
                            }
                            else
                            {
                                // Export the key first to an encrypted PEM to avoid issues with System.Security.Cryptography.Cng indicating that the operation is not supported.
                                // This is likely by design to avoid exporting the key by mistake.
                                // To bypass it, we export the certificate to pem temporarily and then we import it and export it as unprotected PEM.
                                keyBytes = key.ExportEncryptedPkcs8PrivateKey("", new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 1));
                                pem = PemEncoding.Write("ENCRYPTED PRIVATE KEY", keyBytes);
                                key.Dispose();
                                key = RSA.Create();
                                key.ImportFromEncryptedPem(pem, "");
                                Array.Clear(keyBytes, 0, keyBytes.Length);
                                Array.Clear(pem, 0, pem.Length);
                                keyBytes = key.ExportPkcs8PrivateKey();
                                pem = PemEncoding.Write("PRIVATE KEY", keyBytes);
                                pemEnvelope = Encoding.ASCII.GetBytes(pem);
                            }

                            Array.Clear(keyBytes, 0, keyBytes.Length);
                            Array.Clear(pem, 0, pem.Length);

                            bytes = Encoding.ASCII.GetBytes(PemEncoding.Write("CERTIFICATE", certificate.Export(X509ContentType.Cert)));
                            break;
                        default:
                            throw new InvalidOperationException("Unknown format.");
                    }
                }
                else
                {
                    if (format == CertificateKeyExportFormat.Pem)
                    {
                        bytes = Encoding.ASCII.GetBytes(PemEncoding.Write("CERTIFICATE", certificate.Export(X509ContentType.Cert)));
                    }
                    else
                    {
                        bytes = certificate.Export(X509ContentType.Cert);
                    }
                }
            }
            catch (Exception e) when (Log.IsEnabled())
            {
                Log.ExportCertificateError(e.ToString());
                throw;
            }
            finally
            {
                key?.Dispose();
            }

            try
            {
                Log.WriteCertificateToDisk(path);
                File.WriteAllBytes(path, bytes);
            }
            catch (Exception ex) when (Log.IsEnabled())
            {
                Log.WriteCertificateToDiskError(ex.ToString());
                throw;
            }
            finally
            {
                Array.Clear(bytes, 0, bytes.Length);
            }

            if (includePrivateKey && format == CertificateKeyExportFormat.Pem)
            {
                Debug.Assert(pemEnvelope != null);

                try
                {
                    var keyPath = Path.ChangeExtension(path, ".key");
                    Log.WritePemKeyToDisk(keyPath);
                    File.WriteAllBytes(keyPath, pemEnvelope);
                }
                catch (Exception ex) when (Log.IsEnabled())
                {
                    Log.WritePemKeyToDiskError(ex.ToString());
                    throw;
                }
                finally
                {
                    Array.Clear(pemEnvelope, 0, pemEnvelope.Length);
                }
            }
        }

        internal X509Certificate2 CreateAspNetCoreHttpsDevelopmentCertificate(DateTimeOffset notBefore, DateTimeOffset notAfter)
        {
            var subject = new X500DistinguishedName(Subject);
            var extensions = new List<X509Extension>();
            var sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddDnsName(LocalhostHttpsDnsName);

            var keyUsage = new X509KeyUsageExtension(X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature, critical: true);
            var enhancedKeyUsage = new X509EnhancedKeyUsageExtension(
                new OidCollection() {
                    new Oid(
                        ServerAuthenticationEnhancedKeyUsageOid,
                        ServerAuthenticationEnhancedKeyUsageOidFriendlyName)
                },
                critical: true);

            var basicConstraints = new X509BasicConstraintsExtension(
                certificateAuthority: false,
                hasPathLengthConstraint: false,
                pathLengthConstraint: 0,
                critical: true);

            byte[] bytePayload;

            if (AspNetHttpsCertificateVersion != 0)
            {
                bytePayload = new byte[1];
                bytePayload[0] = (byte)AspNetHttpsCertificateVersion;
            }
            else
            {
                bytePayload = Encoding.ASCII.GetBytes(AspNetHttpsOidFriendlyName);
            }

            var aspNetHttpsExtension = new X509Extension(
                new AsnEncodedData(
                    new Oid(AspNetHttpsOid, AspNetHttpsOidFriendlyName),
                    bytePayload),
                critical: false);

            extensions.Add(basicConstraints);
            extensions.Add(keyUsage);
            extensions.Add(enhancedKeyUsage);
            extensions.Add(sanBuilder.Build(critical: true));
            extensions.Add(aspNetHttpsExtension);

            var certificate = CreateSelfSignedCertificate(subject, extensions, notBefore, notAfter);
            return certificate;
        }

        internal X509Certificate2 SaveCertificate(X509Certificate2 certificate)
        {
            var name = StoreName.My;
            var location = StoreLocation.CurrentUser;

            if (Log.IsEnabled())
            {
                Log.SaveCertificateInStoreStart(GetDescription(certificate), name, location);
            }

            certificate = SaveCertificateCore(certificate);

            Log.SaveCertificateInStoreEnd();
            return certificate;
        }

        internal void TrustCertificate(X509Certificate2 certificate)
        {
            try
            {
                if (Log.IsEnabled())
                {
                    Log.TrustCertificateStart(GetDescription(certificate));
                }
                TrustCertificateCore(certificate);
                Log.TrustCertificateEnd();
            }
            catch (Exception ex) when (Log.IsEnabled())
            {
                Log.TrustCertificateError(ex.ToString());
                throw;
            }
        }

        // Internal, for testing purposes only.
        internal void RemoveAllCertificates(StoreName storeName, StoreLocation storeLocation)
        {
            var certificates = GetCertificatesToRemove(storeName, storeLocation);
            var certificatesWithName = certificates.Where(c => c.Subject == Subject);

            var removeLocation = storeName == StoreName.My ? RemoveLocations.Local : RemoveLocations.Trusted;

            foreach (var certificate in certificates)
            {
                RemoveCertificate(certificate, removeLocation);
            }

            DisposeCertificates(certificates);
        }

        internal void RemoveCertificate(X509Certificate2 certificate, RemoveLocations locations)
        {
            switch (locations)
            {
                case RemoveLocations.Undefined:
                    throw new InvalidOperationException($"'{nameof(RemoveLocations.Undefined)}' is not a valid location.");
                case RemoveLocations.Local:
                    RemoveCertificateFromUserStore(certificate);
                    break;
                case RemoveLocations.Trusted:
                    RemoveCertificateFromTrustedRoots(certificate);
                    break;
                case RemoveLocations.All:
                    RemoveCertificateFromTrustedRoots(certificate);
                    RemoveCertificateFromUserStore(certificate);
                    break;
                default:
                    throw new InvalidOperationException("Invalid location.");
            }
        }

        internal abstract CheckCertificateStateResult CheckCertificateState(X509Certificate2 candidate, bool interactive);

        internal abstract void CorrectCertificateState(X509Certificate2 candidate);

        internal X509Certificate2 CreateSelfSignedCertificate(
            X500DistinguishedName subject,
            IEnumerable<X509Extension> extensions,
            DateTimeOffset notBefore,
            DateTimeOffset notAfter)
        {
            using var key = CreateKeyMaterial(RSAMinimumKeySizeInBits);

            var request = new CertificateRequest(subject, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            foreach (var extension in extensions)
            {
                request.CertificateExtensions.Add(extension);
            }

            var result = request.CreateSelfSigned(notBefore, notAfter);
            return result;

            RSA CreateKeyMaterial(int minimumKeySize)
            {
                var rsa = RSA.Create(minimumKeySize);
                if (rsa.KeySize < minimumKeySize)
                {
                    throw new InvalidOperationException($"Failed to create a key with a size of {minimumKeySize} bits");
                }

                return rsa;
            }
        }

        internal static void DisposeCertificates(IEnumerable<X509Certificate2> disposables)
        {
            foreach (var disposable in disposables)
            {
                try
                {
                    disposable.Dispose();
                }
                catch
                {
                }
            }
        }

        private static void RemoveCertificateFromUserStore(X509Certificate2 certificate)
        {
            try
            {
                if (Log.IsEnabled())
                {
                    Log.RemoveCertificateFromUserStoreStart(GetDescription(certificate));
                }
                using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);
                var matching = store.Certificates
                    .OfType<X509Certificate2>()
                    .Single(c => c.SerialNumber == certificate.SerialNumber);

                store.Remove(matching);
                store.Close();
                Log.RemoveCertificateFromUserStoreEnd();
            }
            catch (Exception ex) when (Log.IsEnabled())
            {
                Log.RemoveCertificateFromUserStoreError(ex.ToString());
                throw;
            }
        }

        internal static string ToCertificateDescription(IEnumerable<X509Certificate2> matchingCertificates) =>
            string.Join(Environment.NewLine, matchingCertificates
                .OrderBy(c => c.Thumbprint)
                .Select(c => GetDescription(c))
                .ToArray());

        internal static string GetDescription(X509Certificate2 c) =>
            $"{c.Thumbprint[0..6]} - {c.Subject} - {c.GetEffectiveDateString()} - {c.GetExpirationDateString()} - {Instance.IsHttpsDevelopmentCertificate(c)} - {Instance.IsExportable(c)}";

        [EventSource(Name = "Dotnet-dev-certs")]
        public class CertificateManagerEventSource : EventSource
        {
            [Event(1, Level = EventLevel.Verbose)]
            public void ListCertificatesStart(StoreLocation location, StoreName storeName)
            {
                if (IsEnabled())
                {
                    WriteEvent(1, $"Listing certificates from {location}\\{storeName}");
                }
            }

            [Event(2, Level = EventLevel.Verbose)]
            public void DescribeFoundCertificates(string matchingCertificates) => WriteEvent(2, matchingCertificates);

            [Event(3, Level = EventLevel.Verbose)]
            public void CheckCertificatesValidity() => WriteEvent(3, "Checking certificates validity");

            [Event(4, Level = EventLevel.Verbose)]
            public void DescribeValidCertificates(string validCertificates) => WriteEvent(4, validCertificates);

            [Event(5, Level = EventLevel.Verbose)]
            public void DescribeInvalidValidCertificates(string invalidCertificates) => WriteEvent(5, invalidCertificates);

            [Event(6, Level = EventLevel.Verbose)]
            public void ListCertificatesEnd() => WriteEvent(6, "Finished listing certificates.");

            [Event(7, Level = EventLevel.Error)]
            public void ListCertificatesError(string e)
            {
                if (IsEnabled())
                {
                    WriteEvent(7, $"An error ocurred while listing the certificates: {e}");
                }
            }

            [Event(8, Level = EventLevel.Verbose)]
            public void FilteredCertificates(string filteredCertificates) => WriteEvent(8, filteredCertificates);

            [Event(9, Level = EventLevel.Verbose)]
            public void ExcludedCertificates(string excludedCertificates) => WriteEvent(9, excludedCertificates);

            [Event(11, Level = EventLevel.Verbose)]
            public void MacOSMakeCertificateAccessibleAcrossPartitionsStart(string cert)
            {
                if (IsEnabled())
                {
                    WriteEvent(11, $"Trying to make certificate accessible across partitions: {cert}");
                }
            }

            [Event(12, Level = EventLevel.Verbose)]
            public void MacOSMakeCertificateAccessibleAcrossPartitionsEnd() => WriteEvent(12, "Finished making the certificate accessible across partitions.");

            [Event(13, Level = EventLevel.Error)]
            public void MacOSMakeCertificateAccessibleAcrossPartitionsError(string ex)
            {
                if (IsEnabled())
                {
                    WriteEvent(13, $"An error ocurred while making the certificate accessible across partitions : {ex}");
                }
            }

            [Event(14, Level = EventLevel.Verbose)]
            public void ValidCertificatesFound(string certificates) => WriteEvent(14, certificates);

            [Event(15, Level = EventLevel.Verbose)]
            public void SelectedCertificate(string certificate)
            {
                if (IsEnabled())
                {
                    WriteEvent(15, $"Selected certificate: {certificate}");
                }
            }

            [Event(16, Level = EventLevel.Verbose)]
            public void NoValidCertificatesFound() => WriteEvent(16, "No valid certificates found.");


            [Event(17, Level = EventLevel.Verbose)]
            public void CreateDevelopmentCertificateStart() => WriteEvent(17, "Generating HTTPS development certificate.");

            [Event(18, Level = EventLevel.Verbose)]
            public void CreateDevelopmentCertificateEnd() => WriteEvent(18, "Finished generating HTTPS development certificate.");

            [Event(19, Level = EventLevel.Error)]
            public void CreateDevelopmentCertificateError(string e)
            {
                if (IsEnabled())
                {
                    WriteEvent(19, $"An error has occurred generating the certificate: {e}.");
                }
            }

            [Event(20, Level = EventLevel.Verbose)]
            public void SaveCertificateInStoreStart(string certificate, StoreName name, StoreLocation location) => WriteEvent(20, $"Saving certificate '{certificate}' to store {location}\\{name}.");

            [Event(21, Level = EventLevel.Verbose)]
            public void SaveCertificateInStoreEnd() => WriteEvent(21, "Finished saving certificate to the store.");

            [Event(22, Level = EventLevel.Error)]
            public void SaveCertificateInStoreError(string e)
            {
                if (IsEnabled())
                {
                    WriteEvent(22, $"An error has occurred saving the certificate: {e}.");
                }
            }

            [Event(23, Level = EventLevel.Verbose)]
            public void ExportCertificateStart(string certificate, string path, bool includePrivateKey)
            {
                if (IsEnabled())
                {
                    WriteEvent(23, $"Saving certificate '{certificate}' to {path} {(includePrivateKey ? "with" : "without")} private key.");
                }
            }

            [Event(24, Level = EventLevel.Verbose)]
            public void NoPasswordForCertificate() => WriteEvent(24, "Exporting certificate with private key but no password");

            [Event(25, Level = EventLevel.Verbose)]
            public void CreateExportCertificateDirectory(string path)
            {
                if (IsEnabled())
                {
                    WriteEvent(25, $"Creating directory {path}.");
                }
            }

            [Event(26, Level = EventLevel.Error)]
            public void ExportCertificateError(string ex)
            {
                if (IsEnabled())
                {
                    WriteEvent(26, $"An error has ocurred while exporting the certificate: {ex}.");
                }
            }

            [Event(27, Level = EventLevel.Verbose)]
            public void WriteCertificateToDisk(string path)
            {
                if (IsEnabled())
                {
                    WriteEvent(27, $"Writing the certificate to: {path}.");
                }
            }

            [Event(28, Level = EventLevel.Error)]
            public void WriteCertificateToDiskError(string ex)
            {
                if (IsEnabled())
                {
                    WriteEvent(28, $"An error has ocurred while writing the certificate to disk: {ex}.");
                }
            }

            [Event(29, Level = EventLevel.Verbose)]
            public void TrustCertificateStart(string certificate)
            {
                if (IsEnabled())
                {
                    WriteEvent(29, $"Trusting the certificate to: {certificate}.");
                }
            }

            [Event(30, Level = EventLevel.Verbose)]
            public void TrustCertificateEnd() =>WriteEvent(30, "Finished trusting the certificate.");

            [Event(31, Level = EventLevel.Error)]
            public void TrustCertificateError(string ex)
            {
                if (IsEnabled())
                {
                    WriteEvent(31, $"An error has ocurred while trusting the certificate: {ex}.");
                }
            }

            [Event(32, Level = EventLevel.Verbose)]
            public void MacOSTrustCommandStart(string command)
            {
                if (IsEnabled())
                {
                    WriteEvent(32, $"Running the trust command {command}.");
                }
            }

            [Event(33, Level = EventLevel.Verbose)]
            public void MacOSTrustCommandEnd() => WriteEvent(33, "Finished running the trust command.");

            [Event(34, Level = EventLevel.Verbose)]
            public void MacOSTrustCommandError(int exitCode)
            {
                if (IsEnabled())
                {
                    WriteEvent(34, $"An error has ocurred while running the trust command: {exitCode}.");
                }
            }

            [Event(35, Level = EventLevel.Verbose)]
            public void MacOSRemoveCertificateTrustRuleStart(string certificate)
            {
                if (IsEnabled())
                {
                    WriteEvent(35, $"Running the remove trust command for {certificate}.");
                }
            }

            [Event(36, Level = EventLevel.Verbose)]
            public void MacOSRemoveCertificateTrustRuleEnd() => WriteEvent(36, "Finished running the remove trust command.");

            [Event(37, Level = EventLevel.Verbose)]
            public void MacOSRemoveCertificateTrustRuleError(int exitCode)
            {
                if (IsEnabled())
                {
                    WriteEvent(37, $"An error has ocurred while running the remove trust command: {exitCode}.");
                }
            }

            [Event(38, Level = EventLevel.Verbose)]
            public void MacOSCertificateUntrusted(string certificate)
            {
                if (IsEnabled())
                {
                    WriteEvent(38, $"The certificate is not trusted: {certificate}.");
                }
            }


            [Event(39, Level = EventLevel.Verbose)]
            public void MacOSRemoveCertificateFromKeyChainStart(string keyChain, string certificate)
            {
                if (IsEnabled())
                {
                    WriteEvent(39, $"Removing the certificate from the keychain {keyChain} {certificate}.");
                }
            }

            [Event(40, Level = EventLevel.Verbose)]
            public void MacOSRemoveCertificateFromKeyChainEnd() => WriteEvent(40, "Finished removing the certificate from the keychain.");

            [Event(41, Level = EventLevel.Verbose)]
            public void MacOSRemoveCertificateFromKeyChainError(int exitCode)
            {
                if (IsEnabled())
                {
                    WriteEvent(41, $"An error has ocurred while running the remove trust command: {exitCode}.");
                }
            }


            [Event(42, Level = EventLevel.Verbose)]
            public void RemoveCertificateFromUserStoreStart(string certificate)
            {
                if (IsEnabled())
                {
                    WriteEvent(42, $"Removing the certificate from the user store {certificate}.");
                }
            }

            [Event(43, Level = EventLevel.Verbose)]
            public void RemoveCertificateFromUserStoreEnd() => WriteEvent(43, "Finished removing the certificate from the user store.");

            [Event(44, Level = EventLevel.Error)]
            public void RemoveCertificateFromUserStoreError(string ex)
            {
                if (IsEnabled())
                {
                    WriteEvent(44, $"An error has ocurred while removing the certificate from the user store: {ex}.");
                }
            }

            [Event(45, Level = EventLevel.Verbose)]
            public void WindowsAddCertificateToRootStore() => WriteEvent(45, "Adding certificate to the trusted root certification authority store.");

            [Event(46, Level = EventLevel.Verbose)]
            public void WindowsCertificateAlreadyTrusted() => WriteEvent(46, "The certificate is already trusted");

            [Event(47, Level = EventLevel.Verbose)]
            public void WindowsCertificateTrustCanceled() => WriteEvent(47, "Trusting the certificate was cancelled by the user.");

            [Event(48, Level = EventLevel.Verbose)]
            public void WindowsRemoveCertificateFromRootStoreStart() => WriteEvent(48, "Removing the certificate from the trusted root certification authority store.");

            [Event(49, Level = EventLevel.Verbose)]
            public void WindowsRemoveCertificateFromRootStoreEnd() => WriteEvent(49, "Finished removing the certificate from the trusted root certification authority store.");

            [Event(50, Level = EventLevel.Verbose)]
            public void WindowsRemoveCertificateFromRootStoreNotFound() => WriteEvent(50, "The certificate was not trusted.");

            [Event(51, Level = EventLevel.Verbose)]
            public void CorrectCertificateStateStart(string certificate)
            {
                if (IsEnabled())
                {
                    WriteEvent(51, $"Correcting the the certificate state for '{certificate}'");
                }
            }

            [Event(52, Level = EventLevel.Verbose)]
            public void CorrectCertificateStateEnd() => WriteEvent(52, "Finished correcting the certificate state");

            [Event(53, Level = EventLevel.Error)]
            public void CorrectCertificateStateError(string error)
            {
                if (IsEnabled())
                {
                    WriteEvent(53, $"An error has ocurred while correcting the certificate state: {error}.");
                }
            }

            [Event(54, Level = EventLevel.Verbose)]
            internal void MacOSAddCertificateToKeyChainStart(string keychain, string certificate)
            {
                if (IsEnabled())
                {
                    WriteEvent(54, $"Importing the certificate {certificate} to the keychain '{keychain}'");
                }
            }

            [Event(55, Level = EventLevel.Verbose)]
            internal void MacOSAddCertificateToKeyChainEnd() => WriteEvent(55, "Finished importing the certificate to the key chain.");

            [Event(56, Level = EventLevel.Error)]
            internal void MacOSAddCertificateToKeyChainError(int exitCode)
            {
                if (IsEnabled())
                {
                    WriteEvent(56, $"An error has ocurred while importing the certificate to the keychain: {exitCode}.");
                }
            }

            [Event(57, Level = EventLevel.Verbose)]
            public void WritePemKeyToDisk(string path)
            {
                if (IsEnabled())
                {
                    WriteEvent(57, $"Writing the certificate to: {path}.");
                }
            }

            [Event(58, Level = EventLevel.Error)]
            public void WritePemKeyToDiskError(string ex)
            {
                if (IsEnabled())
                {
                    WriteEvent(58, $"An error has ocurred while writing the certificate to disk: {ex}.");
                }
            }

            [Event(59, Level = EventLevel.Error)]
            internal void ImportCertificateMissingFile(string certificatePath)
            {
                if (IsEnabled())
                {
                    WriteEvent(59, $"The file '{certificatePath}' does not exist.");
                }
            }

            [Event(60, Level = EventLevel.Error)]
            internal void ImportCertificateExistingCertificates(string certificateDescription)
            {
                if (IsEnabled())
                {
                    WriteEvent(60, $"One or more HTTPS certificates exist '{certificateDescription}'.");
                }
            }

            [Event(61, Level = EventLevel.Verbose)]
            internal void LoadCertificateStart(string certificatePath)
            {
                if (IsEnabled())
                {
                    WriteEvent(61, $"Loading certificate from path '{certificatePath}'.");
                }
            }

            [Event(62, Level = EventLevel.Verbose)]
            internal void LoadCertificateEnd(string description)
            {
                if (IsEnabled())
                {
                    WriteEvent(62, $"The certificate '{description}' has been loaded successfully.");
                }
            }

            [Event(63, Level = EventLevel.Error)]
            internal void LoadCertificateError(string ex)
            {
                if (IsEnabled())
                {
                    WriteEvent(63, $"An error has ocurred while loading the certificate from disk: {ex}.");
                }
            }

            [Event(64, Level = EventLevel.Error)]
            internal void NoHttpsDevelopmentCertificate(string description)
            {
                if (IsEnabled())
                {
                    WriteEvent(64, $"The provided certificate '{description}' is not a valid ASP.NET Core HTTPS development certificate.");
                }
            }
        }

        internal class UserCancelledTrustException : Exception
        {
        }

        internal struct CheckCertificateStateResult
        {
            public bool Success { get; }
            public string? FailureMessage { get; }

            public CheckCertificateStateResult(bool success, string? failureMessage)
            {
                Success = success;
                FailureMessage = failureMessage;
            }
        }

        internal enum RemoveLocations
        {
            Undefined,
            Local,
            Trusted,
            All
        }
    }
}
