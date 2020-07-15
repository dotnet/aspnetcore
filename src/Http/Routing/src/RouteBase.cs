// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Logging;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Routing
{
    public abstract class RouteBase : IRouter, INamedRouter
    {
        private readonly object _loggersLock = new object();

        private TemplateMatcher? _matcher;
        private TemplateBinder? _binder;
        private ILogger? _logger;
        private ILogger? _constraintLogger;

        public RouteBase(
            string? template,
            string? name,
            IInlineConstraintResolver constraintResolver,
            RouteValueDictionary? defaults,
            IDictionary<string, object>? constraints,
            RouteValueDictionary? dataTokens)
        {
            if (constraintResolver == null)
            {
                throw new ArgumentNullException(nameof(constraintResolver));
            }

            template = template ?? string.Empty;
            Name = name;
            ConstraintResolver = constraintResolver;
            DataTokens = dataTokens ?? new RouteValueDictionary();

            try
            {
                // Data we parse from the template will be used to fill in the rest of the constraints or
                // defaults. The parser will throw for invalid routes.
                ParsedTemplate = TemplateParser.Parse(template);

                Constraints = GetConstraints(constraintResolver, ParsedTemplate, constraints);
                Defaults = GetDefaults(ParsedTemplate, defaults);
            }
            catch (Exception exception)
            {
                throw new RouteCreationException(Resources.FormatTemplateRoute_Exception(name, template), exception);
            }
        }

        public virtual IDictionary<string, IRouteConstraint> Constraints { get; protected set; }

        protected virtual IInlineConstraintResolver ConstraintResolver { get; set; }

        public virtual RouteValueDictionary DataTokens { get; protected set; }

        public virtual RouteValueDictionary Defaults { get; protected set; }

        public virtual string? Name { get; protected set; }

        public virtual RouteTemplate ParsedTemplate { get; protected set; }

        protected abstract Task OnRouteMatched(RouteContext context);

        protected abstract VirtualPathData? OnVirtualPathGenerated(VirtualPathContext context);

        /// <inheritdoc />
        public virtual Task RouteAsync(RouteContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            EnsureMatcher();
            EnsureLoggers(context.HttpContext);

            var requestPath = context.HttpContext.Request.Path;

            if (!_matcher.TryMatch(requestPath, context.RouteData.Values))
            {
                // If we got back a null value set, that means the URI did not match
                return Task.CompletedTask;
            }

            // Perf: Avoid accessing dictionaries if you don't need to write to them, these dictionaries are all
            // created lazily.
            if (DataTokens.Count > 0)
            {
                MergeValues(context.RouteData.DataTokens, DataTokens);
            }

            if (!RouteConstraintMatcher.Match(
                Constraints,
                context.RouteData.Values,
                context.HttpContext,
                this,
                RouteDirection.IncomingRequest,
                _constraintLogger))
            {
                return Task.CompletedTask;
            }
            _logger.RequestMatchedRoute(Name!, ParsedTemplate.TemplateText);

            return OnRouteMatched(context);
        }

        /// <inheritdoc />
        public virtual VirtualPathData? GetVirtualPath(VirtualPathContext context)
        {
            EnsureBinder(context.HttpContext);
            EnsureLoggers(context.HttpContext);

            var values = _binder.GetValues(context.AmbientValues, context.Values);
            if (values == null)
            {
                // We're missing one of the required values for this route.
                return null;
            }

            if (!RouteConstraintMatcher.Match(
                Constraints,
                values.CombinedValues,
                context.HttpContext,
                this,
                RouteDirection.UrlGeneration,
                _constraintLogger))
            {
                return null;
            }

            context.Values = values.CombinedValues;

            var pathData = OnVirtualPathGenerated(context);
            if (pathData != null)
            {
                // If the target generates a value then that can short circuit.
                return pathData;
            }

            // If we can produce a value go ahead and do it, the caller can check context.IsBound
            // to see if the values were validated.

            // When we still cannot produce a value, this should return null.
            var virtualPath = _binder.BindValues(values.AcceptedValues);
            if (virtualPath == null)
            {
                return null;
            }

            pathData = new VirtualPathData(this, virtualPath);
            if (DataTokens != null)
            {
                foreach (var dataToken in DataTokens)
                {
                    pathData.DataTokens.Add(dataToken.Key, dataToken.Value);
                }
            }

            return pathData;
        }

        protected static IDictionary<string, IRouteConstraint> GetConstraints(
            IInlineConstraintResolver inlineConstraintResolver,
            RouteTemplate parsedTemplate,
            IDictionary<string, object>? constraints)
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

        protected static RouteValueDictionary GetDefaults(
            RouteTemplate parsedTemplate,
            RouteValueDictionary? defaults)
        {
            var result = defaults == null ? new RouteValueDictionary() : new RouteValueDictionary(defaults);

            foreach (var parameter in parsedTemplate.Parameters)
            {
                if (parameter.DefaultValue != null)
                {
#if RVD_TryAdd
                    if (!result.TryAdd(parameter.Name, parameter.DefaultValue))
                    {
                        throw new InvalidOperationException(
                          Resources.FormatTemplateRoute_CannotHaveDefaultValueSpecifiedInlineAndExplicitly(
                              parameter.Name));
                    }
#else
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
#endif
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

        [MemberNotNull(nameof(_binder))]
        private void EnsureBinder(HttpContext context)
        {
            if (_binder == null)
            {
                var binderFactory = context.RequestServices.GetRequiredService<TemplateBinderFactory>();
                _binder = binderFactory.Create(ParsedTemplate, Defaults);
            }
        }

        [MemberNotNull(nameof(_logger), nameof(_constraintLogger))]
        private void EnsureLoggers(HttpContext context)
        {
            // We check first using the _logger to see if the loggers have been initialized to avoid taking
            // the lock on the most common case.
            if (_logger == null)
            {
                // We need to lock here to ensure that _constraintLogger and _logger get initialized atomically.
                lock (_loggersLock)
                {
                    if (_logger != null)
                    {
                        // Multiple threads might have tried to acquire the lock at the same time. Technically
                        // there is nothing wrong if things get reinitialized by a second thread, but its easy
                        // to prevent by just rechecking and returning here.
                        Debug.Assert(_constraintLogger != null);

                        return;
                    }

                    var factory = context.RequestServices.GetRequiredService<ILoggerFactory>();
                    _constraintLogger = factory.CreateLogger(typeof(RouteConstraintMatcher).FullName);
                    _logger = factory.CreateLogger(typeof(RouteBase).FullName);
                }

            }

            Debug.Assert(_constraintLogger != null);
        }

        [MemberNotNull(nameof(_matcher))]
        private void EnsureMatcher()
        {
            if (_matcher == null)
            {
                _matcher = new TemplateMatcher(ParsedTemplate, Defaults);
            }
        }

        public override string ToString()
        {
            return ParsedTemplate.TemplateText;
        }
    }
}
