// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging
{
    internal static class LoggingExtensions
    {
        private static Action<ILogger, Exception> _noCertificate;

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
