// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.AspNetCore.Certificates.Generation;

internal sealed class MacOSCertificateManager : CertificateManager
{
    private static readonly string MacOSUserKeyChain = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/Library/Keychains/login.keychain-db";

    // Verify the certificate {0} for the SSL and X.509 Basic Policy.
    private const string MacOSVerifyCertificateCommandLine = "security";
    private const string MacOSVerifyCertificateCommandLineArgumentsFormat = $"verify-cert -c {{0}} -p basic -p ssl";

    // Delete a certificate with the specified SHA-256 (or SHA-1) hash {0} from keychain {1}.
    private const string MacOSDeleteCertificateCommandLine = "sudo";
    private const string MacOSDeleteCertificateCommandLineArgumentsFormat = "security delete-certificate -Z {0} {1}";

    // Add a certificate to the per-user trust settings in the user keychain. The trust policy
    // for the certificate will be set to be always trusted for SSL and X.509 Basic Policy.
    // Note: This operation will require user authentication via a dialog.
    private const string MacOSTrustCertificateCommandLine = "security";
    private static readonly string MacOSTrustCertificateCommandLineArguments = $"add-trusted-cert -p basic -p ssl -k {MacOSUserKeyChain} ";

    // Well-known location on disk where dev-certs are stored.
    private static readonly string MacOSUserHttpsCertificateLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".aspnet", "dev-certs", "https");

    // Import a pkcs12 certificate into the user keychain using the unwrapping passphrase {1}, and
    // allow any application to access the imported key without warning.
    private const string MacOSAddCertificateToKeyChainCommandLine = "security";
    private static readonly string MacOSAddCertificateToKeyChainCommandLineArgumentsFormat = "import {0} -k " + MacOSUserKeyChain + " -t cert -f pkcs12 -P {1} -A";

    public const string InvalidCertificateState =
        "The ASP.NET Core developer certificate is in an invalid state. " +
        "To fix this issue, run 'dotnet dev-certs https --clean' and 'dotnet dev-certs https' " +
        "to remove all existing ASP.NET Core development certificates " +
        "and create a new untrusted developer certificate. " +
        "On macOS or Windows, use 'dotnet dev-certs https --trust' to trust the new certificate.";

    public const string KeyNotAccessibleWithoutUserInteraction =
        "The application is trying to access the ASP.NET Core developer certificate key. " +
        "A prompt might appear to ask for permission to access the key. " +
        "When that happens, select 'Always Allow' to grant 'dotnet' access to the certificate key in the future.";

    public MacOSCertificateManager()
    {
    }

    internal MacOSCertificateManager(string subject, int version)
        : base(subject, version)
    {
    }

    protected override void TrustCertificateCore(X509Certificate2 publicCertificate)
    {
        if (IsTrusted(publicCertificate))
        {
            Log.MacOSCertificateAlreadyTrusted();
            return;
        }

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
        var sentinelPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet", $"certificates.{candidate.GetCertHashString(HashAlgorithmName.SHA256)}.sentinel");
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

    // Use verify-cert to verify the certificate for the SSL and X.509 Basic Policy.
    public override bool IsTrusted(X509Certificate2 certificate)
    {
        var tmpFile = Path.GetTempFileName();
        try
        {
            ExportCertificate(certificate, tmpFile, includePrivateKey: false, password: null, CertificateKeyExportFormat.Pem);

            using var checkTrustProcess = Process.Start(new ProcessStartInfo(
                MacOSVerifyCertificateCommandLine,
                string.Format(CultureInfo.InvariantCulture, MacOSVerifyCertificateCommandLineArgumentsFormat, tmpFile))
            {
                RedirectStandardOutput = true,
                // Do this to avoid showing output to the console when the cert is not trusted. It is trivial to export
                // the cert and replicate the command to see details.
                RedirectStandardError = true,
            });
            checkTrustProcess!.WaitForExit();
            return checkTrustProcess.ExitCode == 0;
        }
        finally
        {
            if (File.Exists(tmpFile))
            {
                File.Delete(tmpFile);
            }
        }
    }

    protected override void RemoveCertificateFromTrustedRoots(X509Certificate2 certificate)
    {
        RemoveCertificateFromKeyChain(MacOSUserKeyChain, certificate);
        RemoveCertificateFromUserStoreCore(certificate);
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

    // We don't have a good way of checking on the underlying implementation if it is exportable, so just return true.
    protected override bool IsExportable(X509Certificate2 c) => true;

    protected override X509Certificate2 SaveCertificateCore(X509Certificate2 certificate, StoreName storeName, StoreLocation storeLocation)
    {
        SaveCertificateToUserKeychain(certificate);

        try
        {
            var certBytes = certificate.Export(X509ContentType.Pfx);

            if (Log.IsEnabled())
            {
                Log.MacOSAddCertificateToUserProfileDirStart(MacOSUserKeyChain, GetDescription(certificate));
            }

            // Ensure that the directory exists before writing to the file.
            Directory.CreateDirectory(MacOSUserHttpsCertificateLocation);

            File.WriteAllBytes(GetCertificateFilePath(certificate), certBytes);
        }
        catch (Exception ex)
        {
            Log.MacOSAddCertificateToUserProfileDirError(certificate.Thumbprint, ex.Message);
        }

        Log.MacOSAddCertificateToKeyChainEnd();
        Log.MacOSAddCertificateToUserProfileDirEnd();

        return certificate;
    }

    private static bool SaveCertificateToUserKeychain(X509Certificate2 certificate)
    {
        var passwordBytes = new byte[48];
        RandomNumberGenerator.Fill(passwordBytes.AsSpan()[0..35]);
        var password = Convert.ToBase64String(passwordBytes, 0, 36);
        var certBytes = certificate.Export(X509ContentType.Pfx, password);
        var certificatePath = Path.GetTempFileName();
        File.WriteAllBytes(certificatePath, certBytes);

        var processInfo = new ProcessStartInfo(
            MacOSAddCertificateToKeyChainCommandLine,
            string.Format(CultureInfo.InvariantCulture,MacOSAddCertificateToKeyChainCommandLineArgumentsFormat, certificatePath, password))
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
                Log.MacOSAddCertificateToKeyChainError(process.ExitCode, output);
                throw new InvalidOperationException("Failed to add the certificate to the keychain. Are you running in a non-interactive session perhaps?");
            }
        }

        Log.MacOSAddCertificateToKeyChainEnd();
        return true;
    }

    private static string GetCertificateFilePath(X509Certificate2 certificate) =>
        Path.Combine(MacOSUserHttpsCertificateLocation, $"aspnetcore-localhost-{certificate.Thumbprint}.pfx");

    protected override IList<X509Certificate2> GetCertificatesToRemove(StoreName storeName, StoreLocation storeLocation)
    {
        return ListCertificates(StoreName.My, StoreLocation.CurrentUser, isValid: false);
    }

    protected override void PopulateCertificatesFromStore(X509Store store, List<X509Certificate2> certificates)
    {
        if (store.Name! != StoreName.My.ToString() || store.Location != StoreLocation.CurrentUser)
        {
            base.PopulateCertificatesFromStore(store, certificates);
        }
        else
        {
            if (Directory.Exists(MacOSUserHttpsCertificateLocation))
            {
                var certificateFiles = Directory.EnumerateFiles(MacOSUserHttpsCertificateLocation, "aspnetcore-localhost-*.pfx");
                foreach (var file in certificateFiles)
                {
                    try
                    {
                        var certificate = new X509Certificate2(file);
                        certificates.Add(certificate);
                    }
                    catch (Exception)
                    {
                        Log.MacOSFileIsNotAValidCertificate(file);
                        throw;
                    }
                }
            }

        }
    }

    protected override void RemoveCertificateFromUserStoreCore(X509Certificate2 certificate)
    {
        try
        {
            var certificatePath = GetCertificateFilePath(certificate);
            if (File.Exists(certificatePath))
            {
                File.Delete(certificatePath);
            }
        }
        catch (Exception ex)
        {
            Log.MacOSRemoveCertificateFromUserProfileDirError(certificate.Thumbprint, ex.Message);
        }
    }
}
