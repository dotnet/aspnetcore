// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using Microsoft.Framework.Logging.Internal;

namespace Microsoft.Framework.Logging
{
    /// <summary>
    /// Helpful extension methods on <see cref="ILogger"/>.
    /// Methods ending in *F take <see cref="FormattableString"/> as a parameter.
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

        public static void LogDebugF(this ILogger logger, FormattableString message)
        {
            logger.LogDebug(message.Format, message.GetArguments());
        }

        public static void LogDebugF(this ILogger logger, Exception error, FormattableString message)
        {
            logger.LogDebug(new FormattedLogValues(message.Format, message.GetArguments()), error);
        }

        public static void LogError(this ILogger logger, Exception error, string message)
        {
            logger.LogError(message, error);
        }

        public static void LogErrorF(this ILogger logger, Exception error, FormattableString message)
        {
            logger.LogError(new FormattedLogValues(message.Format, message.GetArguments()), error);
        }

        public static void LogInformationF(this ILogger logger, FormattableString message)
        {
            logger.LogInformation(message.Format, message.GetArguments());
        }

        public static void LogVerboseF(this ILogger logger, FormattableString message)
        {
            logger.LogVerbose(message.Format, message.GetArguments());
        }

        public static void LogWarningF(this ILogger logger, FormattableString message)
        {
            logger.LogWarning(message.Format, message.GetArguments());
        }

        public static void LogWarningF(this ILogger logger, Exception error, FormattableString message)
        {
            logger.LogWarning(new FormattedLogValues(message.Format, message.GetArguments()), error);
        }
    }
}
