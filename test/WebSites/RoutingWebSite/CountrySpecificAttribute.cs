// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace RoutingWebSite
{
    public class CountrySpecificAttribute : RouteConstraintAttribute
    {
        public CountrySpecificAttribute(string countryCode)
            : base("country", countryCode, blockNonAttributedActions: false)
        {
        }
    }
}