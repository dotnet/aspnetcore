// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    public class FrameConnectionContext
    {
        public string ConnectionId { get; set; }
        public long FrameConnectionId { get; set; }
        public ServiceContext ServiceContext { get; set; }
        public List<IConnectionAdapter> ConnectionAdapters { get; set; }
        public IConnectionInformation ConnectionInformation { get; set; }

        public IPipe Input { get; set; }
        public IPipe Output { get; set; }
    }
}
