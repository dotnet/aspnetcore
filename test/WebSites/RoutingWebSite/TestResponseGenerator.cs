// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.DependencyInjection;

namespace RoutingWebSite
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
            return new JsonResult(new
            {
                expectedUrls = expectedUrls,
                actualUrl = _actionContext.HttpContext.Request.Path.Value,
                routeValues = _actionContext.RouteData.Values,

                action = _actionContext.ActionDescriptor.Name,
                controller = ((ReflectedActionDescriptor)_actionContext.ActionDescriptor).ControllerDescriptor.Name,
            });
        }
    }
}