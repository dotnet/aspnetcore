// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.Logging;
using Microsoft.AspNet.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Mvc
{
    public class RedirectToActionResult : ActionResult, IKeepTempDataResult
    {
        public RedirectToActionResult(
            string actionName,
            string controllerName,
            IDictionary<string, object> routeValues)
            : this(actionName, controllerName, routeValues, permanent: false)
        {
        }

        public RedirectToActionResult(
            string actionName,
            string controllerName,
            IDictionary<string, object> routeValues,
            bool permanent)
        {
            ActionName = actionName;
            ControllerName = controllerName;
            RouteValues = routeValues;
            Permanent = permanent;
        }

        public IUrlHelper UrlHelper { get; set; }

        public string ActionName { get; set; }

        public string ControllerName { get; set; }

        public IDictionary<string, object> RouteValues { get; set; }

        public bool Permanent { get; set; }

        public override void ExecuteResult(ActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var loggerFactory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<RedirectToActionResult>();

            var urlHelper = GetUrlHelper(context);

            var destinationUrl = urlHelper.Action(ActionName, ControllerName, RouteValues);
            if (string.IsNullOrEmpty(destinationUrl))
            {
                throw new InvalidOperationException(Resources.NoRoutesMatched);
            }

            logger.RedirectToActionResultExecuting(destinationUrl);
            context.HttpContext.Response.Redirect(destinationUrl, Permanent);
        }

        private IUrlHelper GetUrlHelper(ActionContext context)
        {
            return UrlHelper ?? context.HttpContext.RequestServices.GetRequiredService<IUrlHelper>();
        }
    }
}
