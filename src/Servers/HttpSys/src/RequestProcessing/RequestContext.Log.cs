// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
