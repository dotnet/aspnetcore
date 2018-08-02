// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing.Matching
{
    internal class DefaultMatchProcessorFactory : MatchProcessorFactory
    {
        private readonly RouteOptions _options;
        private readonly IServiceProvider _serviceProvider;

        public DefaultMatchProcessorFactory(
            IOptions<RouteOptions> options,
            IServiceProvider serviceProvider)
        {
            _options = options.Value;
            _serviceProvider = serviceProvider;
        }

        public override MatchProcessor Create(string parameterName, IRouteConstraint value, bool optional)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return InitializeMatchProcessor(parameterName, optional, value, argument: null);
        }

        public override MatchProcessor Create(string parameterName, MatchProcessor value, bool optional)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return InitializeMatchProcessor(parameterName, optional, value, argument: null);
        }

        public override MatchProcessor Create(string parameterName, string value, bool optional)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            // Example:
            // {productId:regex(\d+)}
            //
            // ParameterName: productId
            // value: regex(\d+)
            // name: regex
            // argument: \d+
            (var name, var argument) = Parse(value);

            if (!_options.ConstraintMap.TryGetValue(name, out var type))
            {
                throw new InvalidOperationException(Resources.FormatRoutePattern_ConstraintReferenceNotFound(
                    name,
                    typeof(RouteOptions),
                    nameof(RouteOptions.ConstraintMap)));
            }

            if (typeof(MatchProcessor).IsAssignableFrom(type))
            {
                var matchProcessor = (MatchProcessor)_serviceProvider.GetRequiredService(type);
                return InitializeMatchProcessor(parameterName, optional, matchProcessor, argument);
            }

            if (typeof(IRouteConstraint).IsAssignableFrom(type))
            {
                var constraint = DefaultInlineConstraintResolver.CreateConstraint(type, argument);
                return InitializeMatchProcessor(parameterName, optional, constraint, argument);
            }

            var message = Resources.FormatRoutePattern_InvalidStringConstraintReference(
                type,
                name,
                typeof(IRouteConstraint),
                typeof(MatchProcessor));
            throw new InvalidOperationException(message);
        }

        private MatchProcessor InitializeMatchProcessor(
            string parameterName,
            bool optional,
            IRouteConstraint constraint,
            string argument)
        {
            var matchProcessor = (MatchProcessor)new RouteConstraintMatchProcessor(parameterName, constraint);
            return InitializeMatchProcessor(parameterName, optional, matchProcessor, argument);
        }

        private MatchProcessor InitializeMatchProcessor(
            string parameterName,
            bool optional,
            MatchProcessor matchProcessor,
            string argument)
        {
            if (optional)
            {
                matchProcessor = new OptionalMatchProcessor(matchProcessor);
            }

            matchProcessor.Initialize(parameterName, argument);
            return matchProcessor;
        }

        private (string name, string argument) Parse(string text)
        {
            string name;
            string argument;
            var indexOfFirstOpenParens = text.IndexOf('(');
            if (indexOfFirstOpenParens >= 0 && text.EndsWith(")", StringComparison.Ordinal))
            {
                name = text.Substring(0, indexOfFirstOpenParens);
                argument = text.Substring(
                    indexOfFirstOpenParens + 1,
                    text.Length - indexOfFirstOpenParens - 2);
            }
            else
            {
                name = text;
                argument = null;
            }
            return (name, argument);
        }
    }
}
