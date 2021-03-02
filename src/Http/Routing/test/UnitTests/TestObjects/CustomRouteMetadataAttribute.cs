// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Routing.TestObjects
{
    internal class CustomRouteMetadataAttribute : Attribute, IRoutePatternMetadata, IHttpMethodMetadata, IRouteNameMetadata, IRouteOrderMetadata
    {
        public string? Pattern { get; set; }

        public string? Name { get; set; }

        public int Order { get; set; } = 0;

        public string[] Methods { get; set; } = Array.Empty<string>();

        string? IRoutePatternMetadata.RoutePattern => Pattern;

        string? IRouteNameMetadata.RouteName => Name;

        int? IRouteOrderMetadata.RouteOrder => Order;

        IReadOnlyList<string> IHttpMethodMetadata.HttpMethods => Methods;

        bool IHttpMethodMetadata.AcceptCorsPreflight => false;
    }
}
