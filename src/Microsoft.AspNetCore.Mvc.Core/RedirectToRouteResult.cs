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
    public class RedirectToRouteResult : ActionResult, IKeepTempDataResult
    {
        public RedirectToRouteResult(object routeValues)
            : this(routeName: null, routeValues: routeValues)
        {
        }

        public RedirectToRouteResult(
            string routeName,
            object routeValues)
            : this(routeName, routeValues, permanent: false)
        {
        }

        public RedirectToRouteResult(
            string routeName,
            object routeValues,
            bool permanent)
        {
            RouteName = routeName;
            RouteValues = routeValues == null ? null : new RouteValueDictionary(routeValues);
            Permanent = permanent;
        }

        /// <summary>
        /// Gets or sets the <see cref="IUrlHelper" /> used to generate URLs.
        /// </summary>
        public IUrlHelper UrlHelper { get; set; }

        /// <summary>
        /// Gets or sets the name of the route to use for generating the URL.
        /// </summary>
        public string RouteName { get; set; }

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
            var logger = loggerFactory.CreateLogger<RedirectToRouteResult>();

            var urlHelper = GetUrlHelper(context);

            var destinationUrl = urlHelper.RouteUrl(RouteName, RouteValues);
            if (string.IsNullOrEmpty(destinationUrl))
            {
                throw new InvalidOperationException(Resources.NoRoutesMatched);
            }

            logger.RedirectToRouteResultExecuting(destinationUrl, RouteName);
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
