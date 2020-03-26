// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.AspNetCore.Certificates.Generation
{
    internal class CertificateManager
    {
        public const string AspNetHttpsOid = "1.3.6.1.4.1.311.84.1.1";
        public const string AspNetHttpsOidFriendlyName = "ASP.NET Core HTTPS development certificate";

        private const string ServerAuthenticationEnhancedKeyUsageOid = "1.3.6.1.5.5.7.3.1";
        private const string ServerAuthenticationEnhancedKeyUsageOidFriendlyName = "Server Authentication";

        private const string LocalhostHttpsDnsName = "localhost";
        private const string LocalhostHttpsDistinguishedName = "CN=" + LocalhostHttpsDnsName;

        public const int RSAMinimumKeySizeInBits = 2048;

        private static readonly TimeSpan MaxRegexTimeout = TimeSpan.FromMinutes(1);
        private const string CertificateSubjectRegex = "CN=(.*[^,]+).*";
        private const string MacOSSystemKeyChain = "/Library/Keychains/System.keychain";
        private static readonly string MacOSUserKeyChain = Environment.GetEnvironmentVariable("HOME") + "/Library/Keychains/login.keychain-db";
        private const string MacOSFindCertificateCommandLine = "security";
        private static readonly string MacOSFindCertificateCommandLineArgumentsFormat = "find-certificate -c {0} -a -Z -p " + MacOSSystemKeyChain;
        private const string MacOSFindCertificateOutputRegex = "SHA-1 hash: ([0-9A-Z]+)";
        private const string MacOSRemoveCertificateTrustCommandLine = "sudo";
        private const string MacOSRemoveCertificateTrustCommandLineArgumentsFormat = "security remove-trusted-cert -d {0}";
        private const string MacOSDeleteCertificateCommandLine = "sudo";
        private const string MacOSDeleteCertificateCommandLineArgumentsFormat = "security delete-certificate -Z {0} {1}";
        private const string MacOSTrustCertificateCommandLine = "sudo";
        private static readonly string MacOSTrustCertificateCommandLineArguments = "security add-trusted-cert -d -r trustRoot -k " + MacOSSystemKeyChain + " ";
        private const int UserCancelledErrorCode = 1223;
        private const string MacOSSetPartitionKeyPermissionsCommandLine = "sudo";
        private static readonly string MacOSSetPartitionKeyPermissionsCommandLineArguments = "security set-key-partition-list -D localhost -S unsigned:,teamid:UBF8T346G9 " + MacOSUserKeyChain;

        // Setting to 0 means we don't append the version byte,
        // which is what all machines currently have.
        public static int AspNetHttpsCertificateVersion { get; set; } = 1;

        public static bool IsHttpsDevelopmentCertificate(X509Certificate2 certificate) =>
            certificate.Extensions.OfType<X509Extension>()
            .Any(e => string.Equals(AspNetHttpsOid, e.Oid.Value, StringComparison.Ordinal));

        public static IList<X509Certificate2> ListCertificates(
            CertificatePurpose purpose,
            StoreName storeName,
            StoreLocation location,
            bool isValid,
            bool requireExportable = true,
            DiagnosticInformation diagnostics = null)
        {
            diagnostics?.Debug($"Listing '{purpose.ToString()}' certificates on '{location}\\{storeName}'.");
            var certificates = new List<X509Certificate2>();
            try
            {
                using (var store = new X509Store(storeName, location))
                {
                    store.Open(OpenFlags.ReadOnly);
                    certificates.AddRange(store.Certificates.OfType<X509Certificate2>());
                    IEnumerable<X509Certificate2> matchingCertificates = certificates;
                    switch (purpose)
                    {
                        case CertificatePurpose.All:
                            matchingCertificates = matchingCertificates
                                .Where(c => HasOid(c, AspNetHttpsOid));
                            break;
                        case CertificatePurpose.HTTPS:
                            matchingCertificates = matchingCertificates
                                .Where(c => HasOid(c, AspNetHttpsOid));
                            break;
                        default:
                            break;
                    }

                    diagnostics?.Debug(diagnostics.DescribeCertificates(matchingCertificates));
                    if (isValid)
                    {
                        // Ensure the certificate hasn't expired, has a private key and its exportable
                        // (for container/unix scenarios).
                        diagnostics?.Debug("Checking certificates for validity.");
                        var now = DateTimeOffset.Now;
                        var validCertificates = matchingCertificates
                            .Where(c => c.NotBefore <= now &&
                                now <= c.NotAfter &&
                                (!requireExportable || !RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || IsExportable(c))
                                && MatchesVersion(c))
                            .ToArray();

                        var invalidCertificates = matchingCertificates.Except(validCertificates);

                        diagnostics?.Debug("Listing valid certificates");
                        diagnostics?.Debug(diagnostics.DescribeCertificates(validCertificates));
                        diagnostics?.Debug("Listing invalid certificates");
                        diagnostics?.Debug(diagnostics.DescribeCertificates(invalidCertificates));

                        matchingCertificates = validCertificates;
                    }

                    // We need to enumerate the certificates early to prevent dispoisng issues.
                    matchingCertificates = matchingCertificates.ToList();

                    var certificatesToDispose = certificates.Except(matchingCertificates);
                    DisposeCertificates(certificatesToDispose);

                    store.Close();

                    return (IList<X509Certificate2>)matchingCertificates;
                }
            }
            catch
            {
                DisposeCertificates(certificates);
                certificates.Clear();
                return certificates;
            }

            bool HasOid(X509Certificate2 certificate, string oid) =>
                certificate.Extensions.OfType<X509Extension>()
                    .Any(e => string.Equals(oid, e.Oid.Value, StringComparison.Ordinal));

            bool MatchesVersion(X509Certificate2 c)
            {
                var byteArray = c.Extensions.OfType<X509Extension>()
                    .Where(e => string.Equals(AspNetHttpsOid, e.Oid.Value, StringComparison.Ordinal))
                    .Single()
                    .RawData;

                if ((byteArray.Length == AspNetHttpsOidFriendlyName.Length && byteArray[0] == (byte)'A') || byteArray.Length == 0)
                {
                    // No Version set, default to 0
                    return 0 >= AspNetHttpsCertificateVersion;
                }
                else
                {
                    // Version is in the only byte of the byte array.
                    return byteArray[0] >= AspNetHttpsCertificateVersion;
                }
            }
#if !XPLAT
            bool IsExportable(X509Certificate2 c) =>
                ((c.GetRSAPrivateKey() is RSACryptoServiceProvider rsaPrivateKey &&
                    rsaPrivateKey.CspKeyContainerInfo.Exportable) ||
                (c.GetRSAPrivateKey() is RSACng cngPrivateKey &&
                    cngPrivateKey.Key.ExportPolicy == CngExportPolicies.AllowExport));
#else
            // Only check for RSA CryptoServiceProvider and do not fail in XPlat tooling as
            // System.Security.Cryptography.Cng is not part of the shared framework and we don't
            // want to bring the dependency in on CLI scenarios. This functionality will be used
            // on CLI scenarios as part of the first run experience, so checking the exportability
            // of the certificate is not important.
            bool IsExportable(X509Certificate2 c) =>
                ((c.GetRSAPrivateKey() is RSACryptoServiceProvider rsaPrivateKey &&
                    rsaPrivateKey.CspKeyContainerInfo.Exportable) || !(c.GetRSAPrivateKey() is RSACryptoServiceProvider));
#endif
        }

        private static void DisposeCertificates(IEnumerable<X509Certificate2> disposables)
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

        internal bool HasValidCertificateWithInnaccessibleKeyAcrossPartitions()
        {
            var certificates = GetHttpsCertificates();
            if (certificates.Count == 0)
            {
                return false;
            }

            // We need to check all certificates as a new one might be created that hasn't been correctly setup.
            var result = false;
            foreach (var certificate in certificates)
            {
                result = result || !CanAccessCertificateKeyAcrossPartitions(certificate);
            }

            return result;
        }

        public IList<X509Certificate2> GetHttpsCertificates() =>
            ListCertificates(CertificatePurpose.HTTPS, StoreName.My, StoreLocation.CurrentUser, isValid: true, requireExportable: true);

        public X509Certificate2 CreateAspNetCoreHttpsDevelopmentCertificate(DateTimeOffset notBefore, DateTimeOffset notAfter, string subjectOverride, DiagnosticInformation diagnostics = null)
        {
            var subject = new X500DistinguishedName(subjectOverride ?? LocalhostHttpsDistinguishedName);
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
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                certificate.FriendlyName = AspNetHttpsOidFriendlyName;
            }

            return certificate;
        }

        internal static bool CheckDeveloperCertificateKey(X509Certificate2 candidate)
        {
            // Tries to use the certificate key to validate it can't access it
            try
            {
                var rsa = candidate.GetRSAPrivateKey();
                if (rsa == null)
                {
                    return false;
                }

                // Encrypting a random value is the ultimate test for a key validity.
                // Windows and Mac OS both return HasPrivateKey = true if there is (or there has been) a private key associated
                // with the certificate at some point.
                var value = new byte[32];
                RandomNumberGenerator.Fill(value);
                rsa.Decrypt(rsa.Encrypt(value, RSAEncryptionPadding.Pkcs1), RSAEncryptionPadding.Pkcs1);

                // Being able to encrypt and decrypt a payload is the strongest guarantee that the key is valid.
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public X509Certificate2 CreateSelfSignedCertificate(
            X500DistinguishedName subject,
            IEnumerable<X509Extension> extensions,
            DateTimeOffset notBefore,
            DateTimeOffset notAfter)
        {
            var key = CreateKeyMaterial(RSAMinimumKeySizeInBits);

            var request = new CertificateRequest(subject, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            foreach (var extension in extensions)
            {
                request.CertificateExtensions.Add(extension);
            }

            return request.CreateSelfSigned(notBefore, notAfter);

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

        public X509Certificate2 SaveCertificateInStore(X509Certificate2 certificate, StoreName name, StoreLocation location, DiagnosticInformation diagnostics = null)
        {
            diagnostics?.Debug("Saving the certificate into the certificate store.");
            var imported = certificate;
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // On non OSX systems we need to export the certificate and import it so that the transient
                // key that we generated gets persisted.
                var export = certificate.Export(X509ContentType.Pkcs12, "");
                imported = new X509Certificate2(export, "", X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
                Array.Clear(export, 0, export.Length);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                imported.FriendlyName = certificate.FriendlyName;
            }

            using (var store = new X509Store(name, location))
            {
                store.Open(OpenFlags.ReadWrite);
                store.Add(imported);
                store.Close();
            };

            return imported;
        }

        public void ExportCertificate(X509Certificate2 certificate, string path, bool includePrivateKey, string password, DiagnosticInformation diagnostics = null)
        {
            diagnostics?.Debug(
                $"Exporting certificate to '{path}'",
                includePrivateKey ? "The certificate will contain the private key" : "The certificate will not contain the private key");
            if (includePrivateKey && password == null)
            {
                diagnostics?.Debug("No password was provided for the certificate.");
            }

            var targetDirectoryPath = Path.GetDirectoryName(path);
            if (targetDirectoryPath != "")
            {
                diagnostics?.Debug($"Ensuring that the directory for the target exported certificate path exists '{targetDirectoryPath}'");
                Directory.CreateDirectory(targetDirectoryPath);
            }

            byte[] bytes;
            if (includePrivateKey)
            {
                try
                {
                    diagnostics?.Debug($"Exporting the certificate including the private key.");
                    bytes = certificate.Export(X509ContentType.Pkcs12, password);
                }
                catch (Exception e)
                {
                    diagnostics?.Error($"Failed to export the certificate with the private key", e);
                    throw;
                }
            }
            else
            {
                try
                {
                    diagnostics?.Debug($"Exporting the certificate without the private key.");
                    bytes = certificate.Export(X509ContentType.Cert);
                }
                catch (Exception ex)
                {
                    diagnostics?.Error($"Failed to export the certificate without the private key", ex);
                    throw;
                }
            }
            try
            {
                diagnostics?.Debug($"Writing exported certificate to path '{path}'.");
                File.WriteAllBytes(path, bytes);
            }
            catch (Exception ex)
            {
                diagnostics?.Error("Failed writing the certificate to the target path", ex);
                throw;
            }
            finally
            {
                Array.Clear(bytes, 0, bytes.Length);
            }
        }

        public void TrustCertificate(X509Certificate2 certificate, DiagnosticInformation diagnostics = null)
        {
            // Strip certificate of the private key if any.
            var publicCertificate = new X509Certificate2(certificate.Export(X509ContentType.Cert));

            if (!IsTrusted(publicCertificate))
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    diagnostics?.Debug("Trusting the certificate on Windows.");
                    TrustCertificateOnWindows(certificate, publicCertificate, diagnostics);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    diagnostics?.Debug("Trusting the certificate on MAC.");
                    TrustCertificateOnMac(publicCertificate, diagnostics);
                }
            }
        }

        private void TrustCertificateOnMac(X509Certificate2 publicCertificate, DiagnosticInformation diagnostics)
        {
            var tmpFile = Path.GetTempFileName();
            try
            {
                ExportCertificate(publicCertificate, tmpFile, includePrivateKey: false, password: null);
                diagnostics?.Debug("Running the trust command on Mac OS");
                using (var process = Process.Start(MacOSTrustCertificateCommandLine, MacOSTrustCertificateCommandLineArguments + tmpFile))
                {
                    process.WaitForExit();
                    if (process.ExitCode != 0)
                    {
                        throw new InvalidOperationException("There was an error trusting the certificate.");
                    }
                }
            }
            finally
            {
                try
                {
                    if (File.Exists(tmpFile))
                    {
                        File.Delete(tmpFile);
                    }
                }
                catch
                {
                    // We don't care if we can't delete the temp file.
                }
            }
        }

        private static void TrustCertificateOnWindows(X509Certificate2 certificate, X509Certificate2 publicCertificate, DiagnosticInformation diagnostics = null)
        {
            publicCertificate.FriendlyName = certificate.FriendlyName;

            using (var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadWrite);
                var existing = store.Certificates.Find(X509FindType.FindByThumbprint, publicCertificate.Thumbprint, validOnly: false);
                if (existing.Count > 0)
                {
                    diagnostics?.Debug("Certificate already trusted. Skipping trust step.");
                    DisposeCertificates(existing.OfType<X509Certificate2>());
                    return;
                }

                try
                {
                    diagnostics?.Debug("Adding certificate to the store.");
                    store.Add(publicCertificate);
                }
                catch (CryptographicException exception) when (exception.HResult == UserCancelledErrorCode)
                {
                    diagnostics?.Debug("User cancelled the trust prompt.");
                    throw new UserCancelledTrustException();
                }
                store.Close();
            };
        }

        public bool IsTrusted(X509Certificate2 certificate)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return ListCertificates(CertificatePurpose.HTTPS, StoreName.Root, StoreLocation.CurrentUser, isValid: true, requireExportable: false)
                    .Any(c => c.Thumbprint == certificate.Thumbprint);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var subjectMatch = Regex.Match(certificate.Subject, CertificateSubjectRegex, RegexOptions.Singleline, MaxRegexTimeout);
                if (!subjectMatch.Success)
                {
                    throw new InvalidOperationException($"Can't determine the subject for the certificate with subject '{certificate.Subject}'.");
                }
                var subject = subjectMatch.Groups[1].Value;
                using (var checkTrustProcess = Process.Start(new ProcessStartInfo(
                    MacOSFindCertificateCommandLine,
                    string.Format(MacOSFindCertificateCommandLineArgumentsFormat, subject))
                {
                    RedirectStandardOutput = true
                }))
                {
                    var output = checkTrustProcess.StandardOutput.ReadToEnd();
                    checkTrustProcess.WaitForExit();
                    var matches = Regex.Matches(output, MacOSFindCertificateOutputRegex, RegexOptions.Multiline, MaxRegexTimeout);
                    var hashes = matches.OfType<Match>().Select(m => m.Groups[1].Value).ToList();
                    return hashes.Any(h => string.Equals(h, certificate.Thumbprint, StringComparison.Ordinal));
                }
            }
            else
            {
                return false;
            }
        }

        public void CleanupHttpsCertificates(string subject = LocalhostHttpsDistinguishedName)
        {
            CleanupCertificates(CertificatePurpose.HTTPS, subject);
        }

        public void CleanupCertificates(CertificatePurpose purpose, string subject)
        {
            // On OS X we don't have a good way to manage trusted certificates in the system keychain
            // so we do everything by invoking the native toolchain.
            // This has some limitations, like for example not being able to identify our custom OID extension. For that
            // matter, when we are cleaning up certificates on the machine, we start by removing the trusted certificates.
            // To do this, we list the certificates that we can identify on the current user personal store and we invoke
            // the native toolchain to remove them from the sytem keychain. Once we have removed the trusted certificates,
            // we remove the certificates from the local user store to finish up the cleanup.
            var certificates = ListCertificates(purpose, StoreName.My, StoreLocation.CurrentUser, isValid: false);
            foreach (var certificate in certificates)
            {
                RemoveCertificate(certificate, RemoveLocations.All);
            }
        }

        public DiagnosticInformation CleanupHttpsCertificates2(string subject = LocalhostHttpsDistinguishedName)
        {
            return CleanupCertificates2(CertificatePurpose.HTTPS, subject);
        }

        public DiagnosticInformation CleanupCertificates2(CertificatePurpose purpose, string subject)
        {
            var diagnostics = new DiagnosticInformation();
            // On OS X we don't have a good way to manage trusted certificates in the system keychain
            // so we do everything by invoking the native toolchain.
            // This has some limitations, like for example not being able to identify our custom OID extension. For that
            // matter, when we are cleaning up certificates on the machine, we start by removing the trusted certificates.
            // To do this, we list the certificates that we can identify on the current user personal store and we invoke
            // the native toolchain to remove them from the sytem keychain. Once we have removed the trusted certificates,
            // we remove the certificates from the local user store to finish up the cleanup.
            var certificates = ListCertificates(purpose, StoreName.My, StoreLocation.CurrentUser, isValid: false, requireExportable: true, diagnostics);
            foreach (var certificate in certificates)
            {
                RemoveCertificate(certificate, RemoveLocations.All, diagnostics);
            }

            return diagnostics;
        }

        public void RemoveAllCertificates(CertificatePurpose purpose, StoreName storeName, StoreLocation storeLocation, string subject = null)
        {
            var certificates = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ?
                ListCertificates(purpose, StoreName.My, StoreLocation.CurrentUser, isValid: false) :
                ListCertificates(purpose, storeName, storeLocation, isValid: false);
            var certificatesWithName = subject == null ? certificates : certificates.Where(c => c.Subject == subject);

            var removeLocation = storeName == StoreName.My ? RemoveLocations.Local : RemoveLocations.Trusted;

            foreach (var certificate in certificates)
            {
                RemoveCertificate(certificate, removeLocation);
            }

            DisposeCertificates(certificates);
        }

        private void RemoveCertificate(X509Certificate2 certificate, RemoveLocations locations, DiagnosticInformation diagnostics = null)
        {
            switch (locations)
            {
                case RemoveLocations.Undefined:
                    throw new InvalidOperationException($"'{nameof(RemoveLocations.Undefined)}' is not a valid location.");
                case RemoveLocations.Local:
                    RemoveCertificateFromUserStore(certificate, diagnostics);
                    break;
                case RemoveLocations.Trusted when !RuntimeInformation.IsOSPlatform(OSPlatform.Linux):
                    RemoveCertificateFromTrustedRoots(certificate, diagnostics);
                    break;
                case RemoveLocations.All:
                    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        RemoveCertificateFromTrustedRoots(certificate, diagnostics);
                    }
                    RemoveCertificateFromUserStore(certificate, diagnostics);
                    break;
                default:
                    throw new InvalidOperationException("Invalid location.");
            }
        }

        private static void RemoveCertificateFromUserStore(X509Certificate2 certificate, DiagnosticInformation diagnostics)
        {
            diagnostics?.Debug($"Trying to remove certificate with thumbprint '{certificate.Thumbprint}' from certificate store '{StoreLocation.CurrentUser}\\{StoreName.My}'.");
            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadWrite);
                var matching = store.Certificates
                    .OfType<X509Certificate2>()
                    .Single(c => c.SerialNumber == certificate.SerialNumber);

                store.Remove(matching);
                store.Close();
            }
        }

        private void RemoveCertificateFromTrustedRoots(X509Certificate2 certificate, DiagnosticInformation diagnostics)
        {
            diagnostics?.Debug($"Trying to remove certificate with thumbprint '{certificate.Thumbprint}' from certificate store '{StoreLocation.CurrentUser}\\{StoreName.Root}'.");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using (var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser))
                {
                    store.Open(OpenFlags.ReadWrite);
                    var matching = store.Certificates
                        .OfType<X509Certificate2>()
                        .SingleOrDefault(c => c.SerialNumber == certificate.SerialNumber);

                    if (matching != null)
                    {
                        store.Remove(matching);
                    }

                    store.Close();
                }
            }
            else
            {
                if (IsTrusted(certificate)) // On OSX this check just ensures its on the system keychain
                {
                    try
                    {
                        diagnostics?.Debug("Trying to remove the certificate trust rule.");
                        RemoveCertificateTrustRule(certificate);
                    }
                    catch
                    {
                        diagnostics?.Debug("Failed to remove the certificate trust rule.");
                        // We don't care if we fail to remove the trust rule if
                        // for some reason the certificate became untrusted.
                        // The delete command will fail if the certificate is
                        // trusted.
                    }
                    RemoveCertificateFromKeyChain(MacOSSystemKeyChain, certificate);
                }
                else
                {
                    diagnostics?.Debug("The certificate was not trusted.");
                }
            }
        }

        private static void RemoveCertificateTrustRule(X509Certificate2 certificate)
        {
            var certificatePath = Path.GetTempFileName();
            try
            {
                var certBytes = certificate.Export(X509ContentType.Cert);
                File.WriteAllBytes(certificatePath, certBytes);
                var processInfo = new ProcessStartInfo(
                    MacOSRemoveCertificateTrustCommandLine,
                    string.Format(
                        MacOSRemoveCertificateTrustCommandLineArgumentsFormat,
                        certificatePath
                    ));
                using (var process = Process.Start(processInfo))
                {
                    process.WaitForExit();
                }
            }
            finally
            {
                try
                {
                    if (File.Exists(certificatePath))
                    {
                        File.Delete(certificatePath);
                    }
                }
                catch
                {
                    // We don't care about failing to do clean-up on a temp file.
                }
            }
        }

        private static void RemoveCertificateFromKeyChain(string keyChain, X509Certificate2 certificate)
        {
            var processInfo = new ProcessStartInfo(
                MacOSDeleteCertificateCommandLine,
                string.Format(
                    MacOSDeleteCertificateCommandLineArgumentsFormat,
                    certificate.Thumbprint.ToUpperInvariant(),
                    keyChain
                ))
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using (var process = Process.Start(processInfo))
            {
                var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($@"There was an error removing the certificate with thumbprint '{certificate.Thumbprint}'.

{output}");
                }
            }
        }

        public DetailedEnsureCertificateResult EnsureAspNetCoreHttpsDevelopmentCertificate(
            DateTimeOffset notBefore,
            DateTimeOffset notAfter,
            string path = null,
            bool trust = false,
            bool includePrivateKey = false,
            string password = null,
            string subject = LocalhostHttpsDistinguishedName,
            bool isInteractive = true)
        {
            return EnsureValidCertificateExists(notBefore, notAfter, CertificatePurpose.HTTPS, path, trust, includePrivateKey, password, subject, isInteractive);
        }

        public DetailedEnsureCertificateResult EnsureValidCertificateExists(
            DateTimeOffset notBefore,
            DateTimeOffset notAfter,
            CertificatePurpose purpose,
            string path = null,
            bool trust = false,
            bool includePrivateKey = false,
            string password = null,
            string subjectOverride = null,
            bool isInteractive = true)
        {
            if (purpose == CertificatePurpose.All)
            {
                throw new ArgumentException("The certificate must have a specific purpose.");
            }

            var result = new DetailedEnsureCertificateResult();

            var certificates = ListCertificates(purpose, StoreName.My, StoreLocation.CurrentUser, isValid: true, requireExportable: true, result.Diagnostics).Concat(
                ListCertificates(purpose, StoreName.My, StoreLocation.LocalMachine, isValid: true, requireExportable: true, result.Diagnostics));

            var filteredCertificates = subjectOverride == null ? certificates : certificates.Where(c => c.Subject == subjectOverride);
            if (subjectOverride != null)
            {
                var excludedCertificates = certificates.Except(filteredCertificates);

                result.Diagnostics.Debug($"Filtering found certificates to those with a subject equal to '{subjectOverride}'");
                result.Diagnostics.Debug(result.Diagnostics.DescribeCertificates(filteredCertificates));
                result.Diagnostics.Debug($"Listing certificates excluded from consideration.");
                result.Diagnostics.Debug(result.Diagnostics.DescribeCertificates(excludedCertificates));
            }
            else
            {
                result.Diagnostics.Debug("Skipped filtering certificates by subject.");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                foreach (var cert in filteredCertificates)
                {
                    if (!CanAccessCertificateKeyAcrossPartitions(cert))
                    {
                        if (!isInteractive)
                        {
                            // If the process is not interactive (first run experience) bail out. We will simply create a certificate
                            // in case there is none or report success during the first run experience.
                            break;
                        }
                        try
                        {
                            // The command we run handles making keys for all localhost certificates accessible across partitions. If it can not run the
                            // command safely (because there are other localhost certificates that were not created by asp.net core, it will throw.
                            MakeCertificateKeyAccessibleAcrossPartitions(cert);
                            break;
                        }
                        catch (Exception ex)
                        {
                            result.Diagnostics.Error("Failed to make certificate key accessible", ex);
                            result.ResultCode = EnsureCertificateResult.FailedToMakeKeyAccessible;
                            return result;
                        }
                    }
                }
            }

            certificates = filteredCertificates;

            result.ResultCode = EnsureCertificateResult.Succeeded;

            X509Certificate2 certificate = null;
            if (certificates.Any())
            {
                result.Diagnostics.Debug("Found valid certificates present on the machine.");
                result.Diagnostics.Debug(result.Diagnostics.DescribeCertificates(certificates));
                certificate = certificates.First();
                result.Diagnostics.Debug("Selected certificate");
                result.Diagnostics.Debug(result.Diagnostics.DescribeCertificates(certificate));
                result.ResultCode = EnsureCertificateResult.ValidCertificatePresent;
            }
            else
            {
                result.Diagnostics.Debug("No valid certificates present on this machine. Trying to create one.");
                try
                {
                    switch (purpose)
                    {
                        case CertificatePurpose.All:
                            throw new InvalidOperationException("The certificate must have a specific purpose.");
                        case CertificatePurpose.HTTPS:
                            certificate = CreateAspNetCoreHttpsDevelopmentCertificate(notBefore, notAfter, subjectOverride, result.Diagnostics);
                            break;
                        default:
                            throw new InvalidOperationException("The certificate must have a purpose.");
                    }
                }
                catch (Exception e)
                {
                    result.Diagnostics.Error("Error creating the certificate.", e);
                    result.ResultCode = EnsureCertificateResult.ErrorCreatingTheCertificate;
                    return result;
                }

                try
                {
                    certificate = SaveCertificateInStore(certificate, StoreName.My, StoreLocation.CurrentUser, result.Diagnostics);
                }
                catch (Exception e)
                {
                    result.Diagnostics.Error($"Error saving the certificate in the certificate store '{StoreLocation.CurrentUser}\\{StoreName.My}'.", e);
                    result.ResultCode = EnsureCertificateResult.ErrorSavingTheCertificateIntoTheCurrentUserPersonalStore;
                    return result;
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && isInteractive)
                {
                    MakeCertificateKeyAccessibleAcrossPartitions(certificate);
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && isInteractive)
                {
                    MakeCertificateKeyAccessibleAcrossPartitions(certificate);
                }
            }
            if (path != null)
            {
                result.Diagnostics.Debug("Trying to export the certificate.");
                result.Diagnostics.Debug(result.Diagnostics.DescribeCertificates(certificate));
                try
                {
                    ExportCertificate(certificate, path, includePrivateKey, password, result.Diagnostics);
                }
                catch (Exception e)
                {
                    result.Diagnostics.Error("An error ocurred exporting the certificate.", e);
                    result.ResultCode = EnsureCertificateResult.ErrorExportingTheCertificate;
                    return result;
                }
            }

            if ((RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) && trust)
            {
                try
                {
                    result.Diagnostics.Debug("Trying to export the certificate.");
                    TrustCertificate(certificate, result.Diagnostics);
                }
                catch (UserCancelledTrustException)
                {
                    result.Diagnostics.Error("The user cancelled trusting the certificate.", null);
                    result.ResultCode = EnsureCertificateResult.UserCancelledTrustStep;
                    return result;
                }
                catch (Exception e)
                {
                    result.Diagnostics.Error("There was an error trusting the certificate.", e);
                    result.ResultCode = EnsureCertificateResult.FailedToTrustTheCertificate;
                    return result;
                }
            }

            return result;
        }

        private void MakeCertificateKeyAccessibleAcrossPartitions(X509Certificate2 certificate)
        {
            if (OtherNonAspNetCoreHttpsCertificatesPresent())
            {
                throw new InvalidOperationException("Unable to make HTTPS certificate key trusted across security partitions.");
            }
            using (var process = Process.Start(MacOSSetPartitionKeyPermissionsCommandLine, MacOSSetPartitionKeyPermissionsCommandLineArguments))
            {
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException("Error making the key accessible across partitions.");
                }
            }

            var certificateSentinelPath = GetCertificateSentinelPath(certificate);
            File.WriteAllText(certificateSentinelPath, "true");
        }

        private static string GetCertificateSentinelPath(X509Certificate2 certificate) =>
            Path.Combine(Environment.GetEnvironmentVariable("HOME"), ".dotnet", $"certificate.{certificate.GetCertHashString(HashAlgorithmName.SHA256)}.sentinel");

        private bool OtherNonAspNetCoreHttpsCertificatesPresent()
        {
            var certificates = new List<X509Certificate2>();
            try
            {
                using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
                {
                    store.Open(OpenFlags.ReadOnly);
                    certificates.AddRange(store.Certificates.OfType<X509Certificate2>());
                    IEnumerable<X509Certificate2> matchingCertificates = certificates;
                        // Ensure the certificate hasn't expired, has a private key and its exportable
                        // (for container/unix scenarios).
                        var now = DateTimeOffset.Now;
                        matchingCertificates = matchingCertificates
                            .Where(c => c.NotBefore <= now &&
                                now <= c.NotAfter && c.Subject == LocalhostHttpsDistinguishedName);

                    // We need to enumerate the certificates early to prevent dispoisng issues.
                    matchingCertificates = matchingCertificates.ToList();

                    var certificatesToDispose = certificates.Except(matchingCertificates);
                    DisposeCertificates(certificatesToDispose);

                    store.Close();

                    return matchingCertificates.All(c => !HasOid(c, AspNetHttpsOid));
                }
            }
            catch
            {
                DisposeCertificates(certificates);
                certificates.Clear();
                return true;
            }

            bool HasOid(X509Certificate2 certificate, string oid) =>
                certificate.Extensions.OfType<X509Extension>()
                .Any(e => string.Equals(oid, e.Oid.Value, StringComparison.Ordinal));
        }

        private bool CanAccessCertificateKeyAcrossPartitions(X509Certificate2 certificate)
        {
            var certificateSentinelPath = GetCertificateSentinelPath(certificate);
            return File.Exists(certificateSentinelPath);
        }

        private class UserCancelledTrustException : Exception
        {
        }

        private enum RemoveLocations
        {
            Undefined,
            Local,
            Trusted,
            All
        }

        internal class DetailedEnsureCertificateResult
        {
            public EnsureCertificateResult ResultCode { get; set; }
            public DiagnosticInformation Diagnostics { get; set; } = new DiagnosticInformation();
        }

        internal class DiagnosticInformation
        {
            public IList<string> Messages { get; } = new List<string>();

            public IList<Exception> Exceptions { get; } = new List<Exception>();

            internal void Debug(params string[] messages)
            {
                foreach (var message in messages)
                {
                    Messages.Add(message);
                }
            }

            internal string[] DescribeCertificates(params X509Certificate2[] certificates)
            {
                return DescribeCertificates(certificates.AsEnumerable());
            }

            internal string[] DescribeCertificates(IEnumerable<X509Certificate2> certificates)
            {
                var result = new List<string>();
                result.Add($"'{certificates.Count()}' found matching the criteria.");
                result.Add($"SUBJECT - THUMBPRINT - NOT BEFORE - EXPIRES - HAS PRIVATE KEY");
                foreach (var certificate in certificates)
                {
                    result.Add(DescribeCertificate(certificate));
                }

                return result.ToArray();
            }

            private static string DescribeCertificate(X509Certificate2 certificate) =>
                $"{certificate.Subject} - {certificate.Thumbprint} - {certificate.NotBefore} - {certificate.NotAfter} - {certificate.HasPrivateKey}";

            internal void Error(string preamble, Exception e)
            {
                Messages.Add(preamble);
                if (Exceptions.Count > 0 && Exceptions[Exceptions.Count - 1] == e)
                {
                    return;
                }

                var ex = e;
                while (ex != null)
                {
                    Messages.Add("Exception message: " + ex.Message);
                    ex = ex.InnerException;
                }

            }
        }
    }
}
