// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RoutingWebSite
{
    public class RouteDataController : Controller
    {
        public object Conventional()
        {
            return GetData();
        }

        [Route("RouteData/Attribute")]
        public object Attribute()
        {
            return GetData();
        }

        public object DataTokens()
        {
            return GetData();
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.RouteData.DataTokens.ContainsKey("actionName"))
            {
                context.RouteData.DataTokens.Add("actionName", ((ControllerActionDescriptor)context.ActionDescriptor).ActionName);
            }
        }

        private object GetData()
        {
            var routers = RouteData.Routers.Select(r => r.GetType().FullName).ToArray();
            var dataTokens = RouteData.DataTokens;

            return new
            {
                DataTokens = dataTokens,
                Routers = routers
            };
        }
    }
}