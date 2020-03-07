// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.HostFiltering
{
    internal static class LoggerExtensions
    {
        private static readonly Action<ILogger, Exception> _wildcardDetected =
            LoggerMessage.Define(LogLevel.Debug, new EventId(0, "WildcardDetected"), "Wildcard detected, all requests with hosts will be allowed.");

        private static readonly Action<ILogger, string, Exception> _allowedHosts =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(1, "AllowedHosts"), "Allowed hosts: {Hosts}");

        private static readonly Action<ILogger, Exception> _allHostsAllowed =
            LoggerMessage.Define(LogLevel.Trace, new EventId(2, "AllHostsAllowed"), "All hosts are allowed.");

        private static readonly Action<ILogger, string, Exception> _requestRejectedMissingHost =
            LoggerMessage.Define<string>(LogLevel.Information, new EventId(3, "RequestRejectedMissingHost"), "{Protocol} request rejected due to missing or empty host header.");

        private static readonly Action<ILogger, string, Exception> _requestAllowedMissingHost =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(4, "RequestAllowedMissingHost"), "{Protocol} request allowed with missing or empty host header.");

        private static readonly Action<ILogger, string, Exception> _allowedHostMatched =
            LoggerMessage.Define<string>(LogLevel.Trace, new EventId(5, "AllowedHostMatched"), "The host '{Host}' matches an allowed host.");

        private static readonly Action<ILogger, string, Exception> _noAllowedHostMatched =
            LoggerMessage.Define<string>(LogLevel.Information, new EventId(6, "NoAllowedHostMatched"), "The host '{Host}' does not match an allowed host.");

        public static void WildcardDetected(this ILogger logger) => _wildcardDetected(logger, null);
        public static void AllowedHosts(this ILogger logger, string allowedHosts) => _allowedHosts(logger, allowedHosts, null);
        public static void AllHostsAllowed(this ILogger logger) => _allHostsAllowed(logger, null);
        public static void RequestRejectedMissingHost(this ILogger logger, string protocol) => _requestRejectedMissingHost(logger, protocol, null);
        public static void RequestAllowedMissingHost(this ILogger logger, string protocol) => _requestAllowedMissingHost(logger, protocol, null);
        public static void AllowedHostMatched(this ILogger logger, string host) => _allowedHostMatched(logger, host, null);
        public static void NoAllowedHostMatched(this ILogger logger, string host) => _noAllowedHostMatched(logger, host, null);
    }
}
