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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Core;

namespace Microsoft.AspNet.Mvc
{
    public class RedirectToActionResult : ActionResult
    {
        public RedirectToActionResult([NotNull] IUrlHelper urlHelper, string actionName,
                                            string controllerName, IDictionary<string, object> routeValues)
            : this(urlHelper, actionName, controllerName, routeValues, permanent: false)
        {
        }

        public RedirectToActionResult([NotNull] IUrlHelper urlHelper, string actionName,
                                        string controllerName, IDictionary<string, object> routeValues, bool permanent)
        {
            UrlHelper = urlHelper;
            ActionName = actionName;
            ControllerName = controllerName;
            RouteValues = routeValues;
            Permanent = permanent;
        }

        public IUrlHelper UrlHelper { get; private set; }

        public string ActionName { get; private set; }

        public string ControllerName { get; private set; }

        public IDictionary<string, object> RouteValues { get; private set; }

        public bool Permanent { get; private set; }

        public override void ExecuteResult([NotNull] ActionContext context)
        {
            var destinationUrl = UrlHelper.Action(ActionName, ControllerName, RouteValues);

            if (string.IsNullOrEmpty(destinationUrl))
            {
                throw new InvalidOperationException(Resources.NoRoutesMatched);
            }

            context.HttpContext.Response.Redirect(destinationUrl, Permanent);
        }
    }
}
