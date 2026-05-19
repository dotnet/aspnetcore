// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal;

// Ideally this type should be readonly and initialized with a constructor.
// Tests use TestServiceContext which inherits from this type and sets properties.
// Changing this type would be a lot of work.
#pragma warning disable CA1852 // Seal internal types
internal class ServiceContext
#pragma warning restore CA1852 // Seal internal types
{
    public KestrelTrace Log { get; set; } = default!;

    public PipeScheduler Scheduler { get; set; } = default!;

    public IHttpParser<Http1ParsingHandler> HttpParser { get; set; } = default!;

    public TimeProvider TimeProvider { get; set; } = default!;

    public DateHeaderValueManager DateHeaderValueManager { get; set; } = default!;

    public ConnectionManager ConnectionManager { get; set; } = default!;

    public Heartbeat Heartbeat { get; set; } = default!;

    public KestrelServerOptions ServerOptions { get; set; } = default!;

    public DiagnosticSource? DiagnosticSource { get; set; }

    public KestrelMetrics Metrics { get; set; } = default!;
}
