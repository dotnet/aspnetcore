// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System.Collections.Generic;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Routing;

namespace Microsoft.AspNet.Mvc
{
    public class ActionContext
    {
        public ActionContext([NotNull] ActionContext actionContext)
            : this(actionContext.HttpContext, actionContext.Router, actionContext.RouteValues, actionContext.ActionDescriptor)
        {
            ModelState = actionContext.ModelState;
            Controller = actionContext.Controller;
        }

        public ActionContext(HttpContext httpContext, IRouter router, IDictionary<string, object> routeValues, ActionDescriptor actionDescriptor)
        {
            HttpContext = httpContext;
            Router = router;
            RouteValues = routeValues;
            ActionDescriptor = actionDescriptor;
            ModelState = new ModelStateDictionary();
        }

        public HttpContext HttpContext { get; private set; }

        public IRouter Router { get; private set; }

        public IDictionary<string, object> RouteValues { get; private set; }

        public ModelStateDictionary ModelState { get; private set; }

        public ActionDescriptor ActionDescriptor { get; private set; }

        /// <summary>
        /// The controller is available only after the controller factory runs.
        /// </summary>
        public object Controller { get; set; }
    }
}
