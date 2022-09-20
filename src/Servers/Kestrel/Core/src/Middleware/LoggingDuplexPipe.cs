// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal;

internal sealed class LoggingDuplexPipe : DuplexPipeStreamAdapter<LoggingStream>
{
    private static readonly StreamPipeReaderOptions _defaultReaderOptions = new(useZeroByteReads: true);
    private static readonly StreamPipeWriterOptions _defaultWriterOptions = new();

    public LoggingDuplexPipe(IDuplexPipe transport, ILogger logger) :
        base(transport, _defaultReaderOptions, _defaultWriterOptions, stream => new LoggingStream(stream, logger))
    {
    }
}
