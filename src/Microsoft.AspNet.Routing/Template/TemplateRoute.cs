// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Routing.Constraints;

namespace Microsoft.AspNet.Routing.Template
{
    public class TemplateRoute : INamedRouter
    {
        private readonly IDictionary<string, object> _defaults;
        private readonly IDictionary<string, IRouteConstraint> _constraints;
        private readonly IRouter _target;
        private readonly Template _parsedTemplate;
        private readonly string _routeTemplate;
        private readonly TemplateMatcher _matcher;
        private readonly TemplateBinder _binder;

        public TemplateRoute(IRouter target, string routeTemplate, IInlineConstraintResolver inlineConstraintResolver)
            : this(target, routeTemplate, null, null, inlineConstraintResolver)
        {
        }

        public TemplateRoute([NotNull] IRouter target,
                             string routeTemplate,
                             IDictionary<string, object> defaults,
                             IDictionary<string, object> constraints,
                             IInlineConstraintResolver inlineConstraintResolver)
            : this(target, null, routeTemplate, defaults, constraints, inlineConstraintResolver)
        {
        }

        public TemplateRoute([NotNull] IRouter target,
                             string routeName,
                             string routeTemplate,
                             IDictionary<string, object> defaults,
                             IDictionary<string, object> constraints,
                             IInlineConstraintResolver inlineConstraintResolver)
        {
            _target = target;
            _routeTemplate = routeTemplate ?? string.Empty;
            Name = routeName;
            _defaults = defaults ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            _constraints = RouteConstraintBuilder.BuildConstraints(constraints, _routeTemplate) ??
                                                            new Dictionary<string, IRouteConstraint>();

            // The parser will throw for invalid routes.
            _parsedTemplate = TemplateParser.Parse(RouteTemplate, inlineConstraintResolver);
            UpdateInlineDefaultValuesAndConstraints();

            _matcher = new TemplateMatcher(_parsedTemplate);
            _binder = new TemplateBinder(_parsedTemplate, _defaults);
        }

        public string Name { get; private set; }

        public IDictionary<string, object> Defaults
        {
            get { return _defaults; }
        }

        public string RouteTemplate
        {
            get { return _routeTemplate; }
        }

        public IDictionary<string, IRouteConstraint> Constraints
        {
            get { return _constraints; }
        }

        public async virtual Task RouteAsync([NotNull] RouteContext context)
        {
            var requestPath = context.RequestPath;
            if (!string.IsNullOrEmpty(requestPath) && requestPath[0] == '/')
            {
                requestPath = requestPath.Substring(1);
            }

            var values = _matcher.Match(requestPath, Defaults);
            if (values == null)
            {
                // If we got back a null value set, that means the URI did not match
                return;
            }
            else
            {
                // Not currently doing anything to clean this up if it's not a match. Consider hardening this.
                context.Values = values;

                if (RouteConstraintMatcher.Match(Constraints,
                                                 values,
                                                 context.HttpContext,
                                                 this,
                                                 RouteDirection.IncomingRequest))
                {
                    await _target.RouteAsync(context);
                }
            }
        }

        public string GetVirtualPath(VirtualPathContext context)
        {
            var values = _binder.GetAcceptedValues(context.AmbientValues, context.Values);
            if (values == null)
            {
                // We're missing one of the required values for this route.
                return null;
            }

            if (!RouteConstraintMatcher.Match(Constraints,
                                              values,
                                              context.Context,
                                              this,
                                              RouteDirection.UrlGeneration))
            {
                return null;
            }

            // Validate that the target can accept these values.
            var childContext = CreateChildVirtualPathContext(context, values);
            var path = _target.GetVirtualPath(childContext);
            if (path != null)
            {
                // If the target generates a value then that can short circuit.
                context.IsBound = true;
                return path;
            }
            else if (!childContext.IsBound)
            {
                // The target has rejected these values.
                return null;
            }

            path = _binder.BindValues(values);
            if (path != null)
            {
                context.IsBound = true;
            }

            return path;
        }

        private VirtualPathContext CreateChildVirtualPathContext(
            VirtualPathContext context,
            IDictionary<string, object> acceptedValues)
        {
            // We want to build the set of values that would be provided if this route were to generated
            // a link and then immediately match it. This includes all the accepted parameter values, and
            // the defaults. Accepted values that would go in the query string aren't included.
            var providedValues = new RouteValueDictionary();

            foreach (var parameter in _parsedTemplate.Parameters)
            {
                object value;
                if (acceptedValues.TryGetValue(parameter.Name, out value))
                {
                    providedValues.Add(parameter.Name, value);
                }
            }

            foreach (var kvp in _defaults)
            {
                if (!providedValues.ContainsKey(kvp.Key))
                {
                    providedValues.Add(kvp.Key, kvp.Value);
                }
            }

            return new VirtualPathContext(context.Context, context.AmbientValues, context.Values)
            {
                ProvidedValues = providedValues,
            };
        }

        private void UpdateInlineDefaultValuesAndConstraints()
        {
            foreach (var parameter in _parsedTemplate.Parameters)
            {
                if (parameter.InlineConstraint != null)
                {
                    IRouteConstraint constraint;
                    if (_constraints.TryGetValue(parameter.Name, out constraint))
                    {
                        _constraints[parameter.Name] =
                            new CompositeRouteConstraint(new[] { constraint, parameter.InlineConstraint });
                    }
                    else
                    {
                        _constraints[parameter.Name] = parameter.InlineConstraint;
                    }
                }

                if (parameter.DefaultValue != null)
                {
                    if (_defaults.ContainsKey(parameter.Name))
                    {
                        throw new InvalidOperationException(
                            Resources.
                             FormatTemplateRoute_CannotHaveDefaultValueSpecifiedInlineAndExplicitly(parameter.Name));
                    }
                    else
                    {
                        _defaults[parameter.Name] = parameter.DefaultValue;
                    }
                }
            }
        }
    }
}
