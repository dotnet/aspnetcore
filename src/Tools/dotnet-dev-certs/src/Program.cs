// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Certificates.Generation;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.AspNetCore.DeveloperCertificates.Tools
{
    internal class Program
    {
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

        public static readonly TimeSpan HttpsCertificateValidity = TimeSpan.FromDays(365);

        public static int Main(string[] args)
        {
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
                        "Password to use when exporting the certificate with the private key into a pfx file",
                        CommandOptionType.SingleValue);

                    var check = c.Option(
                        "-c|--check",
                        "Check for the existence of the certificate but do not perform any action",
                        CommandOptionType.NoValue);

                    var clean = c.Option(
                        "--clean",
                        "Cleans all HTTPS development certificates from the machine.",
                        CommandOptionType.NoValue);

                    CommandOption trust = null;
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        trust = c.Option("-t|--trust",
                            "Trust the certificate on the current platform",
                            CommandOptionType.NoValue);
                    }

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
                        if ((clean.HasValue() && (exportPath.HasValue() || password.HasValue() || trust?.HasValue() == true)) ||
                            (check.HasValue() && (exportPath.HasValue() || password.HasValue() || clean.HasValue())))
                        {
                            reporter.Error(@"Incompatible set of flags. Sample usages
'dotnet dev-certs https'
'dotnet dev-certs https --clean'
'dotnet dev-certs https --check --trust'
'dotnet dev-certs https -ep ./certificate.pfx -p password --trust'");
                        }

                        if (check.HasValue())
                        {
                            return CheckHttpsCertificate(trust, reporter);
                        }

                        if (clean.HasValue())
                        {
                            return CleanHttpsCertificates(reporter);
                        }

                        return EnsureHttpsCertificate(exportPath, password, trust, reporter);
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

        private static int CleanHttpsCertificates(IReporter reporter)
        {
            var manager = new CertificateManager();
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

                manager.CleanupHttpsCertificates();
                reporter.Output("HTTPS development certificates successfully removed from the machine.");
                return Success;
            }
            catch(Exception e)
            {
                reporter.Error("There was an error trying to clean HTTPS development certificates on this machine.");
                reporter.Error(e.Message);

                return ErrorCleaningUpCertificates;
            }
        }

        private static int CheckHttpsCertificate(CommandOption trust, IReporter reporter)
        {
            var now = DateTimeOffset.Now;
            var certificateManager = new CertificateManager();
            var certificates = certificateManager.ListCertificates(CertificatePurpose.HTTPS, StoreName.My, StoreLocation.CurrentUser, isValid: true);
            if (certificates.Count == 0)
            {
                reporter.Output("No valid certificate found.");
                return ErrorNoValidCertificateFound;
            }
            else
            {
                reporter.Output("A valid certificate was found.");
            }

            if (trust != null && trust.HasValue())
            {
                var store = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? StoreName.My : StoreName.Root;
                var trustedCertificates = certificateManager.ListCertificates(CertificatePurpose.HTTPS, store, StoreLocation.CurrentUser, isValid: true);
                if (!certificates.Any(c => certificateManager.IsTrusted(c)))
                {
                    reporter.Output($@"The following certificates were found, but none of them is trusted:
{string.Join(Environment.NewLine, certificates.Select(c => $"{c.Subject} - {c.Thumbprint}"))}");
                    return ErrorCertificateNotTrusted;
                }
                else
                {
                    reporter.Output("A trusted certificate was found.");
                }
            }

            return Success;
        }

        private static int EnsureHttpsCertificate(CommandOption exportPath, CommandOption password, CommandOption trust, IReporter reporter)
        {
            var now = DateTimeOffset.Now;
            var manager = new CertificateManager();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && trust?.HasValue() == true)
            {
                reporter.Warn("Trusting the HTTPS development certificate was requested. If the certificate is not " +
                    "already trusted we will run the following command:" + Environment.NewLine +
                    "'sudo security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain <<certificate>>'" +
                    Environment.NewLine + "This command might prompt you for your password to install the certificate " +
                    "on the system keychain.");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && trust?.HasValue() == true)
            {
                reporter.Warn("Trusting the HTTPS development certificate was requested. A confirmation prompt will be displayed " +
                    "if the certificate was not previously trusted. Click yes on the prompt to trust the certificate.");
            }

            var result = manager.EnsureAspNetCoreHttpsDevelopmentCertificate(
                now,
                now.Add(HttpsCertificateValidity),
                exportPath.Value(),
                trust == null ? false : trust.HasValue(),
                password.HasValue(),
                password.Value());

            reporter.Verbose(string.Join(Environment.NewLine, result.Diagnostics.Messages));

            switch (result.ResultCode)
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
                    reporter.Warn("There was an error exporting HTTPS developer certificate to a file.");
                    return ErrorExportingTheCertificate;
                case EnsureCertificateResult.FailedToTrustTheCertificate:
                    reporter.Warn("There was an error trusting HTTPS developer certificate.");
                    return ErrorTrustingTheCertificate;
                case EnsureCertificateResult.UserCancelledTrustStep:
                    reporter.Warn("The user cancelled the trust step.");
                    return ErrorUserCancelledTrustPrompt;
                default:
                    reporter.Error("Something went wrong. The HTTPS developer certificate could not be created.");
                    return CriticalError;
            }
        }
    }
}
