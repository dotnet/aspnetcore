// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace Microsoft.AspNetCore.Certificates.Generation;

internal sealed class UnixCertificateManager : CertificateManager
{
    private List<CertificateTrustPrerequisite> _trustPrerequisites;
    private string _openSSLPath;
    private string _openSSLDir;
    private bool _supportedOpenSSLVersion;
    private string _cRehashPath;
    private string _certUtilPath;
    private string _chromePath;
    private string _edgePath;
    private string _firefoxPath;
    private string _googleChromeAndEdgeNssDbPath;
    private string _firefoxNssDbPath;

    public UnixCertificateManager()
    {
    }

    internal UnixCertificateManager(string subject, int version)
        : base(subject, version)
    {
    }

    public override IList<CertificateTrustPrerequisite> CheckTrustPrerequisites()
    {
        if (_trustPrerequisites != null)
        {
            return _trustPrerequisites;
        }

        _trustPrerequisites = new List<CertificateTrustPrerequisite>();
        _openSSLPath = IsInstalled("openssl");
        if (_openSSLPath == null)
        {
            Log.MissingOpenSsl();
            _trustPrerequisites.Add(new("openssl", true, "'openssl' is not installed. We will not be able to validate that openssl and dotnet trust the certificate."));
        }
        else
        {
            _supportedOpenSSLVersion = IsSupportedOpenSslVersion(out var version);
            if (!_supportedOpenSSLVersion)
            {
                Log.OldOpenSSLVersion(version);
                _trustPrerequisites.Add(new("openssl", true, $"The available version '{version}' of Open SSL is too old. Update to a version of Open SSL 1.1.1k or newer."));
            }
            else
            {
                Log.ValidOpenSSLVersion(version);
            }

            _openSSLDir = GetOpenSSLDirectory();
            if (string.IsNullOrEmpty(_openSSLDir))
            {
                var openSslDirPrereq = new CertificateTrustPrerequisite(
                    "openssl",
                    true,
                    $"Unable to determine the OPENSSLDIR via 'openssl version -d'. Alternatively, provide the directory manually via the 'SSL_CERT_DIR' environment variable.");
                _trustPrerequisites.Add(openSslDirPrereq);
            }
        }
        _cRehashPath = IsInstalled("c_rehash");
        if (_cRehashPath == null)
        {
            _trustPrerequisites.Add(new("c_rehash", true, "'c_rehash' is not installed. We will not be able to make openssl and dotnet trust the certificate."));
        }

        _certUtilPath = IsInstalled("certutil");
        if (_certUtilPath == null)
        {
            _trustPrerequisites.Add(new("certutil", true, "'certutil' is not installed. We will not be able to make Firefox, Edge or Chrome trust the certificate."));
            Log.MissingCertUtil("certutil is not available on the path.");
        }
        else
        {
            Log.FoundCertUtil();
        }

        _chromePath = IsInstalled("google-chrome");
        _edgePath = IsInstalled("microsoft-edge");
        _firefoxPath = IsInstalled("firefox");

        if (_chromePath == null)
        {
            _trustPrerequisites.Add(new("chrome", false, "'Google Chrome' not detected."));
        }

        if (_edgePath == null)
        {
            _trustPrerequisites.Add(new("edge", false, "'Microsoft Edge' not detected."));
        }

        if (_firefoxPath == null)
        {
            _trustPrerequisites.Add(new("firefox", false, "'Mozilla Firefox' not detected."));
        }

        _googleChromeAndEdgeNssDbPath = GetEdgeAndChromeDbDirectory();

        if (_certUtilPath != null && (_chromePath != null || _edgePath != null) && _googleChromeAndEdgeNssDbPath == null)
        {
            _trustPrerequisites.Add(new("edge", true, $"We could not detect the path of the NSS database used by Edge and Chrome. Alternatively, provide the path manually via " +
                $"the 'ASPNETCORE_DEV_CERTS_EDGE_CHROME_NSSDB_PATH' environment variable. " +
                $"Locations searched:" + Environment.NewLine + Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".pki/nssdb")));
        }

        _firefoxNssDbPath = GetFirefoxCertificateDbDirectory();

