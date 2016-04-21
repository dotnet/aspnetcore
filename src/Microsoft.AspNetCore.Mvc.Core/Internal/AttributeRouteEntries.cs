// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Routing.Tree;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class AttributeRouteEntries
    {
        public List<InboundRouteEntry> InboundEntries { get; } = new List<InboundRouteEntry>();

        public List<OutboundRouteEntry> OutboundEntries { get; } = new List<OutboundRouteEntry>();
    }
}
