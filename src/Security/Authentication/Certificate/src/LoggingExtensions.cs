// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Logging
{
    internal static class LoggingExtensions
    {
        private static Action<ILogger, Exception> _noCertificate;
        private static Action<ILogger, string, string, Exception> _certRejected;
        private static Action<ILogger, string, string, Exception> _certFailedValidation;

        static LoggingExtensions()
        {
            _noCertificate = LoggerMessage.Define(
                eventId: new EventId(0, "NoCertificate"),
                logLevel: LogLevel.Debug,
                formatString: "No client certificate found.");

            _certRejected = LoggerMessage.Define<string, string>(
                eventId: new EventId(1, "CertificateRejected"),
                logLevel: LogLevel.Warning,
                formatString: "{CertificateType} certificate rejected, subject was {Subject}.");

            _certFailedValidation = LoggerMessage.Define<string, string>(
                eventId: new EventId(2, "CertificateFailedValidation"),
                logLevel: LogLevel.Warning,
                formatString: "Certificate validation failed, subject was {Subject}." + Environment.NewLine + "{ChainErrors}");
        }

        public static void NoCertificate(this ILogger logger)
        {
            _noCertificate(logger, null);
        }

        public static void CertificateRejected(this ILogger logger, string certificateType, string subject)
        {
            _certRejected(logger, certificateType, subject, null);
        }

        public static void CertificateFailedValidation(this ILogger logger, string subject, IEnumerable<string> chainedErrors)
        {
            _certFailedValidation(logger, subject, String.Join(Environment.NewLine, chainedErrors), null);
        }
    }
}
