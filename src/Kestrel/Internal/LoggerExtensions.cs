using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal
{
    internal static class LoggerExtensions
    {
        // Category: DefaultHttpsProvider
        private static readonly Action<ILogger, string, string, Exception> _locatedDevelopmentCertificate =
            LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(0, nameof(LocatedDevelopmentCertificate)), "Using development certificate: {certificateSubjectName} (Thumbprint: {certificateThumbprint})");

        private static readonly Action<ILogger, Exception> _unableToLocateDevelopmentCertificate =
            LoggerMessage.Define(LogLevel.Error, new EventId(1, nameof(UnableToLocateDevelopmentCertificate)), "Unable to locate an appropriate development https certificate.");

        public static void LocatedDevelopmentCertificate(this ILogger logger, X509Certificate2 certificate) => _locatedDevelopmentCertificate(logger, certificate.Subject, certificate.Thumbprint, null);

        public static void UnableToLocateDevelopmentCertificate(this ILogger logger) => _unableToLocateDevelopmentCertificate(logger, null);
    }
}
