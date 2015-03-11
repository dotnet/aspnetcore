// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using Microsoft.Framework.Logging.Internal;

namespace Microsoft.Framework.Logging
{
    /// <summary>
    /// Helpful extension methods on ILogger.
    /// </summary>
    internal static class LoggingExtensions
    {
        /// <summary>
        /// Returns a value stating whether the 'debug' log level is enabled.
        /// Returns false if the logger instance is null.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDebugLevelEnabled(this ILogger logger)
        {
            return IsLogLevelEnabledCore(logger, LogLevel.Debug);
        }

        /// <summary>
        /// Returns a value stating whether the 'error' log level is enabled.
        /// Returns false if the logger instance is null.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsErrorLevelEnabled(this ILogger logger)
        {
            return IsLogLevelEnabledCore(logger, LogLevel.Error);
        }

        /// <summary>
        /// Returns a value stating whether the 'information' log level is enabled.
        /// Returns false if the logger instance is null.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInformationLevelEnabled(this ILogger logger)
        {
            return IsLogLevelEnabledCore(logger, LogLevel.Information);
        }

        /// <summary>
        /// Returns a value stating whether the 'verbose' log level is enabled.
        /// Returns false if the logger instance is null.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsVerboseLevelEnabled(this ILogger logger)
        {
            return IsLogLevelEnabledCore(logger, LogLevel.Verbose);
        }

        /// <summary>
        /// Returns a value stating whether the 'warning' log level is enabled.
        /// Returns false if the logger instance is null.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsWarningLevelEnabled(this ILogger logger)
        {
            return IsLogLevelEnabledCore(logger, LogLevel.Warning);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsLogLevelEnabledCore(ILogger logger, LogLevel level)
        {
            return (logger != null && logger.IsEnabled(level));
        }

        public static void LogDebug(this ILogger logger, Exception error, string message, params object[] args)
        {
            logger.LogDebug(new FormattedLogValues(message, args), error);
        }

        public static void LogError(this ILogger logger, Exception error, string message, params object[] args)
        {
            logger.LogError(new FormattedLogValues(message, args), error);
        }

        public static void LogWarning(this ILogger logger, Exception error, string message, params object[] args)
        {
            logger.LogWarning(new FormattedLogValues(message, args), error);
        }
    }
}
