// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.HttpsPolicy
{
    internal static class HstsLoggingExtensions
    {
        private static readonly Action<ILogger, Exception> _notSecure;
        private static readonly Action<ILogger, string, Exception> _excludedHost;
        private static readonly Action<ILogger, Exception> _addingHstsHeader;

        static HstsLoggingExtensions()
        {
            _notSecure = LoggerMessage.Define(
                LogLevel.Debug,
                new EventId(1, "NotSecure"),
                "The request is insecure. Skipping HSTS header.");

            _excludedHost = LoggerMessage.Define<string>(
                LogLevel.Debug,
                new EventId(2, "ExcludedHost"),
                "The host '{host}' is excluded. Skipping HSTS header.");

            _addingHstsHeader = LoggerMessage.Define(
                LogLevel.Trace,
                new EventId(3, "AddingHstsHeader"),
                "Adding HSTS header to response.");
        }

        public static void SkippingInsecure(this ILogger logger)
        {
            _notSecure(logger, null);
        }

        public static void SkippingExcludedHost(this ILogger logger, string host)
        {
            _excludedHost(logger, host, null);
        }

        public static void AddingHstsHeader(this ILogger logger)
        {
            _addingHstsHeader(logger, null);
        }
    }
}
