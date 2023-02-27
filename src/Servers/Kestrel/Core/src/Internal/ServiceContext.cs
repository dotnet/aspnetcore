// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal;

// Ideally this type should be readonly and initialized with a constructor.
// Tests use TestServiceContext which inherits from this type and sets properties.
// Changing this type would be a lot of work.
#pragma warning disable CA1852 // Seal internal types
internal class ServiceContext
#pragma warning restore CA1852 // Seal internal types
{
    // For test subtypes
    internal ServiceContext()
    {
    }

    public ServiceContext(
        IOptions<KestrelServerOptions> options,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        var serverOptions = options.Value ?? new KestrelServerOptions();
        var trace = new KestrelTrace(loggerFactory);
        var connectionManager = new ConnectionManager(
            trace,
            serverOptions.Limits.MaxConcurrentUpgradedConnections);

        var heartbeatManager = new HeartbeatManager(connectionManager);
        var dateHeaderValueManager = new DateHeaderValueManager();

        var heartbeat = new Heartbeat(
            new IHeartbeatHandler[] { dateHeaderValueManager, heartbeatManager },
            new SystemClock(),
            DebuggerWrapper.Singleton,
            trace);

        Log = trace;
        Scheduler = PipeScheduler.ThreadPool;
        HttpParser = new HttpParser<Http1ParsingHandler>(trace.IsEnabled(LogLevel.Information), serverOptions.DisableHttp1LineFeedTerminators);
        SystemClock = heartbeatManager;
        DateHeaderValueManager = dateHeaderValueManager;
        ConnectionManager = connectionManager;
        Heartbeat = heartbeat;
        ServerOptions = serverOptions;
    }

    public ServiceContext(
        IOptions<KestrelServerOptions> options,
        ILoggerFactory loggerFactory,
        DiagnosticSource diagnosticSource)
        : this(options, loggerFactory)
    {
        DiagnosticSource = diagnosticSource;
    }

    public KestrelTrace Log { get; set; } = default!;

    public PipeScheduler Scheduler { get; set; } = default!;

    public IHttpParser<Http1ParsingHandler> HttpParser { get; set; } = default!;

    public ISystemClock SystemClock { get; set; } = default!;

    public DateHeaderValueManager DateHeaderValueManager { get; set; } = default!;

    public ConnectionManager ConnectionManager { get; set; } = default!;

    public Heartbeat Heartbeat { get; set; } = default!;

    public KestrelServerOptions ServerOptions { get; set; } = default!;

    public DiagnosticSource? DiagnosticSource { get; set; }
}
