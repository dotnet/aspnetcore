// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNet.Mvc;

namespace BasicWebSite
{
    public class RoutingController : Controller
    {
        public object Conventional()
        {
            return GetData();
        }

        [Route("Routing/Attribute")]
        public object Attribute()
        {
            return GetData();
        }

        private object GetData()
        {
            var routers = ActionContext.RouteData.Routers.Select(r => r.GetType().FullName).ToArray();
            return new { Routers = routers };
        }
    }
}