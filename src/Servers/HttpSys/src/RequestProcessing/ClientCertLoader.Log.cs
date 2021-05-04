// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    internal partial class ClientCertLoader
    {
        private static class Log
        {
            private static readonly Action<ILogger, Exception?> _channelBindingMissing =
                LoggerMessage.Define(LogLevel.Error, LoggerEventIds.ChannelBindingMissing, "GetChannelBindingFromTls");

            private static readonly Action<ILogger, Exception?> _channelBindingUnsupported =
                LoggerMessage.Define(LogLevel.Error, LoggerEventIds.ChannelBindingUnsupported, "GetChannelBindingFromTls; Channel binding is not supported.");

            public static void ChannelBindingMissing(ILogger logger, Exception exception)
            {
                _channelBindingMissing(logger, exception);
            }

            public static void ChannelBindingUnsupported(ILogger logger)
            {
                _channelBindingUnsupported(logger, null);
            }
        }
    }
}
