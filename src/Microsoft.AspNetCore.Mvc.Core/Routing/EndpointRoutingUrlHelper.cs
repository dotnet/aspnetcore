// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
        private readonly IEndpointFinder<RouteValuesAddress> _routeValuesBasedEndpointFinder;

        /// <summary>
        /// Initializes a new instance of the <see cref="EndpointRoutingUrlHelper"/> class using the specified
        /// <paramref name="actionContext"/>.
        /// </summary>
        /// <param name="actionContext">The <see cref="Mvc.ActionContext"/> for the current request.</param>
        /// <param name="routeValuesBasedEndpointFinder">
        /// The <see cref="IEndpointFinder{T}"/> which finds endpoints by required route values.
        /// </param>
        /// <param name="linkGenerator">The <see cref="LinkGenerator"/> used to generate the link.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        public EndpointRoutingUrlHelper(
            ActionContext actionContext,
            IEndpointFinder<RouteValuesAddress> routeValuesBasedEndpointFinder,
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
            _routeValuesBasedEndpointFinder = routeValuesBasedEndpointFinder;
            _logger = logger;
        }

        /// <inheritdoc />
        public override string Action(UrlActionContext urlActionContext)
        {
            if (urlActionContext == null)
            {
                throw new ArgumentNullException(nameof(urlActionContext));
            }

            var valuesDictionary = GetValuesDictionary(urlActionContext.Values);

            if (urlActionContext.Action == null)
            {
                if (!valuesDictionary.ContainsKey("action") &&
                    AmbientValues.TryGetValue("action", out var action))
                {
                    valuesDictionary["action"] = action;
                }
            }
            else
            {
                valuesDictionary["action"] = urlActionContext.Action;
            }

            if (urlActionContext.Controller == null)
            {
                if (!valuesDictionary.ContainsKey("controller") &&
                    AmbientValues.TryGetValue("controller", out var controller))
                {
                    valuesDictionary["controller"] = controller;
                }
            }
            else
            {
                valuesDictionary["controller"] = urlActionContext.Controller;
            }

            var endpoints = _routeValuesBasedEndpointFinder.FindEndpoints(
                new RouteValuesAddress()
                {
                    ExplicitValues = valuesDictionary,
                    AmbientValues = AmbientValues
                });

            var successfullyGeneratedLink = _linkGenerator.TryGetLink(
                new LinkGeneratorContext
                {
                    HttpContext = ActionContext.HttpContext,
                    Endpoints = endpoints,
                    ExplicitValues = valuesDictionary,
                    AmbientValues = AmbientValues
                },
                out var link);

            if (!successfullyGeneratedLink)
            {
                //TODO: log here

                return null;
            }

            return GenerateUrl(urlActionContext.Protocol, urlActionContext.Host, link, urlActionContext.Fragment);
        }

        /// <inheritdoc />
        public override string RouteUrl(UrlRouteContext routeContext)
        {
            if (routeContext == null)
            {
                throw new ArgumentNullException(nameof(routeContext));
            }

            var valuesDictionary = routeContext.Values as RouteValueDictionary ?? GetValuesDictionary(routeContext.Values);

            var endpoints = _routeValuesBasedEndpointFinder.FindEndpoints(
                new RouteValuesAddress()
                {
                    RouteName = routeContext.RouteName,
                    ExplicitValues = valuesDictionary,
                    AmbientValues = AmbientValues
                });

            var successfullyGeneratedLink = _linkGenerator.TryGetLink(
                new LinkGeneratorContext
                {
                    HttpContext = ActionContext.HttpContext,
                    Endpoints = endpoints,
                    ExplicitValues = valuesDictionary,
                    AmbientValues = AmbientValues
                },
                out var link);

            if (!successfullyGeneratedLink)
            {
                return null;
            }

            return GenerateUrl(routeContext.Protocol, routeContext.Host, link, routeContext.Fragment);
        }
    }
}