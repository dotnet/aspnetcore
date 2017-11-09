// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Build.Utilities;

namespace RepoTasks.Utilities
{
    public static class LoggingExtensions
    {
        public static void LogKoreBuildError(this TaskLoggingHelper logger, int code, string message, params object[] messageArgs)
            => LogKoreBuildError(logger, null, code, message, messageArgs: messageArgs);

        public static void LogKoreBuildError(this TaskLoggingHelper logger, string filename, int code, string message, params object[] messageArgs)
        {
            logger.LogError(null, KoreBuildErrors.Prefix + code, null, filename, 0, 0, 0, 0, message, messageArgs: messageArgs);
        }

        public static void LogKoreBuildWarning(this TaskLoggingHelper logger, int code, string message, params object[] messageArgs)
            => LogKoreBuildWarning(logger, null, code, message, messageArgs: messageArgs);

        public static void LogKoreBuildWarning(this TaskLoggingHelper logger, string filename, int code, string message, params object[] messageArgs)
        {
            logger.LogWarning(null, KoreBuildErrors.Prefix + code, null, filename, 0, 0, 0, 0, message, messageArgs: messageArgs);
        }
    }
}
