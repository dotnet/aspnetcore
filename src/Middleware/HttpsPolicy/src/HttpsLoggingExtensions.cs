// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.HttpsPolicy
{
    internal static class HttpsLoggingExtensions
    {
        private static readonly Action<ILogger, string, Exception> _redirectingToHttps;
        private static readonly Action<ILogger, int, Exception> _portLoadedFromConfig;
        private static readonly Action<ILogger, Exception> _failedToDeterminePort;
        private static readonly Action<ILogger, Exception> _failedMultiplePorts;
        private static readonly Action<ILogger, int, Exception> _portFromServer;

        static HttpsLoggingExtensions()
        {
            _redirectingToHttps = LoggerMessage.Define<string>(
                LogLevel.Debug,
                new EventId(1, "RedirectingToHttps"),
                "Redirecting to '{redirect}'.");

            _portLoadedFromConfig = LoggerMessage.Define<int>(
                LogLevel.Debug,
                new EventId(2, "PortLoadedFromConfig"),
                "Https port '{port}' loaded from configuration.");

            _failedToDeterminePort = LoggerMessage.Define(
                LogLevel.Warning,
                new EventId(3, "FailedToDeterminePort"),
                "Failed to determine the https port for redirect.");

            _failedMultiplePorts = LoggerMessage.Define(
                LogLevel.Warning,
                new EventId(4, "FailedMultiplePorts"),
                "Cannot determine the https port from IServerAddressesFeature, multiple values were found. " +
                "Please set the desired port explicitly on HttpsRedirectionOptions.HttpsPort.");

            _portFromServer = LoggerMessage.Define<int>(
                LogLevel.Debug,
                new EventId(5, "PortFromServer"),
                "Https port '{httpsPort}' discovered from server endpoints.");
        }

        public static void RedirectingToHttps(this ILogger logger, string redirect)
        {
            _redirectingToHttps(logger, redirect, null);
        }

        public static void PortLoadedFromConfig(this ILogger logger, int port)
        {
            _portLoadedFromConfig(logger, port, null);
        }

        public static void FailedToDeterminePort(this ILogger logger)
        {
            _failedToDeterminePort(logger, null);
        }

        public static void FailedMultiplePorts(this ILogger logger)
        {
            _failedMultiplePorts(logger, null);
        }

        public static void PortFromServer(this ILogger logger, int port)
        {
            _portFromServer(logger, port, null);
        }
    }
}
