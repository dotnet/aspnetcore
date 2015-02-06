// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Identity.Logging
{
    public static class IdentityLoggerExtensions
    {
        private static TResult Log<TResult>(this ILogger logger, TResult result, Func<TResult, LogLevel> getLevel,
            Func<string> messageAccessor)
        {
            var logLevel = getLevel(result);

            // Check if log level is enabled before creating the message.
            if (logger.IsEnabled(logLevel))
            {
                logger.Log(logLevel, 0, messageAccessor(), null, (msg, exp) => (string)msg);
            }

            return result;
        }

        public static SignInResult Log(this ILogger logger, SignInResult result, [CallerMemberName]string methodName = null)
           => logger.Log(result, r => r.GetLogLevel(), () => Resources.FormatLoggingResult(methodName, result));

        public static IdentityResult Log(this ILogger logger, IdentityResult result, [CallerMemberName]string methodName = null)
            => logger.Log(result, r => r.GetLogLevel(), () => Resources.FormatLoggingResult(methodName, result));

        public static bool Log(this ILogger logger, bool result, [CallerMemberName]string methodName = null)
            => logger.Log(result, b => b ? LogLevel.Verbose : LogLevel.Warning,
                               () => Resources.FormatLoggingResult(methodName, result));
    }
}