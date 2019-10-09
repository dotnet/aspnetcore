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
        private static readonly Action<ILogger, string, string, Exception> _locatedDevelopmentCertificate =
            LoggerMessage.Define<string, string>(
                LogLevel.Debug,
                new EventId(0, "LocatedDevelopmentCertificate"),
                "Using development certificate: {certificateSubjectName} (Thumbprint: {certificateThumbprint})");

        private static readonly Action<ILogger, Exception> _unableToLocateDevelopmentCertificate =
            LoggerMessage.Define(
                LogLevel.Debug,
                new EventId(1, "UnableToLocateDevelopmentCertificate"),
                "Unable to locate an appropriate development https certificate.");

        private static readonly Action<ILogger, string, Exception> _failedToLocateDevelopmentCertificateFile =
            LoggerMessage.Define<string>(
                LogLevel.Debug,
                new EventId(2, "FailedToLocateDevelopmentCertificateFile"),
                "Failed to locate the development https certificate at '{certificatePath}'.");

        private static readonly Action<ILogger, string, Exception> _failedToLoadDevelopmentCertificate =
            LoggerMessage.Define<string>(
                LogLevel.Debug,
                new EventId(3, "FailedToLoadDevelopmentCertificate"),
                "Failed to load the development https certificate at '{certificatePath}'.");

        public static void LocatedDevelopmentCertificate(this ILogger logger, X509Certificate2 certificate) => _locatedDevelopmentCertificate(logger, certificate.Subject, certificate.Thumbprint, null);

        public static void UnableToLocateDevelopmentCertificate(this ILogger logger) => _unableToLocateDevelopmentCertificate(logger, null);

        public static void FailedToLocateDevelopmentCertificateFile(this ILogger logger, string certificatePath) => _failedToLocateDevelopmentCertificateFile(logger, certificatePath, null);

        public static void FailedToLoadDevelopmentCertificate(this ILogger logger, string certificatePath) => _failedToLoadDevelopmentCertificate(logger, certificatePath, null);
    }
}
