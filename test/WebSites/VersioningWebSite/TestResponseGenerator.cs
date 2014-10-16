// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.DependencyInjection;

namespace VersioningWebSite
{
    // Generates a response based on the expected URL and action context
    public class TestResponseGenerator
    {
        private readonly ActionContext _actionContext;

        public TestResponseGenerator(IContextAccessor<ActionContext> contextAccessor)
        {
            _actionContext = contextAccessor.Value;
            if (_actionContext == null)
            {
                throw new InvalidOperationException("ActionContext should not be null here.");
            }
        }

        public JsonResult Generate(params string[] expectedUrls)
        {
            var link = (string)null;
            var query = _actionContext.HttpContext.Request.Query;
            if (query.ContainsKey("link"))
            {
                var values = query
                    .Where(kvp => kvp.Key != "link" && kvp.Key != "link_action" && kvp.Key != "link_controller")
                    .ToDictionary(kvp => kvp.Key.Substring("link_".Length), kvp => (object)kvp.Value[0]);

                var urlHelper = _actionContext.HttpContext.RequestServices.GetRequiredService<IUrlHelper>();
                link = urlHelper.Action(query["link_action"], query["link_controller"], values);
            }

            var attributeRoutingInfo = _actionContext.ActionDescriptor.AttributeRouteInfo;

            return new JsonResult(new
            {
                expectedUrls = expectedUrls,
                actualUrl = _actionContext.HttpContext.Request.Path.Value,
                routeName = attributeRoutingInfo == null ? null : attributeRoutingInfo.Name,
                routeValues = new Dictionary<string, object>(_actionContext.RouteData.Values),

                action = _actionContext.ActionDescriptor.Name,
                controller = ((ControllerActionDescriptor)_actionContext.ActionDescriptor).ControllerDescriptor.Name,

                link,
            });
        }
    }
}