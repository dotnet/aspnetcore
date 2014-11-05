// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing.Logging;
using Microsoft.AspNet.Routing.Logging.Internal;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Routing.Template
{
    public class TemplateRoute : INamedRouter
    {
        private readonly IDictionary<string, object> _defaults;
        private readonly IDictionary<string, IRouteConstraint> _constraints;
        private readonly IDictionary<string, object> _dataTokens;
        private readonly IRouter _target;
        private readonly RouteTemplate _parsedTemplate;
        private readonly string _routeTemplate;
        private readonly TemplateMatcher _matcher;
        private readonly TemplateBinder _binder;
        private ILogger _logger;
        private ILogger _constraintLogger;

        public TemplateRoute(IRouter target, string routeTemplate, IInlineConstraintResolver inlineConstraintResolver)
                        : this(target,
                               routeTemplate,
                               defaults: null,
                               constraints: null,
                               dataTokens: null,
                               inlineConstraintResolver: inlineConstraintResolver)
        {
        }

        public TemplateRoute([NotNull] IRouter target,
                             string routeTemplate,
                             IDictionary<string, object> defaults,
                             IDictionary<string, object> constraints,
                             IDictionary<string, object> dataTokens,
                             IInlineConstraintResolver inlineConstraintResolver)
            : this(target, null, routeTemplate, defaults, constraints, dataTokens, inlineConstraintResolver)
        {
        }

        public TemplateRoute([NotNull] IRouter target,
                             string routeName,
                             string routeTemplate,
                             IDictionary<string, object> defaults,
                             IDictionary<string, object> constraints,
                             IDictionary<string, object> dataTokens,
                             IInlineConstraintResolver inlineConstraintResolver)
        {
            _target = target;
            _routeTemplate = routeTemplate ?? string.Empty;
            Name = routeName;
            _defaults = defaults ?? new RouteValueDictionary();
            _constraints = RouteConstraintBuilder.BuildConstraints(constraints, _routeTemplate) ??
                                                            new Dictionary<string, IRouteConstraint>();
            _dataTokens = dataTokens ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

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

        public IDictionary<string, object> DataTokens
        {
            get { return _dataTokens; }
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
            EnsureLoggers(context.HttpContext);
            using (_logger.BeginScope("TemplateRoute.RouteAsync"))
            {
                var requestPath = context.HttpContext.Request.Path.Value;

                if (!string.IsNullOrEmpty(requestPath) && requestPath[0] == '/')
                {
                    requestPath = requestPath.Substring(1);
                }

                var values = _matcher.Match(requestPath, Defaults);

                if (values == null)
                {
                    if (_logger.IsEnabled(LogLevel.Verbose))
                    {
                        _logger.WriteValues(CreateRouteAsyncValues(
                            requestPath,
                            context.RouteData.Values,
                            matchedValues: false,
                            matchedConstraints: false,
                            handled: context.IsHandled));
                    }

                    // If we got back a null value set, that means the URI did not match
                    return;
                }

                var oldRouteData = context.RouteData;

                var newRouteData = new RouteData(oldRouteData);
                MergeValues(newRouteData.DataTokens, _dataTokens);
                newRouteData.Routers.Add(_target);
                MergeValues(newRouteData.Values, values);

                if (!RouteConstraintMatcher.Match(
                    Constraints,
                    newRouteData.Values,
                    context.HttpContext,
                    this,
                    RouteDirection.IncomingRequest,
                    _constraintLogger))
                {
                    if (_logger.IsEnabled(LogLevel.Verbose))
                    {
                        _logger.WriteValues(CreateRouteAsyncValues(
                            requestPath,
                            newRouteData.Values,
                            matchedValues: true,
                            matchedConstraints: false,
                            handled: context.IsHandled));
                    }

                    return;
                }

                try
                {
                    context.RouteData = newRouteData;

                    await _target.RouteAsync(context);

                    if (_logger.IsEnabled(LogLevel.Verbose))
                    {
                        _logger.WriteValues(CreateRouteAsyncValues(
                            requestPath,
                            newRouteData.Values,
                            matchedValues: true,
                            matchedConstraints: true,
                            handled: context.IsHandled));
                    }
                }
                finally
                {
                    // Restore the original values to prevent polluting the route data.
                    if (!context.IsHandled)
                    {
                        context.RouteData = oldRouteData;
                    }
                }
            }
        }

        public virtual string GetVirtualPath(VirtualPathContext context)
        {
            var values = _binder.GetValues(context.AmbientValues, context.Values);
            if (values == null)
            {
                // We're missing one of the required values for this route.
                return null;
            }

            EnsureLoggers(context.Context);
            if (!RouteConstraintMatcher.Match(Constraints,
                                              values.CombinedValues,
                                              context.Context,
                                              this,
                                              RouteDirection.UrlGeneration,
                                              _constraintLogger))
            {
                return null;
            }

            // Validate that the target can accept these values.
            var childContext = CreateChildVirtualPathContext(context, values.AcceptedValues);
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

            path = _binder.BindValues(values.AcceptedValues);
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
                          Resources
                            .FormatTemplateRoute_CannotHaveDefaultValueSpecifiedInlineAndExplicitly(parameter.Name));
                    }
                    else
                    {
                        _defaults[parameter.Name] = parameter.DefaultValue;
                    }
                }
            }
        }

        private TemplateRouteRouteAsyncValues CreateRouteAsyncValues(
            string requestPath,
            IDictionary<string, object> producedValues,
            bool matchedValues,
            bool matchedConstraints,
            bool handled)
        {
            var values = new TemplateRouteRouteAsyncValues();
            values.Template = _routeTemplate;
            values.RequestPath = requestPath;
            values.DefaultValues = Defaults;
            values.ProducedValues = producedValues;
            values.Constraints = _constraints;
            values.Target = _target;
            values.MatchedTemplate = matchedValues;
            values.MatchedConstraints = matchedConstraints;
            values.Matched = matchedValues && matchedConstraints;
            values.Handled = handled;
            return values;
        }

        private static void MergeValues(
            IDictionary<string, object> destination,
            IDictionary<string, object> values)
        {
            foreach (var kvp in values)
            {
                // This will replace the original value for the specified key.
                // Values from the matched route will take preference over previous
                // data in the route context.
                destination[kvp.Key] = kvp.Value;
            }
        }

        private void EnsureLoggers(HttpContext context)
        {
            if (_logger == null)
            {
                var factory = context.RequestServices.GetRequiredService<ILoggerFactory>();
                _logger = factory.Create<TemplateRoute>();
                _constraintLogger = factory.Create(typeof(RouteConstraintMatcher).FullName);
            }
        }

        public override string ToString()
        {
            return _routeTemplate;
        }
    }
}
