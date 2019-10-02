// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Diagnostics.RazorViews
{
    internal class EndpointModel
    {
        public string DisplayName { get; set; }
        public string RoutePattern { get; set; }
        public int? Order { get; set; }
        public string HttpMethods { get; set; }
    }
}
