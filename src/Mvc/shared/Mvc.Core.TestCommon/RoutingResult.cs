// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Mvc
{
    // See TestResponseGenerator for the code that generates this data.
    public class RoutingResult
    {
        public string[] ExpectedUrls { get; set; }

        public string ActualUrl { get; set; }

        public Dictionary<string, object> RouteValues { get; set; }

        public string RouteName { get; set; }

        public string Action { get; set; }

        public string Controller { get; set; }

        public string Link { get; set; }
    }
}
