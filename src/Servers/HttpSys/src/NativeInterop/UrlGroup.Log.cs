// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.HttpSys;

internal partial class UrlGroup
{
    private static partial class Log
    {
        [LoggerMessage(LoggerEventIds.CloseUrlGroupError, LogLevel.Error, "HttpCloseUrlGroup; Result: {StatusCode}", EventName = "CloseUrlGroupError")]
        public static partial void CloseUrlGroupError(ILogger logger, uint statusCode);

        [LoggerMessage(LoggerEventIds.RegisteringPrefix, LogLevel.Debug, "Listening on prefix: {UriPrefix}", EventName = "RegisteringPrefix")]
        public static partial void RegisteringPrefix(ILogger logger, string uriPrefix);

        [LoggerMessage(LoggerEventIds.SetUrlPropertyError, LogLevel.Error, "SetUrlGroupProperty", EventName = "SetUrlPropertyError")]
        public static partial void SetUrlPropertyError(ILogger logger, Exception exception);

        [LoggerMessage(LoggerEventIds.UnregisteringPrefix, LogLevel.Information, "Stop listening on prefix: {UriPrefix}", EventName = "UnregisteringPrefix")]
        public static partial void UnregisteringPrefix(ILogger logger, string uriPrefix);
    }
}
