// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace Microsoft.AspNetCore.Certificates.Generation;

/// <remarks>
/// On Unix, we trust the certificate in the following locations:
///   1. dotnet (i.e. the CurrentUser/Root store)
///   2. OpenSSL (i.e. adding it to a directory in $SSL_CERT_DIR)
///   3. Firefox &amp; Chromium (i.e. adding it to an NSS DB for each browser)
/// All of these locations are per-user.
/// </remarks>
internal sealed partial class UnixCertificateManager : CertificateManager
{
	private const UnixFileMode DirectoryPermissions = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;

    /// <summary>The name of an environment variable consumed by OpenSSL to locate certificates.</summary>
    private const string OpenSslCertificateDirectoryVariableName = "SSL_CERT_DIR";

    private const string OpenSslCertDirectoryOverrideVariableName = "DOTNET_DEV_CERTS_OPENSSL_CERTIFICATE_DIRECTORY";
    private const string NssDbOverrideVariableName = "DOTNET_DEV_CERTS_NSSDB_PATHS";
    // CONSIDER: we could have a distinct variable for Mozilla NSS DBs, but detecting them from the path seems sufficient for now.

    private const string BrowserFamilyChromium = "Chromium";
    private const string BrowserFamilyFirefox = "Firefox";

    private const string OpenSslCommand = "openssl";
    private const string CertUtilCommand = "certutil";

    private const int MaxHashCollisions = 10; // Something is going badly wrong if we have this many dev certs with the same hash

    private HashSet<string>? _availableCommands;

    public UnixCertificateManager()
    {
    }

    internal UnixCertificateManager(string subject, int version)
        : base(subject, version)
    {
    }

