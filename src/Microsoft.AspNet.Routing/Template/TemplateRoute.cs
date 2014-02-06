// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Routing.Template
{
    /// <summary>
    /// Route class for self-host (i.e. hosted outside of ASP.NET). This class is mostly the
    /// same as the System.Web.Routing.Route implementation.
    /// This class has the same URL matching functionality as System.Web.Routing.Route. However,
    /// in order for this route to match when generating URLs, a special "httproute" key must be
    /// specified when generating the URL.
    /// </summary>
    public class TemplateRoute : IRoute
    {
        /// <summary>
        /// Key used to signify that a route URL generation request should include HTTP routes (e.g. Web API).
        /// If this key is not specified then no HTTP routes will match.
        /// </summary>
        public static readonly string HttpRouteKey = "httproute";

        private string _routeTemplate;
        private IDictionary<string, object> _defaults;
        private IDictionary<string, object> _constraints;
        private IDictionary<string, object> _dataTokens;

        public TemplateRoute()
            : this(routeTemplate: null, defaults: null, constraints: null, dataTokens: null, handler: null)
        {
        }

        public TemplateRoute(string routeTemplate)
            : this(routeTemplate, defaults: null, constraints: null, dataTokens: null, handler: null)
        {
        }

        public TemplateRoute(string routeTemplate, IDictionary<string, object> defaults)
            : this(routeTemplate, defaults, constraints: null, dataTokens: null, handler: null)
        {
        }

        public TemplateRoute(string routeTemplate, IDictionary<string, object> defaults, IDictionary<string, object> constraints)
            : this(routeTemplate, defaults, constraints, dataTokens: null, handler: null)
        {
        }

        public TemplateRoute(string routeTemplate, IDictionary<string, object> defaults, IDictionary<string, object> constraints, IDictionary<string, object> dataTokens)
            : this(routeTemplate, defaults, constraints, dataTokens, handler: null)
        {
        }

        public TemplateRoute(string routeTemplate, IDictionary<string, object> defaults, IDictionary<string, object> constraints, IDictionary<string, object> dataTokens, IRouteEndpoint handler)
        {
            _routeTemplate = routeTemplate == null ? String.Empty : routeTemplate;
            _defaults = defaults ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            _constraints = constraints ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            _dataTokens = dataTokens ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            Handler = handler;

            // The parser will throw for invalid routes.
            ParsedRoute = TemplateRouteParser.Parse(RouteTemplate);
        }

        public IDictionary<string, object> Defaults
        {
            get { return _defaults; }
        }

        public IDictionary<string, object> Constraints
        {
            get { return _constraints; }
        }

        public IDictionary<string, object> DataTokens
        {
            get { return _dataTokens; }
        }

        public IRouteEndpoint Handler { get; private set; }

        public string RouteTemplate
        {
            get { return _routeTemplate; }
        }

        internal TemplateParsedRoute ParsedRoute { get; private set; }

        public virtual RouteMatch GetRouteData(HttpContext request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            var requestPath = request.Request.Path.Value;
            if (!String.IsNullOrEmpty(requestPath) && requestPath[0] == '/')
            {
                requestPath = requestPath.Substring(1);
            }

            IDictionary<string, object> values = ParsedRoute.Match(requestPath, _defaults);
            if (values == null)
            {
                // If we got back a null value set, that means the URI did not match
                return null;
            }

            // Validate the values
            if (!ProcessConstraints(request, values, RouteDirection.UriResolution))
            {
                return null;
            }

            return new RouteMatch(null, values);
        }

        /// <summary>
        /// Attempt to generate a URI that represents the values passed in based on current
        /// values from the <see cref="HttpRouteData"/> and new values using the specified <see cref="TemplateRoute"/>.
        /// </summary>
        /// <param name="request">The HTTP request message.</param>
        /// <param name="values">The route values.</param>
        /// <returns>A <see cref="VirtualPathData"/> instance or null if URI cannot be generated.</returns>
        public virtual IVirtualPathData GetVirtualPath(HttpContext request, IDictionary<string, object> values)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            // Only perform URL generation if the "httproute" key was specified. This allows these
            // routes to be ignored when a regular MVC app tries to generate URLs. Without this special
            // key an HTTP route used for Web API would normally take over almost all the routes in a
            // typical app.
            if (values != null && !values.Keys.Contains(HttpRouteKey, StringComparer.OrdinalIgnoreCase))
            {
                return null;
            }
            // Remove the value from the collection so that it doesn't affect the generated URL
            var newValues = GetRouteDictionaryWithoutHttpRouteKey(values);

            IRouteValues routeData = request.GetFeature<IRouteValues>();
            IDictionary<string, object> requestValues = routeData == null ? null : routeData.Values;

            BoundRouteTemplate result = ParsedRoute.Bind(requestValues, newValues, _defaults, _constraints);
            if (result == null)
            {
                return null;
            }

            // Assert that the route matches the validation rules
            if (!ProcessConstraints(request, result.Values, RouteDirection.UriGeneration))
            {
                return null;
            }

            return new VirtualPathData(this, result.BoundTemplate);
        }

        private static IDictionary<string, object> GetRouteDictionaryWithoutHttpRouteKey(IDictionary<string, object> routeValues)
        {
            var newRouteValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (routeValues != null)
            {
                foreach (var routeValue in routeValues)
                {
                    if (!String.Equals(routeValue.Key, HttpRouteKey, StringComparison.OrdinalIgnoreCase))
                    {
                        newRouteValues.Add(routeValue.Key, routeValue.Value);
                    }
                }
            }
            return newRouteValues;
        }

        protected virtual bool ProcessConstraint(HttpContext request, object constraint, string parameterName, IDictionary<string, object> values, RouteDirection routeDirection)
        {
            ITemplateRouteConstraint customConstraint = constraint as ITemplateRouteConstraint;
            if (customConstraint != null)
            {
                return customConstraint.Match(request, this, parameterName, values, routeDirection);
            }

            // If there was no custom constraint, then treat the constraint as a string which represents a Regex.
            string constraintsRule = constraint as string;
            if (constraintsRule == null)
            {
                throw new InvalidOperationException(String.Format(
                    CultureInfo.CurrentCulture,
                    Resources.TemplateRoute_ValidationMustBeStringOrCustomConstraint,
                    parameterName,
                    RouteTemplate,
                    typeof(ITemplateRouteConstraint).Name));
            }

            object parameterValue;
            values.TryGetValue(parameterName, out parameterValue);
            string parameterValueString = Convert.ToString(parameterValue, CultureInfo.InvariantCulture);
            string constraintsRegEx = "^(" + constraintsRule + ")$";
            return Regex.IsMatch(parameterValueString, constraintsRegEx, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        }

        private bool ProcessConstraints(HttpContext request, IDictionary<string, object> values, RouteDirection routeDirection)
        {
            if (Constraints != null)
            {
                foreach (KeyValuePair<string, object> constraintsItem in Constraints)
                {
                    if (!ProcessConstraint(request, constraintsItem.Value, constraintsItem.Key, values, routeDirection))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public RouteMatch Match(RouteContext context)
        {
            throw new NotImplementedException();
        }
    }
}
