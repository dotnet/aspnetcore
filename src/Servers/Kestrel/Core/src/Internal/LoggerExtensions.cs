// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    internal static class LoggerExtensions
    {
        // Category: DefaultHttpsProvider
        private static readonly Action<ILogger, string, string, Exception?> _locatedDevelopmentCertificate =
            LoggerMessage.Define<string, string>(
                LogLevel.Debug,
                new EventId(0, "LocatedDevelopmentCertificate"),
                "Using development certificate: {certificateSubjectName} (Thumbprint: {certificateThumbprint})");

        private static readonly Action<ILogger, Exception?> _unableToLocateDevelopmentCertificate =
            LoggerMessage.Define(
                LogLevel.Debug,
                new EventId(1, "UnableToLocateDevelopmentCertificate"),
                "Unable to locate an appropriate development https certificate.");

        private static readonly Action<ILogger, string, Exception?> _failedToLocateDevelopmentCertificateFile =
            LoggerMessage.Define<string>(
                LogLevel.Debug,
                new EventId(2, "FailedToLocateDevelopmentCertificateFile"),
                "Failed to locate the development https certificate at '{certificatePath}'.");

        private static readonly Action<ILogger, string, Exception?> _failedToLoadDevelopmentCertificate =
            LoggerMessage.Define<string>(
                LogLevel.Debug,
                new EventId(3, "FailedToLoadDevelopmentCertificate"),
                "Failed to load the development https certificate at '{certificatePath}'.");

        private static readonly Action<ILogger, Exception?> _badDeveloperCertificateState =
            LoggerMessage.Define(
                LogLevel.Error,
                new EventId(4, "BadDeveloperCertificateState"),
                CoreStrings.BadDeveloperCertificateState);

        private static readonly Action<ILogger, string, Exception?> _developerCertificateFirstRun =
            LoggerMessage.Define<string>(
                LogLevel.Warning,
                new EventId(5, "DeveloperCertificateFirstRun"),
                "{Message}");

        private static readonly Action<ILogger, string, Exception?> _failedToLoadCertificate =
            LoggerMessage.Define<string>(
                LogLevel.Error,
                new EventId(6, "MissingOrInvalidCertificateFile"),
                "The certificate file at '{CertificateFilePath}' can not be found, contains malformed data or does not contain a certificate.");

        private static readonly Action<ILogger, string, Exception?> _failedToLoadCertificateKey =
            LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(7, "MissingOrInvalidCertificateKeyFile"),
            "The certificate key file at '{CertificateKeyFilePath}' can not be found, contains malformed data or does not contain a PEM encoded key in PKCS8 format.");

        public static void LocatedDevelopmentCertificate(this ILogger<KestrelServer> logger, X509Certificate2 certificate) => _locatedDevelopmentCertificate(logger, certificate.Subject, certificate.Thumbprint, null);

        public static void UnableToLocateDevelopmentCertificate(this ILogger<KestrelServer> logger) => _unableToLocateDevelopmentCertificate(logger, null);

        public static void FailedToLocateDevelopmentCertificateFile(this ILogger<KestrelServer> logger, string certificatePath) => _failedToLocateDevelopmentCertificateFile(logger, certificatePath, null);

        public static void FailedToLoadDevelopmentCertificate(this ILogger<KestrelServer> logger, string certificatePath) => _failedToLoadDevelopmentCertificate(logger, certificatePath, null);

        public static void BadDeveloperCertificateState(this ILogger<KestrelServer> logger) => _badDeveloperCertificateState(logger, null);

        public static void DeveloperCertificateFirstRun(this ILogger<KestrelServer> logger, string message) => _developerCertificateFirstRun(logger, message, null);

        public static void FailedToLoadCertificate(this ILogger<KestrelServer> logger, string certificatePath) => _failedToLoadCertificate(logger, certificatePath, null);

        public static void FailedToLoadCertificateKey(this ILogger<KestrelServer> logger, string certificateKeyPath) => _failedToLoadCertificateKey(logger, certificateKeyPath, null);
    }
}
