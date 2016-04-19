// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Routing.Template;

namespace Microsoft.AspNetCore.Routing.Tree
{
    public class InboundMatch
    {
        public InboundRouteEntry Entry { get; set; }

        public TemplateMatcher TemplateMatcher { get; set; }
    }
}
