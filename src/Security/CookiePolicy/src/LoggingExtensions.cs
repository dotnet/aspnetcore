// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging
{
    internal static class LoggingExtensions
    {
        private static Action<ILogger, bool, Exception> _needsConsent;
        private static Action<ILogger, bool, Exception> _hasConsent;
        private static Action<ILogger, Exception> _consentGranted;
        private static Action<ILogger, Exception> _consentWithdrawn;
        private static Action<ILogger, string, Exception> _cookieSuppressed;
        private static Action<ILogger, string, Exception> _deleteCookieSuppressed;
        private static Action<ILogger, string, Exception> _upgradedToSecure;
        private static Action<ILogger, string, string, Exception> _upgradedSameSite;
        private static Action<ILogger, string, Exception> _upgradedToHttpOnly;

        static LoggingExtensions()
        {
            _needsConsent = LoggerMessage.Define<bool>(
                eventId: new EventId(1, "NeedsConsent"),
                logLevel: LogLevel.Trace,
                formatString: "Needs consent: {needsConsent}.");
            _hasConsent = LoggerMessage.Define<bool>(
                eventId: new EventId(2, "HasConsent"),
                logLevel: LogLevel.Trace,
                formatString: "Has consent: {hasConsent}.");
            _consentGranted = LoggerMessage.Define(
                eventId: new EventId(3, "ConsentGranted"),
                logLevel: LogLevel.Debug,
                formatString: "Consent granted.");
            _consentWithdrawn = LoggerMessage.Define(
                eventId: new EventId(4, "ConsentWithdrawn"),
                logLevel: LogLevel.Debug,
                formatString: "Consent withdrawn.");
            _cookieSuppressed = LoggerMessage.Define<string>(
                eventId: new EventId(5, "CookieSuppressed"),
                logLevel: LogLevel.Debug,
                formatString: "Cookie '{key}' suppressed due to consent policy.");
            _deleteCookieSuppressed = LoggerMessage.Define<string>(
                eventId: new EventId(6, "DeleteCookieSuppressed"),
                logLevel: LogLevel.Debug,
                formatString: "Delete cookie '{key}' suppressed due to developer policy.");
            _upgradedToSecure = LoggerMessage.Define<string>(
                eventId: new EventId(7, "UpgradedToSecure"),
                logLevel: LogLevel.Debug,
                formatString: "Cookie '{key}' upgraded to 'secure'.");
            _upgradedSameSite = LoggerMessage.Define<string, string>(
                eventId: new EventId(8, "UpgradedSameSite"),
                logLevel: LogLevel.Debug,
                formatString: "Cookie '{key}' same site mode upgraded to '{mode}'.");
            _upgradedToHttpOnly = LoggerMessage.Define<string>(
                eventId: new EventId(9, "UpgradedToHttpOnly"),
                logLevel: LogLevel.Debug,
                formatString: "Cookie '{key}' upgraded to 'httponly'.");
        }

        public static void NeedsConsent(this ILogger logger, bool needsConsent)
        {
            _needsConsent(logger, needsConsent, null);
        }

        public static void HasConsent(this ILogger logger, bool hasConsent)
        {
            _hasConsent(logger, hasConsent, null);
        }

        public static void ConsentGranted(this ILogger logger)
        {
            _consentGranted(logger, null);
        }

        public static void ConsentWithdrawn(this ILogger logger)
        {
            _consentWithdrawn(logger, null);
        }

        public static void CookieSuppressed(this ILogger logger, string key)
        {
            _cookieSuppressed(logger, key, null);
        }

        public static void DeleteCookieSuppressed(this ILogger logger, string key)
        {
            _deleteCookieSuppressed(logger, key, null);
        }

        public static void CookieUpgradedToSecure(this ILogger logger, string key)
        {
            _upgradedToSecure(logger, key, null);
        }

        public static void CookieSameSiteUpgraded(this ILogger logger, string key, string mode)
        {
            _upgradedSameSite(logger, key, mode, null);
        }

        public static void CookieUpgradedToHttpOnly(this ILogger logger, string key)
        {
            _upgradedToHttpOnly(logger, key, null);
        }
    }
}
