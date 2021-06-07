
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace Microsoft.AspNetCore.Certificates.Generation
{
    internal class MacOSCertificateManager : CertificateManager
    {
        private const string CertificateSubjectRegex = "CN=(.*[^,]+).*";
        private static readonly string MacOSUserKeyChain = Environment.GetEnvironmentVariable("HOME") + "/Library/Keychains/login.keychain-db";
        private const string MacOSSystemKeyChain = "/Library/Keychains/System.keychain";
        private const string MacOSFindCertificateCommandLine = "security";
        private const string MacOSFindCertificateCommandLineArgumentsFormat = "find-certificate -c {0} -a -Z -p " + MacOSSystemKeyChain;
        private const string MacOSFindCertificateOutputRegex = "SHA-1 hash: ([0-9A-Z]+)";
        private const string MacOSRemoveCertificateTrustCommandLine = "sudo";
        private const string MacOSRemoveCertificateTrustCommandLineArgumentsFormat = "security remove-trusted-cert -d {0}";
        private const string MacOSDeleteCertificateCommandLine = "sudo";
        private const string MacOSDeleteCertificateCommandLineArgumentsFormat = "security delete-certificate -Z {0} {1}";
        private const string MacOSTrustCertificateCommandLine = "sudo";
        private const string MacOSTrustCertificateCommandLineArguments = "security add-trusted-cert -d -r trustRoot -k " + MacOSSystemKeyChain + " ";

        private const string MacOSAddCertificateToKeyChainCommandLine = "security";
        private static readonly string MacOSAddCertificateToKeyChainCommandLineArgumentsFormat = "import {0} -k " + MacOSUserKeyChain + " -t cert -f pkcs12 -P {1} -A";

        public const string InvalidCertificateState = "The ASP.NET Core developer certificate is in an invalid state. " +
            "To fix this issue, run the following commands 'dotnet dev-certs https --clean' and 'dotnet dev-certs https' to remove all existing ASP.NET Core development certificates " +
            "and create a new untrusted developer certificate. " +
            "On macOS or Windows, use 'dotnet dev-certs https --trust' to trust the new certificate.";

        public const string KeyNotAccessibleWithoutUserInteraction =
            "The application is trying to access the ASP.NET Core developer certificate key. " +
            "A prompt might appear to ask for permission to access the key. " +
            "When that happens, select 'Always Allow' to grant 'dotnet' access to the certificate key in the future.";

        private static readonly TimeSpan MaxRegexTimeout = TimeSpan.FromMinutes(1);

        public MacOSCertificateManager()
        {
        }

        internal MacOSCertificateManager(string subject, int version)
            : base(subject, version)
        {
        }

        protected override void TrustCertificateCore(X509Certificate2 publicCertificate)
        {
            var tmpFile = Path.GetTempFileName();
            try
            {
                ExportCertificate(publicCertificate, tmpFile, includePrivateKey: false, password: null, CertificateKeyExportFormat.Pfx);
                if (Log.IsEnabled())
                {
                    Log.MacOSTrustCommandStart($"{MacOSTrustCertificateCommandLine} {MacOSTrustCertificateCommandLineArguments}{tmpFile}");
                }
                using (var process = Process.Start(MacOSTrustCertificateCommandLine, MacOSTrustCertificateCommandLineArguments + tmpFile))
                {
                    process.WaitForExit();
                    if (process.ExitCode != 0)
                    {
                        Log.MacOSTrustCommandError(process.ExitCode);
                        throw new InvalidOperationException("There was an error trusting the certificate.");
                    }
                }
                Log.MacOSTrustCommandEnd();
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

        internal override CheckCertificateStateResult CheckCertificateState(X509Certificate2 candidate, bool interactive)
        {
            var sentinelPath = Path.Combine(Environment.GetEnvironmentVariable("HOME")!, ".dotnet", $"certificates.{candidate.GetCertHashString(HashAlgorithmName.SHA256)}.sentinel");
            if (!interactive && !File.Exists(sentinelPath))
            {
                return new CheckCertificateStateResult(false, KeyNotAccessibleWithoutUserInteraction);
            }

            // Tries to use the certificate key to validate it can't access it
            try
            {
                using var rsa = candidate.GetRSAPrivateKey();
                if (rsa == null)
                {
                    return new CheckCertificateStateResult(false, InvalidCertificateState);
                }

                // Encrypting a random value is the ultimate test for a key validity.
                // Windows and Mac OS both return HasPrivateKey = true if there is (or there has been) a private key associated
                // with the certificate at some point.
                var value = new byte[32];
                RandomNumberGenerator.Fill(value);
                rsa.Decrypt(rsa.Encrypt(value, RSAEncryptionPadding.Pkcs1), RSAEncryptionPadding.Pkcs1);

                // If we were able to access the key, create a sentinel so that we don't have to show a prompt
                // on every kestrel run.
                if (Directory.Exists(Path.GetDirectoryName(sentinelPath)) && !File.Exists(sentinelPath))
                {
                    File.WriteAllText(sentinelPath, "true");
                }

                // Being able to encrypt and decrypt a payload is the strongest guarantee that the key is valid.
                return new CheckCertificateStateResult(true, null);
            }
            catch (Exception)
            {
                return new CheckCertificateStateResult(false, InvalidCertificateState);
            }
        }


        internal override void CorrectCertificateState(X509Certificate2 candidate)
        {
            var status = CheckCertificateState(candidate, true);
            if (!status.Success)
            {
                throw new InvalidOperationException(InvalidCertificateState);
            }
        }


        public override bool IsTrusted(X509Certificate2 certificate)
        {
            var subjectMatch = Regex.Match(certificate.Subject, CertificateSubjectRegex, RegexOptions.Singleline, MaxRegexTimeout);
            if (!subjectMatch.Success)
            {
                throw new InvalidOperationException($"Can't determine the subject for the certificate with subject '{certificate.Subject}'.");
            }
            var subject = subjectMatch.Groups[1].Value;
            using var checkTrustProcess = Process.Start(new ProcessStartInfo(
                MacOSFindCertificateCommandLine,
                string.Format(CultureInfo.InvariantCulture, MacOSFindCertificateCommandLineArgumentsFormat, subject))
            {
                RedirectStandardOutput = true
            });
            var output = checkTrustProcess!.StandardOutput.ReadToEnd();
            checkTrustProcess.WaitForExit();
            var matches = Regex.Matches(output, MacOSFindCertificateOutputRegex, RegexOptions.Multiline, MaxRegexTimeout);
            var hashes = matches.OfType<Match>().Select(m => m.Groups[1].Value).ToList();
            return hashes.Any(h => string.Equals(h, certificate.Thumbprint, StringComparison.Ordinal));
        }

        public override bool SupportsTrust => true;

        protected override void RemoveCertificateFromTrustedRoots(X509Certificate2 certificate)
        {
            if (IsTrusted(certificate)) // On OSX this check just ensures its on the system keychain
            {
                // A trusted certificate in OSX is installed into the system keychain and
                // as a "trust rule" applied to it.
                // To remove the certificate we first need to remove the "trust rule" and then
                // remove the certificate from the keychain.
                // We don't care if we fail to remove the trust rule if
                // for some reason the certificate became untrusted.
                // Trying to remove the certificate from the keychain will fail if the certificate is
                // trusted.
                try
                {
                    RemoveCertificateTrustRule(certificate);
                }
                catch
                {
                }

                RemoveCertificateFromKeyChain(MacOSSystemKeyChain, certificate);
            }
            else
            {
                Log.MacOSCertificateUntrusted(GetDescription(certificate));
            }
        }

        private static void RemoveCertificateTrustRule(X509Certificate2 certificate)
        {
            Log.MacOSRemoveCertificateTrustRuleStart(GetDescription(certificate));
            var certificatePath = Path.GetTempFileName();
            try
            {
                var certBytes = certificate.Export(X509ContentType.Cert);
                File.WriteAllBytes(certificatePath, certBytes);
                var processInfo = new ProcessStartInfo(
                    MacOSRemoveCertificateTrustCommandLine,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        MacOSRemoveCertificateTrustCommandLineArgumentsFormat,
                        certificatePath
                    ));
                using var process = Process.Start(processInfo);
                process!.WaitForExit();
                if (process.ExitCode != 0)
                {
                    Log.MacOSRemoveCertificateTrustRuleError(process.ExitCode);
                }
                Log.MacOSRemoveCertificateTrustRuleEnd();
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
                    CultureInfo.InvariantCulture,
                    MacOSDeleteCertificateCommandLineArgumentsFormat,
                    certificate.Thumbprint.ToUpperInvariant(),
                    keyChain
                ))
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            if (Log.IsEnabled())
            {
                Log.MacOSRemoveCertificateFromKeyChainStart(keyChain, GetDescription(certificate));
            }

            using (var process = Process.Start(processInfo))
            {
                var output = process!.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    Log.MacOSRemoveCertificateFromKeyChainError(process.ExitCode);
                    throw new InvalidOperationException($@"There was an error removing the certificate with thumbprint '{certificate.Thumbprint}'.

{output}");
                }
            }

            Log.MacOSRemoveCertificateFromKeyChainEnd();
        }

        // We don't have a good way of checking on the underlying implementation if ti is exportable, so just return true.
        protected override bool IsExportable(X509Certificate2 c) => true;

        protected override X509Certificate2 SaveCertificateCore(X509Certificate2 certificate, StoreName storeName, StoreLocation storeLocation)
        {
            // security import https.pfx -k $loginKeyChain -t cert -f pkcs12 -P password -A;
            var passwordBytes = new byte[48];
            RandomNumberGenerator.Fill(passwordBytes.AsSpan()[0..35]);
            var password = Convert.ToBase64String(passwordBytes, 0, 36);
            var certBytes = certificate.Export(X509ContentType.Pfx, password);
            var certificatePath = Path.GetTempFileName();
            File.WriteAllBytes(certificatePath, certBytes);

            var processInfo = new ProcessStartInfo(
                MacOSAddCertificateToKeyChainCommandLine,
            string.Format(
                CultureInfo.InvariantCulture,
                MacOSAddCertificateToKeyChainCommandLineArgumentsFormat,
                certificatePath,
                password
            ))
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            if (Log.IsEnabled())
            {
                Log.MacOSAddCertificateToKeyChainStart(MacOSUserKeyChain, GetDescription(certificate));
            }

            using (var process = Process.Start(processInfo))
            {
                var output = process!.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    Log.MacOSAddCertificateToKeyChainError(process.ExitCode);
                    throw new InvalidOperationException($@"There was an error importing the certificate into the user key chain '{certificate.Thumbprint}'.

{output}");
                }
            }

            Log.MacOSAddCertificateToKeyChainEnd();

            return certificate;
        }

        protected override IList<X509Certificate2> GetCertificatesToRemove(StoreName storeName, StoreLocation storeLocation)
        {
            return ListCertificates(StoreName.My, StoreLocation.CurrentUser, isValid: false);
        }
    }
}
