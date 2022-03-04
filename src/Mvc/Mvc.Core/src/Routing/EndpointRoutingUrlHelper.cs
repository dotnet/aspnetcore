// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Routing
{
    /// <summary>
    /// An implementation of <see cref="IUrlHelper"/> that uses <see cref="LinkGenerator"/> to build URLs 
    /// for ASP.NET MVC within an application.
    /// </summary>
    internal class EndpointRoutingUrlHelper : UrlHelperBase
    {
        private readonly ILogger<EndpointRoutingUrlHelper> _logger;
        private readonly LinkGenerator _linkGenerator;

        /// <summary>
        /// Initializes a new instance of the <see cref="EndpointRoutingUrlHelper"/> class using the specified
        /// <paramref name="actionContext"/>.
        /// </summary>
        /// <param name="actionContext">The <see cref="Mvc.ActionContext"/> for the current request.</param>
        /// <param name="linkGenerator">The <see cref="LinkGenerator"/> used to generate the link.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        public EndpointRoutingUrlHelper(
            ActionContext actionContext,
            LinkGenerator linkGenerator,
            ILogger<EndpointRoutingUrlHelper> logger)
            : base(actionContext)
        {
            if (linkGenerator == null)
            {
                throw new ArgumentNullException(nameof(linkGenerator));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _linkGenerator = linkGenerator;
            _logger = logger;
        }

        /// <inheritdoc />
        public override string Action(UrlActionContext urlActionContext)
        {
            if (urlActionContext == null)
            {
                throw new ArgumentNullException(nameof(urlActionContext));
            }

            var values = GetValuesDictionary(urlActionContext.Values);

            if (urlActionContext.Action == null)
            {
                if (!values.ContainsKey("action") &&
                    AmbientValues.TryGetValue("action", out var action))
                {
                    values["action"] = action;
                }
            }
            else
            {
                values["action"] = urlActionContext.Action;
            }

            if (urlActionContext.Controller == null)
            {
                if (!values.ContainsKey("controller") &&
                    AmbientValues.TryGetValue("controller", out var controller))
                {
                    values["controller"] = controller;
                }
            }
            else
            {
                values["controller"] = urlActionContext.Controller;
            }


            var path = _linkGenerator.GetPathByRouteValues(
                ActionContext.HttpContext,
                routeName: null,
                values,
                fragment: new FragmentString(urlActionContext.Fragment == null ? null : "#" + urlActionContext.Fragment));
            return GenerateUrl(urlActionContext.Protocol, urlActionContext.Host, path);
        }

        /// <inheritdoc />
        public override string RouteUrl(UrlRouteContext routeContext)
        {
            if (routeContext == null)
            {
                throw new ArgumentNullException(nameof(routeContext));
            }

            var path = _linkGenerator.GetPathByRouteValues(
                ActionContext.HttpContext,
                routeContext.RouteName,
                routeContext.Values,
                fragment: new FragmentString(routeContext.Fragment == null ? null : "#" + routeContext.Fragment));
            return GenerateUrl(routeContext.Protocol, routeContext.Host, path);
        }
    }
}