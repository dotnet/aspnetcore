// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc
{
    // Generates a response based on the expected URL and action context
    public class TestResponseGenerator
    {
        private readonly ActionContext _actionContext;

        public TestResponseGenerator(IActionContextAccessor contextAccessor)
        {
            _actionContext = contextAccessor.ActionContext;
            if (_actionContext == null)
            {
                throw new InvalidOperationException("ActionContext should not be null here.");
            }
        }

        public ActionResult Generate(params string[] expectedUrls)
        {
            var link = (string)null;
            var query = _actionContext.HttpContext.Request.Query;
            if (query.ContainsKey("link"))
            {
                var values = query
                    .Where(kvp => kvp.Key != "link" && kvp.Key != "link_action" && kvp.Key != "link_controller")
                    .ToDictionary(kvp => kvp.Key.Substring("link_".Length), kvp => (object)kvp.Value[0]);

                var urlHelper = GetUrlHelper(_actionContext);
                link = urlHelper.Action(query["link_action"], query["link_controller"], values);
            }

            var attributeRoutingInfo = _actionContext.ActionDescriptor.AttributeRouteInfo;

            return new OkObjectResult(new
            {
                expectedUrls = expectedUrls,
                actualUrl = _actionContext.HttpContext.Request.Path.Value,
                routeName = attributeRoutingInfo == null ? null : attributeRoutingInfo.Name,
                routeValues = new Dictionary<string, object>(_actionContext.RouteData.Values),

                action = ((ControllerActionDescriptor)_actionContext.ActionDescriptor).ActionName,
                controller = ((ControllerActionDescriptor)_actionContext.ActionDescriptor).ControllerName,

                link,
            });
        }

        private IUrlHelper GetUrlHelper(ActionContext context)
        {
            var services = context.HttpContext.RequestServices;
            var urlHelper = services.GetRequiredService<IUrlHelperFactory>().GetUrlHelper(context);
            return urlHelper;
        }
    }
}
