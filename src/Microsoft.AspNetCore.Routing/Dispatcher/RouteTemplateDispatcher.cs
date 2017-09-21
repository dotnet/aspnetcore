// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Dispatcher;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Template;

namespace Microsoft.AspNetCore.Routing.Dispatcher
{
    public class RouteTemplateDispatcher : DispatcherBase
    {
        private readonly IDictionary<string, IRouteConstraint> _constraints;
        private readonly RouteValueDictionary _defaults;
        private readonly TemplateMatcher _matcher;
        private readonly RouteTemplate _parsedTemplate;

        public RouteTemplateDispatcher(
            string routeTemplate,
            IInlineConstraintResolver constraintResolver)
            : this(routeTemplate, constraintResolver, null, null)
        {
        }

        public RouteTemplateDispatcher(
            string routeTemplate,
            IInlineConstraintResolver constraintResolver,
            RouteValueDictionary defaults)
            : this(routeTemplate, constraintResolver, defaults, null)
        {
        }

        public RouteTemplateDispatcher(
            string routeTemplate,
            IInlineConstraintResolver constraintResolver,
            RouteValueDictionary defaults,
            IDictionary<string, object> constraints)
        {
            if (routeTemplate == null)
            {
                throw new ArgumentNullException(nameof(routeTemplate));
            }

            if (constraintResolver == null)
            {
                throw new ArgumentNullException(nameof(constraintResolver));
            }

            RouteTemplate = routeTemplate;

            try
            {
                // Data we parse from the template will be used to fill in the rest of the constraints or
                // defaults. The parser will throw for invalid routes.
                _parsedTemplate = TemplateParser.Parse(routeTemplate);

                _constraints = GetConstraints(constraintResolver, _parsedTemplate, constraints);
                _defaults = GetDefaults(_parsedTemplate, defaults);
            }
            catch (Exception exception)
            {
                throw new RouteCreationException(Resources.FormatTemplateRoute_Exception(string.Empty, routeTemplate), exception);
            }

            _matcher = new TemplateMatcher(_parsedTemplate, _defaults);
        }

        public string RouteTemplate { get; }

        protected override Task<bool> TryMatchAsync(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            var feature = httpContext.Features.Get<IDispatcherFeature>();
            feature.Values = feature.Values ?? new RouteValueDictionary();

            if (!_matcher.TryMatch(httpContext.Request.Path, (RouteValueDictionary)feature.Values))
            {
                // If we got back a null value set, that means the URI did not match
                return Task.FromResult(false);
            }

            foreach (var kvp in _constraints)
            {
                var constraint = kvp.Value;
                if (!constraint.Match(httpContext, null, kvp.Key, (RouteValueDictionary)feature.Values, RouteDirection.IncomingRequest))
                {
                    return Task.FromResult(false);
                }
            }

            return Task.FromResult(true);
        }

        private static IDictionary<string, IRouteConstraint> GetConstraints(
            IInlineConstraintResolver inlineConstraintResolver,
            RouteTemplate parsedTemplate,
            IDictionary<string, object> constraints)
        {
            var constraintBuilder = new RouteConstraintBuilder(inlineConstraintResolver, parsedTemplate.TemplateText);

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
            RouteValueDictionary defaults)
        {
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
    }
}
