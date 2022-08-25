// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

internal sealed partial class KestrelTrace : ILogger
{
    private readonly ILogger _generalLogger;
    private readonly ILogger _badRequestsLogger;
    private readonly ILogger _connectionsLogger;
    private readonly ILogger _http2Logger;
    private readonly ILogger _http3Logger;

    public KestrelTrace(ILoggerFactory loggerFactory)
    {
        _generalLogger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel");
        _badRequestsLogger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel.BadRequests");
        _connectionsLogger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel.Connections");
        _http2Logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel.Http2");
        _http3Logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel.Http3");
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        => _generalLogger.Log(logLevel, eventId, state, exception, formatter);

    public bool IsEnabled(LogLevel logLevel) => _generalLogger.IsEnabled(logLevel);

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _generalLogger.BeginScope(state);
}
