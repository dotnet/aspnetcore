// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks.Shared
{
    public class TestConnectionContext : ConnectionContext
    {
        public override string ConnectionId { get; set; }
        public override IFeatureCollection Features { get; } = new FeatureCollection();
        public override IDictionary<object, object> Items { get; set; }
        public override IDuplexPipe Transport { get; set; }
    }
}