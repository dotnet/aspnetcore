// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Certificates.Generation;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.AspNetCore.DeveloperCertificates.Tools;

internal sealed class Program
{
    // NOTE: Exercise caution when touching these exit codes, since existing tooling
    // might depend on some of these values.
    private const int CriticalError = -1;
    private const int Success = 0;
    private const int ErrorCreatingTheCertificate = 1;
    private const int ErrorSavingTheCertificate = 2;
    private const int ErrorExportingTheCertificate = 3;
    private const int ErrorTrustingTheCertificate = 4;
    private const int ErrorUserCancelledTrustPrompt = 5;
    private const int ErrorNoValidCertificateFound = 6;
    private const int ErrorCertificateNotTrusted = 7;
    private const int ErrorCleaningUpCertificates = 8;
    private const int InvalidCertificateState = 9;
    private const int InvalidKeyExportFormat = 10;
    private const int ErrorImportingCertificate = 11;
    private const int MissingCertificateFile = 12;
    private const int FailedToLoadCertificate = 13;
    private const int NoDevelopmentHttpsCertificate = 14;
    private const int ExistingCertificatesPresent = 15;

    private const string InvalidUsageErrorMessage = @"Incompatible set of flags. Sample usages
'dotnet dev-certs https'
'dotnet dev-certs https --clean'
'dotnet dev-certs https --clean --import ./certificate.pfx -p password'
'dotnet dev-certs https --check --trust'
'dotnet dev-certs https -ep ./certificate.pfx -p password --trust'
'dotnet dev-certs https -ep ./certificate.crt --trust --format Pem'
'dotnet dev-certs https -ep ./certificate.crt -p password --trust --format Pem'";

    public static readonly TimeSpan HttpsCertificateValidity = TimeSpan.FromDays(365);

    public static int Main(string[] args)
    {
        if (args.Contains("--debug"))
        {
            Console.WriteLine("Press any key to continue...");
            _ = Console.ReadKey();
            var newArgs = new List<string>(args);
            newArgs.Remove("--debug");
            args = newArgs.ToArray();
        }

        try
        {
            var app = new CommandLineApplication
            {
                Name = "dotnet dev-certs"
            };

            app.Command("https", c =>
            {
                var exportPath = c.Option("-ep|--export-path",
                    "Full path to the exported certificate",
                    CommandOptionType.SingleValue);

                var password = c.Option("-p|--password",
                    "Password to use when exporting the certificate with the private key into a pfx file or to encrypt the Pem exported key",
                    CommandOptionType.SingleValue);

                // We want to force generating a key without a password to not be an accident.
                var noPassword = c.Option("-np|--no-password",
                    "Explicitly request that you don't use a password for the key when exporting a certificate to a PEM format",
                    CommandOptionType.NoValue);

                var check = c.Option(
                    "-c|--check",
                    "Check for the existence of the certificate but do not perform any action",
                    CommandOptionType.NoValue);

                var clean = c.Option(
                    "--clean",
                    "Cleans all HTTPS development certificates from the machine.",
                    CommandOptionType.NoValue);

                var import = c.Option(
                    "-i|--import",
                    "Imports the provided HTTPS development certificate into the machine. All other HTTPS developer certificates will be cleared out",
                    CommandOptionType.SingleValue);

                var format = c.Option(
                    "--format",
                    "Export the certificate in the given format. Valid values are Pfx and Pem. Pfx is the default.",
                    CommandOptionType.SingleValue);

                CommandOption trust = null;
                trust = c.Option("-t|--trust",
                    "Trust the certificate on the current platform. When combined with the --check option, validates that the certificate is trusted.",
                    CommandOptionType.NoValue);

                var verbose = c.Option("-v|--verbose",
                    "Display more debug information.",
                    CommandOptionType.NoValue);

                var quiet = c.Option("-q|--quiet",
                    "Display warnings and errors only.",
                    CommandOptionType.NoValue);

                c.HelpOption("-h|--help");

                c.OnExecute(() =>
                {
                    var reporter = new ConsoleReporter(PhysicalConsole.Singleton, verbose.HasValue(), quiet.HasValue());

                    if (verbose.HasValue())
                    {
                        var listener = new ReporterEventListener(reporter);
                        listener.EnableEvents(CertificateManager.Log, System.Diagnostics.Tracing.EventLevel.Verbose);
                    }

                    if (clean.HasValue())
                    {
                        if (exportPath.HasValue() || trust?.HasValue() == true || format.HasValue() || noPassword.HasValue() || check.HasValue() ||
                           (!import.HasValue() && password.HasValue()) ||
                           (import.HasValue() && !password.HasValue()))
                        {
                            reporter.Error(InvalidUsageErrorMessage);
                            return CriticalError;
                        }
                    }

                    if (check.HasValue())
                    {
                        if (exportPath.HasValue() || password.HasValue() || noPassword.HasValue() || clean.HasValue() || format.HasValue() || import.HasValue())
                        {
                            reporter.Error(InvalidUsageErrorMessage);
                            return CriticalError;
                        }
                    }

                    if (!clean.HasValue() && !check.HasValue())
                    {
                        if (password.HasValue() && noPassword.HasValue())
                        {
                            reporter.Error(InvalidUsageErrorMessage);
                            return CriticalError;
                        }

                        if (noPassword.HasValue() && !(format.HasValue() && string.Equals(format.Value(), "PEM", StringComparison.OrdinalIgnoreCase)))
                        {
                            reporter.Error(InvalidUsageErrorMessage);
                            return CriticalError;
                        }

                        if (import.HasValue())
                        {
                            reporter.Error(InvalidUsageErrorMessage);
                            return CriticalError;
                        }
                    }

                    if (check.HasValue())
                    {
                        return CheckHttpsCertificate(trust, verbose, reporter);
                    }

                    if (clean.HasValue())
                    {
                        var cleanResult = CleanHttpsCertificates(reporter);
                        if (cleanResult != Success || !import.HasValue())
                        {
                            return cleanResult;
                        }

                        return ImportCertificate(import, password, reporter);
                    }

                    return EnsureHttpsCertificate(exportPath, password, noPassword, trust, format, reporter);
                });
            });

            app.HelpOption("-h|--help");

            app.OnExecute(() =>
            {
                app.ShowHelp();
                return Success;
            });

            return app.Execute(args);
        }
        catch
        {
            return CriticalError;
        }
    }

