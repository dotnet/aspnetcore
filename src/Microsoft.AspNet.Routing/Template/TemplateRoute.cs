// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Routing.Template
{
    public class TemplateRoute : INamedRouter
    {
        private readonly IReadOnlyDictionary<string, IRouteConstraint> _constraints;
        private readonly IReadOnlyDictionary<string, object> _dataTokens;
        private readonly IReadOnlyDictionary<string, object> _defaults;
        private readonly IRouter _target;
        private readonly RouteTemplate _parsedTemplate;
        private readonly string _routeTemplate;
        private readonly TemplateMatcher _matcher;
        private readonly TemplateBinder _binder;
        private ILogger _logger;
        private ILogger _constraintLogger;

        public TemplateRoute(
            IRouter target,
            string routeTemplate,
            IInlineConstraintResolver inlineConstraintResolver)
            : this(
                target,
                routeTemplate,
                defaults: null,
                constraints: null,
                dataTokens: null,
                inlineConstraintResolver: inlineConstraintResolver)
        {
        }

        public TemplateRoute(
            IRouter target,
            string routeTemplate,
            IDictionary<string, object> defaults,
            IDictionary<string, object> constraints,
            IDictionary<string, object> dataTokens,
            IInlineConstraintResolver inlineConstraintResolver)
            : this(target, null, routeTemplate, defaults, constraints, dataTokens, inlineConstraintResolver)
        {
        }

        public TemplateRoute(
            IRouter target,
            string routeName,
            string routeTemplate,
            IDictionary<string, object> defaults,
            IDictionary<string, object> constraints,
            IDictionary<string, object> dataTokens,
            IInlineConstraintResolver inlineConstraintResolver)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            _target = target;
            _routeTemplate = routeTemplate ?? string.Empty;
            Name = routeName;

            _dataTokens = dataTokens == null ? RouteValueDictionary.Empty : new RouteValueDictionary(dataTokens);

            // Data we parse from the template will be used to fill in the rest of the constraints or
            // defaults. The parser will throw for invalid routes.
            _parsedTemplate = TemplateParser.Parse(RouteTemplate);

            _constraints = GetConstraints(inlineConstraintResolver, RouteTemplate, _parsedTemplate, constraints);
            _defaults = GetDefaults(_parsedTemplate, defaults);

            _matcher = new TemplateMatcher(_parsedTemplate, Defaults);
            _binder = new TemplateBinder(_parsedTemplate, Defaults);
        }

        public string Name { get; private set; }

        public IReadOnlyDictionary<string, object> Defaults
        {
            get { return _defaults; }
        }

        public IReadOnlyDictionary<string, object> DataTokens
        {
            get { return _dataTokens; }
        }

        public RouteTemplate ParsedTemplate
        {
            get { return _parsedTemplate; }
        }

        public string RouteTemplate
        {
            get { return _routeTemplate; }
        }

        public IReadOnlyDictionary<string, IRouteConstraint> Constraints
        {
            get { return _constraints; }
        }

        public async virtual Task RouteAsync(RouteContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            EnsureLoggers(context.HttpContext);

            var requestPath = context.HttpContext.Request.Path;
            var values = _matcher.Match(requestPath);

            if (values == null)
            {
                // If we got back a null value set, that means the URI did not match
                return;
            }

            var oldRouteData = context.RouteData;

            var newRouteData = new RouteData(oldRouteData);

            // Perf: Avoid accessing data tokens if you don't need to write to it, these dictionaries are all
            // created lazily.
            if (_dataTokens.Count > 0)
            {
                MergeValues(newRouteData.DataTokens, _dataTokens);
            }

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
                return;
            }

            _logger.LogVerbose(
                "Request successfully matched the route with name '{RouteName}' and template '{RouteTemplate}'.",
                Name,
                RouteTemplate);

            try
            {
                context.RouteData = newRouteData;

                await _target.RouteAsync(context);
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

        public virtual VirtualPathData GetVirtualPath(VirtualPathContext context)
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

            var pathData = _target.GetVirtualPath(childContext);
            if (pathData != null)
            {
                // If the target generates a value then that can short circuit.
                return pathData;
            }

            // If we can produce a value go ahead and do it, the caller can check context.IsBound
            // to see if the values were validated.

            // When we still cannot produce a value, this should return null.
            var tempPath = _binder.BindValues(values.AcceptedValues);
            if (tempPath == null)
            {
                return null;
            }

            pathData = new VirtualPathData(this, tempPath);
            if (DataTokens != null)
            {
                foreach (var dataToken in DataTokens)
                {
                    pathData.DataTokens.Add(dataToken.Key, dataToken.Value);
                }
            }

            context.IsBound = childContext.IsBound;

            return pathData;
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

        private static IReadOnlyDictionary<string, IRouteConstraint> GetConstraints(
            IInlineConstraintResolver inlineConstraintResolver,
            string template,
            RouteTemplate parsedTemplate,
            IDictionary<string, object> constraints)
        {
            var constraintBuilder = new RouteConstraintBuilder(inlineConstraintResolver, template);

            if (constraints != null)
            {
                foreach (var kvp in constraints)
                {
                    constraintBuilder.AddConstraint(kvp.Key, kvp.Value);
                }
            }

            foreach (var parameter in parsedTemplate.Parameters)
            {
                if (parameter.IsOptional)
                {
                    constraintBuilder.SetOptional(parameter.Name);
                }

                foreach (var inlineConstraint in parameter.InlineConstraints)
                {
                    constraintBuilder.AddResolvedConstraint(parameter.Name, inlineConstraint.Constraint);
                }
            }

            return constraintBuilder.Build();
        }

        private static RouteValueDictionary GetDefaults(
            RouteTemplate parsedTemplate,
            IDictionary<string, object> defaults)
        {
            // Do not use RouteValueDictionary.Empty for defaults, it might be modified inside
            // UpdateInlineDefaultValuesAndConstraints()
            var result = defaults == null ? new RouteValueDictionary() : new RouteValueDictionary(defaults);

            foreach (var parameter in parsedTemplate.Parameters)
            {
                if (parameter.DefaultValue != null)
                {
                    if (result.ContainsKey(parameter.Name))
                    {
                        throw new InvalidOperationException(
                          Resources.FormatTemplateRoute_CannotHaveDefaultValueSpecifiedInlineAndExplicitly(
                              parameter.Name));
                    }
                    else
                    {
                        result.Add(parameter.Name, parameter.DefaultValue);
                    }
                }
            }

            return result;
        }

        private static void MergeValues(
            RouteValueDictionary destination,
            RouteValueDictionary values)
        {
            foreach (var kvp in values)
            {
                // This will replace the original value for the specified key.
                // Values from the matched route will take preference over previous
                // data in the route context.
                destination[kvp.Key] = kvp.Value;
            }
        }

        // Needed because IDictionary<> is not an IReadOnlyDictionary<>
        private static void MergeValues(
            IDictionary<string, object> destination,
            IReadOnlyDictionary<string, object> values)
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
                _logger = factory.CreateLogger<TemplateRoute>();
                _constraintLogger = factory.CreateLogger(typeof(RouteConstraintMatcher).FullName);
            }
        }

        public override string ToString()
        {
            return _routeTemplate;
        }
    }
}
