// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Server.Kestrel.Adapter;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal
{
    public class FrameConnectionContext
    {
        public string ConnectionId { get; set; }
        public ServiceContext ServiceContext { get; set; }
        public PipeFactory PipeFactory { get; set; }
        public List<IConnectionAdapter> ConnectionAdapters { get; set; }
        public Frame Frame { get; set; }
        public SocketOutputProducer OutputProducer { get; set; }

        public IPipe Input { get; set; }
        public IPipe Output { get; set; }
    }
}
