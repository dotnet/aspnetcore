// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Certificates.Generation;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.AspNetCore.DeveloperCertificates.Tools
{
    class Program
    {
        private const int CriticalError = -1;
        private const int Success = 0;
        private const int ErrorCreatingTheCertificate = 1;
        private const int ErrorSavingTheCertificate = 2;
        private const int ErrorExportingTheCertificate = 3;
        private const int ErrorTrustingTheCertificate = 4;

        public static readonly TimeSpan HttpsCertificateValidity = TimeSpan.FromDays(365);

        public static int Main(string[] args)
        {
            try
            {
                var app = new CommandLineApplication
                {
                    Name = "dotnet-developercertificates"
                };

                app.Command("https", c =>
                {
                    var exportPath = c.Option("-ep|--export-path",
                        "Full path to the exported certificate",
                        CommandOptionType.SingleValue);

                    var password = c.Option("-p|--password",
                        "Password to use when exporting the certificate with the private key into a pfx file",
                        CommandOptionType.SingleValue);

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

            var result = manager.EnsureAspNetCoreHttpsDevelopmentCertificate(
                now,
                now.Add(HttpsCertificateValidity),
                exportPath.Value(),
                trust == null ? false : trust.HasValue(),
                password.HasValue(),
                password.Value());

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
                    reporter.Warn("There was an error exporting HTTPS developer certificate to a file.");
                    return ErrorExportingTheCertificate;
                case EnsureCertificateResult.FailedToTrustTheCertificate:
                    reporter.Warn("There was an error trusting HTTPS developer certificate.");
                    return ErrorTrustingTheCertificate;
                default:
                    reporter.Error("Something went wrong. The HTTPS developer certificate could not be created.");
                    return CriticalError;
            }
        }
    }
}
