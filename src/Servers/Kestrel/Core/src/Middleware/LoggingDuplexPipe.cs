// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    internal class LoggingDuplexPipe : DuplexPipeStreamAdapter<LoggingStream>
    {
        public LoggingDuplexPipe(IDuplexPipe transport, ILogger logger) :
            base(transport, stream => new LoggingStream(stream, logger))
        {
        }
    }
}