    private static int ImportCertificate(CommandOption import, CommandOption password, ConsoleReporter reporter)
    {
        var manager = CertificateManager.Instance;
        try
        {
            var result = manager.ImportCertificate(import.Value(), password.Value());
            switch (result)
            {
                case ImportCertificateResult.Succeeded:
                    reporter.Output("The certificate was successfully imported.");
                    break;
                case ImportCertificateResult.CertificateFileMissing:
                    reporter.Error($"The certificate file '{import.Value()}' does not exist.");
                    return MissingCertificateFile;
                case ImportCertificateResult.InvalidCertificate:
                    reporter.Error($"The provided certificate file '{import.Value()}' is not a valid PFX file or the password is incorrect.");
                    return FailedToLoadCertificate;
                case ImportCertificateResult.NoDevelopmentHttpsCertificate:
                    reporter.Error($"The certificate at '{import.Value()}' is not a valid ASP.NET Core HTTPS development certificate.");
                    return NoDevelopmentHttpsCertificate;
                case ImportCertificateResult.ExistingCertificatesPresent:
                    reporter.Error($"There are one or more ASP.NET Core HTTPS development certificates present in the environment. Remove them before importing the given certificate.");
                    return ExistingCertificatesPresent;
                case ImportCertificateResult.ErrorSavingTheCertificateIntoTheCurrentUserPersonalStore:
                    reporter.Error("There was an error saving the HTTPS developer certificate to the current user personal certificate store.");
                    return ErrorSavingTheCertificate;
                default:
                    break;
            }
        }
        catch (Exception exception)
        {
            reporter.Error($"An unexpected error occurred: {exception}");
            return ErrorImportingCertificate;
        }

        return Success;
    }

