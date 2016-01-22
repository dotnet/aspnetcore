// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.Logging;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.AspNet.Mvc.ViewFeatures;
using Microsoft.AspNet.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Mvc
{
    public class RedirectToActionResult : ActionResult, IKeepTempDataResult
    {
        public RedirectToActionResult(
            string actionName,
            string controllerName,
            object routeValues)
            : this(actionName, controllerName, routeValues, permanent: false)
        {
        }

        public RedirectToActionResult(
            string actionName,
            string controllerName,
            object routeValues,
            bool permanent)
        {
            ActionName = actionName;
            ControllerName = controllerName;
            RouteValues = routeValues == null ? null : new RouteValueDictionary(routeValues);
            Permanent = permanent;
        }

        /// <summary>
        /// Gets or sets the <see cref="IUrlHelper" /> used to generate URLs.
        /// </summary>
        public IUrlHelper UrlHelper { get; set; }

        /// <summary>
        /// Gets or sets the name of the action to use for generating the URL.
        /// </summary>
        public string ActionName { get; set; }

        /// <summary>
        /// Gets or sets the name of the controller to use for generating the URL.
        /// </summary>
        public string ControllerName { get; set; }

        /// <summary>
        /// Gets or sets the route data to use for generating the URL.
        /// </summary>
        public RouteValueDictionary RouteValues { get; set; }

        public bool Permanent { get; set; }

        /// <inheritdoc />
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
            var urlHelper = UrlHelper;
            if (urlHelper == null)
            {
                var services = context.HttpContext.RequestServices;
                urlHelper = services.GetRequiredService<IUrlHelperFactory>().GetUrlHelper(context);
            }

            return urlHelper;
        }
    }
}