    public override TrustLevel GetTrustLevel(X509Certificate2 certificate)
    {
        var sawTrustSuccess = false;
        var sawTrustFailure = false;

        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(OpenSslCertDirectoryOverrideVariableName)))
        {
            // Warn but don't bail.
            Log.UnixOpenSslCertificateDirectoryOverrideIgnored(OpenSslCertDirectoryOverrideVariableName);
        }

        // Building the chain will check whether dotnet trusts the cert.  We could, instead,
        // enumerate the Root store and/or look for the file in the OpenSSL directory, but
        // this tests the real-world behavior.
        using var chain = new X509Chain();
        // This is just a heuristic for whether or not we should prompt the user to re-run with `--trust`
        // so we don't need to check revocation (which doesn't really make sense for dev certs anyway)
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        if (chain.Build(certificate))
        {
            sawTrustSuccess = true;
        }
        else
        {
            sawTrustFailure = true;
            Log.UnixNotTrustedByDotnet();
        }

        // Will become the name of the file on disk and the nickname in the NSS DBs
        var certificateNickname = GetCertificateNickname(certificate);

        var sslCertDirString = Environment.GetEnvironmentVariable(OpenSslCertificateDirectoryVariableName);
        if (string.IsNullOrEmpty(sslCertDirString))
        {
            sawTrustFailure = true;
            Log.UnixNotTrustedByOpenSsl(OpenSslCertificateDirectoryVariableName);
        }
        else
        {
            var foundCert = false;
            var sslCertDirs = sslCertDirString.Split(Path.PathSeparator);
            foreach (var sslCertDir in sslCertDirs)
            {
                var certPath = Path.Combine(sslCertDir, certificateNickname + ".pem");
                if (File.Exists(certPath))
                {
                    var candidate = X509CertificateLoader.LoadCertificateFromFile(certPath);
                    if (AreCertificatesEqual(certificate, candidate))
                    {
                        foundCert = true;
                        break;
                    }
                }
            }

            if (foundCert)
            {
                sawTrustSuccess = true;
            }
            else
            {
                sawTrustFailure = true;
                Log.UnixNotTrustedByOpenSsl(OpenSslCertificateDirectoryVariableName);
            }
        }

        var nssDbs = GetNssDbs(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        if (nssDbs.Count > 0)
        {
            if (!IsCommandAvailable(CertUtilCommand))
            {
                // If there are browsers but we don't have certutil, we can't check trust and,
                // in all probability, we can't have previously established it.
                Log.UnixMissingCertUtilCommand(CertUtilCommand);
                sawTrustFailure = true;
            }
            else
            {
                foreach (var nssDb in nssDbs)
                {
                    if (IsCertificateInNssDb(certificateNickname, nssDb))
                    {
                        sawTrustSuccess = true;
                    }
                    else
                    {
                        sawTrustFailure = true;
                        Log.UnixNotTrustedByNss(nssDb.Path, nssDb.IsFirefox ? BrowserFamilyFirefox : BrowserFamilyChromium);
                    }
                }
            }
        }

        // Success & Failure => Partial; Success => Full; Failure => None
        return sawTrustSuccess
            ? sawTrustFailure
                ? TrustLevel.Partial
                : TrustLevel.Full
            : TrustLevel.None;
    }

    protected override X509Certificate2 SaveCertificateCore(X509Certificate2 certificate, StoreName storeName, StoreLocation storeLocation)
    {
        var export = certificate.Export(X509ContentType.Pkcs12, "");
        certificate.Dispose();
        certificate = new X509Certificate2(export, "", X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
        Array.Clear(export, 0, export.Length);

        using (var store = new X509Store(storeName, storeLocation))
        {
            store.Open(OpenFlags.ReadWrite);
            store.Add(certificate);
            store.Close();
        };

        return certificate;
    }

    internal override CheckCertificateStateResult CheckCertificateState(X509Certificate2 candidate)
    {
        // Return true as we don't perform any check.
        // This is about checking storage, not trust.
        return new CheckCertificateStateResult(true, null);
    }

    internal override void CorrectCertificateState(X509Certificate2 candidate)
    {
        // Do nothing since we don't have anything to check here.
        // This is about correcting storage, not trust.
    }

    protected override bool IsExportable(X509Certificate2 c) => true;

    protected override TrustLevel TrustCertificateCore(X509Certificate2 certificate)
    {
        var sawTrustFailure = false;
        var sawTrustSuccess = false;

        using var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
        store.Open(OpenFlags.ReadWrite);

        if (TryFindCertificateInStore(store, certificate, out _))
        {
            sawTrustSuccess = true;
        }
        else
        {
            try
            {
                using var publicCertificate = X509CertificateLoader.LoadCertificate(certificate.Export(X509ContentType.Cert));
                // FriendlyName is Windows-only, so we don't set it here.
                store.Add(publicCertificate);
                Log.UnixDotnetTrustSucceeded();
                sawTrustSuccess = true;
            }
            catch (Exception ex)
            {
                sawTrustFailure = true;
                Log.UnixDotnetTrustException(ex.Message);
            }
        }

        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        // Rather than create a temporary file we'll have to clean up, we prefer to export the dev cert
        // to its final location in the OpenSSL directory.  As a result, any failure up until that point
        // is fatal (i.e. we can't trust the cert in other locations).

        var certDir = GetOpenSslCertificateDirectory(homeDirectory)!; // May not exist

        var nickname = GetCertificateNickname(certificate);
        var certPath = Path.Combine(certDir, nickname) + ".pem";

        var needToExport = true;

        // We do our own check for file collisions since ExportCertificate silently overwrites.
        if (File.Exists(certPath))
        {
            try
            {
                using var existingCert = X509CertificateLoader.LoadCertificateFromFile(certPath);
                if (!AreCertificatesEqual(existingCert, certificate))
                {
                    Log.UnixNotOverwritingCertificate(certPath);
                    return TrustLevel.None;
                }

                needToExport = false; // If the bits are on disk, we don't need to re-export
            }
            catch
            {
                // If we couldn't load the file, then we also can't safely overwite it.
                Log.UnixNotOverwritingCertificate(certPath);
                return TrustLevel.None;
            }
        }

        if (needToExport)
        {
            // Security: we don't need the private key for trust, so we don't export it.
            // Note that this will create directories as needed.  We control `certPath`, so the permissions should be fine.
            ExportCertificate(certificate, certPath, includePrivateKey: false, password: null, CertificateKeyExportFormat.Pem);
        }

        // Once the certificate is on disk, we prefer not to throw - some subsequent trust step might succeed.

        var openSslTrustSucceeded = false;

        var isOpenSslAvailable = IsCommandAvailable(OpenSslCommand);
        if (isOpenSslAvailable)
        {
            if (TryRehashOpenSslCertificates(certDir))
            {
                openSslTrustSucceeded = true;
            }
        }
        else
        {
            Log.UnixMissingOpenSslCommand(OpenSslCommand);
        }

        if (openSslTrustSucceeded)
        {
            Log.UnixOpenSslTrustSucceeded();
            sawTrustSuccess = true;
        }
        else
        {
            // The helpers log their own failure reasons - we just describe the consequences
            Log.UnixOpenSslTrustFailed();
            sawTrustFailure = true;
        }

        var nssDbs = GetNssDbs(homeDirectory);
        if (nssDbs.Count > 0)
        {
            var isCertUtilAvailable = IsCommandAvailable(CertUtilCommand);
            if (!isCertUtilAvailable)
            {
                Log.UnixMissingCertUtilCommand(CertUtilCommand);
                // We'll loop over the nssdbs anyway so they'll be listed
            }

            foreach (var nssDb in nssDbs)
            {
                if (isCertUtilAvailable && TryAddCertificateToNssDb(certPath, nickname, nssDb))
                {
                    if (IsCertificateInNssDb(nickname, nssDb))
                    {
                        Log.UnixNssDbTrustSucceeded(nssDb.Path);
                        sawTrustSuccess = true;
                    }
                    else
                    {
                        // If the dev cert is in the db under a different nickname, adding it will succeed (and probably even cause it to be trusted)
                        // but IsTrusted won't find it.  This is unlikely to happen in practice, so we warn here, rather than hardening IsTrusted.
                        Log.UnixNssDbTrustFailedWithProbableConflict(nssDb.Path, nssDb.IsFirefox ? BrowserFamilyFirefox : BrowserFamilyChromium);
                        sawTrustFailure = true;
                    }
                }
                else
                {
                    Log.UnixNssDbTrustFailed(nssDb.Path, nssDb.IsFirefox ? BrowserFamilyFirefox : BrowserFamilyChromium);
                    sawTrustFailure = true;
                }
            }
        }

        if (sawTrustFailure)
        {
            if (sawTrustSuccess)
            {
                // Untrust throws in this case, but we're more lenient since a partially trusted state may be useful in practice.
                Log.UnixTrustPartiallySucceeded();
            }
            else
            {
                return TrustLevel.None;
            }
        }

        if (openSslTrustSucceeded)
        {
            Debug.Assert(IsCommandAvailable(OpenSslCommand), "How did we trust without the openssl command?");

            var homeDirectoryWithSlash = homeDirectory[^1] == Path.DirectorySeparatorChar
                ? homeDirectory
                : homeDirectory + Path.DirectorySeparatorChar;

            var prettyCertDir = certDir.StartsWith(homeDirectoryWithSlash, StringComparison.Ordinal)
                ? Path.Combine("$HOME", certDir[homeDirectoryWithSlash.Length..])
                : certDir;

            if (TryGetOpenSslDirectory(out var openSslDir))
            {
                Log.UnixSuggestSettingEnvironmentVariable(prettyCertDir, Path.Combine(openSslDir, "certs"), OpenSslCertificateDirectoryVariableName);
            }
            else
            {
                Log.UnixSuggestSettingEnvironmentVariableWithoutExample(prettyCertDir, OpenSslCertificateDirectoryVariableName);
            }
        }

        return sawTrustFailure
            ? TrustLevel.Partial
            : TrustLevel.Full;
    }

    protected override void RemoveCertificateFromTrustedRoots(X509Certificate2 certificate)
    {
        var sawUntrustFailure = false;

        using var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
        store.Open(OpenFlags.ReadWrite);

        if (TryFindCertificateInStore(store, certificate, out var matching))
        {
            try
            {
                store.Remove(matching);
            }
            catch (Exception ex)
            {
                Log.UnixDotnetUntrustException(ex.Message);
                sawUntrustFailure = true;
            }
        }

        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)!;

        // We don't attempt to remove the directory when it's empty - it's a standard location
        // and will almost certainly be used in the future.
        var certDir = GetOpenSslCertificateDirectory(homeDirectory); // May not exist

        var nickname = GetCertificateNickname(certificate);
        var certPath = Path.Combine(certDir, nickname) + ".pem";

        if (File.Exists(certPath))
        {
            var openSslUntrustSucceeded = false;

            if (IsCommandAvailable(OpenSslCommand))
            {
                if (TryDeleteCertificateFile(certPath) && TryRehashOpenSslCertificates(certDir))
                {
                    openSslUntrustSucceeded = true;
                }
            }
            else
            {
                Log.UnixMissingOpenSslCommand(OpenSslCommand);
            }

            if (openSslUntrustSucceeded)
            {
                Log.UnixOpenSslUntrustSucceeded();
            }
            else
            {
                // The helpers log their own failure reasons - we just describe the consequences
                Log.UnixOpenSslUntrustFailed();
                sawUntrustFailure = true;
            }
        }
        else
        {
            Log.UnixOpenSslUntrustSkipped(certPath);
        }

        var nssDbs = GetNssDbs(homeDirectory);
        if (nssDbs.Count > 0)
        {
            var isCertUtilAvailable = IsCommandAvailable(CertUtilCommand);
            if (!isCertUtilAvailable)
            {
                Log.UnixMissingCertUtilCommand(CertUtilCommand);
                // We'll loop over the nssdbs anyway so they'll be listed
            }

            foreach (var nssDb in nssDbs)
            {
                if (isCertUtilAvailable && TryRemoveCertificateFromNssDb(nickname, nssDb))
                {
                    Log.UnixNssDbUntrustSucceeded(nssDb.Path);
                }
                else
                {
                    Log.UnixNssDbUntrustFailed(nssDb.Path);
                    sawUntrustFailure = true;
                }
            }
        }

        if (sawUntrustFailure)
        {
            // It might be nice to include more specific error information in the exception message, but we've logged it anyway.
            throw new InvalidOperationException($@"There was an error removing the certificate with thumbprint '{certificate.Thumbprint}'.");
        }
    }

    protected override IList<X509Certificate2> GetCertificatesToRemove(StoreName storeName, StoreLocation storeLocation)
    {
        return ListCertificates(StoreName.My, StoreLocation.CurrentUser, isValid: false, requireExportable: false);
    }

    protected override void CreateDirectoryWithPermissions(string directoryPath)
    {
#pragma warning disable CA1416 // Validate platform compatibility (not supported on Windows)
        var dirInfo = new DirectoryInfo(directoryPath);
        if (dirInfo.Exists)
        {
            if ((dirInfo.UnixFileMode & ~DirectoryPermissions) != 0)
            {
                Log.DirectoryPermissionsNotSecure(dirInfo.FullName);
            }
        }
        else
        {
            Directory.CreateDirectory(directoryPath, DirectoryPermissions);
        }
#pragma warning restore CA1416 // Validate platform compatibility
    }

    private static string GetChromiumNssDb(string homeDirectory)
    {
        return Path.Combine(homeDirectory, ".pki", "nssdb");
    }

    private static string GetChromiumSnapNssDb(string homeDirectory)
    {
        return Path.Combine(homeDirectory, "snap", "chromium", "current", ".pki", "nssdb");
    }

    private static string GetFirefoxDirectory(string homeDirectory)
    {
        return Path.Combine(homeDirectory, ".mozilla", "firefox");
    }

    private static string GetFirefoxSnapDirectory(string homeDirectory)
    {
        return Path.Combine(homeDirectory, "snap", "firefox", "common", ".mozilla", "firefox");
    }

    private bool IsCommandAvailable(string command)
    {
        _availableCommands ??= FindAvailableCommands();
        return _availableCommands.Contains(command);
    }

    private static HashSet<string> FindAvailableCommands()
    {
        var availableCommands = new HashSet<string>();

        // We need OpenSSL 1.1.1h or newer (to pick up https://github.com/openssl/openssl/pull/12357),
        // but, given that all of v1 is EOL, it doesn't seem worthwhile to check the version.
        var commands = new[] { OpenSslCommand, CertUtilCommand };

        var searchPath = Environment.GetEnvironmentVariable("PATH");

        if (searchPath is null)
        {
            return availableCommands;
        }

        var searchFolders = searchPath.Split(Path.PathSeparator);

        foreach (var searchFolder in searchFolders)
        {
            foreach (var command in commands)
            {
                if (!availableCommands.Contains(command))
                {
                    try
                    {
                        if (File.Exists(Path.Combine(searchFolder, command)))
                        {
                            availableCommands.Add(command);
                        }
                    }
                    catch
                    {
                        // It's not interesting to report (e.g.) permission errors here.
                    }
                }
            }

            // Stop early if we've found all the required commands.
            // They're usually all in the same folder (/bin or /usr/bin).
            if (availableCommands.Count == commands.Length)
            {
                break;
            }
        }

        return availableCommands;
    }

    private static string GetCertificateNickname(X509Certificate2 certificate)
    {
        return $"aspnetcore-localhost-{certificate.Thumbprint}";
    }

    /// <remarks>
    /// It is the caller's responsibility to ensure that <see cref="CertUtilCommand"/> is available.
    /// </remarks>
    private static bool IsCertificateInNssDb(string nickname, NssDb nssDb)
    {
        // -V will validate that a cert can be used for a given purpose, in this case, server verification.
        // There is no corresponding -V check for the "Trusted CA" status required by Firefox, so we just check for existence.
        // (The docs suggest that "-V -u A" should do this, but it seems to accept all certs.)
        var operation = nssDb.IsFirefox ? "-L" : "-V -u V";

        var startInfo = new ProcessStartInfo(CertUtilCommand, $"-d sql:{nssDb.Path} -n {nickname} {operation}")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        try
        {
            using var process = Process.Start(startInfo)!;
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            Log.UnixNssDbCheckException(nssDb.Path, ex.Message);
            // This method is used to determine whether more trust is needed, so it's better to underestimate the amount of trust.
            return false;
        }
    }

    /// <remarks>
    /// It is the caller's responsibility to ensure that <see cref="CertUtilCommand"/> is available.
    /// </remarks>
    private static bool TryAddCertificateToNssDb(string certificatePath, string nickname, NssDb nssDb)
    {
        // Firefox doesn't seem to respected the more correct "trusted peer" (P) usage, so we use "trusted CA" (C) instead.
        var usage = nssDb.IsFirefox ? "C" : "P";

        // This silently clobbers an existing entry, so there's no need to check for existence first.
        var startInfo = new ProcessStartInfo(CertUtilCommand, $"-d sql:{nssDb.Path} -n {nickname} -A -i {certificatePath} -t \"{usage},,\"")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        try
        {
            using var process = Process.Start(startInfo)!;
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            Log.UnixNssDbAdditionException(nssDb.Path, ex.Message);
            return false;
        }
    }

    /// <remarks>
    /// It is the caller's responsibility to ensure that <see cref="CertUtilCommand"/> is available.
    /// </remarks>
    private static bool TryRemoveCertificateFromNssDb(string nickname, NssDb nssDb)
    {
        var startInfo = new ProcessStartInfo(CertUtilCommand, $"-d sql:{nssDb.Path} -D -n {nickname}")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        try
        {
            using var process = Process.Start(startInfo)!;
            process.WaitForExit();
            if (process.ExitCode == 0)
            {
                return true;
            }

            // Maybe it wasn't in there because the overrides have change or trust only partially succeeded.
            return !IsCertificateInNssDb(nickname, nssDb);
        }
        catch (Exception ex)
        {
            Log.UnixNssDbRemovalException(nssDb.Path, ex.Message);
            return false;
        }
    }

    private static IEnumerable<string> GetFirefoxProfiles(string firefoxDirectory)
    {
        try
        {
            var profiles = Directory.GetDirectories(firefoxDirectory, "*.default", SearchOption.TopDirectoryOnly).Concat(
                Directory.GetDirectories(firefoxDirectory, "*.default-*", SearchOption.TopDirectoryOnly)); // There can be one of these for each release channel
            if (!profiles.Any())
            {
                // This is noteworthy, given that we're in a firefox directory.
                Log.UnixNoFirefoxProfilesFound(firefoxDirectory);
            }
            return profiles;
        }
        catch (Exception ex)
        {
            Log.UnixFirefoxProfileEnumerationException(firefoxDirectory, ex.Message);
            return [];
        }
    }

    private static string GetOpenSslCertificateDirectory(string homeDirectory)
    {
        var @override = Environment.GetEnvironmentVariable(OpenSslCertDirectoryOverrideVariableName);
        if (!string.IsNullOrEmpty(@override))
        {
            Log.UnixOpenSslCertificateDirectoryOverridePresent(OpenSslCertDirectoryOverrideVariableName);
            return @override;
        }

        return Path.Combine(homeDirectory, ".aspnet", "dev-certs", "trust");
    }

    private static bool TryDeleteCertificateFile(string certPath)
    {
        try
        {
            File.Delete(certPath);
            return true;
        }
        catch (Exception ex)
        {
            Log.UnixCertificateFileDeletionException(certPath, ex.Message);
            return false;
        }
    }

    private static bool TryGetNssDbOverrides(out IReadOnlyList<string> overrides)
    {
        var nssDbOverride = Environment.GetEnvironmentVariable(NssDbOverrideVariableName);
        if (string.IsNullOrEmpty(nssDbOverride))
        {
            overrides = [];
            return false;
        }

        // Normally, we'd let the caller log this, since it's not really an exceptional condition,
        // but it's not worth duplicating the code and the work.
        Log.UnixNssDbOverridePresent(NssDbOverrideVariableName);

        var nssDbs = new List<string>();

        var paths = nssDbOverride.Split(Path.PathSeparator); // May be empty - the user may not want to add browser trust
        foreach (var path in paths)
        {
            var nssDb = Path.GetFullPath(path);
            if (!Directory.Exists(nssDb))
            {
                Log.UnixNssDbDoesNotExist(nssDb, NssDbOverrideVariableName);
                continue;
            }
            nssDbs.Add(nssDb);
        }

        overrides = nssDbs;
        return true;
    }

    private static List<NssDb> GetNssDbs(string homeDirectory)
    {
        var nssDbs = new List<NssDb>();

        if (TryGetNssDbOverrides(out var nssDbOverrides))
        {
            foreach (var nssDb in nssDbOverrides)
            {
                // Our Firefox approach is a hack, so we'd rather under-recognize it than over-recognize it.
                var isFirefox = nssDb.Contains("/.mozilla/firefox/", StringComparison.Ordinal);
                nssDbs.Add(new NssDb(nssDb, isFirefox));
            }

            return nssDbs;
        }

        if (!Directory.Exists(homeDirectory))
        {
            Log.UnixHomeDirectoryDoesNotExist(homeDirectory, Environment.UserName);
            return nssDbs;
        }

        // Chrome, Chromium, and Edge all use this directory
        var chromiumNssDb = GetChromiumNssDb(homeDirectory);
        if (Directory.Exists(chromiumNssDb))
        {
            nssDbs.Add(new NssDb(chromiumNssDb, isFirefox: false));
        }

        // Chromium Snap, when launched under snap confinement, uses this directory
        // (On Ubuntu, the GUI launcher uses confinement, but the terminal does not)
        var chromiumSnapNssDb = GetChromiumSnapNssDb(homeDirectory);
        if (Directory.Exists(chromiumSnapNssDb))
        {
            nssDbs.Add(new NssDb(chromiumSnapNssDb, isFirefox: false));
        }

        var firefoxDir = GetFirefoxDirectory(homeDirectory);
        if (Directory.Exists(firefoxDir))
        {
            var profileDirs = GetFirefoxProfiles(firefoxDir);
            foreach (var profileDir in profileDirs)
            {
                nssDbs.Add(new NssDb(profileDir, isFirefox: true));
            }
        }

        var firefoxSnapDir = GetFirefoxSnapDirectory(homeDirectory);
        if (Directory.Exists(firefoxSnapDir))
        {
            var profileDirs = GetFirefoxProfiles(firefoxSnapDir);
            foreach (var profileDir in profileDirs)
            {
                nssDbs.Add(new NssDb(profileDir, isFirefox: true));
            }
        }

        return nssDbs;
    }

    [GeneratedRegex("OPENSSLDIR:\\s*\"([^\"]+)\"")]
    private static partial Regex OpenSslVersionRegex { get; }

    /// <remarks>
    /// It is the caller's responsibility to ensure that <see cref="OpenSslCommand"/> is available.
    /// </remarks>
    private static bool TryGetOpenSslDirectory([NotNullWhen(true)] out string? openSslDir)
    {
        openSslDir = null;

        try
        {
            var processInfo = new ProcessStartInfo(OpenSslCommand, $"version -d")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(processInfo);
            var stdout = process!.StandardOutput.ReadToEnd();

            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                Log.UnixOpenSslVersionFailed();
                return false;
            }

            var match = OpenSslVersionRegex.Match(stdout);
            if (!match.Success)
            {
                Log.UnixOpenSslVersionParsingFailed();
                return false;
            }

            openSslDir = match.Groups[1].Value;
            return true;
        }
        catch (Exception ex)
        {
            Log.UnixOpenSslVersionException(ex.Message);
            return false;
        }
    }

    /// <remarks>
    /// It is the caller's responsibility to ensure that <see cref="OpenSslCommand"/> is available.
    /// </remarks>
    private static bool TryGetOpenSslHash(string certificatePath, [NotNullWhen(true)] out string? hash)
    {
        hash = null;

        try
        {
            // c_rehash actually does this twice: once with -subject_hash (equivalent to -hash) and again
            // with -subject_hash_old.  Old hashes are only  needed for pre-1.0.0, so we skip that.
            var processInfo = new ProcessStartInfo(OpenSslCommand, $"x509 -hash -noout -in {certificatePath}")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(processInfo);
            var stdout = process!.StandardOutput.ReadToEnd();

            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                Log.UnixOpenSslHashFailed(certificatePath);
                return false;
            }

            hash = stdout.Trim();
            return true;
        }
        catch (Exception ex)
        {
            Log.UnixOpenSslHashException(certificatePath, ex.Message);
            return false;
        }
    }

    [GeneratedRegex("^[0-9a-f]+\\.[0-9]+$")]
    private static partial Regex OpenSslHashFilenameRegex { get; }

    /// <remarks>
    /// We only ever use .pem, but someone will eventually put their own cert in this directory,
    /// so we should handle the same extensions as c_rehash (other than .crl).
    /// </remarks>
    [GeneratedRegex("\\.(pem|crt|cer)$")]
    private static partial Regex OpenSslCertificateExtensionRegex { get; }

    /// <remarks>
    /// This is a simplified version of c_rehash from OpenSSL.  Using the real one would require
    /// installing the OpenSSL perl tools and perl itself, which might be annoying in a container.
    /// </remarks>
    private static bool TryRehashOpenSslCertificates(string certificateDirectory)
    {
        try
        {
            // First, delete all the existing symlinks, so we don't have to worry about fragmentation or leaks.
            var certs = new List<FileInfo>();

            var dirInfo = new DirectoryInfo(certificateDirectory);
            foreach (var file in dirInfo.EnumerateFiles())
            {
                var isSymlink = (file.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
                if (isSymlink && OpenSslHashFilenameRegex.IsMatch(file.Name))
                {
                    file.Delete();
                }
                else if (OpenSslCertificateExtensionRegex.IsMatch(file.Name))
                {
                    certs.Add(file);
                }
            }

            // Then, enumerate all certificates - there will usually be zero or one.

            // c_rehash doesn't create additional symlinks for certs with the same fingerprint,
            // but we don't expect this to happen, so we favor slightly slower look-ups when it
            // does, rather than slightly slower rehashing when it doesn't.

            foreach (var cert in certs)
            {
                if (!TryGetOpenSslHash(cert.FullName, out var hash))
                {
                    return false;
                }

                var linkCreated = false;
                for (var i = 0; i < MaxHashCollisions; i++)
                {
                    var linkPath = Path.Combine(certificateDirectory, $"{hash}.{i}");
                    if (!File.Exists(linkPath))
                    {
                        // As in c_rehash, we link using a relative path.
                        File.CreateSymbolicLink(linkPath, cert.Name);
                        linkCreated = true;
                        break;
                    }
                }

                if (!linkCreated)
                {
                    Log.UnixOpenSslRehashTooManyHashes(cert.FullName, hash, MaxHashCollisions);
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            Log.UnixOpenSslRehashException(ex.Message);
            return false;
        }

        return true;
    }

    private sealed class NssDb(string path, bool isFirefox)
    {
        public string Path => path;
        public bool IsFirefox => isFirefox;
    }
}