        if (_certUtilPath != null && _firefoxPath != null && _firefoxNssDbPath == null)
        {
            _trustPrerequisites.Add(new("firefox", true, $"We could not detect the Firefox profile. Alternatively, provide the path manually via " +
                $"the 'ASPNETCORE_DEV_CERTS_FIREFOX_PROFILE_PATH' environment variable. You can locate the Firefox profile by visiting about:profiles in Firefox. " +
                $"Locations searched:" + Environment.NewLine + string.Join($"  {Environment.NewLine}", GetFirefoxProfileLocations())));
        }

        return _trustPrerequisites;

        static IEnumerable<string> GetFirefoxProfileLocations()
        {
            yield return "~/.mozilla/firefox/*.default-release";
            yield return "~/snap/firefox/common/.mozilla/firefox/*.default-release";
            yield return "~/snap/firefox/common/.mozilla/firefox/*.default";
        }
    }

    private static string IsInstalled(string tool)
    {
        using var process = Process.Start(new ProcessStartInfo("which", tool) { RedirectStandardError = true, RedirectStandardOutput = true });
        process.WaitForExit();
        return process.ExitCode == 0 ? process.StandardOutput.ReadToEnd() : null;
    }

    public override bool IsTrusted(X509Certificate2 certificate)
    {
        var trustChecks = CalculateTrustDetails(certificate);
        return trustChecks.IsTrusted();
    }

    private TrustChecks CalculateTrustDetails(X509Certificate2 certificate)
    {
        var tempCertificate = Path.Combine(Path.GetTempPath(), $"aspnetcore-localhost-{certificate.Thumbprint}.crt");
        bool? trustedByOpenSSL = null;
        bool? trustedByFirefox = null;
        bool? trustedByEdgeChrome = null;

        try
        {
            File.WriteAllText(tempCertificate, certificate.ExportCertificatePem());
            if (!_trustPrerequisites.Any(p => p.Tool == "openssl"))
            {
                var program = RunScriptAndCaptureOutput("openssl", $"verify {tempCertificate}");
                trustedByOpenSSL = program.ExitCode == 0;
            }
            else
            {
                Log.UnixNoDotNetToDotNetTrustCheck();
            }
        }
        finally
        {
            if (File.Exists(tempCertificate))
            {
                File.Delete(tempCertificate);
            }
        }

        if (_certUtilPath != null && _firefoxNssDbPath != null)
        {
            trustedByFirefox = IsTrustedInNssDb(_firefoxNssDbPath, certificate);
        }

        if (_certUtilPath != null && _googleChromeAndEdgeNssDbPath != null)
        {
            trustedByEdgeChrome = IsTrustedInNssDb(_googleChromeAndEdgeNssDbPath, certificate);
        }

        return new(trustedByOpenSSL, trustedByFirefox, trustedByEdgeChrome);
    }

    private static string GetEdgeAndChromeDbDirectory()
    {
        var pathFromEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_DEV_CERTS_EDGE_CHROME_NSSDB_PATH");
        if (!string.IsNullOrEmpty(pathFromEnvironment))
        {
            return pathFromEnvironment;
        }

        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".pki/nssdb");
        return Directory.Exists(directory) ? directory : null;
    }

    private static bool IsTrustedInNssDb(string dbPath, X509Certificate2 certificate)
    {
        if (dbPath != null)
        {
            // The fact that the certificate is present in this database is enough to
            // consider it trusted. Otherwise, it would be a bug in our code.
            var (exitCode, _, _) = RunScriptAndCaptureOutput(
                "certutil",
                $"-L -d sql:{dbPath} -n aspnetcore-localhost-{certificate.Thumbprint[0..6]}");

            return exitCode == 0;
        }

        return false;
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

    internal override CheckCertificateStateResult CheckCertificateState(X509Certificate2 candidate, bool interactive)
    {
        // Return true as we don't perform any check.
        return new CheckCertificateStateResult(true, null);
    }

    internal override void CorrectCertificateState(X509Certificate2 candidate)
    {
        // Do nothing since we don't have anything to check here.
    }

    protected override bool IsExportable(X509Certificate2 c) => true;

    protected override void TrustCertificateCore(X509Certificate2 certificate)
    {
        // We already did all the needed checks and this method tells us the information we need to determine if
        // we need to attempt trusting the certificate in any of the components we support.
        var trustDetails = CalculateTrustDetails(certificate);

        var exceptions = new List<Exception>();
        var hasErrors = false;

        var certificateName = $"aspnetcore-localhost-{certificate.Thumbprint}.crt";
        var tempCertificate = Path.Combine(Path.GetTempPath(), certificateName);
        File.WriteAllText(tempCertificate, certificate.ExportCertificatePem());

        if (trustDetails.OpenSSL == false && !_trustPrerequisites.Any(p => p.Tool == "c_rehash"))
        {
            try
            {
                // Copy
                var (copyExitCode, _, copyError) = RunScriptAndCaptureOutput("sudo", $"cp {tempCertificate} {_openSSLDir}");
                if (copyExitCode != 0)
                {
                    Log.UnixCopyCertificateToOpenSSLCertificateStoreError(copyError);
                    hasErrors = true;
                }

                // Rehash
                try
                {
                    var (exitCode, _, rehashError) = RunScriptAndCaptureOutput("sudo", "c_rehash");
                    if (exitCode != 0)
                    {
                        Log.UnixTrustCertificateFromRootStoreOpenSSLRehashFailed(rehashError);
                        hasErrors = true;
                    }
                }
                catch (Exception ex)
                {
                    Log.UnixTrustCertificateFromRootStoreOpenSSLRehashFailed(ex.Message);
                    exceptions.Add(ex);
                }
            }
            catch (Exception ex)
            {
                Log.UnixCopyCertificateToOpenSSLCertificateStoreError(ex.Message);
                exceptions.Add(ex);
            }
        }
        else
        {
            if (trustDetails.OpenSSL == true)
            {
                Log.UnixOpensslCertificateAlreadyTrusted(GetDescription(certificate));
            }
            else
            {
                Log.UnixCannotTrustDotNetToDotNet();
            }
        }

        if (trustDetails.Firefox == false)
        {
            try
            {
                if (!TryTrustCertificateInNssDb(_firefoxNssDbPath, certificate, tempCertificate, out var command, out var errorMessage))
                {
                    Log.UnixTrustCertificateFirefoxRootStoreError($"Failed to run the command '{command}'.{Environment.NewLine}{errorMessage}");
                    hasErrors = true;
                }
            }
            catch (Exception ex)
            {
                Log.UnixTrustCertificateFirefoxRootStoreError(ex.Message);
                exceptions.Add(ex);
            }
        }
        else
        {
            if (trustDetails.Firefox == true)
            {
                Log.UnixFirefoxCertificateAlreadyTrusted(GetDescription(certificate));
            }
            else
            {
                Log.UnixCannotTrustFirefox();
            }
        }

        if (trustDetails.EdgeChrome == false)
        {
            try
            {
                if (!TryTrustCertificateInNssDb(_googleChromeAndEdgeNssDbPath, certificate, tempCertificate, out var command, out var errorMessage))
                {
                    Log.UnixTrustCertificateCommonEdgeChromeRootStoreError($"Failed to run the command '{command}'.{Environment.NewLine}{errorMessage}");
                    hasErrors = true;
                }
            }
            catch (Exception ex)
            {
                Log.UnixTrustCertificateCommonEdgeChromeRootStoreError(ex.Message);
                exceptions.Add(ex);
            }
        }
        else
        {
            if (trustDetails.EdgeChrome == true)
            {
                Log.UnixEdgeChromeCertificateAlreadyTrusted(GetDescription(certificate));
            }
            else
            {
                Log.UnixCannotTrustEdgeChrome();
            }

        }

        if (hasErrors || exceptions.Any())
        {
            if (hasErrors)
            {
                throw new InvalidOperationException("There were some errors trusting the certificate. Use --verbose to get more details.");
            }
            else
            {
                throw exceptions.Count == 1 ? exceptions[0] : new AggregateException("There were some errors trusting the certificate. Use --verbose to get more details.", exceptions);
            }
        }
    }

    private static bool TryTrustCertificateInNssDb(string dbPath, X509Certificate2 certificate, string certificatePath, out string command, out string error)
    {
        command = null;
        error = null;
        if (dbPath != null)
        {
            var result = RunScriptAndCaptureOutput(
                "certutil",
                $"-A -d sql:{dbPath} -t \"C,,\" -n aspnetcore-localhost-{certificate.Thumbprint[0..6]} -i {certificatePath}");
            if (result.ExitCode != 0)
            {
                command = $"certutil -A -d sql:{dbPath} -t \"C,,\" -n aspnetcore-localhost-{certificate.Thumbprint[0..6]} -i {certificatePath}";
                error = result.Output + Environment.NewLine + result.Error;
                return false;
            }
        }

        return true;
    }

    private static string GetFirefoxCertificateDbDirectory()
    {
        var fromEnv = Environment.GetEnvironmentVariable("ASPNETCORE_DEV_CERTS_FIREFOX_PROFILE_PATH");
        if (!string.IsNullOrEmpty(fromEnv))
        {
            return fromEnv;
        }

        return EnumerateIfExistsInUserProfile(".mozilla/firefox/", "*.default-release") ??
                EnumerateIfExistsInUserProfile("snap/firefox/common/.mozilla/firefox/", "*.default-release") ??
                EnumerateIfExistsInUserProfile("snap/firefox/common/.mozilla/firefox/", "*.default");
    }

    private static string EnumerateIfExistsInUserProfile(string subpath, string pattern)
    {
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), subpath);
        if (!Directory.Exists(directory))
        {
            return null;
        }

        return Directory.EnumerateDirectories(directory, pattern).SingleOrDefault();
    }

    private static string GetOpenSSLDirectory()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable("SSL_CERT_DIR");
        if (!string.IsNullOrEmpty(fromEnvironment))
        {
            Log.UnixOpenSSLDirectoryLocatedAt(fromEnvironment);
            return fromEnvironment;
        }

        var (directoryExitCode, openSSLDirectory, directoryError) = RunScriptAndCaptureOutput(
            "openssl",
            "version -d",
            "OPENSSLDIR: \"(?<libpath>.+?)\"",
            "libpath");

        if (directoryExitCode != 0 || string.IsNullOrEmpty(openSSLDirectory))
        {
            Log.UnixFailedToLocateOpenSSLDirectory(directoryError);
            return null;
        }
        else
        {
            Log.UnixOpenSSLDirectoryLocatedAt(openSSLDirectory);
        }

        return Path.Combine(openSSLDirectory, "certs");
    }

    private static bool IsSupportedOpenSslVersion(out string output)
    {
        (var exitCode, output, _) = RunScriptAndCaptureOutput(
            "openssl",
            "version",
            @"OpenSSL (?<version>\d\.\d.\d(\.\d\w)?)",
            "version");

        if (exitCode != 0 || string.IsNullOrEmpty(output))
        {
            return false;
        }

        var version = output.Split('.');
        var major = version[0];
        var letter = version.Length > 3 ? version[3][1] : 'a';
        return int.Parse(major, CultureInfo.InvariantCulture) >= 3 || letter >= 'k';
    }

    private static ProgramOutput RunScriptAndCaptureOutput(string name, string arguments, [StringSyntax("Regex")] string regex = null, string captureName = null)
    {
        var processInfo = new ProcessStartInfo(name, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        using var process = Process.Start(processInfo);
        process.WaitForExit();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        if (process.ExitCode == -1)
        {
            return new(process.ExitCode, null, error);
        }

        if (regex == null || captureName == null)
        {
            return new(process.ExitCode, output, null);
        }

        var versionMatch = Regex.Match(output, regex);
        if (!versionMatch.Success)
        {
            return new(process.ExitCode, null, null);
        }

        return new(process.ExitCode, versionMatch.Groups[captureName].Value, null);
    }

    protected override void RemoveCertificateFromTrustedRoots(X509Certificate2 certificate)
    {
        // We already did all the needed checks and this method tells us the information we need to determine if
        // we need to attempt trusting the certificate in any of the components we support.
        var trustDetails = CalculateTrustDetails(certificate);

        var exceptions = new List<Exception>();
        var hasErrors = false;

        if (trustDetails.OpenSSL == true && !_trustPrerequisites.Any(p => p.Tool == "c_rehash"))
        {
            var installedCertificate = Path.Combine(_openSSLDir, "certs", $"aspnetcore-localhost-{certificate.Thumbprint}.crt");
            try
            {
                Log.UnixRemoveCertificateFromRootStoreStart();
                if (!File.Exists(installedCertificate))
                {
                    Log.UnixRemoveCertificateFromRootStoreNotFound();
                }
                else
                {
                    var rmResult = RunScriptAndCaptureOutput("sudo", $"rm {installedCertificate}");
                    if (rmResult.ExitCode != 0)
                    {
                        Log.UnixRemoveCertificateFromRootStoreFailedtoDeleteFile(installedCertificate, rmResult.Error);
                        hasErrors = true;
                    }
                }

                if (!hasErrors)
                {
                    try
                    {
                        var reHashResult = RunScriptAndCaptureOutput("sudo", "c_rehash");
                        if (reHashResult.ExitCode != 0)
                        {
                            Log.UnixRemoveCertificateFromRootStoreOpenSSLRehashFailed(reHashResult.Error);
                            hasErrors = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.UnixRemoveCertificateFromRootStoreOpenSSLRehashFailed(ex.Message);
                        exceptions.Add(ex);
                    }
                }
                Log.UnixRemoveCertificateFromRootStoreEnd();
            }
            catch (Exception ex)
            {
                Log.UnixRemoveCertificateFromRootStoreFailedtoDeleteFile(installedCertificate, ex.Message);
                exceptions.Add(ex);
            }
        }
        else
        {
            if (trustDetails.OpenSSL == false)
            {
                Log.UnixOpensslCertificateAlreadyUntrusted(GetDescription(certificate));
            }
            else
            {
                Log.UnixCannotUntrustDotNetToDotNet();
            }
        }

        if (trustDetails.Firefox == true)
        {
            try
            {
                if (!TryRemoveCertificateFromNssDb(_firefoxNssDbPath, certificate, out var command, out var error))
                {
                    Log.UnixRemoveCertificateFromFirefoxRootStoreError($"Failed to run the command '{command}'.{Environment.NewLine}{error}");
                    hasErrors = true;
                }
            }
            catch (Exception ex)
            {
                Log.UnixRemoveCertificateFromFirefoxRootStoreError(ex.Message);
                exceptions.Add(ex);
            }
        }
        else
        {
            if (trustDetails.Firefox == false)
            {
                Log.UnixFirefoxCertificateAlreadyUntrusted(GetDescription(certificate));
            }
            else
            {
                Log.UnixCannotUntrustFirefox();
            }
        }

        if (trustDetails.EdgeChrome == false)
        {
            try
            {
                if (!TryRemoveCertificateFromNssDb(_googleChromeAndEdgeNssDbPath, certificate, out var command, out var error))
                {
                    Log.UnixRemoveCertificateFromFirefoxRootStoreError($"Failed to run the command '{command}'.{Environment.NewLine}{error}");
                    hasErrors = true;
                }
            }
            catch (Exception ex)
            {
                Log.UnixRemoveCertificateFromCommonEdgeChromeRootStoreError(ex.Message);
                exceptions.Add(ex);
            }
        }
        else
        {
            if (trustDetails.Firefox == false)
            {
                Log.UnixEdgeChromeCertificateAlreadyUntrusted(GetDescription(certificate));
            }
            else
            {
                Log.UnixCannotUntrustEdgeChrome();
            }
        }

        if (hasErrors || exceptions.Any())
        {
            if (hasErrors)
            {
                throw new InvalidOperationException("There were some errors trusting the certificate. Use --verbose to get more details.");
            }
            else
            {
                throw exceptions.Count == 1 ? exceptions[0] : new AggregateException("There were some removing the certificate trust. Use --verbose to get more details.", exceptions);
            }
        }

        Log.UnixRemoveCertificateFromRootStoreEnd();
    }

    private static bool TryRemoveCertificateFromNssDb(string dbPath, X509Certificate2 certificate, out string command, out string error)
    {
        command = null;
        error = null;
        if (dbPath != null)
        {
            var result = RunScriptAndCaptureOutput(
                "certutil",
                $"-D -d sql:{dbPath} -n aspnetcore-localhost-{certificate.Thumbprint[0..6]}");
            if (result.ExitCode != 0)
            {
                command = "certutil " + $"-D -d sql:{dbPath} -n aspnetcore-localhost-{certificate.Thumbprint[0..6]}";
                error = result.Output + Environment.NewLine + result.Error;
                return false;
            }
        }

        return true;
    }

    protected override IList<X509Certificate2> GetCertificatesToRemove(StoreName storeName, StoreLocation storeLocation)
    {
        return ListCertificates(StoreName.My, StoreLocation.CurrentUser, isValid: false, requireExportable: false);
    }

    private record struct ProgramOutput(int ExitCode, string Output, string Error);

    private record struct TrustChecks(bool? OpenSSL, bool? Firefox, bool? EdgeChrome)
    {
        internal bool IsTrusted() => (OpenSSL ?? true) && (Firefox ?? true) && (EdgeChrome ?? true);
    }
}
