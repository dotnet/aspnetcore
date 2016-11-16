// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO.Pipelines;
using System.Security.Claims;

namespace Microsoft.AspNetCore.Sockets
{
    public class Connection
    {
        public string ConnectionId { get; set; }
        public ClaimsPrincipal User { get; set; }
        public IPipelineConnection Channel { get; set; }
        public ConnectionMetadata Metadata { get; } = new ConnectionMetadata();
    }
}
