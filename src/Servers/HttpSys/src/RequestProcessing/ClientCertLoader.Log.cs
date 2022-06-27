// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.HttpSys;

internal partial class ClientCertLoader
{
    private static partial class Log
    {
        [LoggerMessage(LoggerEventIds.ChannelBindingMissing, LogLevel.Error, "GetChannelBindingFromTls", EventName = "ChannelBindingMissing")]
        public static partial void ChannelBindingMissing(ILogger logger, Exception exception);

        [LoggerMessage(LoggerEventIds.ChannelBindingUnsupported, LogLevel.Error, "GetChannelBindingFromTls; Channel binding is not supported.", EventName = "ChannelBindingUnsupported")]
        public static partial void ChannelBindingUnsupported(ILogger logger);
    }
}
