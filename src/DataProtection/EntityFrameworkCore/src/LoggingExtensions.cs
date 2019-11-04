// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging
{
    internal static class LoggingExtensions
    {
        private static readonly Action<ILogger, string, Exception> _anExceptionOccurredWhileParsingKeyXml;
        private static readonly Action<ILogger, string, string, Exception> _savingKeyToDbContext;

        static LoggingExtensions()
        {
            _anExceptionOccurredWhileParsingKeyXml = LoggerMessage.Define<string>(
                eventId: new EventId(1, "ExceptionOccurredWhileParsingKeyXml"),
                logLevel: LogLevel.Warning,
                formatString: "An exception occurred while parsing the key xml '{Xml}'.");
            _savingKeyToDbContext = LoggerMessage.Define<string, string>(
                eventId: new EventId(2, "SavingKeyToDbContext"),
                logLevel: LogLevel.Debug,
                formatString: "Saving key '{FriendlyName}' to '{DbContext}'.");
        }

        public static void LogExceptionWhileParsingKeyXml(this ILogger logger, string keyXml, Exception exception)
            => _anExceptionOccurredWhileParsingKeyXml(logger, keyXml, exception);

        public static void LogSavingKeyToDbContext(this ILogger logger, string friendlyName, string contextName)
            => _savingKeyToDbContext(logger, friendlyName, contextName, null);
    }
}
