// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Routing.Template
{
    public class TemplateRoute : IRoute
    {
        private readonly IDictionary<string, object> _defaults;
        private readonly IRouteEndpoint _endpoint;
        private readonly Template _parsedTemplate;
        private readonly string _routeTemplate;
        private readonly TemplateMatcher _matcher;
        private readonly TemplateBinder _binder;

        public TemplateRoute(IRouteEndpoint endpoint, string routeTemplate)
            : this(endpoint, routeTemplate, null)
        {
        }

        public TemplateRoute(IRouteEndpoint endpoint, string routeTemplate, IDictionary<string, object> defaults)
        {
            if (endpoint == null)
            {
                throw new ArgumentNullException("endpoint");
            }

            _endpoint = endpoint;
            _routeTemplate = routeTemplate ?? string.Empty;
            _defaults = defaults ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            // The parser will throw for invalid routes.
            _parsedTemplate = TemplateParser.Parse(RouteTemplate);

            _matcher = new TemplateMatcher(_parsedTemplate);
            _binder = new TemplateBinder(_parsedTemplate);
        }

        public IDictionary<string, object> Defaults
        {
            get { return _defaults; }
        }

        public IRouteEndpoint Endpoint
        {
            get { return _endpoint; }
        }

        public string RouteTemplate
        {
            get { return _routeTemplate; }
        }

        public virtual RouteMatch Match(RouteContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            var requestPath = context.RequestPath;
            if (!string.IsNullOrEmpty(requestPath) && requestPath[0] == '/')
            {
                requestPath = requestPath.Substring(1);
            }

            var values = _matcher.Match(requestPath, _defaults);
            if (values == null)
            {
                // If we got back a null value set, that means the URI did not match
                return null;
            }
            else
            {
                return new RouteMatch(_endpoint, values);
            }
        }

        public RouteBindResult Bind(RouteBindContext context)
        {
            var path = _binder.Bind(_defaults, context.AmbientValues, context.Values);
            return path == null ? null : new RouteBindResult(path);
        }
    }
}
