// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.HttpSys.RequestProcessing;

internal sealed partial class TlsListener : IDisposable
{
    private static partial class Log
    {
        [LoggerMessage(LoggerEventIds.TlsListenerError, LogLevel.Error, "Error during closed connection cleanup.", EventName = "TlsListenerCleanupClosedConnectionError")]
        public static partial void CleanupClosedConnectionError(ILogger logger, Exception exception);
    }
}
