// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Routing.Template;

namespace Microsoft.AspNetCore.Routing.Tree
{
    public class OutboundMatch
    {
        public OutboundRouteEntry Entry { get; set; }

        public TemplateBinder TemplateBinder { get; set; }
    }
}
