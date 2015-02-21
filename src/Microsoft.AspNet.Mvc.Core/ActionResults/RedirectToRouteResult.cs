// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    public class RedirectToRouteResult : ActionResult
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
            RouteValues = TypeHelper.ObjectToDictionary(routeValues);
            Permanent = permanent;
        }

        public IUrlHelper UrlHelper { get; set; }

        public string RouteName { get; set; }

        public IDictionary<string, object> RouteValues { get; set; }

        public bool Permanent { get; set; }

        public override void ExecuteResult([NotNull] ActionContext context)
        {
            var urlHelper = GetUrlHelper(context);

            var destinationUrl = urlHelper.RouteUrl(RouteValues);
            if (string.IsNullOrEmpty(destinationUrl))
            {
                throw new InvalidOperationException(Resources.NoRoutesMatched);
            }

            context.HttpContext.Response.Redirect(destinationUrl, Permanent);
        }

        private IUrlHelper GetUrlHelper(ActionContext context)
        {
            return UrlHelper ?? context.HttpContext.RequestServices.GetRequiredService<IUrlHelper>();
        }
    }
}
