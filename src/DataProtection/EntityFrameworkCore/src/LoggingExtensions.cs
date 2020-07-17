// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging
{
    internal static class LoggingExtensions
    {
        private static readonly Action<ILogger, string?, string?, Exception?> _readingXmlFromKey;
        private static readonly Action<ILogger, string, string, Exception?> _savingKeyToDbContext;

        static LoggingExtensions()
        {
            _readingXmlFromKey = LoggerMessage.Define<string?, string?>(
                eventId: new EventId(1, "ReadKeyFromElement"),
                logLevel: LogLevel.Debug,
                formatString: "Reading data with key '{FriendlyName}', value '{Value}'.");
            _savingKeyToDbContext = LoggerMessage.Define<string, string>(
                eventId: new EventId(2, "SavingKeyToDbContext"),
                logLevel: LogLevel.Debug,
                formatString: "Saving key '{FriendlyName}' to '{DbContext}'.");
        }

        public static void ReadingXmlFromKey(this ILogger logger, string? friendlyName, string? keyXml)
            => _readingXmlFromKey(logger, friendlyName, keyXml, null);

        public static void LogSavingKeyToDbContext(this ILogger logger, string friendlyName, string contextName)
            => _savingKeyToDbContext(logger, friendlyName, contextName, null);
    }
}
