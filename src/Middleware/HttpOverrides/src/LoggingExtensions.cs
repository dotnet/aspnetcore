// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Logging
{
    internal static class LoggingExtensions
    {
        private static Action<ILogger, Exception?> _noCertificate;

        static LoggingExtensions()
        {
            _noCertificate = LoggerMessage.Define(
                eventId: new EventId(0, "NoCertificate"),
                logLevel: LogLevel.Warning,
                formatString: "Could not read certificate from header.");
        }

        public static void NoCertificate(this ILogger logger, Exception exception)
        {
            _noCertificate(logger, exception);
        }
    }
}
