// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO.Pipelines;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    // Ideally this type should be readonly and initialized with a constructor.
    // Tests use TestServiceContext which inherits from this type and sets properties.
    // Changing this type would be a lot of work.
    internal class ServiceContext
    {
        public IKestrelTrace Log { get; set; } = default!;

        public PipeScheduler Scheduler { get; set; } = default!;

        public IHttpParser<Http1ParsingHandler> HttpParser { get; set; } = default!;

        public ISystemClock SystemClock { get; set; } = default!;

        public DateHeaderValueManager DateHeaderValueManager { get; set; } = default!;

        public ConnectionManager ConnectionManager { get; set; } = default!;

        public Heartbeat Heartbeat { get; set; } = default!;

        public KestrelServerOptions ServerOptions { get; set; } = default!;
    }
}
