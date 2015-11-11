// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using System;
using System.Collections.Generic;

namespace InlineConstraintsWebSite.Controllers
{
    public class InlineConstraints_StoreController : Controller
    {
        public IDictionary<string, object> GetStoreById(Guid id)
        {
            return RouteData.Values;
        }

        public IDictionary<string, object> GetStoreByLocation(string location)
        {
            return RouteData.Values;
        }
    }
}