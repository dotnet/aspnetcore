// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Routing
{
    internal class DefaultEndpointDataSourceBuilder : EndpointDataSourceBuilder
    {
        public IApplicationBuilder ApplicationBuilder { get; set; }

        public override ICollection<EndpointBuilder> Endpoints { get; } = new List<EndpointBuilder>();

        public override IApplicationBuilder CreateApplicationBuilder() => ApplicationBuilder.New();
    }
}