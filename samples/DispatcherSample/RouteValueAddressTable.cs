// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Routing;

namespace DispatcherSample
{
    public class RouteValueAddressTable
    {
        public IList<RouteValueAddress> Addresses
        {
            get
            {
                var addresses = new List<RouteValueAddress>
                {
                    new RouteValueAddress("Mickey", new RouteValueDictionary (new { Character = "Mickey" })),
                    new RouteValueAddress("Hakuna Matata", new RouteValueDictionary (new { Movie = "The Lion King"})),
                    new RouteValueAddress("Simba", new RouteValueDictionary (new { Movie = "The Lion King", Character = "Simba" })),
                    new RouteValueAddress("Mufasa", new RouteValueDictionary (new { Movie = "The Lion King", Character = "Mufasa" })),
                    new RouteValueAddress("Aladdin", new RouteValueDictionary (new { Movie = "Aladdin", Character = "Genie" })),
                };

                return addresses;
            }
        }
    }
}
