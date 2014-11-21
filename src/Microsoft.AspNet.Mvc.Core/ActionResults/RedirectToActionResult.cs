// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
