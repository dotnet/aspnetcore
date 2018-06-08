// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Routing
{
    public class LinkGeneratorContext
    {
        public Address Address { get; set; }

        public RouteValueDictionary AmbientValues { get; set; }

        public RouteValueDictionary SuppliedValues { get; set; }
    }
}