    private static int CleanHttpsCertificates(IReporter reporter)
    {
        var manager = CertificateManager.Instance;
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                reporter.Output("Cleaning HTTPS development certificates from the machine. A prompt might get " +
                    "displayed to confirm the removal of some of the certificates.");
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                reporter.Output("Cleaning HTTPS development certificates from the machine. This operation might " +
                    "require elevated privileges. If that is the case, a prompt for credentials will be displayed.");
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                reporter.Output("Cleaning HTTPS development certificates from the machine. You may wish to update the " +
                    "SSL_CERT_DIR environment variable. " +
                    "See https://aka.ms/dev-certs-trust for more information.");
            }

            manager.CleanupHttpsCertificates();
            reporter.Output("HTTPS development certificates successfully removed from the machine.");
            return Success;
        }
        catch (Exception e)
        {
            reporter.Error("There was an error trying to clean HTTPS development certificates on this machine.");
            reporter.Error(e.Message);

            return ErrorCleaningUpCertificates;
        }
    }

    private static int CheckHttpsCertificate(CommandOption trust, CommandOption verbose, IReporter reporter)
    {
        var certificateManager = CertificateManager.Instance;
        var certificates = certificateManager.ListCertificates(StoreName.My, StoreLocation.CurrentUser, isValid: true);
        var validCertificates = new List<X509Certificate2>();
        if (certificates.Count == 0)
        {
            reporter.Output("No valid certificate found.");
            return ErrorNoValidCertificateFound;
        }
        else
        {
            foreach (var certificate in certificates)
            {
                // We never want check to require interaction.
                // When IDEs run dotnet dev-certs https after calling --check, we will try to access the key and
                // that will trigger a prompt if necessary.
                var status = certificateManager.CheckCertificateState(certificate);
                if (!status.Success)
                {
                    reporter.Warn(status.FailureMessage);
                    return InvalidCertificateState;
                }
                validCertificates.Add(certificate);
            }
        }

        if (trust != null && trust.HasValue())
        {
            var trustedCertificates = certificates.Where(cert => certificateManager.GetTrustLevel(cert) == CertificateManager.TrustLevel.Full).ToList();
            if (trustedCertificates.Count == 0)
            {
                reporter.Output($@"The following certificates were found, but none of them is trusted: {CertificateManager.ToCertificateDescription(certificates)}");
                if (verbose == null || !verbose.HasValue())
                {
                    reporter.Output($@"Run the command with --verbose for more details.");
                }
                return ErrorCertificateNotTrusted;
            }
            else
            {
                ReportCertificates(reporter, trustedCertificates, "trusted");
            }
        }
        else
        {
            ReportCertificates(reporter, validCertificates, "valid");
            reporter.Output("Run the command with both --check and --trust options to ensure that the certificate is not only valid but also trusted.");
        }

        return Success;
    }

    private static void ReportCertificates(IReporter reporter, IReadOnlyList<X509Certificate2> certificates, string certificateState)
    {
        reporter.Output(certificates.Count switch
        {
            1 => $"A {certificateState} certificate was found: {CertificateManager.GetDescription(certificates[0])}",
            _ => $"{certificates.Count} {certificateState} certificates were found: {CertificateManager.ToCertificateDescription(certificates)}"
        });
    }

    private static int EnsureHttpsCertificate(CommandOption exportPath, CommandOption password, CommandOption noPassword, CommandOption trust, CommandOption exportFormat, IReporter reporter)
    {
        var now = DateTimeOffset.Now;
        var manager = CertificateManager.Instance;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var certificates = manager.ListCertificates(StoreName.My, StoreLocation.CurrentUser, isValid: true, exportPath.HasValue());
            foreach (var certificate in certificates)
            {
                var status = manager.CheckCertificateState(certificate);
                if (!status.Success)
                {
                    reporter.Warn("One or more certificates might be in an invalid state. We will try to access the certificate key " +
                        "for each certificate and as a result you might be prompted one or more times to enter " +
                        "your password to access the user keychain. " +
                        "When that happens, select 'Always Allow' to grant 'dotnet' access to the certificate key in the future.");
                }

                break;
            }
        }

        var isTrustOptionSet = trust?.HasValue() == true;

        if (isTrustOptionSet)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                reporter.Warn("Trusting the HTTPS development certificate was requested. If the certificate is not " +
                    "already trusted we will run the following command:" + Environment.NewLine +
                    "'security add-trusted-cert -p basic -p ssl -k <<login-keychain>> <<certificate>>'" +
                    Environment.NewLine + "This command might prompt you for your password to install the certificate " +
                    "on the keychain. To undo these changes: 'security remove-trusted-cert <<certificate>>'" + Environment.NewLine);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                reporter.Warn("Trusting the HTTPS development certificate was requested. A confirmation prompt will be displayed " +
                    "if the certificate was not previously trusted. Click yes on the prompt to trust the certificate.");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                reporter.Warn("Trusting the HTTPS development certificate was requested. " +
                    "Trust is per-user and may require additional configuration. " +
                    "See https://aka.ms/dev-certs-trust for more information.");
            }
        }

        var format = CertificateKeyExportFormat.Pfx;
        if (exportFormat.HasValue() && !Enum.TryParse(exportFormat.Value(), ignoreCase: true, out format))
        {
            reporter.Error($"Unknown key format '{exportFormat.Value()}'.");
            return InvalidKeyExportFormat;
        }

        var result = manager.EnsureAspNetCoreHttpsDevelopmentCertificate(
            now,
            now.Add(HttpsCertificateValidity),
            exportPath.Value(),
            isTrustOptionSet,
            password.HasValue() || (noPassword.HasValue() && format == CertificateKeyExportFormat.Pem),
            password.Value(),
            exportFormat.HasValue() ? format : CertificateKeyExportFormat.Pfx);

        switch (result)
        {
            case EnsureCertificateResult.Succeeded:
                reporter.Output("The HTTPS developer certificate was generated successfully.");
                if (exportPath.Value() != null)
                {
                    reporter.Verbose($"The certificate was exported to {Path.GetFullPath(exportPath.Value())}");
                }
                return Success;
            case EnsureCertificateResult.ValidCertificatePresent:
                reporter.Output("A valid HTTPS certificate is already present.");
                if (exportPath.Value() != null)
                {
                    reporter.Verbose($"The certificate was exported to {Path.GetFullPath(exportPath.Value())}");
                }
                return Success;
            case EnsureCertificateResult.ErrorCreatingTheCertificate:
                reporter.Error("There was an error creating the HTTPS developer certificate.");
                return ErrorCreatingTheCertificate;
            case EnsureCertificateResult.ErrorSavingTheCertificateIntoTheCurrentUserPersonalStore:
                reporter.Error("There was an error saving the HTTPS developer certificate to the current user personal certificate store.");
                return ErrorSavingTheCertificate;
            case EnsureCertificateResult.ErrorExportingTheCertificate:
                reporter.Warn("There was an error exporting the HTTPS developer certificate to a file.");
                return ErrorExportingTheCertificate;
            case EnsureCertificateResult.ErrorExportingTheCertificateToNonExistentDirectory:
                // A distinct warning is useful, but a distinct error code is probably not.
                reporter.Warn("There was an error exporting the HTTPS developer certificate to a file. Please create the target directory before exporting. Choose permissions carefully when creating it.");
                return ErrorExportingTheCertificate;
            case EnsureCertificateResult.PartiallyFailedToTrustTheCertificate:
                // A distinct warning is useful, but a distinct error code is probably not.
                reporter.Warn("There was an error trusting the HTTPS developer certificate. It will be trusted by some clients but not by others.");
                return ErrorTrustingTheCertificate;
            case EnsureCertificateResult.FailedToTrustTheCertificate:
                reporter.Warn("There was an error trusting the HTTPS developer certificate.");
                return ErrorTrustingTheCertificate;
            case EnsureCertificateResult.UserCancelledTrustStep:
                reporter.Warn("The user cancelled the trust step.");
                return ErrorUserCancelledTrustPrompt;
            case EnsureCertificateResult.ExistingHttpsCertificateTrusted:
                reporter.Output("Successfully trusted the existing HTTPS certificate.");
                return Success;
            case EnsureCertificateResult.NewHttpsCertificateTrusted:
                reporter.Output("Successfully created and trusted a new HTTPS certificate.");
                return Success;
            default:
                reporter.Error("Something went wrong. The HTTPS developer certificate could not be created.");
                return CriticalError;
        }
    }
}
