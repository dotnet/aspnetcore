using System;
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Dispatcher;
using Microsoft.AspNetCore.Routing;

namespace DispatcherSample
{
    public class RouteValueAddress : Address
    {
        public RouteValueAddress(string displayName, RouteValueDictionary dictionary)
        {
            DisplayName = displayName;
            RouteValueDictionary = dictionary;
        }

        public override string DisplayName { get; }

        public RouteValueDictionary RouteValueDictionary { get; set; }
    }
}
