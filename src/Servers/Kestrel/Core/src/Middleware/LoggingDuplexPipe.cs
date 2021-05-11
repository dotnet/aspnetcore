// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
