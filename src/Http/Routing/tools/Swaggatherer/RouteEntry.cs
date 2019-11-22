// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Routing.Template;

namespace Swaggatherer
{
    internal class RouteEntry
    {
        public RouteTemplate Template { get; set; }
        public string Method { get; set; }
        public decimal Precedence { get; set; }
        public string RequestUrl { get; set; }
    }
}
