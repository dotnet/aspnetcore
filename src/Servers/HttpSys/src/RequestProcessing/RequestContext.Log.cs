// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    internal partial class RequestContext
    {
        private static class Log
        {
            private static readonly Action<ILogger, Exception?> _abortError =
                LoggerMessage.Define(LogLevel.Debug, LoggerEventIds.AbortError, "Abort");

            private static readonly Action<ILogger, Exception?> _channelBindingNeedsHttps =
                LoggerMessage.Define(LogLevel.Debug, LoggerEventIds.ChannelBindingNeedsHttps, "TryGetChannelBinding; Channel binding requires HTTPS.");

            private static readonly Action<ILogger, Exception?> _channelBindingRetrieved =
                LoggerMessage.Define(LogLevel.Debug, LoggerEventIds.ChannelBindingRetrieved, "Channel binding retrieved.");

            public static void AbortError(ILogger logger, Exception exception)
            {
                _abortError(logger, exception);
            }

            public static void ChannelBindingNeedsHttps(ILogger logger)
            {
                _channelBindingNeedsHttps(logger, null);
            }

            public static void ChannelBindingRetrieved(ILogger logger)
            {
                _channelBindingRetrieved(logger, null);
            }
        }
    }
}
